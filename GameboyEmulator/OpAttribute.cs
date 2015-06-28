using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameboyEmulator
{
    internal class OpAttribute : Attribute
    {
        public int OpCode { get; private set; }
        public int Cycles { get; private set; }
        public string AssemblyInstruction { get; private set; }

        public OpAttribute(int opCode, int cycles, string assemblyInstruction) : base()
        {
            OpCode = opCode;
            Cycles = cycles;
            AssemblyInstruction = assemblyInstruction;
        }
    }
}
