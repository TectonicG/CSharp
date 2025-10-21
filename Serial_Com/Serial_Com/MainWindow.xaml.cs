using System;
using System.Diagnostics;
using System.IO.Ports;
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
using Google.Protobuf;
using System.Runtime.InteropServices;
using Serial_Com.Services.Serial;
using Serial_Com.Services.Cobs;


namespace Serial_Com
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        //Make an instance of the Serial Service class
        SerialReaderWriter fluidicsSerial = new SerialReaderWriter();
        //Make a label and a dropdown window
        Label com_port_label = new Label { Content = "Available Ports: ", Margin = new Thickness(10), VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center };
        ComboBox com_port_dropdown = new ComboBox { Margin = new Thickness(10), MinWidth = 100, MinHeight = 20, IsEditable = false, VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center };
        Button connect_button = new Button { Margin = new Thickness(10), Content = "Connect" };
        const int NUM_VALVES = 10;
        Button[] valve_buttons = new Button[NUM_VALVES];
        Button queryFlowButton = new Button { Margin = new Thickness(10), Content = "Query Flow", Height = 20, Width = 150 , IsEnabled = false}; 
        CancellationToken cancelToken = new CancellationToken();
        Grid grid = new Grid { Margin = new Thickness(12) };


        public MainWindow()
        {

            InitializeComponent();

            int rows = 8;
            int cols = 2;

            SetupUI(rows, cols);

            //Connect Signals
            connect_button.Click += ConnectButtonClicked;
            fluidicsSerial.ConnectionChanged += ConnectionChange;
            //serialHelper.MessageReceived += IncomingData;
            com_port_dropdown.DropDownOpened += ComPortComboBoxOpen;
            queryFlowButton.Click += QueryFlow;

            //Set Content 
            Content = grid;
        }

        private async void QueryFlow(object? sender, RoutedEventArgs e)
        {
            if (sender is not Button btn){
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

            await fluidicsSerial.WriteCommand(hstMsg);

        }


        private async void ToggleValve(object? sender, RoutedEventArgs e)
        {
            //Valves are buttons
            if (sender is not Button btn)
            {
                return;
            }

            HostMessage hstMsg = new HostMessage();
            FluidicsCommand cmd = new FluidicsCommand();
            ValveControl valveControl = new ValveControl();

            valveControl.ValveNumber = (int)btn.Tag;

            if ((string)btn.Content == $"Enable Valve {btn.Tag}")
            {
                btn.Content = $"Disable Valve {btn.Tag}";
                btn.Background = Brushes.Green;
                valveControl.ValveState = EnableDisableDef.Enable;
            }
            else
            {
                btn.Content = $"Enable Valve {btn.Tag}";
                btn.Background = Brushes.Red;
                valveControl.ValveState = EnableDisableDef.Disable;
            }
            cmd.Valves = valveControl;
            hstMsg.FluidicsCommand = cmd;

            byte[] message = hstMsg.ToByteArray();

            await fluidicsSerial.WriteCommand(hstMsg);
        }
        //private async Task WriteSerialData(byte[] data)
        //{

        //    await serialHelper.WriteAsync(data.AsMemory(0, data.Length));
        //}
        private void IncomingData(object? sender, ReadOnlyMemory<byte> data)
        {

            Debug.WriteLine($"{BitConverter.ToString(data.ToArray())}\n");
            var decodedData = Cobs.CobsDecode(data);
            var parsedData = DeviceMessage.Parser.ParseFrom(decodedData);
            Debug.WriteLine(JsonFormatter.Default.Format(parsedData));
        }

        private void ComPortComboBoxOpen(object? sender, EventArgs e)
        {
            if (sender is ComboBox box)
            {
                box.ItemsSource = SerialPort.GetPortNames();
            }

        }

        private void ConnectionChange(object? sender, bool connectionState)
        {
            Dispatcher.Invoke(() =>
            {
                connect_button.Content = connectionState ? "Disconnect" : "Connect";
                com_port_dropdown.IsEnabled = !connectionState;
                queryFlowButton.IsEnabled = connectionState;
                foreach (Button button in valve_buttons)
                {
                    button.IsEnabled = connectionState;
                }
            });

        }

        private async void ConnectButtonClicked(object? sender, RoutedEventArgs e)
        {
            if (sender is Button btn)
            {

                string port = (string)com_port_dropdown.SelectedItem;

                //Check to make sure the port is not null
                if (port is null)
                {
                    return;
                }

                if (fluidicsSerial.IsConnected)
                {

                    try
                    {
                        await fluidicsSerial.DisconnectFromPort();
                    }
                    catch
                    {
                        MessageBox.Show($"Disconnection Error : {port}");
                    }
                }
                else
                {
                    try
                    {
                        await fluidicsSerial.ConnectToPort(port, cancelToken);
                    }
                    catch
                    {
                        MessageBox.Show($"Could Not Connect to : {port}");
                        return;
                    }

                }

            }
        }

        private void SetupUI(int rows, int cols)
        {

            //Make as many rows as requested (in auto)
            for (int i = 0; i < rows; i++)
            {
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            }
            //Make as many cols as requested (in auto)
            for (int i = 0; i < cols; i++)
            {
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            }

            CreateValveButtons();

            AddUIElementToGrid(grid, com_port_label, 0, 0);
            AddUIElementToGrid(grid, com_port_dropdown, 0, 1);

            AddUIElementToGrid(grid, valve_buttons[0], 1, 0);
            AddUIElementToGrid(grid, valve_buttons[1], 2, 0);
            AddUIElementToGrid(grid, valve_buttons[2], 3, 0);
            AddUIElementToGrid(grid, valve_buttons[3], 4, 0);
            AddUIElementToGrid(grid, valve_buttons[4], 5, 0);
            AddUIElementToGrid(grid, valve_buttons[5], 1, 1);
            AddUIElementToGrid(grid, valve_buttons[6], 2, 1);
            AddUIElementToGrid(grid, valve_buttons[7], 3, 1);
            AddUIElementToGrid(grid, valve_buttons[8], 4, 1);
            AddUIElementToGrid(grid, valve_buttons[9], 5, 1);

            AddUIElementToGrid(grid, queryFlowButton, 6, 0);

            AddUIElementToGrid(grid, connect_button, 7, 0);

        }

        private void AddUIElementToGrid(Grid? grid, UIElement? element, int row, int col)
        {
            grid?.Children.Add(element);

            if (grid is not null)
            {
                PlaceRowCol(element, row, col);
            }

        }

        private void PlaceRowCol(UIElement? element, int row, int col)
        {
            if (element is not null)
            {
                Grid.SetRow(element, row);
                Grid.SetColumn(element, col);
            }

        }

        private void CreateValveButtons()
        {
            for (int i = 0; i < 10; i++)
            {
                int ID = i + 1;
                valve_buttons[i] = new Button { Margin = new Thickness(10), Content = $"Enable Valve {ID}", IsEnabled = false, Tag = ID, Height = 20, Width = 150, Background = Brushes.Red };
                valve_buttons[i].Click += ToggleValve;

            }
        }


    }
}
