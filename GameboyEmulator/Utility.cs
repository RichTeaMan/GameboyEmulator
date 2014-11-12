using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameboyEmulator
{
    public static class Utility
    {
        public static byte LowNibble(this byte b)
        {
            var mask = 0x0F;
            var result = mask & b;
            return (byte)result;
        }

        public static byte HighNibble(this byte b)
        {
            var mask = 0xF0;
            var result = mask & b;
            return (byte)result;
        }

        public static bool IsBitSet(this byte b, int pos)
        {
            return (b & (1 << pos)) != 0;
        }

        public static byte BitSet(this byte b, int pos)
        {
            byte mask = 0x01;
            mask = (byte)(mask << pos);
            var result = (byte)(b | mask);
            if (!result.IsBitSet(pos))
                throw new IncorrectResultException("BitSet does not match IsBitSet.");
            return result;
        }

        public static byte BitUnset(this byte b, int pos)
        {
            byte mask = 0x01;
            mask = (byte)(mask << pos);
            mask = (byte)~mask;
            var result = (byte)(b & mask);
            if (result.IsBitSet(pos))
                throw new IncorrectResultException("BitUnset does not match IsBitSet.");
            return result;
        }

        public static byte BitToggle(this byte b, int pos)
        {
            byte result;
            if (IsBitSet(b, pos))
                result = BitUnset(b, pos);
            else
                result = BitSet(b, pos);
            return result;
        }

        public static byte BitSet(this byte b, int pos, bool value)
        {
            byte result;
            if (value)
                result = BitSet(b, pos);
            else
                result = BitUnset(b, pos);
            return result;
        }

        public static ushort Add(this byte b, ushort value)
        {
            var result = b + value;
            return (ushort)result;
        }
    }
}
