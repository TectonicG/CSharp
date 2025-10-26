using Google.Protobuf;
using Serial_Com.InternalMessages;
using Serial_Com.Services.Backend;
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



namespace Serial_Com
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        //Backend class
        private readonly Backend backend;
        private CancellationTokenSource _cancelBackend = new CancellationTokenSource();

        public MainWindow()
        {

            InitializeComponent();

            //Start the backend
            backend = new Backend(_cancelBackend.Token);
            _ = backend.RunAsync();
            _ = PumpBackendEventsToUIAsync(_cancelBackend.Token);

        }

        private async Task PumpBackendEventsToUIAsync(CancellationToken ct)
        {
            var reader = backend.Events;
            while (await reader.WaitToReadAsync(ct))
            {
                while (reader.TryRead(out var ev))
                {
                    await Dispatcher.InvokeAsync(() =>
                    {

                        switch (ev)
                        {
                            //Adjust things based on serial connection
                            case ConnectionChanged(var isConnected):
                                connect_button.Content = isConnected ? "Disconnect" : "Connect";
                                //Set the state of all of the buttons used 
                                SetComPortEnabled(isConnected);
                                SetValveButtonsEnable(isConnected);
                                SetPropValveEnabled(isConnected);
                                SetPinchValveEnabled(isConnected);
                                SetSheathPumpEnabled(isConnected);
                                SetWastePumpEnabled(isConnected);
                                break;

                            case DeviceMessageIn(var msg):
                                Debug.WriteLine("This was the message in:");
                                Debug.WriteLine(msg);
                                break;

                            case BackendError(var where, var msg):
                                Debug.WriteLine($"There was a backend error here: {where}, with a message {msg}");
                                break;

                        }

                    });
                }
            }

        }

        private async void QueryFlow(object? sender, RoutedEventArgs e)
        {
            if (sender is not Button btn)
            {
                return;
            }

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

            await backend.SendHostAsync(hstMsg);
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

            HostMessage hstMsg = new HostMessage
            {
                FluidicsCommand = new FluidicsCommand
                {
                    Valves = new ValveControl
                    {
                        ValveNumber = Int32.Parse(btnNum),
                        ValveState = valveState
                    }
                }
            };

            await backend.SendHostAsync(hstMsg);
        }

        private void ComPortComboBoxOpen(object? sender, EventArgs e)
        {
            if (sender is ComboBox box)
            {
                box.ItemsSource = SerialPort.GetPortNames();
            }

        }

        private async void ConnectButtonClicked(object? sender, RoutedEventArgs e)
        {
            //If we are not connected, connect
            if (backend.IsConnected == false)
            {
                string port = (string)com_port_box.SelectedItem;
                //Make a new token when trying to comment
                bool ok = await backend.ConnectAsync(port);
            }
            else
            {
                //Else disconnect
                await backend.DisconnectAsync();
            }
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
            if(!(sender is Slider {Tag : string sldrType} sldr && !string.IsNullOrEmpty(sldrType.ToString())))
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
