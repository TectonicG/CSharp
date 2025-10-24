using Google.Protobuf;
using Serial_Com.Services.Serial;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Channels;
using System.Threading.Tasks;
using Serial_Com.InternalMessages;

namespace Serial_Com.Services.Backend
{
    public sealed class Backend
    {

        //Messages for the UI and Backend
        private readonly Channel<BackendCommand> _cmds = Channel.CreateUnbounded<BackendCommand>();
        private readonly Channel<BackendEvent> _events = Channel.CreateUnbounded<BackendEvent>();
        public ChannelReader<BackendEvent> Events => _events.Reader;
        //Backend controls the serial data
        private SerialReaderWriter _serialControl;
        private readonly Channel<HostMessage> _outgoingSerial = Channel.CreateUnbounded<HostMessage>();
        private readonly Channel<DeviceMessage> _incomingSerial = Channel.CreateUnbounded<DeviceMessage>();
        private readonly CancellationToken _cancelSerial;
        private readonly CancellationToken _cancelBackend;

        //UI uses these helpers (they wrap command posting + awaiting the reply TCS)
        public Task<bool> ConnectAsync(string port) => PostAndWait(new ConnectSerial(port, NewReply()));
        public Task<bool> DisconnectAsync() => PostAndWait(new DisconnectSerial(NewReply()));
        public Task<bool> SendHostAsync(HostMessage hstMsg) => PostAndWait(new SendHostCommand(hstMsg, NewReply()));

        public bool IsConnected => _serialControl.IsConnected;
        private static TaskCompletionSource<bool> NewReply() =>new(TaskCreationOptions.RunContinuationsAsynchronously);



        public Backend(CancellationToken cancelBackend, CancellationToken cancelSerial)
        {
            _serialControl = new SerialReaderWriter(cancelSerial, _outgoingSerial, _incomingSerial);
            _cancelBackend = cancelBackend;
            _cancelSerial = cancelSerial;

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

            await _cmds.Writer.WriteAsync(cmd, _cancelSerial);
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
                                var ok = await _serialControl.ConnectToPort(port);
                                reply.TrySetResult(ok);
                                if (ok)
                                {
                                    _ = _serialControl.StartSerialWriter();
                                }
                                await _events.Writer.WriteAsync(new ConnectionChanged(ok), _cancelSerial);
                                break;

                            case DisconnectSerial(var reply):
                                await _serialControl.DisconnectFromPort();
                                reply.TrySetResult(true);
                                await _events.Writer.WriteAsync(new ConnectionChanged(false), _cancelSerial);
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

        //Backend should run serial reader and writer. 
        //Each should be it's own task so they happen independently 

        //private async Task RunSerialWriter()
        //{
        //    while (true)
        //    {

        //        while (serialWriteChannel.Reader.TryRead(out var msg)){
        //            Task.Run(() =>
        //            {
        //                await serial.WriteCommand(msg);
        //            });
        //        }
        //    }
        //}

        //private void RunSerialReader()
        //{

        //}

    }
}
