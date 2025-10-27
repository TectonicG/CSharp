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
        private readonly ConcurrentDictionary<uint, TaskCompletionSource<ErrorCode>> _pendingOutgoingMsgs;
        public ChannelReader<OutgoingOp> _outgoingSerial;
        private readonly SerialReader _reader;
        private readonly SerialWriter _writer;


        public SerialReaderWriter(ChannelReader<OutgoingOp> outgoingSerial, ConcurrentDictionary<uint, TaskCompletionSource<ErrorCode>> pendingOutgoingMsgs, CancellationToken cts)
        {
            _pendingOutgoingMsgs = pendingOutgoingMsgs;
            _outgoingSerial = outgoingSerial;
            _cts = cts;
            _reader = new SerialReader(_serialHelper, _cts);
            _writer = new SerialWriter(_serialHelper, _outgoingSerial, _pendingOutgoingMsgs, _cts);
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


        private void MakeConnections()
        {
            _serialHelper.ConnectionChanged += OnConnectionChange;
            _reader.MessageReceived += OnMessageReceived;
        }

        private void OnConnectionChange(object? sender, bool state)
        {
            ConnectionChanged?.Invoke(this, state);
        }

        private void OnMessageReceived(object? sender, DeviceMessage msg)
        {
            MessageReceived?.Invoke(this, msg);
        }
    }

}
