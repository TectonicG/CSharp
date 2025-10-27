using Serial_Com.Services.Serial;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;
using System.Windows;
using System.Xml.Serialization;
using static Serial_Com.Services.Serial.SerialWriter;

namespace Serial_Com.Services.Devices
{
    public sealed class Fluidics
    {


        public bool IsConnected => _serialControl?.IsConnected ?? false;
        public EventHandler<bool>? ConnectionChanged;
        private SerialReaderWriter? _serialControl;
        private Task? _queryFlow;
        private CancellationTokenSource? _cancelFluidicsSerial;
        //Channel to write tot he serial writer
        private readonly Channel<OutgoingOp> _outGoingSerial = Channel.CreateUnbounded<OutgoingOp>();
        private uint _token = 0;
        private readonly ConcurrentDictionary<uint, TaskCompletionSource<ErrorCode>> _pendingOutgoingMsgs = new ConcurrentDictionary<uint, TaskCompletionSource<ErrorCode>>();

        /*<--------- PUBLIC --------->*/
        public async Task<bool> ConnectToFluidicsAsync(string port)
        {
            //No need to connect again if we are already connected
            if (_serialControl?.IsConnected == true)
            {
                return true;
            }

            _cancelFluidicsSerial?.Cancel();
            _cancelFluidicsSerial?.Dispose();
            _cancelFluidicsSerial = new CancellationTokenSource();

            var serialReaderWriter = new SerialReaderWriter(_outGoingSerial, _pendingOutgoingMsgs,  _cancelFluidicsSerial.Token);
            serialReaderWriter.ConnectionChanged -= OnConnectionChange;
            serialReaderWriter.ConnectionChanged += OnConnectionChange;
            serialReaderWriter.MessageReceived += OnMessageRecieved;

            var connectionResult = await serialReaderWriter.ConnectToPortAsync(port, 115200, "\0", 200);
            if (connectionResult)
            {
                _serialControl = serialReaderWriter;
                _serialControl.StartSerialWriter();
                StartQueryFlow(_cancelFluidicsSerial.Token);
            }
            else
            {
                _cancelFluidicsSerial.Cancel();
                _cancelFluidicsSerial.Dispose();
                _cancelFluidicsSerial = null;
                serialReaderWriter.ConnectionChanged -= OnConnectionChange;
            }

            return connectionResult;
        }

        public async Task DisconnectFluidicsAsync()
        {
            var sc = _serialControl;
            _serialControl = null;

            if (sc is not null)
            {
                await sc.DisconnectFromPort();
                sc.ConnectionChanged -= OnConnectionChange;
            }

            _cancelFluidicsSerial?.Cancel();
            _cancelFluidicsSerial?.Dispose();
            _cancelFluidicsSerial = null;
        }

        public async Task<ErrorCode> SetValveState(int valveNumber, EnableDisableDef state)
        {
            //Keep it within bounds
            if (valveNumber < 1 && valveNumber > 10)
            {
                return ErrorCode.BadParameter;
            }

            HostMessage hstMsg = new HostMessage
            {
                FluidicsCommand = new FluidicsCommand
                {
                    Valves = new ValveControl
                    {
                        ValveNumber = valveNumber,
                        ValveState = state
                    }
                }
            };

            return await SendAndWaitForResult(hstMsg);
        }

        /*<--------- PRIVATE --------->*/
        private async Task<ErrorCode> SendAndWaitForResult(HostMessage hstMsg)
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
            if (QueueHostmessage(msg)) {
                //If we could queue it, add it to the dictionary
                if(!_pendingOutgoingMsgs.TryAdd(hstMsg.Token, tcs))
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

        private void OnConnectionChange(object? sender, bool connectionState)
        {
            ConnectionChanged?.Invoke(this, connectionState);

            if (!connectionState)
            {
                _cancelFluidicsSerial?.Cancel();
            }
        }

        private void StartQueryFlow(CancellationToken ct)
        {
            _queryFlow = QueryFluidicsSystemAsync(ct);
        }

        private async Task QueryFluidicsSystemAsync(CancellationToken ct)
        {

            int INTERVAL_MS = 100;

            //Want to ask fluidics for a system update every 100ms
            HostMessage hstMsg = new HostMessage
            {
                FluidicsCommand = new FluidicsCommand
                {
                    QuerySystem = new QuerySystem
                    {
                        RequestData = 1
                    }
                }
            };

            while (!ct.IsCancellationRequested)
            {
                //TODO: Put back in, out for testing so i can see the port 
                //if (!QueueHostmessage(hstMsg))
                //{
                //    break;
                //}
                await Task.Delay(INTERVAL_MS, ct).ConfigureAwait(false);
            }

        }

        private bool QueueHostmessage(OutgoingOp msg)
        {
            //Channel is not bounded, it will always succeed unless the writer has been stopped
            return _outGoingSerial.Writer.TryWrite(msg);
        }

        private void OnMessageRecieved(object? sender, DeviceMessage dvcMsg)
        {

            //See if anyone is waiting on us to cancel their waiting
            //Dicionary accessed by token
            uint incomingToken = dvcMsg.Ack.RefToken;
            if (_pendingOutgoingMsgs.TryRemove(incomingToken, out var tcs))
            {
                tcs.TrySetResult(dvcMsg.Ack.ErrorCode);
            }


            //if (dvcMsg == null)
            //{
            //    return;
            //}

            //if (dvcMsg.Ack != null && dvcMsg.Ack.ErrorCode != ErrorCode.Ok)
            //{

            //}
            //else if (dvcMsg.Signal != null)
            //{

            //}
            //else
            //{
            //}
        }
    }
}
