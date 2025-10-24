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
        private CancellationTokenSource _cancelSerial = new CancellationTokenSource();

        public MainWindow()
        {

            InitializeComponent();

            //Start the backend
            backend = new Backend(_cancelBackend.Token, _cancelSerial.Token);
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
                                //If the serial connection has been closed, cancel the things that go along with that
                                if (!isConnected)
                                {
                                    _cancelSerial.Cancel();
                                    _cancelSerial.Dispose();
                                }
                                com_port_box.IsEnabled = !isConnected;
                                query_flow_button.IsEnabled = isConnected;
                                valve_one_button.IsEnabled = isConnected;
                                valve_two_button.IsEnabled = isConnected;
                                valve_three_button.IsEnabled = isConnected;
                                valve_four_button.IsEnabled = isConnected;
                                valve_five_button.IsEnabled = isConnected;
                                valve_six_button.IsEnabled = isConnected;
                                valve_seven_button.IsEnabled = isConnected;
                                valve_eight_button.IsEnabled = isConnected;
                                valve_nine_button.IsEnabled = isConnected;
                                valve_ten_button.IsEnabled = isConnected;                                
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
            if (!backend.IsConnected)
            {
                string port = (string)com_port_box.SelectedItem;
                //Make a new token when trying to comment
                _cancelSerial = new CancellationTokenSource();
                bool ok = await backend.ConnectAsync(port);
            }
            else
            {
                //Else disconnect
                await backend.DisconnectAsync();
            }
        }

    }
}
