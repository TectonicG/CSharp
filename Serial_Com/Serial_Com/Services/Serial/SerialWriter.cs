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

            hostMsg.Sender = Sender.Host;
            //Just for debug
            Debug.WriteLine("This is the message we are sending: ");
            Debug.WriteLine(JsonFormatter.Default.Format(hostMsg));

            //Serialize data with cobs encoding
            var message = Cobs.Cobs.CobsEncode(hostMsg.ToByteArray());
            try
            {
                await _serial.WriteAsync(message).ConfigureAwait(false);
            }
            catch (System.Exception ex)
            {
                Debug.WriteLine($"In serial writer: {ex}");
            }
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

                        //Start the timeout when the message is sent
                        _ = StartTimeoutAsync(msg, _ct);
                    }
                }
            }
        }

        private async Task StartTimeoutAsync(OutgoingOp msg, CancellationToken ct)
        {
            //Starts the timeout for the message
            //If the message is not gotten back in time, then the message is removed from the dictionary and the result is set and unknown error

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

