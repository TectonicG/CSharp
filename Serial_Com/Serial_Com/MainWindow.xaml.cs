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
using System.IO.Ports;
using Serial_Com.Services;
using System.Diagnostics;

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

            Debug.WriteLine("Started\n\r");

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
                Debug.WriteLine("In the dropdown\n\r");
                box.ItemsSource = SerialPort.GetPortNames();
            }

        }

        private async void ConnectButtonClicked(object? sender, RoutedEventArgs e)
        {
            Debug.WriteLine("Connect Button Clicked\n\r");
            if (sender is Button btn)
            {
                Debug.WriteLine("In the Connection button clicked\n\r");

                string? port = com_port_dropdown.SelectedItem as string;
                int baud = baud_rate_dropdown.SelectedItem is int b ? b :115200;

                if (serialHelper.IsConnected)
                {

                    Debug.WriteLine("Inside the else\n\r");
                    try
                    {
                        await serialHelper.DisconnectAsync();
                        btn.Content = "Connect";
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("Hit Exception on Disconnect\n\r");
                        MessageBox.Show($"Disconnection Error : {port}, with Exception {ex}");
                    }
                }
                else
                {
                    Debug.WriteLine("Inside the serialhelper.IsConnected\n\r");
                    try
                    {
                        await serialHelper.ConnectAsync(port, baud, 200, readCancelToken);
                        btn.Content = "Disconnect";
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("Hit Exception on Connect\n\r");
                        MessageBox.Show($"Could Not Connect to : {port}, with Exception {ex}");
                        return;
                    }

                }

            }
        }


    }
}