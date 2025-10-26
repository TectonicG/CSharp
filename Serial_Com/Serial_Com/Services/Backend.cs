using Google.Protobuf;
using Serial_Com.InternalMessages;
using Serial_Com.Services.Serial;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Serial_Com.Services.Backend
{
    public sealed class Backend
    {

        //Messages for the UI and Backend
        private readonly Channel<BackendCommand> _cmds = Channel.CreateUnbounded<BackendCommand>();
        private readonly Channel<BackendEvent> _events = Channel.CreateUnbounded<BackendEvent>();
        public ChannelReader<BackendEvent> Events => _events.Reader;
        //Backend controls the serial data
        private SerialReaderWriter? _serialControl;
        private readonly Channel<HostMessage> _outgoingSerial = Channel.CreateUnbounded<HostMessage>();
        private readonly Channel<DeviceMessage> _incomingSerial = Channel.CreateUnbounded<DeviceMessage>();
        private CancellationTokenSource? _cancelSerial;
        private readonly CancellationToken _cancelBackend;

        //UI uses these helpers (they wrap command posting + awaiting the reply TCS)
        public Task<bool> ConnectAsync(string port) => PostAndWait(new ConnectSerial(port, NewReply()));
        public Task<bool> DisconnectAsync() => PostAndWait(new DisconnectSerial(NewReply()));
        public Task<bool> SendHostAsync(HostMessage hstMsg) => PostAndWait(new SendHostCommand(hstMsg, NewReply()));

        public bool IsConnected => _serialControl?.IsConnected ?? false;
        private static TaskCompletionSource<bool> NewReply() => new(TaskCreationOptions.RunContinuationsAsynchronously);



        public Backend(CancellationToken cancelBackend)
        {
            _cancelBackend = cancelBackend;

        }

        private async Task<bool> PostAndWait(BackendCommand cmd)
        {
            var reply = cmd switch
            {
                ConnectSerial c => c.Reply,
                DisconnectSerial d => d.Reply,
                SendHostCommand s => s.Reply,
                _ => throw new NotImplementedException()
            };

            await _cmds.Writer.WriteAsync(cmd, _cancelBackend);
            return await reply.Task;
        }

        public async Task RunAsync()
        {
            while (await _cmds.Reader.WaitToReadAsync(_cancelBackend))
            {
                while (_cmds.Reader.TryRead(out var cmd))
                {
                    try
                    {
                        switch (cmd)
                        {
                            case ConnectSerial(var port, var reply):
                                //If we try and connect when we are already connected
                                if (_serialControl?.IsConnected == true)
                                {
                                    reply.TrySetResult(true);
                                    break;
                                }

                                _cancelSerial?.Cancel();
                                _cancelSerial?.Dispose();
                                _cancelSerial = new CancellationTokenSource();

                                var serialReaderWriter = new SerialReaderWriter(_cancelSerial.Token, _outgoingSerial, _incomingSerial);
                                serialReaderWriter.ConnectionChanged += OnConnectionChanged;


                                var ok = await serialReaderWriter.ConnectToPort(port);
                                if (ok)
                                {
                                    //Assign internal reference
                                    _serialControl = serialReaderWriter;
                                    _ = serialReaderWriter.StartSerialWriter();
                                }
                                else
                                {
                                    //Clean up after failed attempt
                                    _cancelSerial.Cancel();
                                    _cancelSerial.Dispose();
                                    _cancelSerial = null;
                                }

                                reply.TrySetResult(ok);
                                break;

                            case DisconnectSerial(var reply):
                                var sc = _serialControl;           // capture
                                _serialControl = null;             // clear early (prevents reentrancy races)

                                if (sc is not null)
                                {
                                    await sc.DisconnectFromPort(); // may raise ConnectionChanged(false)
                                    sc.ConnectionChanged -= OnConnectionChanged;
                                }

                                _cancelSerial?.Cancel();
                                _cancelSerial?.Dispose();
                                _cancelSerial = null;

                                reply.TrySetResult(true);
                                break;

                            case SendHostCommand(HostMessage msg, var reply):
                                //Don't wait on the results of the message sent.
                                reply.TrySetResult(_outgoingSerial.Writer.TryWrite(msg));
                                break;

                        }
                    }
                    catch (OperationCanceledException)
                    {
                        //
                        Debug.WriteLine($"OperationCanceledException in backend");
                    }
                    catch (System.Exception ex)
                    {
                        //
                        Debug.WriteLine($"Exception in backend RunAsync {ex}");
                    }
                }
            }


        }

        private async void OnConnectionChanged(object? sender, bool connectionState)
        {

            try
            {
                await _events.Writer.WriteAsync(new ConnectionChanged(connectionState), _cancelBackend);

                if (connectionState == false)
                {
                    _cancelSerial?.Cancel();
                }
            }
            catch (System.Exception ex)
            {
                Debug.WriteLine($"Issue in OnConnectionChange in backend: {ex.Message}");
            }
        }

    }
}
