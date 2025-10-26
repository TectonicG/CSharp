using Google.Protobuf;
using Serial_Com.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Documents;
using Serial_Com.Services.Cobs;
using System.Threading.Channels;

namespace Serial_Com.Services.Serial
{

    public sealed class SerialReaderWriter
    {



        IConnectionService _serialHelper = new SerialService();
        public event EventHandler<bool>? ConnectionChanged;
        public bool? IsConnected => _serialHelper?.IsConnected;
        public event EventHandler<DeviceMessage>? MessageReceived;
        private readonly CancellationToken _cts;

        public ChannelWriter<DeviceMessage> _incomingSerial;
        public ChannelReader<HostMessage> _outgoingSerial;
        private readonly SerialReader _reader;
        private readonly SerialWriter _writer;
        private readonly AckLatch _ackLatch = new AckLatch();


        public SerialReaderWriter(CancellationToken cts, ChannelReader<HostMessage> outgoingSerial, ChannelWriter<DeviceMessage> incomingSerial)
        {
            _incomingSerial = incomingSerial;
            _outgoingSerial = outgoingSerial;
            _cts = cts;
            _reader = new SerialReader(_serialHelper, _incomingSerial, _ackLatch, _cts);
            _writer = new SerialWriter(_serialHelper, _outgoingSerial, _ackLatch, _cts);
            //Connects signals for the class
            MakeConnections();
        }

        public async Task<bool> ConnectToPort(string portName)
        {
            //Setup for communication with the intended devices
            int baud = 115200;
            int timeout = 200;
            string endline = "\0";
            //Just a pass through
            return await _serialHelper.ConnectAsync(portName, baud, endline, timeout, _cts);

        }

        public async Task StartSerialWriter()
        {
            await _writer.TaskAsync();
        }

        public async Task DisconnectFromPort()
        {
            await _serialHelper.DisconnectAsync();

        }


        private void MakeConnections()
        {
            _serialHelper.ConnectionChanged += OnConnectionChange;
        }

        public void OnConnectionChange(object? sender, bool state)
        {
            ConnectionChanged?.Invoke(this, state);
        }
    }

}
