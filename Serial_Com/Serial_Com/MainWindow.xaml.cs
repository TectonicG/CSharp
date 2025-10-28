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

        //Backend class
        //private readonly Backend backend;
        private readonly Fluidics fluidics;
        private CancellationTokenSource _cancelBackend = new CancellationTokenSource();

        public MainWindow()
        {

            InitializeComponent();

            //Start the backend
            fluidics = new Fluidics();
            fluidics.ConnectionChanged += OnFluidicsConnectionChanged;
            fluidics.OnQueryFlowRecieved += ParseQueryFlow;

        }

        private void ParseQueryFlow(object? sender, FluidicsSystemInfo info)
        {
            Debug.WriteLine($"This is QF: {info}");
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

            //If it wnt through
            if (result == ErrorCode.Ok)
            {
                if(btn.Content.ToString() == $"Enable Valve {btn.Tag}")
                {
                    btn.Content = $"Disable Valve {btn.Tag}";
                    btn.Background = Brushes.Green;
                }
                else
                {
                    btn.Content = $"Enable Valve {btn.Tag}";
                    btn.Background = Brushes.Red;
                }
            }

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
                connect_button.Content = connected ? "Disconnect" : "Connect"
            );
        }
        private async void ConnectButtonClicked(object? sender, RoutedEventArgs e)
        {
            //If we are not connected, connect
            if (!fluidics.IsConnected)
            {
                string port = (string)com_port_box.SelectedItem;
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
            valve_one_button.IsEnabled = enabled;
            valve_two_button.IsEnabled = enabled;
            valve_three_button.IsEnabled = enabled;
            valve_four_button.IsEnabled = enabled;
            valve_five_button.IsEnabled = enabled;
            valve_six_button.IsEnabled = enabled;
            valve_seven_button.IsEnabled = enabled;
            valve_eight_button.IsEnabled = enabled;
            valve_nine_button.IsEnabled = enabled;
            valve_ten_button.IsEnabled = enabled;
        }

        //To set the drop down enabled or disabled
        private void SetComPortEnabled(bool enabled)
        {
            com_port_box.IsEnabled = !enabled;
        }

        private void SetPropValveEnabled(bool enabled)
        {
            home_prop_valve_button.IsEnabled = enabled;
            prop_valve_take_steps_button.IsEnabled = enabled;
            disable_prop_valve_button.IsEnabled = enabled;
            prop_valve_step_count.Content = "0";
        }

        private void SetPinchValveEnabled(bool enabled)
        {
            home_pinch_valve_button.IsEnabled = enabled;
            pinch_valve_take_steps_button.IsEnabled = enabled;
            disable_pinch_valve_button.IsEnabled = enabled;
            pinch_valve_step_count.Content = "0";
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
            sheath_pump_on_button.IsEnabled = enabled;
            sheath_pump_off_button.IsEnabled = enabled;
            sheath_pump_auto_button.IsEnabled = enabled;
            sheath_pump_speed_button.IsEnabled = enabled;
            send_sheath_speed_button.IsEnabled = enabled;
            sheath_speed_slider.IsEnabled = enabled;
            if (!enabled)
            {
                sheath_speed_slider.Value = 0;
                sheath_speed_slider_value.Content = "0";
                sheath_speed_indicator.Fill = Brushes.Red;
            }
        }
        private void SetWastePumpEnabled(bool enabled)
        {
            waste_pump_on_button.IsEnabled = enabled;
            waste_pump_off_button.IsEnabled = enabled;
            waste_pump_auto_button.IsEnabled = enabled;
            waste_pump_speed_button.IsEnabled = enabled;
            send_waste_speed_button.IsEnabled = enabled;
            waste_speed_slider.IsEnabled = enabled;
            if (!enabled)
            {
                waste_speed_slider.Value = 0;
                waste_speed_slider_value.Content = "0";
                waste_speed_indicator.Fill = Brushes.Red;
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
                case "waste_slider":
                    waste_speed_slider_value.Content = sliderValue;
                    break;
                case "sheath_slider":
                    sheath_speed_slider_value.Content = sliderValue;
                    break;
                default:
                    throw new NotImplementedException();
            }

        }

    }
}
