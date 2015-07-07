using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameboyEmulator
{
    public class Gameboy
    {
        public delegate void ProcessEventHandler(Gameboy sender, EventArgs e);
        public event ProcessEventHandler ProcessEvent;

        public delegate void DrawEventHandler(Gameboy sender, byte[] screenData, EventArgs e);
        public event DrawEventHandler DrawEvent;

        public Cpu Cpu { get; private set; }
        public Mmu Mmu { get; private set; }
        public Gpu Gpu { get; private set; }

        public bool Paused { get; private set; }
        private bool isPaused { get; set; }

        private DateTime LastProcessTime { get; set; }

        /// <summary>
        /// In kHz.
        /// </summary>
        public int SimulatedClockSpeed { get; set; } = 4000 * 4;

        public Gameboy()
        {
            Cpu = new Cpu();
            Mmu = new Mmu();
            Mmu.Cpu = Cpu;
            Cpu.Mmu = Mmu;
            Gpu = new Gpu(Cpu);
            Mmu.Gpu = Gpu;

            Gpu.DrawEvent += Gpu_DrawEvent;

            LastProcessTime = DateTime.Now;
        }

        private void Gpu_DrawEvent(Gpu sender, byte[] screenData, EventArgs e)
        {
            if(DrawEvent != null)
            {
                DrawEvent.Invoke(this, screenData, new EventArgs());
            }
        }

        public void Pause(bool pause)
        {
            Paused = pause;
            while(isPaused != Paused)
            {
                Task.Delay(10).Wait();
            }
        }


        public void Begin()
        {
            while (true)
            {
                isPaused = Paused;
                if (Paused)
                {
                    Task.Delay(1000).Wait();
                }
                else
                {
                    LastProcessTime = DateTime.Now;
                    Task.Delay(10).Wait();

                    var now = DateTime.Now;
                    var duration = now - LastProcessTime;

                    // get how many cycles should have happened in this time frame
                    int cycles = (int)(duration.TotalMilliseconds * SimulatedClockSpeed);
                    while (cycles > 0)
                    {
                        cycles -= Process();
                    }
                }
            }
        }

        public int Process()
        {
            if (ProcessEvent != null)
            {
                ProcessEvent.Invoke(this, new EventArgs());
            }

            int cycles = Cpu.Process().CpuInstruction.Cycles;

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

            Gpu.Step(cycles);
            return cycles;
        }

    }
}
