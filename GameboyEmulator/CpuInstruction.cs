using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace GameboyEmulator
{
    public class CpuInstruction
    {
        public int OpCode { get; private set; }
        public int Cycles { get; private set; }
        public MethodInfo MethodInfo { get; private set; }
        public Cpu Cpu { get; private set; }
        public string AssemblyInstruction { get; private set; }
        /// <summary>
        /// Gets the program counter of the cpu when this instruction was created.
        /// </summary>
        public int PC { get; private set; }

        public CpuInstruction() { }

        public CpuExecution Execute()
        {
            var execution = new CpuExecution(this, Cpu.PC);
            MethodInfo.Invoke(Cpu, null);
            Cpu.Timer += Cycles;
            return execution;
        }

        public static CpuInstruction GetInstruction(Cpu cpu, MethodInfo methodInfo)
        {
            var opAttr = methodInfo.GetCustomAttributes()
                .FirstOrDefault(a => a.GetType() == typeof(OpAttribute) || a.GetType().GetTypeInfo().IsSubclassOf(typeof(OpAttribute))) as OpAttribute;

            var cpuIns = new CpuInstruction()
            {
                MethodInfo = methodInfo,
                Cycles = opAttr.Cycles,
                OpCode = opAttr.OpCode,
                Cpu = cpu,
                AssemblyInstruction = opAttr.AssemblyInstruction,
                PC = cpu.PC,
            };

            return cpuIns;
        }

        private string createAssemblyText()
        {
            string assTxt = AssemblyInstruction;
            if(assTxt.Contains("nn"))
            {
                var word = string.Format("$0x{0:X4}", Cpu.Mmu.ReadWord(PC + 1));
                assTxt = assTxt.Replace("nn", word);
            }
            if(assTxt.Contains("n"))
            {
                var byt = string.Format("$0x{0:X2}", Cpu.Mmu.ReadByte(PC + 1));
                assTxt = assTxt.Replace("n", byt);
            }

            return assTxt;
        }
    }
}
