using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
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
        private List<CpuExecution> instructions { get; set; }
        private WriteableBitmap bitmap { get; set; }
        private Gameboy Gameboy { get; set; }
        private Thread GameThread { get; set; }
        private int DebugDisplay { get; set; }
        public MainWindow()
        {
            InitializeComponent();
            instructions = new List<CpuExecution>();
            bitmap = new WriteableBitmap(160, 144, 160 / 1.91, 140 / 1.71, PixelFormats.Rgb24, null);
            Gameboy = new Gameboy();
            Gameboy.DrawEvent += Gameboy_DrawEvent;
            Gameboy.Cpu.PostCpuInstructionEvent += Cpu_PostCpuInstructionEvent;
            GameThread = new Thread(new ThreadStart(gameboyThread));
            GameThread.Start();
        }


        private void Cpu_PostCpuInstructionEvent(Cpu sender, CpuExecution execution)
        {
            instructions.Add(execution);
            //if (DebugDisplay % 100 == 0)
            //{
            //    RefreshDebug();
            //}
            DebugDisplay++;
        }

        private void RefreshDebug()
        {
            var sb = new StringBuilder();
            AppendLine(sb, "RegA: {0:X2}\tRegB {1:X2}", Gameboy.Cpu.RegA, Gameboy.Cpu.RegB);
            AppendLine(sb, "RegC: {0:X2}\tRegD {1:X2}", Gameboy.Cpu.RegC, Gameboy.Cpu.RegD);
            AppendLine(sb, "RegE: {0:X2}\tRegF {1:X2}", Gameboy.Cpu.RegE, Gameboy.Cpu.RegF);
            AppendLine(sb, "RegH: {0:X2}\tRegL {1:X2}", Gameboy.Cpu.RegH, Gameboy.Cpu.RegL);
            AppendLine(sb, "PC: {0:X4}\tSP {1:X4}", Gameboy.Cpu.PC, Gameboy.Cpu.SP);

            var display = sb.ToString();

            var ins = instructions.Skip(instructions.Count() - 100).Select(i => i.ToString());
            var insDisplay = string.Join(Environment.NewLine, ins);

            Dispatcher.Invoke((Action)(() =>
            {
                registersTextBlock.Text = display;
            }));

            Dispatcher.Invoke((Action)(() =>
            {
                ins_TextBlock.Text = insDisplay;

            }));
        }

        private StringBuilder AppendLine(StringBuilder sb, string message, params object[] args)
        {
            var m = string.Format(message, args);
            sb.AppendLine(m);
            return sb;
        }

        private async void gameboyThread()
        {
            await Gameboy.Mmu.ReadRom(@"..\..\Tetris.gb");
            Gameboy.Begin();
        }

        public static BitmapImage ToBitmapImage(Bitmap bitmap)
        {
            using (var memory = new MemoryStream())
            {
                bitmap.Save(memory, ImageFormat.Png);
                memory.Position = 0;

                var bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.StreamSource = memory;
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.EndInit();

                return bitmapImage;
            }
        }

        int frame = 0;

        private void Gameboy_DrawEvent(Gameboy sender, byte[] screenData, EventArgs e)
        {
            frame++;

            GameArea.Dispatcher.BeginInvoke((Action)(() =>
            {
                if (frame % 50 == 0)
                {
                    int width = 160; // read from file
                    int height = 144; // read from file

                    BitmapImage img;
                    using (var bitmap = new Bitmap(width, height))
                    using (var ms = new MemoryStream(screenData))
                    {
                        for (int y = 0; y < height; y++)
                        {
                            for (int x = 0; x < width; x++)
                            {
                                var r = ms.ReadByte();
                                var g = ms.ReadByte();
                                var b = ms.ReadByte();
                                var a = ms.ReadByte();
                                bitmap.SetPixel(x, y, System.Drawing.Color.FromArgb(a, r, g, b));
                            }
                        }
                        //bitmap.Save(string.Format("Frame-{0}.bmp", frame));

                        img = ToBitmapImage(bitmap);

                    }

                    //var _rect = new Int32Rect(0, 0, bitmap.PixelWidth, bitmap.PixelHeight);
                    //Update writeable bitmap with the colorArray to the image.
                    //   bitmap.WritePixels(_rect, screenData, 160 * 4, 0);
                    GameArea.Source = img;
                }
            }));

        }

        protected override void OnClosing(CancelEventArgs e)
        {
            //GameThread.Join();
        }

        private void pause_Btn_Click(object sender, RoutedEventArgs e)
        {
            Gameboy.Pause(!Gameboy.Paused);
            if (Gameboy.Paused)
            {
                pause_Btn.Content = "Unpause";
                RefreshDebug();
                var memView = new MemoryView();
                memView.Refresh(Gameboy);
                memView.Show();
            }
            else
            {
                pause_Btn.Content = "Pause";
            }
        }
    }
}
