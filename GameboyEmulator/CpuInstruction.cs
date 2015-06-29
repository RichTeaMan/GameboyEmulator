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

        public CpuInstruction() { }

        public int Execute()
        {
            MethodInfo.Invoke(Cpu, null);
            Cpu.Timer += Cycles;
            return Cycles;
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
                AssemblyInstruction = opAttr.AssemblyInstruction
            };

            return cpuIns;
        }
    }
}
