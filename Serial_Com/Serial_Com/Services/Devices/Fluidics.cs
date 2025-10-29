using Serial_Com.Services.Serial;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
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
        public EventHandler<FluidicsSystemInfo>? OnQueryFlowRecieved;
        private SerialReaderWriter? _serialControl;
        private Task? _queryFlow;
        private CancellationTokenSource? _cancelFluidicsSerial;
        //Channel to write tot he serial writer
        private uint _token = 0;

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

            var serialReaderWriter = new SerialReaderWriter(_cancelFluidicsSerial.Token);
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
            if (valveNumber < 1 || valveNumber > 10l)
            {
                return ErrorCode.BadParameter;
            }

            if (_serialControl == null)
            {
                return ErrorCode.UnknownError;
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

            return await _serialControl.SendAndWaitForResult(hstMsg);
        }

        public async Task<ErrorCode> MovePropValve(int steps)
        {
            HostMessage hstMsg = new HostMessage
            {
                FluidicsCommand = new FluidicsCommand
                {
                    PropValve = new PropValveControl
                    {
                        MoveSteps = steps
                    }
                }
            };

            if (_serialControl == null)
            {
                return ErrorCode.UnknownError;
            }

            return await _serialControl.SendAndWaitForResult(hstMsg);
        }

        public async Task<ErrorCode> HomePropValve()
        {
            HostMessage hstMsg = new HostMessage
            {
                FluidicsCommand = new FluidicsCommand
                {
                    PropValve = new PropValveControl
                    {
                        SendHome = 1
                    }
                }
            };

            if (_serialControl == null)
            {
                return ErrorCode.UnknownError;
            }

            return await _serialControl.SendAndWaitForResult(hstMsg);
        }

        public async Task<ErrorCode> DisablePropValve()
        {
            HostMessage hstMsg = new HostMessage
            {
                FluidicsCommand = new FluidicsCommand
                {
                    PropValve = new PropValveControl
                    {
                        Disable = 1
                    }
                }
            };

            if (_serialControl == null)
            {
                return ErrorCode.UnknownError;
            }

            return await _serialControl.SendAndWaitForResult(hstMsg);
        }


        public async Task<ErrorCode> MovePinchValve(int steps)
        {
            HostMessage hstMsg = new HostMessage
            {
                FluidicsCommand = new FluidicsCommand
                {
                    PinchValve = new PinchValveControl
                    {
                        MoveSteps = steps
                    }
                }
            };

            if (_serialControl == null)
            {
                return ErrorCode.UnknownError;
            }

            return await _serialControl.SendAndWaitForResult(hstMsg);

        }

        public async Task<ErrorCode> HomePinchValve()
        {
            HostMessage hstMsg = new HostMessage
            {
                FluidicsCommand = new FluidicsCommand
                {
                    PinchValve = new PinchValveControl
                    {
                        SendHome = 1
                    }
                }
            };

            if (_serialControl == null)
            {
                return ErrorCode.UnknownError;
            }

            return await _serialControl.SendAndWaitForResult(hstMsg);
        }

        public async Task<ErrorCode> DisablePinchValve()
        {
            HostMessage hstMsg = new HostMessage
            {
                FluidicsCommand = new FluidicsCommand
                {
                    PinchValve = new PinchValveControl
                    {
                        Disable = 1
                    }
                }
            };

            if (_serialControl == null)
            {
                return ErrorCode.UnknownError;
            }

            return await _serialControl.SendAndWaitForResult(hstMsg);
        }

        public async Task<ErrorCode> HandlePump(PumpSelection pump, PumpStates state, uint speed)
        {

            if (speed > 100)
            {
                speed = 100;
            }

            HostMessage hstMsg = new HostMessage
            {
                FluidicsCommand = new FluidicsCommand
                {
                    PumpControl = new PumpControl
                    {
                        PumpSelection = pump,
                        PumpState = state,
                        PumpSpeed = (int)speed
                    }
                }
            };

            if (_serialControl == null)
            {
                return ErrorCode.UnknownError;
            }


            return await _serialControl.SendAndWaitForResult(hstMsg);
        }

        public async Task<ErrorCode> HomeZAxis()
        {
            HostMessage hstMsg = new HostMessage
            {
                FluidicsCommand = new FluidicsCommand
                {
                    ZAxis = new ZAxisControl
                    {
                        SendHome = 1
                    }
                }
            };

            if (_serialControl == null)
            {
                return ErrorCode.UnknownError;
            }

            return await _serialControl.SendAndWaitForResult(hstMsg);
        }

        public async Task<ErrorCode> MoveZAxis(float mm)
        {
            HostMessage hstMsg = new HostMessage
            {
                FluidicsCommand = new FluidicsCommand
                {
                    ZAxis = new ZAxisControl
                    {
                        MoveZAxisMm = mm
                    }
                }
            };

            if (_serialControl == null)
            {
                return ErrorCode.UnknownError;
            }

            return await _serialControl.SendAndWaitForResult(hstMsg);
        }
        public async Task<ErrorCode> SetHeight(SettableZAxisHeights heightSel, float setHeight)
        {
            HostMessage hstMsg = new HostMessage
            {
                FluidicsCommand = new FluidicsCommand
                {
                    ZAxis = new ZAxisControl
                    {
                        SetHeightSelection = heightSel,
                        Height = setHeight
                    }
                }
            };

            if (_serialControl == null)
            {
                return ErrorCode.UnknownError;
            }

            return await _serialControl.SendAndWaitForResult(hstMsg);
        }

        public async Task<ErrorCode> MoveToSetHeight(SettableZAxisHeights heightSel)
        {
            HostMessage hstMsg = new HostMessage
            {
                FluidicsCommand = new FluidicsCommand
                {
                    ZAxis = new ZAxisControl
                    {
                        MoveToSetHeight = heightSel
                    }
                }
            };

            if (_serialControl == null)
            {
                return ErrorCode.UnknownError;
            }

            return await _serialControl.SendAndWaitForResult(hstMsg);
        }

        public async Task<ErrorCode> EStopZAxis()
        {
            HostMessage hstMsg = new HostMessage
            {
                FluidicsCommand = new FluidicsCommand
                {
                    ZAxis = new ZAxisControl
                    {
                        Disable = 1
                    }
                }
            };

            if (_serialControl == null)
            {
                return ErrorCode.UnknownError;
            }

            return await _serialControl.SendAndWaitForResult(hstMsg);
        }

        public async Task<ErrorCode> StartFluidics()
        {
            HostMessage hstMsg = new HostMessage
            {
                FluidicsCommand = new FluidicsCommand
                {
                    Start = new SystemStart
                    {
                        Start = 1
                    }
                }
            };

            if (_serialControl == null)
            {
                return ErrorCode.UnknownError;
            }

            return await _serialControl.SendAndWaitForResult(hstMsg);
        }

        public async Task<ErrorCode> StopFluidics()
        {
            HostMessage hstMsg = new HostMessage
            {
                FluidicsCommand = new FluidicsCommand
                {
                    Stop = new SystemStop
                    {
                        Stop = 1
                    }
                }
            };

            if (_serialControl == null)
            {
                return ErrorCode.UnknownError;
            }

            return await _serialControl.SendAndWaitForResult(hstMsg);
        }

        /*<--------- PRIVATE --------->*/


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

            int INTERVAL_MS = 50;

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

            while (!ct.IsCancellationRequested && _serialControl != null)
            {
                await _serialControl.SendAndWaitForResult(hstMsg);
                await Task.Delay(INTERVAL_MS, ct).ConfigureAwait(false);
            }

        }

        private void OnMessageRecieved(object? sender, DeviceMessage dvcMsg)
        {
            if (dvcMsg == null)
            {
                return;
            }

            if (dvcMsg.Ack != null)
            {
                if (dvcMsg.Ack.ErrorCode != ErrorCode.Ok)
                {
                    Debug.WriteLine($"Error Code was: {dvcMsg.Ack.ErrorCode}");
                }

                if (dvcMsg.Ack.Response?.FluidicsSystemInfo != null)
                {
                    OnQueryFlowRecieved?.Invoke(this, dvcMsg.Ack.Response.FluidicsSystemInfo);
                    //Debug.WriteLine($"Response was: {dvcMsg.Ack.Response}");
                }
            }
            else if (dvcMsg.Signal != null)
            {
                Debug.WriteLine($"Signal was: {dvcMsg.Signal}");
            }
        }
    }
}
