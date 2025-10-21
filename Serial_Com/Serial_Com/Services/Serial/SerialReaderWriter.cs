using Google.Protobuf;
using Serial_Com.Services.Cobs;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Documents;

namespace Serial_Com.Services.Serial
{

    public sealed class SerialReaderWriter
    {



        IConnectionService _serialHelper = new SerialService();
        private List<byte>? readCommands;
        private uint _tokenCount = 0;
        public event EventHandler<bool>? ConnectionChanged;
        public bool IsConnected => _serialHelper.IsConnected;
        public event EventHandler<DeviceMessage>? MessageReceived;
        private List<byte> _msgBuffer = new();
        string endline;
        private byte[] _endLineAsBytes;
        private TaskCompletionSource<bool>? _waiter; //Use to wait in the write thread
        private uint _waiterToken;
        CancellationToken _cancellationToken = new CancellationToken();

        public SerialReaderWriter()
        {
            //Set endlines
            endline = "\0";
            _endLineAsBytes = Encoding.ASCII.GetBytes(endline);
            //Connects signals for the class
            MakeConnections();

        }

        public async Task ConnectToPort(string portName, CancellationToken cancellationToken)
        {
            //Link Cancelation Tokens
            _cancellationToken = cancellationToken;
            //Setup for communication with the intended devices
            int baud = 115200;
            int timeout = 200;
            //Just a pass through
            await _serialHelper.ConnectAsync(portName, baud, endline, timeout, cancellationToken);

        }

        public async Task DisconnectFromPort()
        {
            await _serialHelper.DisconnectAsync();

        }

        public async Task<bool> WriteCommand(HostMessage hostMsg)
        {
            _waiter = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            int timeoutms = 430;
            //_waiter = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            //Need to keep track of the msgs i send so i can look for an ack on it 
            //Just for debug
            Debug.WriteLine("This is the message we are sending: ");
            Debug.WriteLine(JsonFormatter.Default.Format(hostMsg));

            //Set sender and token
            hostMsg.Sender = Sender.Host;
            //Increment on each send
            _tokenCount++;
            hostMsg.Token = _tokenCount;
            _waiterToken = _tokenCount;
            //Serialize data
            byte[] message = hostMsg.ToByteArray();
            //Cobs encode
            message = Cobs.Cobs.CobsEncode(message);
            //Send & wait on result
            for (int i = 0; i < 3; i++)
            {
                //Write data. Will throw exception if it cant
                await _serialHelper.WriteAsync(message).ConfigureAwait(false);
                //Wait for ack to come in
                try
                {
                    if (await _waiter!.Task.WaitAsync(TimeSpan.FromMilliseconds(timeoutms)).ConfigureAwait(false))
                    {
                        Debug.WriteLine("Got the ack in - From writer");
                        _waiter = null;
                        return true;
                    }
                }
                catch
                {
                    Debug.WriteLine($"Writer timeout {i + 1}/3");
                }
            }
            _waiter = null;
            return false;
        }

        public void ReadCommand(byte[] msg)
        {
            //Gotta cobs decode here
            msg = Cobs.Cobs.CobsDecode(msg);
            DeviceMessage parsedData = DeviceMessage.Parser.ParseFrom(msg);
            Debug.WriteLine("Data we got in: ");
            Debug.WriteLine(JsonFormatter.Default.Format(parsedData));

            if (parsedData.Ack?.RefToken == _waiterToken)
            {
                _waiter?.TrySetResult(true);
                Debug.WriteLine("Got the ack in - From reader");
            }

            MessageReceived?.Invoke(this, parsedData);
        }

        private void PieceTogetherMessageFromIncomingSerial(object? sender, ReadOnlyMemory<byte> buf)
        {
            //You can use .ToArray() to convert ROM to an array like byte[]
            //Add the incoming data to the list the holds a collection of the incoming bytes
            _msgBuffer.EnsureCapacity(_msgBuffer.Count + buf.Length);
            _msgBuffer.AddRange(buf.ToArray());
            //Look for the delimiter index
            int delimIndex = CollectionsMarshal.AsSpan(_msgBuffer).IndexOf(_endLineAsBytes);
            //If the delim index was not found the retunr value is -1
            while (delimIndex > 0)
            {
                //Pop out the message from 0 to the delim + enline range (This includes the end line) (Good for me)
                ReadCommand(_msgBuffer.GetRange(0, delimIndex + _endLineAsBytes.Length).ToArray());
                //Remove that message from the msgbuffer list
                _msgBuffer.RemoveRange(0, delimIndex + _endLineAsBytes.Length);
                //Look for another delimiter
                delimIndex = CollectionsMarshal.AsSpan(_msgBuffer).IndexOf(_endLineAsBytes);
            }
        }

        private void OnIncomingSerialData(object? sender, ReadOnlyMemory<byte> data)
        {
            //Wait for a message to come in by checking for the terminator 
        }

        private void MakeConnections()
        {
            _serialHelper.DataReceived += OnIncomingSerialData;
            _serialHelper.ConnectionChanged += onConnectionChange;
            _serialHelper.DataReceived += PieceTogetherMessageFromIncomingSerial;

        }

        public void onConnectionChange(object? sender, bool state)
        {
            ConnectionChanged?.Invoke(this, state);
        }
    }

}
