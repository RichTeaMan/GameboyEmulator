using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameboyEmulator
{
    public class Mmu
    {
        /// <summary>
        /// Read 8-bit byte from a given address.
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        public byte ReadByte(ushort address)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Read 16-bit word from a given address.
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        public ushort ReadWord(ushort address)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Write 8-bit byte to a given address.
        /// </summary>
        /// <param name="address"></param>
        public void WriteByte(ushort address, byte value)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Write 16-bit word to a given address.
        /// </summary>
        /// <param name="address"></param>
        public void WriteWord(ushort address, ushort value)
        {
            throw new NotImplementedException();
        }

    }
}
