using System.Windows;
using System.Windows.Controls;

namespace HelloWPF.Helpers;

public static class GridHelper
{
    /*
     * Need:
     * AddRows
     * Add Cols
     * Place
    */

    static readonly GridLengthConverter _gridLengthConverter = new GridLengthConverter();

    public static void AddRows(Grid g, params string[] sizes)
    {
        //Guards
        if (g is null){
            throw new ArgumentNullException(nameof(g));
        }
        if (sizes is null){
            throw new ArgumentNullException(nameof(sizes));
        }
        if (sizes.Length == 0){
            throw new ArgumentException("At least one size must be provided", nameof(sizes));
        }

        foreach (var s in sizes)
        {
            var obj = _gridLengthConverter.ConvertFromString(s);
            // This checks the runtime type of 'obj' AND, if it's a GridLength,
            // assigns that unboxed value into the new variable 'length'.
            if (obj is not GridLength length)
            {
                throw new FormatException($"Invalid GridLength '{s}'. Use Auto, *, 2*, or a number.");
            }

            g.RowDefinitions.Add(new RowDefinition { Height = length });
        }
        return;
    }

    public static void AddCols(Grid g, params string[] sizes)
    {
        //Guards
        if (g is null)
        {
            throw new ArgumentNullException(nameof(g));
        }
        if (sizes is null)
        {
            throw new ArgumentNullException(nameof(sizes));
        }
        if (sizes.Length == 0)
        {
            throw new ArgumentException("At least one size must be provided", nameof(sizes));
        }

        foreach(var s in sizes)
        {
            var obj = _gridLengthConverter.ConvertFromString(s);
            if(obj is not GridLength length)
            {
                throw new FormatException($"Invalid Gridlength '{s}'. Use Auto, *, 2*, or a number.");
            }
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = length });
        }
        return;
    }

    
}