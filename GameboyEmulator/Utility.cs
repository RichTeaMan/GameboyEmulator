using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameboyEmulator
{
    public static class Utility
    {
        public static ushort CombineUShort(byte a, byte b)
        {
            var bytes = new byte[] { b, a };
            var result = (ushort)BitConverter.ToUInt16(bytes, 0);
            return result;
        }

        public static short CombineShort(byte a, byte b)
        {
            var bytes = new byte[] { b, a };
            var result = (short)BitConverter.ToInt16(bytes, 0);
            return result;
        }

        public static byte[] GetBytes(this ushort value)
        {
            return BitConverter.GetBytes(value);
        }

        public static byte[] GetBytes(this short value)
        {
            return BitConverter.GetBytes(value);
        }

        public static Nibble LowNibble(this byte b)
        {
            var mask = 0x0F;
            var result = mask & b;
            return new Nibble(result);
        }

        public static Nibble HighNibble(this byte b)
        {
            var mask = 0xF0;
            var result = (mask & b) >> 4;
            return new Nibble(result);
        }

        public static byte SwapNibbles(this byte b)
        {
            var h = b.HighNibble();
            var l = b.LowNibble();
            var result = Nibble.GetByteFromNibbles(l, h);
            return result;
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
