using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
//Mine
using HelloWPF.Helpers;

namespace HelloWPF;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        int rows = 3;
        int columns = 3;

        var grid = new Grid { Margin = new Thickness(12) };
        GridHelper.AddRows(grid, "*", "*", "*");
        GridHelper.AddCols(grid, "*", "*", "*");

        for (int i = 0; i < columns; i++)
        {
            Brush bg = System.Windows.Media.Brushes.LightGreen;

            if (i == 0)
            {
                bg = System.Windows.Media.Brushes.LightGray;
            }
            else if (i == 1)
            {
                bg = System.Windows.Media.Brushes.LightBlue;
            }

            for (int j = 0; j < rows; j++)
                {
                var cell = new Border { MinHeight = 50, MinWidth = 50, BorderBrush = Brushes.Black, BorderThickness = new Thickness(1), Background = bg };
                var btn= new Button { MinWidth = 20, MinHeight = 20, Content = "X", Padding = new Thickness(8), HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center, ToolTip = "This is an X"};
                cell.Child = btn;
                //Makes every other one in the whole stack x and o 
                if ((i+j & 1) == 1)
                {
                    btn.Content = "O";
                    btn.ToolTip = "This is an O";
                }
                grid.Children.Add(cell);
                    Grid.SetRow(cell, j);
                    Grid.SetColumn(cell, i);
                }
        }

        this.Background = System.Windows.Media.Brushes.Black;
        this.Content = grid; //Replace the main window content

    }
}