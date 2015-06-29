using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace GameboyEmulator.Wpf
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private WriteableBitmap bitmap { get; set; }
        Gameboy Gameboy { get; set; }
        Thread GameThread { get; set; }
        public MainWindow()
        {
            InitializeComponent();
            bitmap = new WriteableBitmap(160, 144, 160 / 1.91, 140 / 1.71, PixelFormats.Rgb24, null);
            Gameboy = new Gameboy();
            Gameboy.DrawEvent += Gameboy_DrawEvent;
            GameThread = new Thread(new ThreadStart(gameboyThread));
            GameThread.Start();
        }

        private async void gameboyThread()
        {
            await Gameboy.Mmu.ReadRom(@"C:\Users\Thomas\Source\Repos\GameboyEmulator\GameboyEmulator.Wpf\Tetris.gb");
            Gameboy.Begin();
        }

        private void Gameboy_DrawEvent(Gameboy sender, Pixel[] pixels, EventArgs e)
        {
            var bytes = new List<byte>();
            foreach (var p in pixels)
            {
                bytes.Add(p.R);
                bytes.Add(p.G);
                bytes.Add(p.B);
            }

            GameArea.Dispatcher.BeginInvoke((Action)(() =>
            {
                var _rect = new Int32Rect(0, 0, bitmap.PixelWidth, bitmap.PixelHeight);
                //Update writeable bitmap with the colorArray to the image.
                bitmap.WritePixels(_rect, bytes.ToArray(), 160 * 3, 0);
                GameArea.Source = bitmap;
            }));

        }

        protected override void OnClosing(CancelEventArgs e)
        {
            //GameThread.Join();
        }
    }
}
