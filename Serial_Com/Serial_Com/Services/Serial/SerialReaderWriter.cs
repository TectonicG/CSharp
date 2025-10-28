using Google.Protobuf;
using Serial_Com.Services;
using Serial_Com.Services.Cobs;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using System.Windows.Documents;
using static Serial_Com.Services.Serial.SerialWriter;

namespace Serial_Com.Services.Serial
{

    public sealed class SerialReaderWriter
    {



        IConnectionService _serialHelper = new SerialService();
        public event EventHandler<bool>? ConnectionChanged;
        public bool IsConnected => _serialHelper.IsConnected;
        public event EventHandler<DeviceMessage>? MessageReceived;
        private readonly CancellationToken _cts;
        private readonly ConcurrentDictionary<uint, TaskCompletionSource<ErrorCode>> _pendingOutgoingMsgs = new ConcurrentDictionary<uint, TaskCompletionSource<ErrorCode>>();
        private readonly Channel<OutgoingOp> _outGoingSerial = Channel.CreateUnbounded<OutgoingOp>();
        private readonly SerialReader _reader;
        private readonly SerialWriter _writer;
        private uint _token = 0;


        public SerialReaderWriter(CancellationToken cts)
        {
            _cts = cts;
            _reader = new SerialReader(_serialHelper, _cts);
            _writer = new SerialWriter(_serialHelper, _outGoingSerial, _pendingOutgoingMsgs, _cts);
            //Connects signals for the class
            MakeConnections();
        }

        public async Task<bool> ConnectToPortAsync(string portName, int baud, string endline, int timeout)
        {
            //Just a pass through
            return await _serialHelper.ConnectAsync(portName, baud, endline, timeout, _cts);

        }

        public void StartSerialWriter()
        {
            _ = _writer.WriterSerial();
        }

        public async Task DisconnectFromPort()
        {
            await _serialHelper.DisconnectAsync();

        }

        public async Task<ErrorCode> SendAndWaitForResult(HostMessage hstMsg)
        {
            //Create a task to act as a timeout
            var tcs = new TaskCompletionSource<ErrorCode>(TaskCreationOptions.RunContinuationsAsynchronously);

            hstMsg.Token = ++_token;
            //Bundle it all into a message for the serial writer
            OutgoingOp msg = new OutgoingOp
            (
                 hstMsg.Token,
                 hstMsg,
                 tcs,
                 TimeSpan.FromMilliseconds(200)
            );

            //Queue it to serial writer
            if (QueueHostmessage(msg))
            {
                //If we could queue it, add it to the dictionary
                if (!_pendingOutgoingMsgs.TryAdd(hstMsg.Token, tcs))
                {
                    //Couldnt Add it so set bad result and return bad result
                    //Altough it could have sent here? But I have no way to get feedback so...
                    msg.Tcs.TrySetResult(ErrorCode.UnknownError);
                    return ErrorCode.UnknownError;
                }
                //Wait for the task to be over
                return await tcs.Task;
            }
            return ErrorCode.UnknownError;
        }

        private bool QueueHostmessage(OutgoingOp msg)
        {
            //Channel is not bounded, it will always succeed unless the writer has been stopped
            return _outGoingSerial.Writer.TryWrite(msg);
        }

        private void MakeConnections()
        {
            _serialHelper.ConnectionChanged += OnConnectionChange;
            _reader.MessageReceived += OnMessageReceived;
        }

        private void OnConnectionChange(object? sender, bool state)
        {
            ConnectionChanged?.Invoke(this, state);
        }

        private void OnMessageReceived(object? sender, DeviceMessage dvcMsg)
        {
            if (dvcMsg.Ack != null)
            {
                //See if anyone is waiting on us to cancel their waiting
                //Dicionary accessed by token
                uint incomingToken = dvcMsg.Ack.RefToken;
                if (_pendingOutgoingMsgs.TryRemove(incomingToken, out var tcs))
                {
                    tcs.TrySetResult(dvcMsg.Ack.ErrorCode);
                }
            }

            MessageReceived?.Invoke(this, dvcMsg);
        }
    }

}
