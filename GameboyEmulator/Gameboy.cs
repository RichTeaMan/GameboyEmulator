using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameboyEmulator
{
    public class Gameboy
    {
        public delegate void ProcessEventHandler(Gameboy sender, EventArgs e);
        public event ProcessEventHandler ProcessEvent;

        public delegate void DrawEventHandler(Gameboy sender, Pixel[][] pixels, EventArgs e);
        public event DrawEventHandler DrawEvent;

        public Cpu Cpu { get; private set; }
        public Mmu Mmu { get; private set; }
        public Gpu Gpu { get; private set; }

        public Gameboy()
        {
            Cpu = new Cpu();
            Mmu = new Mmu();
            Cpu.Mmu = Mmu;
            Gpu = new Gpu(Cpu);
            Mmu.Gpu = Gpu;

            Gpu.DrawEvent += Gpu_DrawEvent;
        }

        private void Gpu_DrawEvent(Gpu sender, Pixel[] pixels, EventArgs e)
        {
            var screen = new Pixel[160][];
            screen[0] = new Pixel[144];
            int w = 0;
            int h = 0;
            foreach(var p in pixels)
            {
                screen[w][h] = p;
                w++;
                if(w == 160)
                {
                    w = 0;
                    h++;
                    screen[h] = new Pixel[144];
                }
            }
            if(DrawEvent != null)
            {
                DrawEvent.Invoke(this, screen, new EventArgs());
            }
        }
        
        public void Begin()
        {
            while(true)
            {
                int cycles = Process();

                // much too slow
                Task.Delay(cycles * 10);
            }
            
        }

        public int Process()
        {
            if (ProcessEvent != null)
            {
                ProcessEvent.Invoke(this, new EventArgs());
            }

            int cycles = Cpu.Process();
            Gpu.Step();
            return cycles;
        }

    }
}
