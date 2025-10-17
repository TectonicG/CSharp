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

namespace Serial_Com
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
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

            //Make a label and a dropdown window
            var com_port_label = new Label { Content = "Available Ports: ", Margin = new Thickness(10) };
            var com_port_dropdown = new ComboBox { Margin = new Thickness(10), MinWidth = 100, MinHeight = 20, IsEditable = false};
            com_port_dropdown.DropDownOpened += ComPortComboBoxOpen;
            //Add one for the supported baud rates
            var baud_rate_label = new Label { Content = "Selected baud: ", Margin = new Thickness(10) };
            var baud_rate_dropdown = new ComboBox { Margin = new Thickness(10), MinWidth = 100, MinHeight = 20, IsEditable = false };
            baud_rate_dropdown.ItemsSource = new[] { "9600", "19200", "38400", "57600", "115200"};
            baud_rate_dropdown.SelectedIndex = 4;

            var connect_button = new Button { Margin = new Thickness(10), Content = "Connect"};
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

        private void ConnectButtonClicked(object? sender, EventArgs e)
        {
            if (sender is Button btn)
            {
                if ((string)btn.Content == "Connect")
                {
                    btn.Content = "Disconnect";
                }
                else
                {
                    btn.Content = "Connect";
                }

            }
        }


    }
}