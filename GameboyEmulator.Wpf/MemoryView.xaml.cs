using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace GameboyEmulator.Wpf
{
    /// <summary>
    /// Interaction logic for MemoryView.xaml
    /// </summary>
    public partial class MemoryView : Window
    {
        public MemoryView()
        {
            InitializeComponent();
        }

        public void Refresh(Gameboy gameboy)
        {
            var rowColumn = new DataGridTextColumn();
            rowColumn.Header = "Address";
            memGrid.Columns.Add(rowColumn);
            foreach (var i in Enumerable.Range(0, 16))
            {
                var column = new DataGridTextColumn();
                column.Header = i.ToString("X1");
                memGrid.Columns.Add(column);
            }

            List<byte> row = null;
            foreach (var i in Enumerable.Range(0, ushort.MaxValue))
            {
                if(i % 16 == 0)
                {
                    if(row != null)
                    {
                        var rowData = new List<string>();
                        rowData.Add(i.ToString("X4"));
                        rowData.AddRange(row.Select(v => v.ToString("X2")));

                        memGrid.Items.Add(rowData);
                    }
                    row = new List<byte>();
                }
                var value = gameboy.Mmu.ReadByte(i);
                row.Add(value);

            }

        }
    }
}
