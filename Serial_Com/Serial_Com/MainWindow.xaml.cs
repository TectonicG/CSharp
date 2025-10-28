using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Serial_Com.Services;
using Serial_Com.Services.Devices;
using Serial_Com.Services.Serial;
using System;
using System.Data;
using System.Diagnostics;
using System.IO.Ports;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using static System.Runtime.InteropServices.JavaScript.JSType;



namespace Serial_Com
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        private readonly Fluidics fluidics;

        public MainWindow()
        {

            InitializeComponent();

            //Start the backend
            fluidics = new Fluidics();
            fluidics.ConnectionChanged += OnFluidicsConnectionChanged;
            fluidics.OnQueryFlowRecieved += OnQueryFlowRecieved;

        }

        private void OnQueryFlowRecieved(object? sender, FluidicsSystemInfo info)
        {
            Dispatcher.Invoke(() =>
            {
                ParseQueryFlow(info);
            });
        }

        private void ParseQueryFlow(FluidicsSystemInfo info)
        {

            if (info == null)
            {
                return;
            }

            Dictionary<PumpStates, (string, Button, Button)> pumpStates = new Dictionary<PumpStates, (string, Button, Button)>();
            pumpStates.Add(PumpStates.On, ("On", sheathPumpOnButton, wastePumpOnButton));
            pumpStates.Add(PumpStates.Off, ("Off", sheathPumpOffButton, wastePumpOffButton));
            pumpStates.Add(PumpStates.Auto, ("Auto", sheathPumpAutoButton, wastePumpAutoButton));
            pumpStates.Add(PumpStates.SpeedControlled, ("Speed Controlled", sheathPumpSpeedButton, wastePumpSpeedButton));

            //Flow Rates
            sheathRate.Content = $"{info.SheathRate:F4} mL/min";
            sampleVelocity.Content = $"{info.SampleVelocity} mm/s";
            sampleRate.Content = $"{info.SampleRate:F4} uL/min";
            targetSampleRate.Content = $"{info.TargetSample} uL/min";
            sheathRateStableIndicator.Fill = info.SheathRateUnstable == 1 ? Brushes.Green : Brushes.Red;
            sampleRateStableIndicator.Fill = info.SampleRateUnstable == 1 ? Brushes.Green : Brushes.Red;

            //Vacuum Level
            wasteVacuumLevel.Content = $"{info.WasteVacuumLevel:F6} PSI";

            //Tube Detection
            tubeDetectionIndicator.Fill = info.TubeDetection == 1 ? Brushes.Green : Brushes.Red;

            //Level Sensors
            internalSheathLowIndicator.Fill = info.InternalSheathHigh == 1 ? Brushes.Red : Brushes.Green;
            internalWasteHighIndicator.Fill = info.InternalWasteLow == 1 ? Brushes.Red : Brushes.Green;

            externalSheathLowIndicator.Fill = info.ExternalSheathLow == 1 ? Brushes.Red : Brushes.Green;
            externalSheathConnectedIndicator.Fill = info.SheathLevelSensorConnected == 1 ? Brushes.Green : Brushes.Red;

            externalWasteHighIndicator.Fill = info.ExternalWasteHigh == 1 ? Brushes.Red : Brushes.Green;
            externalWasteConnectedIndicator.Fill = info.WasteLevelSensorConnected == 1 ? Brushes.Green : Brushes.Red;

            //Sheath and Waste Pumps
            sheathPumpStateIndicator.Fill = info.SheathPumpState == 1 ? Brushes.Green : Brushes.Red;
            sheathSpeedIndicator.Fill = info.SheathPumpState == 1 ? Brushes.Green : Brushes.Red;

            pumpStates.TryGetValue(info.SheathPumpMode, out var sheathPumpAttributes);
            var (_sheathPumpMode, sheathBtn, _) = sheathPumpAttributes;

            sheathPumpMode.Content = _sheathPumpMode.ToString();
            sheathPumpOnButton.Background = Brushes.Red;
            sheathPumpOffButton.Background = Brushes.Red;
            sheathPumpAutoButton.Background = Brushes.Red;
            sheathPumpSpeedButton.Background = Brushes.Red;
            sheathBtn.Background = Brushes.Green;

            sheathPumpSpeed.Content = $"{info.SheathPumpSpeed}%";
            sheathSpeedValue.Content = $"{info.SheathPumpSpeed}%";

            wastePumpStateIndicator.Fill = info.WastePumpState == 1 ? Brushes.Green : Brushes.Red;
            wasteSpeedIndicator.Fill = info.WastePumpState == 1 ? Brushes.Green : Brushes.Red;
            pumpStates.TryGetValue(info.WastePumpMode, out var wastePumpAttributes);
            var (_wastePumpMode, _, wasteBtn) = wastePumpAttributes;

            wastePumpMode.Content = _wastePumpMode;
            wastePumpOnButton.Background = Brushes.Red;
            wastePumpOffButton.Background = Brushes.Red;
            wastePumpAutoButton.Background = Brushes.Red;
            wastePumpSpeedButton.Background = Brushes.Red;
            wasteBtn.Background = Brushes.Green;

            wastePumpSpeed.Content = $"{info.WastePumpSpeed}%";
            wasteSpeedValue.Content = $"{info.WastePumpSpeed}%";

            //Valve states
            valveOneIndicator.Fill = info.ValveStates[0] == 1 ? Brushes.Green : Brushes.Red;
            valveOneButton.Background = info.ValveStates[0] == 1 ? Brushes.Green : Brushes.Red;
            valveOneButton.Content = info.ValveStates[0] == 1 ? "Disable Valve 1" : "Enable Valve 1";

            valveTwoIndicator.Fill = info.ValveStates[1] == 1 ? Brushes.Green : Brushes.Red;
            valveTwoButton.Background = info.ValveStates[1] == 1 ? Brushes.Green : Brushes.Red;
            valveTwoButton.Content = info.ValveStates[1] == 1 ? "Disable Valve 2" : "Enable Valve 2";

            valveThreeIndicator.Fill = info.ValveStates[2] == 1 ? Brushes.Green : Brushes.Red;
            valveThreeButton.Background = info.ValveStates[2] == 1 ? Brushes.Green : Brushes.Red;
            valveThreeButton.Content = info.ValveStates[2] == 1 ? "Disable Valve 3" : "Enable Valve 3";

            valveFourIndicator.Fill = info.ValveStates[3] == 1 ? Brushes.Green : Brushes.Red;
            valveFourButton.Background = info.ValveStates[3] == 1 ? Brushes.Green : Brushes.Red;
            valveFourButton.Content = info.ValveStates[3] == 1 ? "Disable Valve 4" : "Enable Valve 4";

            valveFiveIndicator.Fill = info.ValveStates[4] == 1 ? Brushes.Green : Brushes.Red;
            valveFiveButton.Background = info.ValveStates[4] == 1 ? Brushes.Green : Brushes.Red;
            valveFiveButton.Content = info.ValveStates[4] == 1 ? "Disable Valve 5" : "Enable Valve 5";

            valveSixIndicator.Fill = info.ValveStates[5] == 1 ? Brushes.Green : Brushes.Red;
            valveSixButton.Background = info.ValveStates[5] == 1 ? Brushes.Green : Brushes.Red;
            valveSixButton.Content = info.ValveStates[5] == 1 ? "Disable Valve 6" : "Enable Valve 6";

            valveSevenIndicator.Fill = info.ValveStates[6] == 1 ? Brushes.Green : Brushes.Red;
            valveSevenButton.Background = info.ValveStates[6] == 1 ? Brushes.Green : Brushes.Red;
            valveSevenButton.Content = info.ValveStates[7] == 1 ? "Disable Valve 7" : "Enable Valve 7";

            valveEightIndicator.Fill = info.ValveStates[7] == 1 ? Brushes.Green : Brushes.Red;
            valveEightButton.Background = info.ValveStates[7] == 1 ? Brushes.Green : Brushes.Red;
            valveEightButton.Content = info.ValveStates[7] == 1 ? "Disable Valve 8" : "Enable Valve 8";

            valveNineIndicator.Fill = info.ValveStates[8] == 1 ? Brushes.Green : Brushes.Red;
            valveNineButton.Background = info.ValveStates[8] == 1 ? Brushes.Green : Brushes.Red;
            valveNineButton.Content = info.ValveStates[8] == 1 ? "Disable Valve 9" : "Enable Valve 9";

            valveTenIndicator.Fill = info.ValveStates[9] == 1 ? Brushes.Green : Brushes.Red;
            valveTenButton.Background = info.ValveStates[9] == 1 ? Brushes.Green : Brushes.Red;
            valveTenButton.Content = info.ValveStates[9] == 1 ? "Disable Valve 10" : "Enable Valve 10";

            //Proportional valves
            pinchValveStepCount.Content = info.PinchValveCurrentPos;
            pinchValveStepPos.Content = info.PinchValveCurrentPos;
            propValveStepCount.Content = info.PropValveCurrentPos;
            propValveStepPos.Content = info.PropValveCurrentPos;

            //Filter
            filterCloggedIndicator.Fill = info.FilterClogged == 1 ? Brushes.Red : Brushes.Green;
            filterDiffPressure.Content = $"{info.FilterDiffPressureValue:F6} PSI";

            //Leak Sensors
            leakDetectorOneStateIndicator.Fill = info.LeakDetectorStates[0] == 1 ? Brushes.Green : Brushes.Red;
            leakDetectorOneConnectedIndicator.Fill = info.LeakDetectorConnectionStates[0] == 1 ? Brushes.Green : Brushes.Red;

            leakDetectorTwoStateIndicator.Fill = info.LeakDetectorStates[1] == 1 ? Brushes.Green : Brushes.Red;
            leakDetectorTwoConnectedIndicator.Fill = info.LeakDetectorConnectionStates[1] == 1 ? Brushes.Green : Brushes.Red;

            leakDetectorThreeStateIndicator.Fill = info.LeakDetectorStates[2] == 1 ? Brushes.Green : Brushes.Red;
            leakDetectorThreeConnectedIndicator.Fill = info.LeakDetectorConnectionStates[2] == 1 ? Brushes.Green : Brushes.Red;

        }

        private async void ToggleValve(object? sender, RoutedEventArgs e)
        {
            //Check to see that the sender is a button and that the button number is not empty
            //{ Tag: string btnNum } creates btnNum and assigns it the value of Tag
            if (!(sender is Button { Tag: string btnNum } btn && !string.IsNullOrEmpty(btnNum)))
            {
                return;
            }

            //Set the sent valve state based on the current state of the button
            var valveState = (string)btn.Content == $"Enable Valve {btn.Tag}" ? EnableDisableDef.Enable : EnableDisableDef.Disable;

            var result = await fluidics.SetValveState(Int32.Parse(btnNum), valveState);

            Debug.WriteLine($"This was the result in the UI for toggle valve: {result}");
        }

        private void ComPortComboBoxOpen(object? sender, EventArgs e)
        {
            if (sender is ComboBox box)
            {
                box.ItemsSource = SerialPort.GetPortNames();
            }

        }

        private void OnFluidicsConnectionChanged(object? sender, bool connected)
        {
            Dispatcher.Invoke(() =>
            {
                connectButton.Content = connected ? "Disconnect" : "Connect";
                SetComPortEnabled(connected);
                SetPropValveEnabled(connected);
                SetPinchValveEnabled(connected);
            });

        }

        private async void ConnectButtonClicked(object? sender, RoutedEventArgs e)
        {
            //If we are not connected, connect
            if (!fluidics.IsConnected)
            {
                string port = (string)comPortBox.SelectedItem;
                //Make a new token when trying to comment
                bool ok = await fluidics.ConnectToFluidicsAsync(port);
            }
            else
            {
                //Else disconnect
                await fluidics.DisconnectFluidicsAsync();
            }
        }

        private void EnableValveChanges(object? sender, RoutedEventArgs e)
        {
            if (!(sender is CheckBox { IsChecked: bool ckd } checkBox))
            {
                return;
            }

            SetValveButtonsEnable(ckd);
        }
        //To set all of the valves enabled or disabled
        private void SetValveButtonsEnable(bool enabled)
        {
            valveOneButton.IsEnabled = enabled;
            valveTwoButton.IsEnabled = enabled;
            valveThreeButton.IsEnabled = enabled;
            valveFourButton.IsEnabled = enabled;
            valveFiveButton.IsEnabled = enabled;
            valveSixButton.IsEnabled = enabled;
            valveSevenButton.IsEnabled = enabled;
            valveEightButton.IsEnabled = enabled;
            valveNineButton.IsEnabled = enabled;
            valveTenButton.IsEnabled = enabled;
        }

        //To set the drop down enabled or disabled
        private void SetComPortEnabled(bool enabled)
        {
            comPortBox.IsEnabled = !enabled;
        }

        private void SetPropValveEnabled(bool enabled)
        {
            homePropValveButton.IsEnabled = enabled;
            propValveTakeStepsButton.IsEnabled = enabled;
            disablePropValveButton.IsEnabled = enabled;
            propValveStepCount.Content = "0";
        }

        private void SetPinchValveEnabled(bool enabled)
        {
            homePinchValveButton.IsEnabled = enabled;
            pinchValveTakeStepsButton.IsEnabled = enabled;
            disablePinchValveButton.IsEnabled = enabled;
            pinchValveStepCount.Content = "0";
        }

        private void EnableSheathPumpChanges(object? sender, RoutedEventArgs e)
        {

            if (!(sender is CheckBox { IsChecked: bool ckd } checkBox))
            {
                return;
            }

            SetSheathPumpEnabled(ckd);
        }

        private void EnableWastePumpChanges(object? sender, RoutedEventArgs e)
        {

            if (!(sender is CheckBox { IsChecked: bool ckd } checkBox))
            {
                return;
            }

            SetWastePumpEnabled(ckd);
        }

        private void SetSheathPumpEnabled(bool enabled)
        {
            sheathPumpOnButton.IsEnabled = enabled;
            sheathPumpOffButton.IsEnabled = enabled;
            sheathPumpAutoButton.IsEnabled = enabled;
            sheathPumpSpeedButton.IsEnabled = enabled;
            sendSheathSpeedButton.IsEnabled = enabled;
            sheathSpeedSlider.IsEnabled = enabled;
            if (!enabled)
            {
                sheathSpeedSlider.Value = 0;
                sheathSpeedSliderValue.Content = "0";
                sheathSpeedIndicator.Fill = Brushes.Red;
            }
        }

        private void SetWastePumpEnabled(bool enabled)
        {
            wastePumpOnButton.IsEnabled = enabled;
            wastePumpOffButton.IsEnabled = enabled;
            wastePumpAutoButton.IsEnabled = enabled;
            wastePumpSpeedButton.IsEnabled = enabled;
            sendWasteSpeedButton.IsEnabled = enabled;
            wasteSpeedSlider.IsEnabled = enabled;
            if (!enabled)
            {
                wasteSpeedSlider.Value = 0;
                wasteSpeedSliderValue.Content = "0";
                wasteSpeedIndicator.Fill = Brushes.Red;
            }
        }

        private void PumpSliderChanged(object? sender, RoutedEventArgs e)
        {
            //Check that the object is a slider and that the tag value is not null
            if (!(sender is Slider { Tag: string sldrType } sldr && !string.IsNullOrEmpty(sldrType.ToString())))
            {
                return;
            }

            string type = sldrType.ToString();
            int sliderValue = Int32.Parse(sldr.Value.ToString());

            switch (type)
            {
                case "wasteSlider":
                    wasteSpeedSliderValue.Content = $"{sliderValue}%";
                    break;
                case "sheathSlider":
                    sheathSpeedSliderValue.Content = $"{sliderValue}%";
                    break;
                default:
                    throw new NotImplementedException();
            }

        }

        private async void SendValveSteps(object? sender, RoutedEventArgs e)
        {

            if (!(sender is Button { Name: string btnName }))
            {
                return;
            }

            if (btnName == propValveTakeStepsButton.Name)
            {
                await fluidics.MovePropValve(Int32.Parse(propValveStepCountBox.Text));
            }
            else if (btnName == pinchValveTakeStepsButton.Name)
            {
                await fluidics.MovePinchValve(Int32.Parse(pinchValveStepCountBox.Text));
            }
            else
            {
                return;
            }

        }

        private async void HomeValve(object? sender, RoutedEventArgs e)
        {

            if (!(sender is Button { Name: string btnName }))
            {
                return;
            }

            if (btnName == homePropValveButton.Name)
            {
                await fluidics.HomePropValve();
            }
            else if (btnName == homePinchValveButton.Name)
            {
                await fluidics.HomePinchValve();
            }
            else
            {
                return;
            }
        }

        private async void DisableValve(object? sender, RoutedEventArgs e)
        {

            if (!(sender is Button { Name: string btnName }))
            {
                return;
            }

            if (btnName == disablePropValveButton.Name)
            {
                await fluidics.DisablePropValve();
            }
            else if (btnName == disablePinchValveButton.Name)
            {
                await fluidics.DisablePinchValve();
            }
            else
            {
                return;
            }

        }

        private async void HandlePump(object? sender, RoutedEventArgs e)
        {

            if (!(sender is Button { Name: string btnName, Tag: string type }))
            {
                return;
            }

            PumpStates state;
            PumpSelection pumpSelection;
            uint speed = 0;

            if (type == "sheath")
            {
                pumpSelection = PumpSelection.SheathPump;
                speed = (uint)sheathSpeedSlider.Value;
            }
            else
            {
                pumpSelection = PumpSelection.WastePump;
                speed = (uint)wasteSpeedSlider.Value;
            }

            if (btnName == sheathPumpOffButton.Name || btnName == wastePumpOffButton.Name)
            {
                state = PumpStates.Off;
            }
            else if (btnName == sheathPumpOnButton.Name || btnName == wastePumpOnButton.Name)
            {
                state = PumpStates.On;
            }
            else if (btnName == sheathPumpAutoButton.Name || btnName == wastePumpAutoButton.Name)
            {
                state = PumpStates.Auto;
            }
            else if (btnName == sheathPumpSpeedButton.Name || btnName == wastePumpSpeedButton.Name)
            {
                state = PumpStates.SpeedControlled;
                speed = 0;
            }
            else
            {
                state = PumpStates.SpeedControlled;
            }

            await fluidics.HandlePump(pumpSelection, state, speed);

        }
    }
}
