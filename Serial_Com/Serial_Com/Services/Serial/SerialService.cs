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
        //Public
        public bool IsConnected => _port?.IsOpen ?? false; //Return the left side unless it's null, then return the right side
        public string? Endpoint => _port is null ? null : $"{_port.PortName} @ {_port.BaudRate}";
        //These have the "?" becuase they may not be assigned by the calling class therefore null
        public event EventHandler<ReadOnlyMemory<byte>>? DataReceived;
        public event EventHandler<bool>? ConnectionChanged;
        //Private
        private SerialPort? _port;
        private CancellationTokenSource? _internalCts;
        private CancellationTokenSource? _linkedCts;
        private Task? _readLoop;

        /*
         * Connects to a serial port Async
         */
        public async Task<bool> ConnectAsync(string portName, int baud, string endline, int timeout, CancellationToken externalCts)
        {
            if (IsConnected)
            {
                await DisconnectAsync();
            }

            _internalCts?.Cancel();
            _internalCts?.Dispose();

            //Serial port options configured
            _port = new SerialPort(portName, baud)
            {
                ReadTimeout = timeout,
                WriteTimeout = timeout,
                NewLine = endline,
            };

            //Open the serial port
            await Task.Run(() => _port.Open());
            _internalCts = new CancellationTokenSource();

            if (!IsConnected)
            {
                return false;
            }

            ConnectionChanged?.Invoke(this, true);

            //Start background read loop
            //Link our internal cancelation token with the one passed to the method so that
            //      either the outside can cancel this loop or intenal to the class we can cancel this loop
            _linkedCts?.Dispose();
            _linkedCts = CancellationTokenSource.CreateLinkedTokenSource(externalCts, _internalCts.Token);
            //Ignore the return value of Task.Run because we are going to fire this loop and forget about it
            //Task.Run schedules ReadLoopAsync on a thread pool so it doesn't block the UI thread
            //We pass the cancelation token here so that it can check to see if it needs to terminate
            _readLoop = Task.Run(() => ReadLoopAsync(_linkedCts.Token));

            return true;

        }


        /*
         * Disonnects a serial port Async
         */
        public async Task DisconnectAsync()
        {
            //Cancel out of read loop
            _internalCts?.Cancel();

            try
            {
                _port?.Close();
            }
            catch
            {
                //Nothing
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
            _linkedCts?.Dispose();
            _linkedCts = null;
            _internalCts?.Dispose();
            _internalCts = null;

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
        private async Task ReadLoopAsync(CancellationToken cts)
        {
            var buf = new byte[4096];
            var port = _port;

            try
            {
                while (!cts.IsCancellationRequested)
                {
                    int numBytesRead = 0;
                    //Cant do anything with the port if it's null
                    if (port is null || !port.IsOpen)
                    {
                        break;
                    }

                    try
                    {
                        numBytesRead = await port.BaseStream.ReadAsync(buf, 0, buf.Length, cts).ConfigureAwait(false);
                        //A 0 really measn the end of the stream (The producer closed the stream)
                        if (numBytesRead == 0)
                        {
                            break;
                        }
                    }
                    //If the user canceled the opperation intentionally 
                    //You get the same thing on an unplug so i wanted to differentiate here
                    catch (OperationCanceledException) when (cts.IsCancellationRequested)
                    {
                        //Cancelation requested
                        break;
                    }
                    catch (System.Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"The exception is: {ex}");
                        //Other exceptions such as being unplugged
                        break;
                    }
                    //Data Received callback (not that useful since it can contain broken up messages)
                    DataReceived?.Invoke(this, buf.AsMemory(0, numBytesRead));
                }
            }
            finally
            {
                //If there is an internal cancelation token (We connect to a port) and we did not set the cancelation token from the disconnect async method, close the port
                if (_internalCts != null && !_internalCts.IsCancellationRequested)
                {
                    DisconnectFromPort();
                }
            }
        }


        public ValueTask WriteAsync(ReadOnlyMemory<byte> data)
        {
            if (data.Length == 0)
            {
                return ValueTask.CompletedTask;
            }
            if (!IsConnected || _port == null)
            {
                throw new InvalidOperationException("Port not open");
            }
            //We should always have a cancelation token here i think but the program is whining
            var ct = _linkedCts?.Token ?? default;
            return _port!.BaseStream.WriteAsync(data, ct);
        }
    }
}

