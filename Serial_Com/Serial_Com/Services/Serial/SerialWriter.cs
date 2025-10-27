using Google.Protobuf;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Serial_Com.Services.Serial
{
    public sealed class SerialWriter
    {

        //Setup vars for the class
        private readonly IConnectionService _serial;
        private readonly ChannelReader<HostMessage> _outgoingSerial;
        private readonly CancellationToken _ct;
        private readonly AckLatch _ackLatch;
        private uint _tokenCount;

        //Constructor
        public SerialWriter(IConnectionService serial, ChannelReader<HostMessage> outgoingSerial, AckLatch ackLatch, CancellationToken ct)
        {
            _serial = serial;
            _ackLatch = ackLatch;
            _outgoingSerial = outgoingSerial;
            _ct = ct;
        }
        //Writing
        public async Task<bool> WriteCommandAsync(HostMessage hostMsg)
        {
            const uint NUM_TRIES = 3;
            const uint TIMEOUT_MS = 200;

            _tokenCount++;
            //Arm the latch token
            var ackTask = _ackLatch.Arm(hostMsg);
            //Need to keep track of the msgs i send so i can look for an ack on it 
            //Just for debug
            Debug.WriteLine("This is the message we are sending: ");
            Debug.WriteLine(JsonFormatter.Default.Format(hostMsg));
            //Set sender and token
            hostMsg.Sender = Sender.Host;
            hostMsg.Token = _tokenCount;
            //Serialize data with cobs encoding
            var message = Cobs.Cobs.CobsEncode(hostMsg.ToByteArray());
            for (int i = 0; i < NUM_TRIES; i++)
            {
                //Send & wait on result
                //Write data. Will throw exception if it cant
                try
                {
                    await _serial.WriteAsync(message).ConfigureAwait(false);
                }
                catch (System.Exception ex)
                {
                    Debug.WriteLine($"In seiral writer: {ex}");
                }
                try
                {
                    //Wait for ack to come in
                    if (await ackTask.WaitAsync(TimeSpan.FromMilliseconds(TIMEOUT_MS), _ct).ConfigureAwait(false))
                    {
                        Debug.WriteLine("Got the ack in - From writer");
                        return true;
                    }
                }
                catch
                {
                    //Bail if the process should cancel
                    if (_ct.IsCancellationRequested)
                    {
                        return false;
                    }
                    Debug.WriteLine($"Writer timeout {i + 1}/3");
                }

            }
            return false;
        }

        //This is what will be run on another thread. It waits for data to come in, if it does, it reads it out and that data gets written to serial 
        public async Task TaskAsync()
        {
            //Wait to read until there is something in the channel, or bail if the cancelation comes through
            while (await _outgoingSerial.WaitToReadAsync(_ct))
            {
                {
                    while (_outgoingSerial.TryRead(out var msg))
                    {
                        await WriteCommandAsync(msg);
                    }
                }
            }
        }
    }
}
