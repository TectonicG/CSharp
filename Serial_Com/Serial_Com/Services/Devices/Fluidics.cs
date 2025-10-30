using Google.Protobuf.WellKnownTypes;
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
        public event EventHandler<Signal>? OnSignalReceived;

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


        public async Task<ErrorCode> StartDebubble()
        {
            HostMessage hstMsg = new HostMessage
            {
                FluidicsCommand = new FluidicsCommand
                {
                    Debubble = new DeBubbleTask
                    {
                        State = StopStartTaskDef.StartTask
                    }
                }
            };

            if (_serialControl == null)
            {
                return ErrorCode.UnknownError;
            }

            return await _serialControl.SendAndWaitForResult(hstMsg);
        }

        public async Task<ErrorCode> StopDebubble()
        {
            HostMessage hstMsg = new HostMessage
            {
                FluidicsCommand = new FluidicsCommand
                {
                    Debubble = new DeBubbleTask
                    {
                        State = StopStartTaskDef.StopTask
                    }
                }
            };

            if (_serialControl == null)
            {
                return ErrorCode.UnknownError;
            }

            return await _serialControl.SendAndWaitForResult(hstMsg);
        }

        public async Task<ErrorCode> StartBackflush()
        {
            HostMessage hstMsg = new HostMessage
            {
                FluidicsCommand = new FluidicsCommand
                {
                    BackFlush = new BackFlushTask
                    {
                        State = StopStartTaskDef.StartTask
                    }
                }
            };

            if (_serialControl == null)
            {
                return ErrorCode.UnknownError;
            }

            return await _serialControl.SendAndWaitForResult(hstMsg);
        }

        public async Task<ErrorCode> StopBackflush()
        {
            HostMessage hstMsg = new HostMessage
            {
                FluidicsCommand = new FluidicsCommand
                {
                    BackFlush = new BackFlushTask
                    {
                        State = StopStartTaskDef.StopTask
                    }
                }
            };

            if (_serialControl == null)
            {
                return ErrorCode.UnknownError;
            }

            return await _serialControl.SendAndWaitForResult(hstMsg);
        }

        public async Task<ErrorCode> StartPurgeFilter()
        {
            HostMessage hstMsg = new HostMessage
            {
                FluidicsCommand = new FluidicsCommand
                {
                    PurgeFilter = new PurgeFilterTask
                    {
                        State = StopStartTaskDef.StartTask
                    }
                }
            };

            if (_serialControl == null)
            {
                return ErrorCode.UnknownError;
            }

            return await _serialControl.SendAndWaitForResult(hstMsg);
        }

        public async Task<ErrorCode> StopPurgeFilter()
        {
            HostMessage hstMsg = new HostMessage
            {
                FluidicsCommand = new FluidicsCommand
                {
                    PurgeFilter = new PurgeFilterTask
                    {
                        State = StopStartTaskDef.StopTask
                    }
                }
            };

            if (_serialControl == null)
            {
                return ErrorCode.UnknownError;
            }

            return await _serialControl.SendAndWaitForResult(hstMsg);
        }

        public async Task<ErrorCode> BleachCleanStepOne()
        {
            HostMessage hstMsg = new HostMessage
            {
                FluidicsCommand = new FluidicsCommand
                {
                    Cleaning = new CleanTask
                    {
                        State = StopStartTaskDef.StartTask,
                        Step = CleaningSteps.BleachCleanStepOne
                    }
                }
            };

            if (_serialControl == null)
            {
                return ErrorCode.UnknownError;
            }

            MessageBox.Show("Please fill bleach container with fresh 10% bleach and switch sheath line to the bleach tank");

            //Home Z-Axis
            var result = await HomeZAxis();
            if (result == ErrorCode.UnknownError)
            {
                return result;
            }

            MessageBox.Show("Please insert sample tube with filled with 3mL of cleaning solution");

            return await _serialControl.SendAndWaitForResult(hstMsg);
        }

        public async Task<ErrorCode> BleachCleanStepTwo()
        {
            HostMessage hstMsg = new HostMessage
            {
                FluidicsCommand = new FluidicsCommand
                {
                    Cleaning = new CleanTask
                    {
                        State = StopStartTaskDef.StartTask,
                        Step = CleaningSteps.BleachCleanStepTwo
                    }
                }
            };

            if (_serialControl == null)
            {
                return ErrorCode.UnknownError;
            }

            MessageBox.Show("Please change container to back to sheath fluid");

            //Home Z-Axis
            var result = await HomeZAxis();
            if (result == ErrorCode.UnknownError)
            {
                return result;
            }

            MessageBox.Show("Please insert sample tube with filled with 3mL of water");

            return await _serialControl.SendAndWaitForResult(hstMsg);
        }
        public async Task<ErrorCode> StopBleachClean()
        {
            HostMessage hstMsg = new HostMessage
            {
                FluidicsCommand = new FluidicsCommand
                {
                    Cleaning = new CleanTask
                    {
                        State = StopStartTaskDef.StopTask
                    }
                }
            };

            if (_serialControl == null)
            {
                return ErrorCode.UnknownError;
            }

            return await _serialControl.SendAndWaitForResult(hstMsg);
        }

        public async Task<ErrorCode> StartSamplePathWashStepOne()
        {
            HostMessage hstMsg = new HostMessage
            {
                FluidicsCommand = new FluidicsCommand
                {
                    Cleaning = new CleanTask
                    {
                        State = StopStartTaskDef.StartTask,
                        Step = CleaningSteps.SampleWash
                    }
                }
            };

            if (_serialControl == null)
            {
                return ErrorCode.UnknownError;
            }

            //Home Z-Axis
            var result = await HomeZAxis();
            if (result == ErrorCode.UnknownError)
            {
                return result;
            }

            MessageBox.Show("Please insert sample tube with filled with 3mL of cleaning solution");

            return await _serialControl.SendAndWaitForResult(hstMsg);
        }

        public async Task<ErrorCode> StartSamplePathWashStepTwo()
        {
            HostMessage hstMsg = new HostMessage
            {
                FluidicsCommand = new FluidicsCommand
                {
                    Cleaning = new CleanTask
                    {
                        State = StopStartTaskDef.StartTask,
                        Step = CleaningSteps.SampleWash
                    }
                }
            };

            if (_serialControl == null)
            {
                return ErrorCode.UnknownError;
            }

            //Home Z-Axis
            var result = await HomeZAxis();
            if (result == ErrorCode.UnknownError)
            {
                return result;
            }

            MessageBox.Show("Please insert sample tube with filled with 3mL of water");

            return await _serialControl.SendAndWaitForResult(hstMsg);
        }

        public async Task<ErrorCode> StopSamplePathWash()
        {
            HostMessage hstMsg = new HostMessage
            {
                FluidicsCommand = new FluidicsCommand
                {
                    Cleaning = new CleanTask
                    {
                        State = StopStartTaskDef.StopTask
                    }
                }
            };

            if (_serialControl == null)
            {
                return ErrorCode.UnknownError;
            }

            return await _serialControl.SendAndWaitForResult(hstMsg);
        }

        public async Task<ErrorCode> SendNewTargetSheathRate(float value)
        {
            HostMessage hstMsg = new HostMessage
            {
                FluidicsCommand = new FluidicsCommand
                {
                    SetSheathRate = new SetSheathRatePID
                    {
                        SheathRate = value
                    }
                }
            };

            if (_serialControl == null)
            {
                return ErrorCode.UnknownError;
            }

            return await _serialControl.SendAndWaitForResult(hstMsg);
        }

        public async Task<ErrorCode> SendNewTargetSampleRate(int value)
        {
            HostMessage hstMsg = new HostMessage
            {
                FluidicsCommand = new FluidicsCommand
                {
                    SetSampleRate = new SetSampleRatePID
                    {
                        SampleRate = value
                    }
                }
            };

            if (_serialControl == null)
            {
                return ErrorCode.UnknownError;
            }

            return await _serialControl.SendAndWaitForResult(hstMsg);
        }

        public async Task<ErrorCode> SendNewTargetSampleVelocity(int value)
        {
            HostMessage hstMsg = new HostMessage
            {
                FluidicsCommand = new FluidicsCommand
                {
                    SetSheathSpeed = new SetSheathSpeedPID
                    {
                        SheathSpeed = value
                    }
                }
            };

            if (_serialControl == null)
            {
                return ErrorCode.UnknownError;
            }

            return await _serialControl.SendAndWaitForResult(hstMsg);
        }

        public async Task<ErrorCode> SendNewSheathKP(float kp)
        {
            HostMessage hstMsg = new HostMessage
            {
                FluidicsCommand = new FluidicsCommand
                {
                    SetPropValvePid = new SetPropValvePID
                    {
                        Proportional = kp
                    }
                }
            };

            if (_serialControl == null)
            {
                return ErrorCode.UnknownError;
            }

            return await _serialControl.SendAndWaitForResult(hstMsg);
        }

        public async Task<ErrorCode> SendNewSheathKI(float ki)
        {
            HostMessage hstMsg = new HostMessage
            {
                FluidicsCommand = new FluidicsCommand
                {
                    SetPropValvePid = new SetPropValvePID
                    {
                        Integral = ki
                    }
                }
            };

            if (_serialControl == null)
            {
                return ErrorCode.UnknownError;
            }

            return await _serialControl.SendAndWaitForResult(hstMsg);
        }

        public async Task<ErrorCode> SendNewSheathKD(float kd)
        {
            HostMessage hstMsg = new HostMessage
            {
                FluidicsCommand = new FluidicsCommand
                {
                    SetPropValvePid = new SetPropValvePID
                    {
                        Derivative = kd
                    }
                }
            };

            if (_serialControl == null)
            {
                return ErrorCode.UnknownError;
            }

            return await _serialControl.SendAndWaitForResult(hstMsg);
        }

        public async Task<ErrorCode> SendNewSampleKP(float kp)
        {
            HostMessage hstMsg = new HostMessage
            {
                FluidicsCommand = new FluidicsCommand
                {
                    SetPinchValvePid = new SetPinchValvePID
                    {
                        Proportional = kp
                    }
                }
            };

            if (_serialControl == null)
            {
                return ErrorCode.UnknownError;
            }

            return await _serialControl.SendAndWaitForResult(hstMsg);
        }

        public async Task<ErrorCode> SendNewSampleKI(float ki)
        {
            HostMessage hstMsg = new HostMessage
            {
                FluidicsCommand = new FluidicsCommand
                {
                    SetPinchValvePid = new SetPinchValvePID
                    {
                        Integral = ki
                    }
                }
            };

            if (_serialControl == null)
            {
                return ErrorCode.UnknownError;
            }

            return await _serialControl.SendAndWaitForResult(hstMsg);
        }

        public async Task<ErrorCode> SendNewSampleKD(float kd)
        {
            HostMessage hstMsg = new HostMessage
            {
                FluidicsCommand = new FluidicsCommand
                {
                    SetPinchValvePid = new SetPinchValvePID
                    {
                        Derivative = kd
                    }
                }
            };

            if (_serialControl == null)
            {
                return ErrorCode.UnknownError;
            }

            return await _serialControl.SendAndWaitForResult(hstMsg);
        }

        public async Task<ErrorCode> SendNewPropValveBacklash(int backlash)
        {
            HostMessage hstMsg = new HostMessage
            {
                FluidicsCommand = new FluidicsCommand
                {
                    SetPropValveBacklash = new SetPropValveBacklashCompensation
                    {
                        BacklashCompensationSteps = backlash
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
                OnSignalReceived?.Invoke(this, dvcMsg.Signal);
            }
        }
    }
}
