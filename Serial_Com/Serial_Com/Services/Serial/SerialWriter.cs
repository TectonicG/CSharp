using Google.Protobuf;
using System;
using System.Collections.Concurrent;
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

        public sealed record OutgoingOp(uint Token, HostMessage Msg, TaskCompletionSource<ErrorCode> Tcs, TimeSpan Timeout);

        //Setup vars for the class
        private readonly IConnectionService _serial;
        private readonly ChannelReader<OutgoingOp> _outgoingSerial;
        private readonly ConcurrentDictionary<uint, TaskCompletionSource<ErrorCode>> _pendingOutgoingMsgs;
        private readonly CancellationToken _ct;

        //Constructor
        public SerialWriter(IConnectionService serial, ChannelReader<OutgoingOp> outgoingSerial, ConcurrentDictionary<uint, TaskCompletionSource<ErrorCode>> pendingOutgoingMsgs, CancellationToken ct)
        {
            _pendingOutgoingMsgs = pendingOutgoingMsgs;
            _serial = serial;
            _outgoingSerial = outgoingSerial;
            _ct = ct;
        }
        //Writing
        public async Task WriteCommandAsync(HostMessage hostMsg)
        {
            //const uint NUM_TRIES = 1;
            //const uint TIMEOUT_MS = 200;

            //Arm the latch token
            //Set sender and token
            hostMsg.Sender = Sender.Host;
            //var ackTask = _ackLatch.Arm(hostMsg.Token);
            //Need to keep track of the msgs i send so i can look for an ack on it 
            //Just for debug
            Debug.WriteLine("This is the message we are sending: ");
            Debug.WriteLine(JsonFormatter.Default.Format(hostMsg));

            //Serialize data with cobs encoding
            var message = Cobs.Cobs.CobsEncode(hostMsg.ToByteArray());
            //for (int i = 0; i < NUM_TRIES; i++)
            //{
            //Send & wait on result
            //Write data. Will throw exception if it cant
            try
            {
                await _serial.WriteAsync(message).ConfigureAwait(false);
            }
            catch (System.Exception ex)
            {
                Debug.WriteLine($"In serial writer: {ex}");
            }
            //try
            //{
            //    //Wait for ack to come in
            //    if (await ackTask.WaitAsync(TimeSpan.FromMilliseconds(TIMEOUT_MS), _ct).ConfigureAwait(false))
            //    {
            //        Debug.WriteLine("Got the ack in - From writer");
            //        return true;
            //    }
            //}
            //catch
            //{
            //    //Bail if the process should cancel
            //    if (_ct.IsCancellationRequested)
            //    {
            //        return false;
            //    }
            //    Debug.WriteLine($"Writer timeout {i + 1} of {NUM_TRIES}");
            //}

            //}
            //return false;
        }

        //This is what will be run on another thread. It waits for data to come in, if it does, it reads it out and that data gets written to serial 
        public async Task WriterSerial()
        {
            //Wait to read until there is something in the channel, or bail if the cancelation comes through
            while (await _outgoingSerial.WaitToReadAsync(_ct))
            {
                {
                    while (_outgoingSerial.TryRead(out var msg))
                    {
                        await WriteCommandAsync(msg.Msg);

                        _ = StartTimeoutAsync(msg, _ct);
                    }
                }
            }
        }

        private async Task StartTimeoutAsync(OutgoingOp msg, CancellationToken ct)
        {


            var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            try
            {
                await msg.Tcs.Task.WaitAsync(msg.Timeout).ConfigureAwait(false);
            }
            catch //timedout 
            {
                //This is where I need to remove the thing from the dictionary
                _pendingOutgoingMsgs.TryRemove(msg.Token, out var _);
                msg.Tcs.TrySetResult(ErrorCode.UnknownError);
                cts.Cancel();
            }

        }


    }
}

