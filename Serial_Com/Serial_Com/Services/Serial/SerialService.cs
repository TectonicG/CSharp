using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace Serial_Com.Services.Serial
{


    //Public sealed class SerialService with the IConnectionService interface build in
    //Public so anyone can use it, sealed so that no other class can inherit from it
    //This will be a complete standalone implementation of serial comm so no inheriting from it
    //Also so overwriting methods
    public sealed class SerialService : IConnectionService
    {

        /*
             ***I HAVE TO BUILD THESE IN AT LEAST***
             bool IsConnected { get; }
            string? Endpoint { get; } //Com port & baud rate
            event EventHandler<string>? DataReceived;

            Task ConnectAsync(string portName, int baud, int timeout, CancellationToken cancellationToken);
            Task DisconnectAsync();
            Task WriteAsync(string text, CancellationToken cancellationToken);
        */

        private SerialPort? _port;
        private CancellationTokenSource? _ctsForReadLoop;
        public bool IsConnected => _port?.IsOpen ?? false;
        public string? Endpoint => _port is null ? null : $"{_port.PortName} @ {_port.BaudRate}";
        public event EventHandler<ReadOnlyMemory<byte>>? DataReceived;
        public event EventHandler<bool>? ConnectionChanged;
        private Task? _readLoop;
        CancellationToken _cancellationToken = new CancellationToken();


        /*
         * Connects to a serial port Async
         */
        public async Task ConnectAsync(string portName, int baud, string endline, int timeout, CancellationToken cancellationToken)
        {
            if (IsConnected)
            {
                await DisconnectAsync();
            }

            //Serial port options configured
            _cancellationToken = cancellationToken;
            _port = new SerialPort(portName, baud)
            {
                ReadTimeout = timeout,
                WriteTimeout = timeout,
                NewLine = endline,
            };

            //Open the serial port
            await Task.Run(() => _port.Open());
            ConnectionChanged?.Invoke(this, true);

            //Start background read loop
            //Link our internal cancelation token with the one passed to the method so that
            //      either the outside can cancel this loop or intenal to the class we can cancel this loop
            _ctsForReadLoop = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            //Ignore the return value of Task.Run because we are going to fire this loop and forget about it
            //Task.Run schedules ReadLoopAsync on a thread pool so it doesn't block the UI thread
            //We pass the cancelation token here so that it can check to see if it needs to terminate
            _readLoop = Task.Run(() => ReadLoopAsync());
        }


        /*
         * Disonnects a serial port Async
         */
        public async Task DisconnectAsync()
        {
            //Cancel out of read loop
            _ctsForReadLoop?.Cancel();

            if (IsConnected)
            {
                try
                {
                    _port!.Close();
                }
                catch
                {
                    //Do Nothing
                }
            }

            if (_readLoop is not null)
            {
                try
                {

                    await _readLoop;
                }
                finally
                {
                    _readLoop = null;
                }

            }

            DisconnectFromPort();
        }


        /*
         * The method that actually closes the serial port and sends out connection changed
         * 
         */
        private void DisconnectFromPort()
        {
            //Try and cancel the read loop
            try
            {
                _ctsForReadLoop?.Dispose();
                _ctsForReadLoop = null;
            }
            catch
            {
                //Do Nothing
            }

            //Close and dispose of the port
            if (_port is not null)
            {
                try
                {
                    if (IsConnected)
                    {
                        _port.Close();
                    }
                }
                finally
                {
                    _port.Dispose();
                    _port = null;
                }
            }

            ConnectionChanged?.Invoke(this, false);

        }

        /*
        * Reads the serial data Async
        */
        private async Task ReadLoopAsync()
        {

            var buf = new byte[4096];

            while (!_cancellationToken.IsCancellationRequested && IsConnected)
            {

                try
                {
                    int numBytesRead = 0;
                    try
                    {
                        numBytesRead = await _port!.BaseStream.ReadAsync(buf, 0, buf.Length, _cancellationToken).ConfigureAwait(false);

                        if (numBytesRead == 0)
                        {
                            break;
                        }
                    }
                    //If the user canceled the opperation intentionally 
                    //You get the same thing on an unplug so i wanted to differentiate here
                    catch (OperationCanceledException) when (_cancellationToken.IsCancellationRequested)
                    {
                        //Cancelation requested
                        break;
                    }

                    //If we actually read bytes
                    if (numBytesRead != 0)
                    {
                        //Data Received callback (not that useful since it can contain broken up messages)
                        DataReceived?.Invoke(this, buf.AsMemory(0, numBytesRead));

                    }
                }
                catch (System.Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"The exception is: {ex}");
                    //Other exceptions such as being unplugged
                    DisconnectFromPort();
                    break;
                }
            }
        }

        public ValueTask WriteAsync(ReadOnlyMemory<byte> data)
        {


            if (data.Length == 0)
            {
                return ValueTask.CompletedTask;
            }
            if(!IsConnected || _port == null)
            {
                throw new InvalidOperationException("Port not open");
            }

            return _port!.BaseStream.WriteAsync(data, _cancellationToken);
        }
    }
}

