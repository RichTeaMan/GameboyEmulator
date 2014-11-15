using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameboyEmulator
{
    /// <summary>
    /// Represents an unsigned nibble, ie 4 bits.
    /// </summary>
    public struct Nibble
    {
        private byte Value;

        public const int MaxValue = 16;
        public const int MinValue = 0;

        public Nibble(int a = 0)
        {
            if (a < MinValue || a >= MaxValue)
                throw new ArgumentOutOfRangeException(string.Format("{0} is not an allowed value for a Nibble. A value must be between {1} and {2}.", a, MinValue, MaxValue));
            Value = (byte)a;
        }

        #region Methods

        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }

        public bool Equals(Nibble nibble)
        {
            return nibble.Value == Value;
        }

        public bool Equals(int integer)
        {
            return integer == Value;
        }

        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }

        public override string ToString()
        {
            return Value.ToString();
        }

        public string ToString(string format)
        {
            return Value.ToString(format);
        }

        public string ToString(IFormatProvider provider)
        {
            return Value.ToString(provider);
        }

        public string ToString(string format, IFormatProvider provider)
        {
            return Value.ToString(format, provider);
        }

        #endregion

        #region Static Methods

        public static byte GetByteFromNibbles(Nibble highNibble, Nibble lowNibble)
        {
            var h = (byte)(highNibble << 4);
            var l = (byte)lowNibble;
            return (byte)(h + l);
        }

        private static byte WrapValue(int value)
        {
            while (value >= MaxValue)
                value -= MaxValue;
            while (value < MinValue)
                value += MaxValue;

            return (byte)value;
        }

        #endregion

        #region Operator Overloads

        public static Nibble operator +(Nibble a, Nibble b)
        {
            var nibble = a + b.Value;
            return nibble;
        }

        public static Nibble operator +(Nibble a, int b)
        {
            var value = WrapValue(a.Value + b);
            var nibble = new Nibble(value);
            return nibble;
        }

        public static Nibble operator ++(Nibble a)
        {
            var nibble = a + 1;
            return nibble;
        }

        public static Nibble operator -(Nibble a, Nibble b)
        {
            var nibble = a - b.Value;
            return nibble;
        }

        public static Nibble operator -(Nibble a, int b)
        {
            var value = WrapValue(a.Value - b);
            var nibble = new Nibble(value);
            return nibble;
        }

        public static Nibble operator --(Nibble a)
        {
            var nibble = a - 1;
            return nibble;
        }

        public static bool operator >(Nibble a, int b)
        {
            return a.Value > b;
        }

        public static bool operator <(Nibble a, int b)
        {
            return a.Value < b;
        }

        public static bool operator >(int a, Nibble b)
        {
            return a > b.Value;
        }

        public static bool operator <(int a, Nibble b)
        {
            return a < b.Value;
        }

        public static bool operator ==(Nibble a, Nibble b)
        {
            return a.Equals(b);
        }

        public static bool operator !=(Nibble a, Nibble b)
        {
            return !a.Equals(b);
        }

        public static bool operator ==(Nibble a, int b)
        {
            return a.Equals(b);
        }

        public static bool operator !=(Nibble a, int b)
        {
            return !a.Equals(b);
        }

        public static bool operator ==(int a, Nibble b)
        {
            return b.Equals(a);
        }

        public static bool operator !=(int a, Nibble b)
        {
            return !b.Equals(a);
        }

        public static implicit operator byte (Nibble a)
        {
            return a.Value;
        }

        public static implicit operator sbyte (Nibble a)
        {
            return (sbyte)a.Value;
        }

        public static implicit operator short (Nibble a)
        {
            return a.Value;
        }

        public static implicit operator ushort (Nibble a)
        {
            return a.Value;
        }

        public static implicit operator int (Nibble a)
        {
            return a.Value;
        }

        public static implicit operator uint (Nibble a)
        {
            return a.Value;
        }

        #endregion

    }
}
