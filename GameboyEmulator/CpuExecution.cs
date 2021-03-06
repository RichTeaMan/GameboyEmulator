﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameboyEmulator
{
    public class CpuExecution
    {
        public CpuInstruction CpuInstruction { get; private set; }

        public ushort PC { get; private set; }

        public string AssemblyInstruction { get; private set; }

        public CpuExecution(CpuInstruction instruction, ushort pc)
        {
            PC = pc;
            CpuInstruction = instruction;
            AssemblyInstruction = createAssemblyText();
        }

        public override string ToString()
        {
            return AssemblyInstruction;
        }

        private string createAssemblyText()
        {
            string assTxt = CpuInstruction.AssemblyInstruction;
            if (assTxt.Contains("nn"))
            {
                var word = string.Format("$0x{0:X4}", CpuInstruction.Cpu.Mmu.ReadWord(PC + CpuInstruction.InstructionSize));
                assTxt = assTxt.Replace("nn", word);
            }
            if (assTxt.Contains("n"))
            {
                var byt = string.Format("$0x{0:X2}", CpuInstruction.Cpu.Mmu.ReadByte(PC + CpuInstruction.InstructionSize));
                assTxt = assTxt.Replace("n", byt);
            }

            assTxt = string.Format("{0:X4} - {1}", PC, assTxt);
            return assTxt;
        }
    }
}
