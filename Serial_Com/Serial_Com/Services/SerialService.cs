using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Serial_Com.Services
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
        public event EventHandler<string>? DataReceived;
        public event EventHandler<bool>? ConnectionChanged;


        //This Opens the serial port on the UI thread - should fix later
        //This starts the ReadLoopAsync method to read in serial data - This reall is async (On another thread) 
        public async Task ConnectAsync(string portName, int baud, int timeout,  CancellationToken cancellationToken)
        {
            if (IsConnected)
            {
                await DisconnectAsync();
            }

            //Serial port options configured
            _port = new SerialPort(portName, baud)
            {
                ReadTimeout = timeout,
                WriteTimeout = timeout,
                NewLine = "\r\n",
                Encoding = Encoding.ASCII
            };

            //Open the serial port
            _port.Open();
            ConnectionChanged?.Invoke(this, true);

            //Start background read loop
            //Link our internal cancelation token with the one passed to the method so that
            //      either the outside can cancel this loop or intenal to the class we can cancel this loop
            _ctsForReadLoop = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            //Ignore the return value of Task.Run because we are going to fire this loop and forget about it
            //Task.Run schedules ReadLoopAsync on a thread pool so it doesn't block the UI thread
            //We pass the cancelation token here so that it can check to see if it needs to terminate
            _ = Task.Run(() => ReadLoopAsync(_ctsForReadLoop.Token));

            //This keeps the Async signiture consistent
            //So I could call this methos as await Task
            await Task.CompletedTask;

        }

        //TODO: Make this method wait until the ReadLoopAsync is done canceling befoe the serial port close
        public async Task DisconnectAsync()
        {

            //Try and cancel the read loop
            try
            {
                _ctsForReadLoop?.Cancel();
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

            await Task.CompletedTask;

        }
        private async Task ReadLoopAsync(CancellationToken ct)
        {

            while (!ct.IsCancellationRequested && IsConnected)
            {

                try
                {
                    string? line = await Task.Run(() =>
                    {
                        try
                        {
                            return _port!.ReadLine();
                        }
                        catch (TimeoutException)
                        {
                            return null;
                        }
                    }, ct);

                    if (!string.IsNullOrEmpty(line))
                    {
                        DataReceived?.Invoke(this, line);
                    }
                }
                //catch (OperationCanceledException)
                //{
                //    //Cancelation requested
                //    break;
                //}
                //catch (ObjectDisposedException)
                //{
                //    //Port was closed/disposed during disconnect
                //    break;
                //}
                //catch (IOException ioEx)
                //{
                //    //IO Exception
                //    //ConnectionChanged?.Invoke(this, IsConnected);
                //    DataReceived?.Invoke(this, $"[ERROR] {ioEx.Message}");
                //    await DisconnectAsync();
                //    break;
                //}
                //catch (UnauthorizedAccessException uaex)
                //{
                //    //Attempted to access a file or directory 
                //    DataReceived?.Invoke(this, $"[ERROR] {uaex.Message}");
                //    await DisconnectAsync();
                //    break;
                //}
                catch (Exception ex)
                {
                    //Any unexpected error - report it, then exit
                    DataReceived?.Invoke(this, $"[ERROR] {ex.Message}");
                    await DisconnectAsync();
                    break;
                }
            }
        }
    }
}
