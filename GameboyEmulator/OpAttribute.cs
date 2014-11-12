using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameboyEmulator
{
    internal class OpAttribute : Attribute
    {
        public byte OpCode { get; private set; }
        public int Cycles { get; private set; }
        public string AssemblyInstruction { get; private set; }

        public OpAttribute(byte opCode, int cycles, string assemblyInstruction) : base()
        {
            OpCode = OpCode;
            Cycles = cycles;
            AssemblyInstruction = assemblyInstruction;
        }
    }
}
