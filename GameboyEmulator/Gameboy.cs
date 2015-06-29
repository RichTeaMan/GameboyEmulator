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

        public delegate void DrawEventHandler(Gameboy sender, Pixel[] pixels, EventArgs e);
        public event DrawEventHandler DrawEvent;

        public Cpu Cpu { get; private set; }
        public Mmu Mmu { get; private set; }
        public Gpu Gpu { get; private set; }

        public Gameboy()
        {
            Cpu = new Cpu();
            Mmu = new Mmu();
            Mmu.Cpu = Cpu;
            Cpu.Mmu = Mmu;
            Gpu = new Gpu(Cpu);
            Mmu.Gpu = Gpu;

            Gpu.DrawEvent += Gpu_DrawEvent;
        }

        private void Gpu_DrawEvent(Gpu sender, Pixel[] pixels, EventArgs e)
        {
            if(DrawEvent != null)
            {
                DrawEvent.Invoke(this, pixels, new EventArgs());
            }
        }
        
        public void Begin()
        {
            while(true)
            {
                int cycles = Process();

                // much too slow
                Task.Delay(cycles * 1);
            }
            
        }

        public int Process()
        {
            if (ProcessEvent != null)
            {
                ProcessEvent.Invoke(this, new EventArgs());
            }

            int cycles = Cpu.Process();

            if (Cpu.Ime == 1 && Mmu.InterruptEnabled > 0 && Mmu.InterruptFlag > 0)
            {
                // Mask off ints that aren't enabled
                var ifired = Mmu.InterruptEnabled & Mmu.InterruptFlag;

                if ((ifired & 0x01) == 1)
                {
                    Mmu.InterruptFlag &= (255 - 0x01);
                    Cpu.RST40();
                }
            }

            Gpu.Step();
            return cycles;
        }

    }
}
