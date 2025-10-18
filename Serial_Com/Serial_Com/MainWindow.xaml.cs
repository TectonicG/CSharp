using Serial_Com.Services;
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

namespace Serial_Com
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        //Make an instance of the Serial Service class
        IConnectionService serialHelper = new SerialService();

        //Make a label and a dropdown window
        Label com_port_label = new Label { Content = "Available Ports: ", Margin = new Thickness(10), VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center };
        ComboBox com_port_dropdown = new ComboBox { Margin = new Thickness(10), MinWidth = 100, MinHeight = 20, IsEditable = false, VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center };
        //Add one for the supported baud rates
        Label baud_rate_label = new Label { Content = "Selected baud: ", Margin = new Thickness(10), VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center };
        ComboBox baud_rate_dropdown = new ComboBox { Margin = new Thickness(10), MinWidth = 100, MinHeight = 20, IsEditable = false, VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center };
        Button connect_button = new Button { Margin = new Thickness(10), Content = "Connect" };
        CancellationToken readCancelToken = new CancellationToken();

        public MainWindow()
        {

            InitializeComponent();

            int rows = 3;
            int cols = 3;

            var grid = new Grid { Margin = new Thickness(12) };
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

            serialHelper.ConnectionChanged += ConnectionChange;

                        com_port_dropdown.DropDownOpened += ComPortComboBoxOpen;
            baud_rate_dropdown.ItemsSource = new[] { 9600, 19200, 38400, 57600, 115200 };
            baud_rate_dropdown.SelectedIndex = 4;
            connect_button.Click += ConnectButtonClicked;

            grid.Children.Add(com_port_label);
            grid.Children.Add(com_port_dropdown);
            grid.Children.Add(baud_rate_label);
            grid.Children.Add(baud_rate_dropdown);
            grid.Children.Add(connect_button);

            Grid.SetRow(com_port_label, 0);
            Grid.SetColumn(com_port_dropdown, 0);
            Grid.SetRow(com_port_dropdown, 0);
            Grid.SetColumn(com_port_dropdown, 1);
            Grid.SetRow(baud_rate_label, 1);
            Grid.SetColumn(baud_rate_label, 0);
            Grid.SetRow(baud_rate_dropdown, 1);
            Grid.SetColumn(baud_rate_dropdown, 1);
            Grid.SetRow(connect_button, 2);
            Grid.SetColumn(connect_button, 0);


            Content = grid;

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

            if (connectionState == true)
            {
                Dispatcher.Invoke(() => connect_button.Content = "Disconnect");
            }
            else
            {
                Dispatcher.Invoke(() => connect_button.Content = "Connect");
            }

        }



        private async void ConnectButtonClicked(object? sender, RoutedEventArgs e)
        {
            if (sender is Button btn)
            {

                string? port = (string)com_port_dropdown.SelectedItem;
                int baud = (int)baud_rate_dropdown.SelectedItem;

                //Check to make sure the port is not null
                if (port is null)
                {
                    return;
                }

                if (serialHelper.IsConnected)
                {

                    try
                    {
                        await serialHelper.DisconnectAsync();
                    }
                    catch //(Exception ex)
                    {
                        MessageBox.Show($"Disconnection Error : {port}");
                    }
                }
                else
                {
                    try
                    {
                        await serialHelper.ConnectAsync(port, baud, 200, readCancelToken);
                    }
                    catch //(Exception ex)
                    {
                        MessageBox.Show($"Could Not Connect to : {port}");
                        return;
                    }

                }

            }
        }


    }
}