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
            var rows = new List<string>();
            rows.Add(string.Join("\t", new[] { "Address" }.Concat( Enumerable.Range(0, 16).Select(v => v.ToString("X1")))));

            List<string> row = null;
            foreach (var i in Enumerable.Range(0, ushort.MaxValue))
            {
                if(i % 16 == 0)
                {
                    if(row != null)
                    {
                        rows.Add(string.Join("\t", row));
                    }
                    row = new List<string>();
                    row.Add(i.ToString("X4"));
                }
                string value;
                try
                {
                    value = gameboy.Mmu.ReadByte(i).ToString("X2");
                }
                catch
                {
                    value = "EX";
                }
                row.Add(value);
            }
            memGrid.Text = string.Join(Environment.NewLine, rows);
        }
    }
}
