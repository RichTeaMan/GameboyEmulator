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

            var insDisplay = string.Join(Environment.NewLine, instructions.Select(i => i.ToString()));

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

        private void pause_Btn_Click(object sender, RoutedEventArgs e)
        {
            Gameboy.Paused = !Gameboy.Paused;
            if (Gameboy.Paused)
            {
                pause_Btn.Content = "Unpause";
                RefreshDebug();
            }
            else
            {
                pause_Btn.Content = "Pause";
            }
        }
    }
}
