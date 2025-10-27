using Google.Protobuf;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;
using System.Windows.Interop;

namespace Serial_Com.Services.Serial
{

    public sealed class SerialReader
    {

        private readonly IConnectionService _serial;
        private readonly ChannelWriter<DeviceMessage> _incomingSerial;
        private readonly CancellationToken _ct;
        public event EventHandler<DeviceMessage>? MessageReceived;
        private readonly List<byte> _buffer = new();
        private readonly byte[] _delimiter = Encoding.ASCII.GetBytes("\0"); //This is what cobs encoding ends with 
        private readonly AckLatch _ackLatch;

        public TaskCompletionSource<bool>? ackSignal;
        public SerialReader(IConnectionService serial, ChannelWriter<DeviceMessage> incomingSerial, AckLatch ackLatch, CancellationToken ct)
        {
            _serial = serial;
            _ackLatch = ackLatch;
            _incomingSerial = incomingSerial;
            _ct = ct;
            _serial.DataReceived += OnDataReceived;
        }
        public void ProcessBuffer()
        {

            //Look for the delimiter index
            int delimIndex = CollectionsMarshal.AsSpan(_buffer).IndexOf(_delimiter);
            //If the delim index was not found the retun value is -1
            while (delimIndex > 0)
            {

                //Pop out the message from 0 to the delim + enline range (This includes the end line) (Good for me)
                var msgBytes = _buffer.GetRange(0, delimIndex + _delimiter.Length).ToArray();
                //Remove that message from the msgbuffer list
                _buffer.RemoveRange(0, delimIndex + _delimiter.Length);
                //Decode and parse the incoming device message
                try
                {
                    var decoded = Cobs.Cobs.CobsDecode(msgBytes);
                    var parsed = DeviceMessage.Parser.ParseFrom(decoded);
                    var (msgFound, hostMsg) = _ackLatch.TrySignal(parsed.Ack.RefToken);
                    if (msgFound)
                    {
                        MessageReceived?.Invoke(this, parsed, hostMsg);
                    }
                    else
                    {
                        MessageReceived?.Invoke(this, parsed);
                    }
                    //Look for another delimiter
                    delimIndex = CollectionsMarshal.AsSpan(_buffer).IndexOf(_delimiter);
                }
                catch (FormatException ex)
                {
                    Debug.WriteLine($"Format exception: {ex}");
                }
                catch (Google.Protobuf.InvalidProtocolBufferException ex)
                {
                    Debug.WriteLine($"Invalid Proto: {ex}");
                }
                catch (System.Exception ex)
                {
                    Debug.WriteLine($"In serial reader: {ex.ToString()}");
                }
            }
        }

        private void OnDataReceived(object? sender, ReadOnlyMemory<byte> buf)
        {
            //You can use .ToArray() to convert ROM to an array like byte[]
            //Add the incoming data to the list the holds a collection of the incoming bytes
            _buffer.AddRange(buf.ToArray());
            ProcessBuffer();
        }

        //Reference
    }
}
