using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameboyEmulator
{
    internal class CbOpAttribute : OpAttribute
    {
        public CbOpAttribute(byte opCode, int cycles, string assemblyInstruction) :
            base(opCode, cycles, assemblyInstruction)
        { }
    }
}
