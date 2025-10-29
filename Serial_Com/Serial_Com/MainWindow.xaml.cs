using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using ScottPlot;
using ScottPlot.Hatches;
using ScottPlot.Plottables;
using Serial_Com.Services;
using Serial_Com.Services.Devices;
using Serial_Com.Services.Serial;
using System;
using System.Data;
using System.Diagnostics;
using System.IO.Ports;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.Arm;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
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





namespace Serial_Com
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        private readonly Fluidics fluidics;
        // Settings
        private readonly List<double> _xs = new();
        private readonly List<double> _sheath = new();
        private readonly List<double> _sampleVel = new();
        private readonly List<double> _sampleRate = new();
        private readonly List<double> _vacuumLevel = new();
        private readonly Dictionary<Plot, (double, double)> graphYAxisLimits = new Dictionary<Plot, (double, double)>();


        private readonly DispatcherTimer _frameTimer = new() { Interval = TimeSpan.FromMilliseconds(50) }; // ~20 FPS
        private const int MaxPoints = 3000;

        private Random _rndm = new Random();
        public MainWindow()
        {

            InitializeComponent();

            //Add lookupable limits to graphs
            graphYAxisLimits.Add(sheathRatePlot.Plot, (-5.5, 5.5));
            graphYAxisLimits.Add(sampleVelocityPlot.Plot, (-3666, 3666));
            graphYAxisLimits.Add(sampleRatePlot.Plot, (-120.0, 120.0));
            graphYAxisLimits.Add(vacuumLevelPlot.Plot, (-5.5, 1));


            //Start the backend
            fluidics = new Fluidics();
            fluidics.ConnectionChanged += OnFluidicsConnectionChanged;
            fluidics.OnQueryFlowRecieved += OnQueryFlowRecieved;

            InitPlots();
        }

        private void InitPlots()
        {
            //Sheath Rate Plot
            var p1 = sheathRatePlot.Plot.Add.Scatter(_xs, _sheath);
            p1.MarkerShape = MarkerShape.None;
            p1.LineColor = ScottPlot.Colors.DarkGreen;
            p1.LineWidth = 2;
            graphYAxisLimits.TryGetValue(sheathRatePlot.Plot, out var sheathYLimits);
            var (sheathMinY, sheathMaxY) = sheathYLimits;
            sheathRatePlot.Plot.Axes.SetLimits(0, MaxPoints, sheathMinY, sheathMaxY);
            sheathRatePlot.Plot.Axes.Left.Label.Text = "Sheath Rate mL/min";
            sheathRatePlot.Plot.Axes.Left.Label.FontSize = 40;
            sheathRatePlot.Plot.Axes.Bottom.Label.Text = "Point";
            sheathRatePlot.Plot.Axes.Bottom.Label.FontSize = 40;
            sheathRatePlot.Plot.Axes.Left.TickLabelStyle.FontSize = 20;
            sheathRatePlot.Plot.Axes.Bottom.TickLabelStyle.FontSize = 20;
            sheathRatePlot.Plot.Axes.AntiAlias(true);


            //Sample Velocity Plot
            var p2 = sampleVelocityPlot.Plot.Add.Scatter(_xs, _sampleVel);
            p2.MarkerShape = MarkerShape.None;
            p2.LineColor = ScottPlot.Colors.DarkBlue;
            p2.LineWidth = 2;
            graphYAxisLimits.TryGetValue(sampleVelocityPlot.Plot, out var velocityYLimits);
            var (velocityMinY, velocityMaxY) = velocityYLimits;
            sampleVelocityPlot.Plot.Axes.SetLimits(0, MaxPoints, velocityMinY, velocityMaxY);
            sampleVelocityPlot.Plot.Axes.Left.Label.Text = "Sample Velocity mm/ss";
            sampleVelocityPlot.Plot.Axes.Left.Label.FontSize = 40;
            sampleVelocityPlot.Plot.Axes.Bottom.Label.Text = "Point";
            sampleVelocityPlot.Plot.Axes.Bottom.Label.FontSize = 40;
            sampleVelocityPlot.Plot.Axes.Left.TickLabelStyle.FontSize = 20;
            sampleVelocityPlot.Plot.Axes.Bottom.TickLabelStyle.FontSize = 20;
            sampleVelocityPlot.Plot.Axes.AntiAlias(true);

            //Sample Rate Plot
            var p3 = sampleRatePlot.Plot.Add.Scatter(_xs, _sampleRate);
            p3.MarkerShape = MarkerShape.None;
            p3.LineColor = ScottPlot.Colors.DarkRed;
            p3.LineWidth = 2;
            graphYAxisLimits.TryGetValue(sampleRatePlot.Plot, out var sampleYLimits);
            var (sampleMinY, sampleMaxY) = sampleYLimits;
            sampleRatePlot.Plot.Axes.SetLimits(0, MaxPoints, sampleMinY, sampleMaxY);
            sampleRatePlot.Plot.Axes.Left.Label.Text = "Sample Rate uL/min";
            sampleRatePlot.Plot.Axes.Left.Label.FontSize = 40;
            sampleRatePlot.Plot.Axes.Bottom.Label.Text = "Point";
            sampleRatePlot.Plot.Axes.Bottom.Label.FontSize = 40;
            sampleRatePlot.Plot.Axes.Left.TickLabelStyle.FontSize = 20;
            sampleRatePlot.Plot.Axes.Bottom.TickLabelStyle.FontSize = 20;
            sampleRatePlot.Plot.Axes.AntiAlias(true);

            //Sample Rate Plot
            var p4 = vacuumLevelPlot.Plot.Add.Scatter(_xs, _vacuumLevel);
            p4.MarkerShape = MarkerShape.None;
            p4.LineColor = ScottPlot.Colors.Black;
            p4.LineWidth = 2;
            graphYAxisLimits.TryGetValue(vacuumLevelPlot.Plot, out var vacYLimits);
            var (vacMinY, vacMaxY) = vacYLimits;
            vacuumLevelPlot.Plot.Axes.SetLimits(0, MaxPoints, vacMinY, vacMaxY);
            vacuumLevelPlot.Plot.Axes.Left.Label.Text = "Vacuum Level PSI";
            vacuumLevelPlot.Plot.Axes.Left.Label.FontSize = 40;
            vacuumLevelPlot.Plot.Axes.Bottom.Label.Text = "Point";
            vacuumLevelPlot.Plot.Axes.Bottom.Label.FontSize = 40;
            vacuumLevelPlot.Plot.Axes.Left.TickLabelStyle.FontSize = 20;
            vacuumLevelPlot.Plot.Axes.Bottom.TickLabelStyle.FontSize = 20;
            vacuumLevelPlot.Plot.Axes.AntiAlias(true);

            //Paced redraw for smoothness
            _frameTimer.Tick += FrameTick;
            _frameTimer.Start();
        }

        private void FrameTick(object? sender, EventArgs e)
        {
            UpdateScrollWindow();
            sheathRatePlot.Refresh();
            sampleVelocityPlot.Refresh();
            sampleRatePlot.Refresh();
            vacuumLevelPlot.Refresh();
        }

        private void AppendData(float sheathRate, int sampleVelocity, float sampleRate, float vacuumLevel)
        {
            //Increment the next x-axis point
            double nextX = _xs.Count == 0 ? 0 : _xs[^1] + 1;
            _xs.Add(nextX);
            _sheath.Add(sheathRate);
            _sampleVel.Add(sampleVelocity);
            _sampleRate.Add(sampleRate);
            _vacuumLevel.Add(vacuumLevel);

            if (_xs.Count >= MaxPoints)
            {
                _xs.RemoveAt(0);
                _sheath.RemoveAt(0);
                _sampleVel.RemoveAt(0);
                _sampleRate.RemoveAt(0);
                _vacuumLevel.RemoveAt(0);
            }
        }

        private void UpdateScrollWindow()
        {
            if (_xs.Count == 0)
            {
                return;
            }

            double lastX = _xs[^1];
            double minX = lastX - MaxPoints;
            minX = minX < 0 ? 0 : minX;

            //Sheath Rate Graph Options
            if (sheathRatePlotAutoScroll.IsChecked == true)
            {
                AutoScrollXAxis(sheathRatePlot.Plot, minX, lastX);
            }
            else
            {
                KeepGraphWithinBounds(sheathRatePlot.Plot, minX, lastX);
            }

            //Sample Rate Graph Options
            if (sampleRatePlotAutoScroll.IsChecked == true)
            {
                AutoScrollXAxis(sampleRatePlot.Plot, minX, lastX);
            }
            else
            {
                KeepGraphWithinBounds(sampleRatePlot.Plot, minX, lastX);
            }

            //Sample Velocity Graph Options
            if (sampleVelocityPlotAutoScroll.IsChecked == true)
            {
                AutoScrollXAxis(sampleVelocityPlot.Plot, minX, lastX);
            }
            else
            {
                KeepGraphWithinBounds(sampleVelocityPlot.Plot, minX, lastX);
            }

            //Vacuum Graph Options
            if (vacuumPlotAutoScroll.IsChecked == true)
            {
                AutoScrollXAxis(vacuumLevelPlot.Plot, minX, lastX);
            }
            else
            {
                KeepGraphWithinBounds(vacuumLevelPlot.Plot, minX, lastX);
            }

        }

        private void AutoScrollXAxis(Plot plot, double xmin, double xmax)
        {
            var currentLimits = plot.Axes.GetLimits();

            double xMinSet = currentLimits.Left;
            double yMinSet = currentLimits.Bottom;
            double yMaxSet = currentLimits.Top;
            graphYAxisLimits.TryGetValue(plot, out var yLimits);
            var (ymin, ymax) = yLimits;

            if (xMinSet < xmin)
            {
                xMinSet = xmin;
            }

            if (yMinSet < ymin)
            {
                yMinSet = ymin;
            }

            if (yMaxSet > ymax)
            {
                yMaxSet = ymax;
            }

            plot.Axes.SetLimits(xMinSet, xmax, yMinSet, yMaxSet);
        }

        private void KeepGraphWithinBounds(Plot plot, double xmin, double xmax)
        {
            //Only resets the limits if it needs to keep the graph within bounds
            var currentLimits = plot.Axes.GetLimits();

            double xMinSet = currentLimits.Left;
            double xMaxSet = currentLimits.Right;
            double yMinSet = currentLimits.Bottom;
            double yMaxSet = currentLimits.Top;

            graphYAxisLimits.TryGetValue(plot, out var yLimits);
            var (ymin, ymax) = yLimits;

            if (xMinSet < xmin)
            {
                xMinSet = xmin;
            }

            if (xMaxSet > xmax)
            {
                xMaxSet = xmax;
            }

            if (yMinSet < ymin)
            {
                yMinSet = ymin;
            }

            if (yMaxSet > ymax)
            {
                yMaxSet = ymax;
            }

            plot.Axes.SetLimits(xMinSet, xMaxSet, yMinSet, yMaxSet);
        }

        private void ResetScaleGraph(Plot plot, double xmin, double xmax)
        {
            graphYAxisLimits.TryGetValue(plot, out var yLimits);
            var (ymin, ymax) = yLimits;
            plot.Axes.SetLimits(xmin, xmax, ymin, ymax);

        }

        private void OnResetScaleGraphClicked(object? sender, RoutedEventArgs e)
        {
            if (!(sender is Button { Name: string name } btn))
            {
                return;
            }

            double lastX = _xs[^1];
            double minX = lastX - MaxPoints;
            minX = minX < 0 ? 0 : minX;

            switch (name)
            {
                case nameof(sheathRateAutoScaleButton):
                    ResetScaleGraph(sheathRatePlot.Plot, minX, lastX);
                    break;
                case nameof(sampleVelocityRateAutoScaleButton):
                    ResetScaleGraph(sampleVelocityPlot.Plot, minX, lastX);
                    break;
                case nameof(sampleRateAutoScaleButton):
                    ResetScaleGraph(sampleRatePlot.Plot, minX, lastX);
                    break;
                case nameof(vacuumAutoScaleButton):
                    ResetScaleGraph(vacuumLevelPlot.Plot, minX, lastX);
                    break;
            }
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
            var _sheathRate = info.SheathRate;
            var _sampleVelocity = info.SampleVelocity;
            var _sampleRate = info.SampleRate;

            //Comment out or delete later:
            _sheathRate = _rndm.NextSingle() * (float)5.5;
            _sampleVelocity = _rndm.Next(0, 3666);
            _sampleRate = _rndm.NextSingle() * 120;

            var sheathRateFormat = $"{_sheathRate:F4} mL/min";
            var sampleVelocityFormat = $"{_sampleVelocity} mm/s";
            var sampleRateFormat = $"{_sampleRate:F4} uL/min";

            sheathRate.Content = sheathRateFormat;
            sheathRateGraphValue.Content = sheathRateFormat;
            sampleVelocity.Content = sampleVelocityFormat;
            sampleVelocityGraphValue.Content = sampleVelocityFormat;
            sampleRate.Content = sampleRateFormat;
            sampleRateGraphValue.Content = sampleRateFormat;
            targetSampleRate.Content = $"{info.TargetSample} uL/min";
            sheathRateStableIndicator.Fill = info.SheathRateUnstable == 1 ? Brushes.Green : Brushes.Red;
            sampleRateStableIndicator.Fill = info.SampleRateUnstable == 1 ? Brushes.Green : Brushes.Red;


            //Vacuum Level
            var _vacLevel = info.WasteVacuumLevel;
            //Comment out or delete later:
            _vacLevel = _rndm.NextSingle() * (float)-5.0;
            var vacLevelFormat = $"{_vacLevel:F6} PSI";
            vacuumLevelGraphValue.Content = vacLevelFormat;
            wasteVacuumLevel.Content = vacLevelFormat;

            AppendData(_sheathRate, _sampleVelocity, _sampleRate, _vacLevel);

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
            valveSevenButton.Content = info.ValveStates[6] == 1 ? "Disable Valve 7" : "Enable Valve 7";

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

            //Start stop button
            var systemState = info.SystemState;
            switch (systemState)
            {
                case FluidicsSystemState.FluidicsStarted:
                    startStopButton.Tag = "started";
                    startStopButton.Content = "Stop";
                    startStopButton.Background = Brushes.Red;
                    break;

                case FluidicsSystemState.FluidicsStarting:
                    startStopButton.Tag = "starting";
                    startStopButton.Content = "Starting...";
                    startStopButton.Background = Brushes.LightGreen;
                    break;

                case FluidicsSystemState.FluidicsStopped:
                    startStopButton.Tag = "stopped";
                    startStopButton.Content = "Start";
                    startStopButton.Background = Brushes.Green;
                    break;

                case FluidicsSystemState.FluidicsStopping:
                    startStopButton.Tag = "stopping";
                    startStopButton.Content = "Stopping...";
                    startStopButton.Background = Brushes.Yellow;
                    break;
            }

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

            if (!(sender is Button { Name: string btnName, Tag: string type } btn))
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

        private async void HomeZAxis(object? sender, RoutedEventArgs e)
        {
            if (sender is not Button)
            {
                return;
            }

            await fluidics.HomeZAxis();
        }

        private async void MoveZAxis(object? sender, RoutedEventArgs e)
        {
            if (sender is not Button)
            {
                return;
            }

            string height = sliderDepthValue.Content.ToString() ?? "0";
            string trimmed = height.Replace(" mm", "");

            await fluidics.MoveZAxis(float.Parse(trimmed));
        }
        private void ZAxisSliderChanged(object? sender, RoutedEventArgs e)
        {
            if (!(sender is Slider { Value: double sldrValue } sldr))
            {
                return;
            }

            sliderDepthValue.Content = $"{(((int)sldrValue) / 100.0).ToString()} mm";
        }

        private void ZAxisTextBoxChanged(object? sender, RoutedEventArgs e)
        {
            if (!(sender is TextBox { Text: string value } txtBx))
            {
                return;
            }

            try
            {
                zaxisSlider.Value = Double.Parse(value);
            }
            catch
            {
                zaxisSlider.Value = 0;
            }

        }

        private void zAxisContectMenuButtonClicked(object? sender, RoutedEventArgs e)
        {
            if (!(sender is Button { Name: string btnName } btn))
            {
                return;
            }

            if (btnName == zaxisSetHeightButton.Name)
            {
                zaxisSetHeightButton.ContextMenu.PlacementTarget = zaxisSetHeightButton;
                zaxisSetHeightButton.ContextMenu.IsOpen = true;
            }
            else if (btnName == zaxisMoveToSetHeightButton.Name)
            {
                zaxisMoveToSetHeightButton.ContextMenu.PlacementTarget = zaxisMoveToSetHeightButton;
                zaxisMoveToSetHeightButton.ContextMenu.IsOpen = true;
            }
        }

        private async void SetHeight(object? sender, RoutedEventArgs e)
        {
            if (!(sender is MenuItem { Name: string btnName } btn))
            {
                return;
            }

            Dictionary<string, SettableZAxisHeights> setHeights = new Dictionary<string, SettableZAxisHeights>();
            setHeights.Add("setTubeTop", SettableZAxisHeights.TubeTop);
            setHeights.Add("setTubeBottom", SettableZAxisHeights.TubeBottom);
            setHeights.Add("setPlateTop", SettableZAxisHeights.PlateTop);
            setHeights.Add("setPlateBottom", SettableZAxisHeights.PlateBottom);

            setHeights.TryGetValue(btnName, out var setHeight);
            string height = sliderDepthValue.Content.ToString() ?? "0";
            string trimmed = height.Replace(" mm", "");

            await fluidics.SetHeight(setHeight, float.Parse(trimmed));
        }

        private async void MoveToSetHeight(object? sender, RoutedEventArgs e)
        {
            if (!(sender is MenuItem { Name: string btnName } btn))
            {
                return;
            }

            Dictionary<string, SettableZAxisHeights> setHeights = new Dictionary<string, SettableZAxisHeights>();
            setHeights.Add("moveTubeTop", SettableZAxisHeights.TubeTop);
            setHeights.Add("moveTubeBottom", SettableZAxisHeights.TubeBottom);
            setHeights.Add("movePlateTop", SettableZAxisHeights.PlateTop);
            setHeights.Add("movePlateBottom", SettableZAxisHeights.PlateBottom);

            setHeights.TryGetValue(btnName, out var setHeight);

            await fluidics.MoveToSetHeight(setHeight);
        }

        private async void EStopZAxis(object? sender, RoutedEventArgs e)
        {
            if (sender is not Button)
            {
                return;
            }

            await fluidics.EStopZAxis();

        }

        private async void StartStop(object? sender, RoutedEventArgs e)
        {
            if (!(sender is Button { Tag: string systemState } btn))
            {
                return;
            }

            switch (systemState)
            {
                case "stopped":
                    await fluidics.StartFluidics();
                    break;
                case "stopping":
                    //do nothing 
                    break;
                case "started":
                    await fluidics.StopFluidics();
                    break;
                case "starting":
                    //do nothing
                    break;
            }



        }
    }
}
