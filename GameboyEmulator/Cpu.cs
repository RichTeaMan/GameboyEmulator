using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

namespace GameboyEmulator
{
    public class Cpu
    {
        const ushort P1 = 0xFF00;

        public int Timer { get; set; }

        public byte RegA { get; protected set; }
        public byte RegB { get; protected set; }
        public byte RegC { get; protected set; }
        public byte RegD { get; protected set; }
        public byte RegE { get; protected set; }
        /// <summary>
        /// Gets the Flag Register.
        /// </summary>
        /// <returns></returns>
        public byte RegF { get; protected set; }
        public byte RegH { get; protected set; }
        public byte RegL { get; protected set; }

        public ushort AF
        {
            get
            {
                return (ushort)((RegA << 8) + RegF);
            }
            set
            {
                var v = (ushort)value;
                RegF = (byte)(v & 0x00FF);
                RegA = (byte)(v >> 8);

                if (AF != value)
                    throw new Exception("AF Setter failed.");
            }
        }

        public ushort BC
        {
            get
            {
                return (ushort)((RegB << 8) + RegC);
            }
            set
            {
                var v = (ushort)value;
                RegC = (byte)(v & 0x00FF);
                RegB = (byte)(v >> 8);

                if (BC != value)
                    throw new Exception("BC Setter failed.");
            }
        }

        public ushort DE
        {
            get
            {
                return (ushort)((RegD << 8) + RegE);
            }
            set
            {
                var v = (ushort)value;
                RegE = (byte)(v & 0x00FF);
                RegD = (byte)(v >> 8);

                if (DE != value)
                    throw new Exception("DE Setter failed.");
            }
        }

        public ushort HL
        {
            get
            {
                return (ushort)((RegH << 8) + RegL);
            }
            set
            {
                var v = (ushort)value;
                RegL = (byte)(v & 0x00FF);
                RegH = (byte)(v >> 8);

                if (HL != value)
                    throw new Exception("HL Setter failed.");
            }
        }

        public bool Stopped { get; protected set; }
        public bool Halted { get; protected set; }
        public bool DisableInterupt { get; protected set; }
        public bool EnableInterupt { get; protected set; }

        /// <summary>
        /// Gets the Program Counter.
        /// </summary>
        /// <returns></returns>
        public ushort PC { get; protected set; }
        /// <summary>
        /// Gets the Stack Pointer.
        /// </summary>
        /// <returns></returns>
        public ushort SP { get; protected set; }

        /// <summary>
        /// Gets or sets the Zero flag (0x80). True if the last operation produced a result of 0.
        /// </summary>
        /// <returns></returns>
        public bool ZeroFlag
        {
            get { return RegF.IsBitSet(7); }
            set { RegF.BitSet(7, value); }
        }

        /// <summary>
        /// Gets or sets the Operation flag (0x40). True if the last operation was a subtraction.
        /// </summary>
        /// <returns></returns>
        public bool OperationFlag
        {
            get { return RegF.IsBitSet(6); }
            set { RegF.BitSet(6, value); }
        }

        /// <summary>
        /// Gets or the sets the Half-carry flag (0x20). True if the lower half of the byte overflowed past 15 in the result of the last operation.
        /// </summary>
        /// <returns></returns>
        public bool HalfCarryFlag
        {
            get { return RegF.IsBitSet(5); }
            set { RegF.BitSet(5, value); }
        }

        /// <summary>
        /// Gets or sets the Carry flag (0x10). True if the last operation produced a result over 255 (for additions) or under 0 (for subtractions).
        /// </summary>
        /// <returns></returns>
        public bool CarryFlag
        {
            get { return RegF.IsBitSet(4); }
            set { RegF.BitSet(4, value); }
        }

        public Mmu Mmu { get; set; }

        private Dictionary<int, CpuInstruction> Map;

        public Cpu()
        {
            PC = 0;
            SP = 0xFFFE;

            BuildInstructionList();
        }

        #region Methods

        public int Process()
        {
            int opCode = Mmu.ReadByte(PC);
            
            if(opCode == 0xCB)
            {
                PC++;
                var secondaryOpCode = Mmu.ReadByte(PC);
                opCode = Utility.CombineUShort(0xCB, secondaryOpCode);
            }
            CpuInstruction ins;
            if(Map.TryGetValue(opCode, out ins))
            {
                int cycles = ins.Execute();
                PC++;
                return cycles;
            }
            else
            {
                throw new Exception("No instruction.");
            }
        }

        private void BuildInstructionList()
        {
            Map = new Dictionary<int, CpuInstruction>();

            var cpuMethods = GetType().GetRuntimeMethods().Where(m => m.CustomAttributes.Where(a => a.AttributeType == typeof(OpAttribute)).Count() > 0);
            foreach(var m in cpuMethods)
            {
                var ins = CpuInstruction.GetInstruction(this, m);
                Map.Add(ins.OpCode, ins);
            }

            var cbCpuMethods = GetType().GetRuntimeMethods().Where(m => m.CustomAttributes.Where(a => a.AttributeType == typeof(CbOpAttribute)).Count() > 0);
            foreach (var m in cbCpuMethods)
            {
                var ins = CpuInstruction.GetInstruction(this, m);
                Map.Add(ins.OpCode, ins);
            }
        }

        void PushByte(byte value)
        {
            SP--;
            Mmu.WriteByte(SP, value);
        }

        void PushWord(ushort value)
        {
            SP -= 2;
            Mmu.WriteWord(SP, value);
        }

        byte PopByte()
        {
            var value = Mmu.ReadByte(SP);
            SP++;
            return value;
        }

        ushort PopWord()
        {
            var value = Mmu.ReadWord(SP);
            SP += 2;
            return value;
        }

        void IncPc()
        {
            PC++;
        }

        byte ReadByte()
        {
            var b = PeekByte();
            IncPc();
            return b;
        }

        byte PeekByte()
        {
            var b = Mmu.ReadByte(PC);
            return b;
        }

        ushort ReadWord()
        {
            var b = PeekWord();
            IncPc();
            IncPc();
            return b;
        }

        ushort PeekWord()
        {
            var b = Mmu.ReadWord(PC);
            return b;
        }

        /// <summary>
        /// Adds the given bytes and sets any affected flags.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        byte AddBytes(byte a, byte b)
        {
            var value = (sbyte)a + (sbyte)b;
            CarryFlag = false;
            while (value > byte.MaxValue)
            {
                CarryFlag = true;
                value = (value - sbyte.MaxValue) + sbyte.MinValue;
            }

            // calculate half carry
            var lowNibbles = a.LowNibble() + b.LowNibble();
            HalfCarryFlag = lowNibbles > 15;

            OperationFlag = false;
            ZeroFlag = value == 0;

            return (byte)value;
        }

        byte IncByte(byte a)
        {
            var value = (sbyte)a + 1;
            while (value > byte.MaxValue)
            {
                value = (value - sbyte.MaxValue) + sbyte.MinValue;
            }

            // calculate half carry
            var lowNibbles = a.LowNibble() + 1;
            HalfCarryFlag = lowNibbles > 15;

            OperationFlag = false;
            ZeroFlag = value == 0;

            return (byte)value;
        }

        byte DecByte(byte a)
        {
            var value = (sbyte)a - 1;
            while (value < sbyte.MinValue)
            {
                value = sbyte.MaxValue - (value - sbyte.MinValue);
            }

            // calculate half carry
            // this is probably wrong
            var lowNibbles = a.LowNibble() - 1;
            HalfCarryFlag = lowNibbles > 0;

            OperationFlag = true;
            ZeroFlag = value == 0;

            return (byte)value;
        }

        byte DecWord(ushort a)
        {
            var value = a - 1;
            while (value < ushort.MinValue)
            {
                value = ushort.MaxValue - (value - ushort.MinValue);
            }

            // calculate half carry
            // this is probably wrong

            var highByte = (byte)(a >> 8);
            var lowNibbles = highByte.LowNibble() - 1;
            HalfCarryFlag = lowNibbles > 0;

            OperationFlag = true;
            ZeroFlag = value == 0;

            return (byte)value;
        }

        byte AndBytes(byte a, byte b)
        {
            var value = a & b;

            ZeroFlag = value == 0;
            OperationFlag = true;
            HalfCarryFlag = true;
            CarryFlag = false;

            return (byte)value;
        }

        byte OrBytes(byte a, byte b)
        {
            var value = a | b;

            ZeroFlag = value == 0;
            OperationFlag = false;
            HalfCarryFlag = false;
            CarryFlag = false;

            return (byte)value;
        }

        byte XorBytes(byte a, byte b)
        {
            var value = a ^ b;

            ZeroFlag = value == 0;
            OperationFlag = false;
            HalfCarryFlag = false;
            CarryFlag = false;

            return (byte)value;
        }

        byte SubBytes(byte a, byte b)
        {
            var value = (sbyte)a - (sbyte)b;
            CarryFlag = false;
            while (value < sbyte.MinValue)
            {
                CarryFlag = true;
                value = sbyte.MaxValue - (value - sbyte.MinValue);
            }

            // calculate half carry
            // this is probably wrong
            var lowNibbles = a.LowNibble() - b.LowNibble();
            HalfCarryFlag = lowNibbles > 0;

            OperationFlag = true;
            ZeroFlag = value == 0;

            return (byte)value;
        }

        void CpBytes(byte a, byte b)
        {
            SubBytes(a, b);
        }

        /// <summary>
        /// Adds the given bytes and sets any affected flags.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        ushort AddWords(ushort a, ushort b)
        {
            var value = a + b;
            CarryFlag = false;
            while (value > ushort.MaxValue)
            {
                CarryFlag = true;
                value = (value - ushort.MaxValue) + ushort.MinValue;
            }

            // calculate half carry
            // this is almost definitely wrong
            HalfCarryFlag = value > 2048;

            OperationFlag = false;
            ZeroFlag = value == 0;

            return (ushort)value;
        }

        ushort IncWord(ushort a)
        {
            var value = a + 1;
            while (value > ushort.MaxValue)
            {
                value = (value - ushort.MaxValue) + ushort.MinValue;
            }

            // calculate half carry
            // this is almost definitely wrong
            HalfCarryFlag = value > 2048;

            OperationFlag = false;
            ZeroFlag = value == 0;

            return (ushort)value;
        }

        /// <summary>
        /// Swaps the high and low nibbles of the byte.
        /// </summary>
        /// <param name="a"></param>
        /// <returns></returns>
        byte Swap(byte a)
        {
            var result = a.SwapNibbles();
            ZeroFlag = result == 0;
            OperationFlag = false;
            HalfCarryFlag = false;
            CarryFlag = false;
            return result;
        }

        private byte RotateLeft(byte value)
        {
            CarryFlag = value.IsBitSet(7);
            var result = (byte)(value << 1);
            ZeroFlag = value == 0;
            OperationFlag = false;
            HalfCarryFlag = false;
            return result;
        }

        private byte RotateLeftThrough(byte value)
        {
            var oldCarry = CarryFlag;
            CarryFlag = value.IsBitSet(7);
            var result = (byte)(value << 1);
            if (oldCarry)
                result = result.BitSet(7);
            ZeroFlag = result == 0;
            OperationFlag = false;
            HalfCarryFlag = false;
            return result;
        }

        private byte RotateRight(byte value)
        {
            CarryFlag = value.IsBitSet(0);
            var result = (byte)(value >> 1);
            ZeroFlag = result == 0;
            OperationFlag = false;
            HalfCarryFlag = false;
            return result;
        }

        private byte RotateRightThrough(byte value)
        {
            var oldCarry = CarryFlag;
            CarryFlag = value.IsBitSet(0);
            var result = (byte)(value >> 1);
            if (oldCarry)
                result = result.BitSet(0);
            ZeroFlag = result == 0;
            OperationFlag = false;
            HalfCarryFlag = false;
            return result;
        }

        private byte ShiftLeft(byte value)
        {
            CarryFlag = value.IsBitSet(7);
            if (CarryFlag)
            {
                // unset bit 7 so the byte doesn't wrap around when it is casted
                // back to a byte after the shift.
                value.BitSet(7, false);
            }
            var result = (byte)(value << 1);
            OperationFlag = false;
            HalfCarryFlag = false;
            ZeroFlag = result == 0;
            return result;
        }

        private byte ShiftRight(byte value)
        {
            CarryFlag = value.IsBitSet(0);
            var result = (byte)(value >> 1);
            OperationFlag = false;
            HalfCarryFlag = false;
            ZeroFlag = result == 0;
            return result;
        }

        private byte ShiftRightArithmetic(byte value)
        {
            var msb = value.IsBitSet(7);
            var result = ShiftRight(value);
            result = result.BitSet(7, msb);
            return result;
        }

        private void TestBit(byte bit, byte value)
        {
            if (bit > 7)
                throw new Exception(string.Format("Invalid value for bit. Value: {0}.", bit));

            OperationFlag = false;
            HalfCarryFlag = true;
            ZeroFlag = !value.IsBitSet(bit);
        }

        private byte SetBit(byte bit, byte value)
        {
            if (bit > 7)
                throw new Exception(string.Format("Invalid value for bit. Value: {0}.", bit));

            var result = value.BitSet(bit);
            return result;
        }

        private byte ResetBit(byte bit, byte value)
        {
            if (bit > 7)
                throw new Exception(string.Format("Invalid value for bit. Value: {0}.", bit));

            var result = value.BitSet(bit, false);
            return result;
        }

        #endregion

        #region Ops

        #region LD

        [Op(0x06, 8, "LD B {0}")]
        void LD_Bn()
        {
            var value = ReadByte();
            RegB = value;
        }

        [Op(0x0E, 8, "LD C {0}")]
        void LD_Cn()
        {
            var value = ReadByte();
            RegC = value;
        }

        [Op(0x16, 8, "LD D {0}")]
        void LD_Dn()
        {
            var value = ReadByte();
            RegD = value;
        }

        [Op(0x1E, 8, "LD E {0}")]
        void LD_En()
        {
            var value = ReadByte();
            RegE = value;
        }

        [Op(0x26, 8, "LD H {0}")]
        void LD_Hn()
        {
            var value = ReadByte();
            RegH = value;
        }

        [Op(0x2E, 8, "LD L {0}")]
        void LD_Ln()
        {
            var value = ReadByte();
            RegL = value;
        }

        [Op(0x7F, 4, "LD A A")]
        void LD_AA()
        {
            RegA = RegA;
        }

        [Op(0x78, 4, "LD A B")]
        void LD_AB()
        {
            RegA = RegB;
        }

        [Op(0x79, 4, "LD A C")]
        void LD_AC()
        {
            RegA = RegC;
        }

        [Op(0x7A, 4, "LD A D")]
        void LD_AD()
        {
            RegA = RegD;
        }

        [Op(0x7B, 4, "LD A E")]
        void LD_AE()
        {
            RegA = RegE;
        }

        [Op(0x7C, 4, "LD A H")]
        void LD_AH()
        {
            RegA = RegH;
        }

        [Op(0x7D, 4, "LD A L")]
        void LD_AL()
        {
            RegA = RegL;
        }

        [Op(0x7E, 8, "LD A HL")]
        void LD_AHL()
        {
            var value = Mmu.ReadByte(HL);
            RegA = value;
        }

        [Op(0x40, 4, "LD B B")]
        void LD_BB()
        {
            RegB = RegB;
        }

        [Op(0x41, 4, "LD B C")]
        void LD_BC()
        {
            RegB = RegC;
        }

        [Op(0x42, 4, "LD B D")]
        void LD_BD()
        {
            RegB = RegD;
        }

        [Op(0x43, 4, "LD B E")]
        void LD_BE()
        {
            RegB = RegE;
        }

        [Op(0x44, 4, "LD B H")]
        void LD_BH()
        {
            RegB = RegH;
        }

        [Op(0x45, 4, "LD B L")]
        void LD_BL()
        {
            RegB = RegL;
        }

        [Op(0x46, 8, "LD B HL")]
        void LD_BHL()
        {
            var value = Mmu.ReadByte(HL);
            RegB = value;
        }

        [Op(0x48, 4, "LD C B")]
        void LD_CB()
        {
            RegC = RegB;
        }

        [Op(0x49, 4, "LD C C")]
        void LD_CC()
        {
            RegC = RegC;
        }

        [Op(0x4A, 4, "LD C D")]
        void LD_CD()
        {
            RegC = RegD;
        }

        [Op(0x4B, 4, "LD C E")]
        void LD_CE()
        {
            RegC = RegE;
        }

        [Op(0x4C, 4, "LD C H")]
        void LD_CH()
        {
            RegC = RegH;
        }

        [Op(0x4D, 4, "LD C L")]
        void LD_CL()
        {
            RegC = RegL;
        }

        [Op(0x4E, 8, "LD C HL")]
        void LD_CHL()
        {
            var value = Mmu.ReadByte(HL);
            RegC = value;
        }

        [Op(0x50, 4, "LD D B")]
        void LD_DB()
        {
            RegD = RegB;
        }

        [Op(0x51, 4, "LD D C")]
        void LD_DC()
        {
            RegD = RegC;
        }

        [Op(0x52, 4, "LD D D")]
        void LD_DD()
        {
            RegD = RegD;
        }

        [Op(0x53, 4, "LD D E")]
        void LD_DE()
        {
            RegD = RegE;
        }

        [Op(0x54, 4, "LD D H")]
        void LD_DH()
        {
            RegD = RegH;
        }

        [Op(0x55, 4, "LD D L")]
        void LD_DL()
        {
            RegD = RegL;
        }

        [Op(0x56, 8, "LD D HL")]
        void LD_DHL()
        {
            var value = Mmu.ReadByte(HL);
            RegD = value;
        }

        [Op(0x58, 4, "LD E B")]
        void LD_EB()
        {
            RegE = RegB;
        }

        [Op(0x59, 4, "LD E C")]
        void LD_EC()
        {
            RegE = RegC;
        }

        [Op(0x5A, 4, "LD E D")]
        void LD_ED()
        {
            RegE = RegD;
        }

        [Op(0x5B, 4, "LD E E")]
        void LD_EE()
        {
            RegE = RegE;
        }

        [Op(0x5C, 4, "LD E H")]
        void LD_EH()
        {
            RegE = RegH;
        }

        [Op(0x5D, 4, "LD E L")]
        void LD_EL()
        {
            RegE = RegL;
        }

        [Op(0x5E, 8, "LD E HL")]
        void LD_EHL()
        {
            var value = Mmu.ReadByte(HL);
            RegE = value;
        }

        [Op(0x60, 4, "LD H B")]
        void LD_HB()
        {
            RegH = RegB;
        }

        [Op(0x61, 4, "LD H C")]
        void LD_HC()
        {
            RegH = RegC;
        }

        [Op(0x62, 4, "LD H D")]
        void LD_HD()
        {
            RegH = RegD;
        }

        [Op(0x63, 4, "LD H E")]
        void LD_HE()
        {
            RegH = RegE;
        }

        [Op(0x64, 4, "LD H H")]
        void LD_HH()
        {
            RegH = RegH;
        }

        [Op(0x65, 4, "LD H L")]
        void LD_HL()
        {
            RegH = RegL;
        }

        [Op(0x66, 8, "LD H HL")]
        void LD_HHL()
        {
            var value = Mmu.ReadByte(HL);
            RegH = value;
        }

        [Op(0x68, 4, "LD L B")]
        void LD_LB()
        {
            RegL = RegB;
        }

        [Op(0x69, 4, "LD L C")]
        void LD_LC()
        {
            RegL = RegC;
        }

        [Op(0x6A, 4, "LD L D")]
        void LD_LD()
        {
            RegL = RegD;
        }

        [Op(0x6B, 4, "LD L E")]
        void LD_LE()
        {
            RegL = RegE;
        }

        [Op(0x6C, 4, "LD L H")]
        void LD_LH()
        {
            RegL = RegH;
        }

        [Op(0x6D, 4, "LD L L")]
        void LD_LL()
        {
            RegL = RegL;
        }

        [Op(0x6E, 8, "LD L HL")]
        void LD_LHL()
        {
            var value = Mmu.ReadByte(HL);
            RegL = value;
        }

        [Op(0x70, 8, "LD HL B")]
        void LD_HLB()
        {
            Mmu.WriteByte(HL, RegB);
        }

        [Op(0x71, 8, "LD HL C")]
        void LD_HLC()
        {
            Mmu.WriteByte(HL, RegC);
        }

        [Op(0x72, 8, "LD HL D")]
        void LD_HLD()
        {
            Mmu.WriteByte(HL, RegD);
        }

        [Op(0x73, 8, "LD HL E")]
        void LD_HLE()
        {
            Mmu.WriteByte(HL, RegE);
        }

        [Op(0x74, 8, "LD HL H")]
        void LD_HLH()
        {
            Mmu.WriteByte(HL, RegH);
        }

        [Op(0x75, 8, "LD HL L")]
        void LD_HLL()
        {
            Mmu.WriteByte(HL, RegL);
        }

        [Op(0x36, 12, "LD HL {0}")]
        void LD_HLn()
        {
            var value = ReadByte();
            Mmu.WriteByte(HL, value);
        }

        [Op(0x0A, 8, "LD A BC")]
        void LD_ABC()
        {
            var value = Mmu.ReadByte(BC);
            RegA = value;
        }

        [Op(0x1A, 8, "LD A DE")]
        void LD_ADE()
        {
            var value = Mmu.ReadByte(DE);
            RegA = value;
        }

        [Op(0xFA, 16, "LD A nn")]
        void LD_Ann()
        {
            var addr = ReadWord();
            var value = Mmu.ReadByte(addr);
            RegA = value;
        }

        [Op(0x3E, 8, "LD A {0}")]
        void LD_An()
        {
            var value = ReadByte();
            RegA = value;
        }

        [Op(0x47, 4, "LD B A")]
        void LD_BA()
        {
            RegB = RegA;
        }

        [Op(0x4F, 4, "LD C A")]
        void LD_CA()
        {
            RegC = RegA;
        }

        [Op(0x57, 4, "LD D A")]
        void LD_DA()
        {
            RegD = RegA;
        }

        [Op(0x5F, 4, "LD E A")]
        void LD_EA()
        {
            RegE = RegA;
        }

        [Op(0x67, 4, "LD H A")]
        void LD_HA()
        {
            RegH = RegA;
        }

        [Op(0x6F, 4, "LD L A")]
        void LD_LA()
        {
            RegL = RegA;
        }

        [Op(0x02, 8, "LD BC A")]
        void LD_BCA()
        {
            Mmu.WriteWord(BC, RegA);
        }

        [Op(0x12, 8, "LD DE A")]
        void LD_DEA()
        {
            Mmu.WriteWord(DE, RegA);
        }

        [Op(0x77, 8, "LD HL A")]
        void LD_HLA()
        {
            Mmu.WriteWord(HL, RegA);
        }

        [Op(0xEA, 16, "LD nn A")]
        void LD_nnA()
        {
            var addr = ReadWord();
            Mmu.WriteByte(addr, RegA);
        }

        [Op(0xF2, 8, "LD A C+FF00")]
        void LD_AC_FF00()
        {
            var addr = RegC.Add(P1);
            var value = Mmu.ReadByte(addr);
            RegA = value;
        }

        [Op(0xE2, 8, "LD C+FF00 A")]
        void LD_C_FF00_A()
        {
            var addr = RegC.Add(P1);
            Mmu.WriteByte(addr, RegA);
        }

        [Op(0x3A, 8, "LD A (HLD)")]
        void LD_AHLD()
        {
            var value = Mmu.ReadByte(HL);
            RegA = value;
            HL--;
        }

        [Op(0x32, 8, "LD (HLD) A")]
        void LD_HLDA()
        {
            Mmu.WriteByte(HL, RegA);
            HL--;
        }

        [Op(0x2A, 8, "LD A (HLI)")]
        void LD_AHLI()
        {
            var value = Mmu.ReadByte(HL);
            RegA = value;
            HL++;
        }

        [Op(0x22, 8, "LD (HLI) A")]
        void LD_HLIA()
        {
            Mmu.WriteByte(HL, RegA);
            HL++;
        }

        [Op(0xE0, 12, "LD nn+FF00 A")]
        void LD_nn_FF00_A()
        {
            var addr = ReadByte().Add(P1);
            Mmu.WriteByte(addr, RegA);
        }

        [Op(0xF0, 12, "LD A (nn+FF00)")]
        void LD_Ann_FF00()
        {
            var addr = ReadByte().Add(P1);
            var value = Mmu.ReadByte(addr);
            RegA = value;
        }

        [Op(0x01, 12, "LD BC nn")]
        void LD_BCnn()
        {
            RegC = ReadByte();
            RegB = ReadByte();
        }

        [Op(0x11, 12, "LD DE nn")]
        void LD_DEnn()
        {
            RegE = ReadByte();
            RegD = ReadByte();
        }

        [Op(0x21, 12, "LD HL nn")]
        void LD_HLnn()
        {
            RegL = ReadByte();
            RegH = ReadByte();       
        }

        [Op(0x31, 12, "LD SP nn")]
        void LD_SPnn()
        {
            SP = ReadWord();
        }

        [Op(0xF9, 8, "LD SP HL")]
        void LD_SPHL()
        {
            SP = HL;
        }

        [Op(0xF8, 12, "LDHL SP n")]
        void LDHL_SPn()
        {
            var n = ReadByte();
            var value = AddWords(SP, n);
            HL = value;

            ZeroFlag = false;
            OperationFlag = false;

        }

        [Op(0x08, 20, "LD nn SP")]
        void LD_nnSP()
        {
            var addr = ReadWord();
            Mmu.WriteWord(addr, SP);

        }

        #endregion

        #region PUSH

        [Op(0xF5, 16, "PUSH AF")]
        void PUSH_AF()
        {
            SP -= 2;
            Mmu.WriteWord(SP, AF);
        }

        [Op(0xC5, 16, "PUSH BC")]
        void PUSH_BC()
        {
            SP -= 2;
            Mmu.WriteWord(SP, BC);
        }

        [Op(0xD5, 16, "PUSH DE")]
        void PUSH_DE()
        {
            SP -= 2;
            Mmu.WriteWord(SP, DE);
        }

        [Op(0xE5, 16, "PUSH HL")]
        void PUSH_HL()
        {
            SP -= 2;
            Mmu.WriteWord(SP, HL);
        }

        #endregion

        #region POP

        [Op(0xF1, 12, "POP AF")]
        void POP_AF()
        {
            var value = Mmu.ReadWord(SP);
            AF = value;
            SP += 2;
        }

        [Op(0xC1, 12, "POP BC")]
        void POP_BC()
        {
            var value = Mmu.ReadWord(SP);
            BC = value;
            SP += 2;
        }

        [Op(0xD1, 12, "POP DE")]
        void POP_DE()
        {
            var value = Mmu.ReadWord(SP);
            DE = value;
            SP += 2;
        }

        [Op(0xE1, 12, "POP HL")]
        void POP_HL()
        {
            var value = Mmu.ReadWord(SP);
            HL = value;
            SP += 2;
        }

        #endregion

        #region ADD

        [Op(0x87, 4, "ADD A A")]
        void ADD_AA()
        {
            RegA = AddBytes(RegA, RegA);
        }

        [Op(0x80, 4, "ADD A B")]
        void ADD_AB()
        {
            RegA = AddBytes(RegA, RegB);
        }

        [Op(0x81, 4, "ADD A D")]
        void ADD_AC()
        {
            RegA = AddBytes(RegA, RegC);
        }

        [Op(0x82, 4, "ADD A D")]
        void ADD_AD()
        {
            RegA = AddBytes(RegA, RegD);
        }

        [Op(0x83, 4, "ADD A E")]
        void ADD_AE()
        {
            RegA = AddBytes(RegA, RegE);
        }

        [Op(0x84, 4, "ADD A H")]
        void ADD_AH()
        {
            RegA = AddBytes(RegA, RegH);
        }

        [Op(0x85, 4, "ADD A L")]
        void ADD_AL()
        {
            RegA = AddBytes(RegA, RegL);
        }

        [Op(0x86, 8, "ADD A (HL)")]
        void ADD_HL()
        {
            var value = Mmu.ReadByte(HL);
            RegA = AddBytes(RegA, value);
        }

        [Op(0xC6, 8, "ADD A x")]
        void ADD_n()
        {
            var value = ReadByte();
            RegA = AddBytes(RegA, value);
        }

        [Op(0x8F, 4, "ADC A A")]
        void ADC_AA()
        {
            var carryValue = (byte)(CarryFlag ? 1 : 0);
            var value = AddBytes(RegA, carryValue);
            RegA = AddBytes(RegA, value);
        }

        [Op(0x88, 4, "ADC A B")]
        void ADC_AB()
        {
            var carryValue = (byte)(CarryFlag ? 1 : 0);
            var value = AddBytes(RegB, carryValue);
            RegA = AddBytes(RegA, value);
        }

        [Op(0x89, 4, "ADC A C")]
        void ADC_AC()
        {
            var carryValue = (byte)(CarryFlag ? 1 : 0);
            var value = AddBytes(RegC, carryValue);
            RegA = AddBytes(RegA, value);
        }

        [Op(0x8A, 4, "ADC A D")]
        void ADC_AD()
        {
            var carryValue = (byte)(CarryFlag ? 1 : 0);
            var value = AddBytes(RegD, carryValue);
            RegA = AddBytes(RegA, value);
        }

        [Op(0x8B, 4, "ADC A E")]
        void ADC_AE()
        {
            var carryValue = (byte)(CarryFlag ? 1 : 0);
            var value = AddBytes(RegE, carryValue);
            RegA = AddBytes(RegA, value);
        }

        [Op(0x8C, 4, "ADC A H")]
        void ADC_AH()
        {
            var carryValue = (byte)(CarryFlag ? 1 : 0);
            var value = AddBytes(RegH, carryValue);
            RegA = AddBytes(RegA, value);
        }

        [Op(0x8D, 4, "ADC A L")]
        void ADC_AL()
        {
            var carryValue = (byte)(CarryFlag ? 1 : 0);
            var value = AddBytes(RegL, carryValue);
            RegA = AddBytes(RegA, value);
        }

        [Op(0x8E, 8, "ADC A (HL)")]
        void ADC_AHL()
        {
            var carryValue = (byte)(CarryFlag ? 1 : 0);
            var storedValue = Mmu.ReadByte(HL);
            var value = AddBytes(storedValue, carryValue);
            RegA = AddBytes(RegA, value);
        }

        [Op(0xCE, 8, "ADC A #")]
        void ADC_An()
        {
            var carryValue = (byte)(CarryFlag ? 1 : 0);
            var storedValue = ReadByte();
            var value = AddBytes(storedValue, carryValue);
            RegA = AddBytes(RegA, value);
        }

        [Op(0x09, 8, "ADD HL BC")]
        void ADD_HLBC()
        {
            HL = AddWords(HL, BC);
        }

        [Op(0x19, 8, "ADD HL DE")]
        void ADD_HLDE()
        {
            HL = AddWords(HL, DE);
        }

        [Op(0x29, 8, "ADD HL HL")]
        void ADD_HLHL()
        {
            HL = AddWords(HL, HL);
        }

        [Op(0x39, 8, "ADD HL SP")]
        void ADD_HLSP()
        {
            HL = AddWords(HL, SP);
        }

        [Op(0xE8, 16, "ADD SP n")]
        void ADD_SPn()
        {
            var value = ReadByte();
            SP = AddWords(SP, value);
            ZeroFlag = false;
            OperationFlag = false;
        }

        #endregion

        #region SUB

        [Op(0x97, 4, "SUB A")]
        void SUB_A()
        {
            RegA = SubBytes(RegA, RegA);
        }

        [Op(0x90, 4, "SUB B")]
        void SUB_B()
        {
            RegA = SubBytes(RegA, RegB);
        }

        [Op(0x91, 4, "SUB C")]
        void SUB_C()
        {
            RegA = SubBytes(RegA, RegC);
        }

        [Op(0x92, 4, "SUB D")]
        void SUB_D()
        {
            RegA = SubBytes(RegA, RegD);
        }

        [Op(0x93, 4, "SUB E")]
        void SUB_E()
        {
            RegA = SubBytes(RegA, RegE);
        }

        [Op(0x94, 4, "SUB H")]
        void SUB_H()
        {
            RegA = SubBytes(RegA, RegH);
        }

        [Op(0x95, 4, "SUB L")]
        void SUB_L()
        {
            RegA = SubBytes(RegA, RegL);
        }

        [Op(0x96, 8, "SUB (HL)")]
        void SUB_HL()
        {
            var value = Mmu.ReadByte(HL);
            RegA = SubBytes(RegA, value);
        }

        [Op(0xD6, 8, "SUB n")]
        void SUB_n()
        {
            var value = ReadByte();
            RegA = SubBytes(RegA, value);
        }

        [Op(0x9F, 4, "SBC A")]
        void SBC_A()
        {
            var carryValue = (byte)(CarryFlag ? 1 : 0);
            var value = AddBytes(RegA, carryValue);
            RegA = SubBytes(RegA, value);
        }

        [Op(0x98, 4, "SBC B")]
        void SBC_B()
        {
            var carryValue = (byte)(CarryFlag ? 1 : 0);
            var value = AddBytes(RegB, carryValue);
            RegA = SubBytes(RegA, value);
        }

        [Op(0x99, 4, "SBC C")]
        void SBC_C()
        {
            var carryValue = (byte)(CarryFlag ? 1 : 0);
            var value = AddBytes(RegC, carryValue);
            RegA = SubBytes(RegA, value);
        }

        [Op(0x9A, 4, "SBC D")]
        void SBC_D()
        {
            var carryValue = (byte)(CarryFlag ? 1 : 0);
            var value = AddBytes(RegD, carryValue);
            RegA = SubBytes(RegA, value);
        }

        [Op(0x9B, 4, "SBC E")]
        void SBC_E()
        {
            var carryValue = (byte)(CarryFlag ? 1 : 0);
            var value = AddBytes(RegE, carryValue);
            RegA = SubBytes(RegA, value);
        }

        [Op(0x9C, 4, "SBC H")]
        void SBC_H()
        {
            var carryValue = (byte)(CarryFlag ? 1 : 0);
            var value = AddBytes(RegH, carryValue);
            RegA = SubBytes(RegA, value);
        }

        [Op(0x9D, 4, "SBC L")]
        void SBC_L()
        {
            var carryValue = (byte)(CarryFlag ? 1 : 0);
            var value = AddBytes(RegL, carryValue);
            RegA = SubBytes(RegA, value);
        }

        [Op(0x9E, 8, "SBC HL")]
        void SBC_HL()
        {
            var carryValue = (byte)(CarryFlag ? 1 : 0);
            var storedValue = ReadByte();
            var value = AddBytes(storedValue, carryValue);
            RegA = SubBytes(RegA, value);
        }

        #endregion

        #region AND

        [Op(0xA7, 4, "AND A")]
        void AND_A()
        {
            RegA = AndBytes(RegA, RegA);
        }

        [Op(0xA0, 4, "AND B")]
        void AND_B()
        {
            RegA = AndBytes(RegA, RegB);
        }

        [Op(0xA1, 4, "AND C")]
        void AND_C()
        {
            RegA = AndBytes(RegA, RegC);
        }

        [Op(0xA2, 4, "AND D")]
        void AND_D()
        {
            RegA = AndBytes(RegA, RegD);
        }

        [Op(0xA3, 4, "AND E")]
        void AND_E()
        {
            RegA = AndBytes(RegA, RegE);
        }

        [Op(0xA4, 4, "AND H")]
        void AND_H()
        {
            RegA = AndBytes(RegA, RegH);
        }

        [Op(0xA5, 4, "AND L")]
        void AND_L()
        {
            RegA = AndBytes(RegA, RegL);
        }

        [Op(0xA6, 8, "AND (HL)")]
        void AND_HL()
        {
            var storedValue = Mmu.ReadByte(HL);
            RegA = AndBytes(RegA, storedValue);
        }

        [Op(0xE6, 8, "AND n")]
        void AND_n()
        {
            var storedValue = ReadByte();
            RegA = AndBytes(RegA, storedValue);
        }

        #endregion

        #region OR

        [Op(0xB7, 4, "OR A")]
        void OR_A()
        {
            RegA = OrBytes(RegA, RegA);
        }

        [Op(0xB0, 4, "OR B")]
        void OR_B()
        {
            RegA = OrBytes(RegA, RegB);
        }

        [Op(0xB1, 4, "OR C")]
        void OR_C()
        {
            RegA = OrBytes(RegA, RegC);
        }

        [Op(0xB2, 4, "OR D")]
        void OR_D()
        {
            RegA = OrBytes(RegA, RegD);
        }

        [Op(0xB3, 4, "OR E")]
        void OR_E()
        {
            RegA = OrBytes(RegA, RegE);
        }

        [Op(0xB4, 4, "OR H")]
        void OR_H()
        {
            RegA = OrBytes(RegA, RegH);
        }

        [Op(0xB5, 4, "OR L")]
        void OR_L()
        {
            RegA = OrBytes(RegA, RegL);
        }

        [Op(0xB6, 48, "OR (HL)")]
        void OR_HL()
        {
            var storedValue = Mmu.ReadByte(HL);
            RegA = OrBytes(RegA, storedValue);
        }

        [Op(0xF6, 8, "OR n")]
        void OR_n()
        {
            var storedValue = ReadByte();
            RegA = OrBytes(RegA, storedValue);
        }

        #endregion

        #region XOR

        [Op(0xAF, 4, "XOR A")]
        void XOR_A()
        {
            RegA = XorBytes(RegA, RegA);
        }

        [Op(0xA8, 4, "XOR B")]
        void XOR_B()
        {
            RegA = XorBytes(RegA, RegB);
        }

        [Op(0xA9, 4, "XOR C")]
        void XOR_C()
        {
            RegA = XorBytes(RegA, RegC);
        }

        [Op(0xAA, 4, "XOR D")]
        void XOR_D()
        {
            RegA = XorBytes(RegA, RegD);
        }

        [Op(0xAB, 4, "XOR E")]
        void XOR_E()
        {
            RegA = XorBytes(RegA, RegE);
        }

        [Op(0xAC, 4, "XOR H")]
        void XOR_H()
        {
            RegA = XorBytes(RegA, RegH);
        }

        [Op(0xAD, 4, "XOR L")]
        void XOR_L()
        {
            RegA = XorBytes(RegA, RegL);
        }

        [Op(0xAE, 8, "XOR (HL)")]
        void XOR_HL()
        {
            var storedValue = Mmu.ReadByte(HL);
            RegA = XorBytes(RegA, storedValue);
        }

        [Op(0xEE, 8, "XOR n")]
        void XOR_n()
        {
            var storedValue = ReadByte();
            RegA = XorBytes(RegA, storedValue);
        }

        #endregion

        #region CP

        [Op(0xBF, 4, "CP A")]
        void CP_A()
        {
            CpBytes(RegA, RegA);
        }

        [Op(0xB8, 4, "CP B")]
        void CP_B()
        {
            CpBytes(RegA, RegB);
        }

        [Op(0xB9, 4, "CP C")]
        void CP_C()
        {
            CpBytes(RegA, RegC);
        }

        [Op(0xBA, 4, "CP D")]
        void CP_D()
        {
            CpBytes(RegA, RegD);
        }

        [Op(0xBB, 4, "CP E")]
        void CP_E()
        {
            CpBytes(RegA, RegE);
        }

        [Op(0xBC, 4, "CP H")]
        void CP_H()
        {
            CpBytes(RegA, RegH);
        }

        [Op(0xBD, 4, "CP L")]
        void CP_L()
        {
            CpBytes(RegA, RegL);
        }

        [Op(0xBE, 8, "CP (HL)")]
        void CP_HL()
        {
            var storedValue = Mmu.ReadByte(HL);
            CpBytes(RegA, storedValue);
        }

        [Op(0xFE, 8, "CP n")]
        void CP_n()
        {
            var storedValue = ReadByte();
            CpBytes(RegA, storedValue);
        }

        #endregion

        #region INC

        [Op(0x3C, 4, "INC A")]
        void INC_A()
        {
            RegA = IncByte(RegA);
        }

        [Op(0x04, 4, "INC B")]
        void INC_B()
        {
            RegB = IncByte(RegB);
        }

        [Op(0x0C, 4, "INC C")]
        void INC_C()
        {
            RegC = IncByte(RegC);
        }

        [Op(0x14, 4, "INC D")]
        void INC_D()
        {
            RegD = IncByte(RegD);
        }

        [Op(0x1C, 4, "INC E")]
        void INC_E()
        {
            RegE = IncByte(RegE);
        }

        [Op(0x24, 4, "INC H")]
        void INC_H()
        {
            RegH = IncByte(RegH);
        }

        [Op(0x2C, 4, "INC L")]
        void INC_L()
        {
            RegL = IncByte(RegL);
        }

        [Op(0x34, 12, "INC (HL)")]
        void INC_bHLb()
        {
            var value = ReadByte();
            HL = IncByte(value);
        }

        [Op(0x03, 8, "INC BC")]
        void INC_BC()
        {
            BC = IncWord(BC);
        }

        [Op(0x13, 8, "INC DE")]
        void INC_DE()
        {
            DE = IncWord(DE);
        }

        [Op(0x23, 8, "INC HL")]
        void INC_HL()
        {
            HL = IncWord(HL);
        }

        [Op(0x33, 8, "INC SP")]
        void INC_SP()
        {
            SP = IncWord(SP);
        }

        #endregion

        #region DEC

        [Op(0x3D, 4, "DEC A")]
        void DEC_A()
        {
            RegA = DecByte(RegA);
        }

        [Op(0x05, 4, "DEC B")]
        void DEC_B()
        {
            RegB = DecByte(RegB);
        }

        [Op(0x0D, 4, "DEC C")]
        void DEC_C()
        {
            RegC = DecByte(RegC);
        }

        [Op(0x15, 4, "DEC D")]
        void DEC_D()
        {
            RegD = DecByte(RegD);
        }

        [Op(0x1D, 4, "DEC E")]
        void DEC_E()
        {
            RegE = DecByte(RegE);
        }

        [Op(0x25, 4, "DEC H")]
        void DEC_H()
        {
            RegH = DecByte(RegH);
        }

        [Op(0x2D, 4, "DEC L")]
        void DEC_L()
        {
            RegL = DecByte(RegL);
        }

        [Op(0x35, 12, "DEC (HL)")]
        void DEC_bHLb()
        {
            var value = ReadByte();
            HL = DecWord(value);
        }

        [Op(0x0B, 8, "DEC BC")]
        void DEC_BC()
        {
            BC = DecWord(BC);
        }

        [Op(0x1B, 8, "DEC DE")]
        void DEC_DE()
        {
            DE = DecWord(DE);
        }

        [Op(0x2B, 8, "DEC HL")]
        void DEC_HL()
        {
            HL = DecWord(HL);
        }

        [Op(0x3B, 8, "DEC SP")]
        void DEC_SP()
        {
            SP = DecWord(SP);
        }

        #endregion

        #region SWAP

        [CbOp(0x37, 8, "Swap A")]
        void SWAP_A()
        {
            RegA = Swap(RegA);
        }

        [CbOp(0x30, 8, "Swap B")]
        void SWAP_B()
        {
            RegB = Swap(RegB);
        }

        [CbOp(0x31, 8, "Swap C")]
        void SWAP_C()
        {
            RegC = Swap(RegC);
        }

        [CbOp(0x32, 8, "Swap D")]
        void SWAP_D()
        {
            RegD = Swap(RegD);
        }

        [CbOp(0x33, 8, "Swap E")]
        void SWAP_E()
        {
            RegE = Swap(RegE);
        }

        [CbOp(0x34, 8, "Swap H")]
        void SWAP_H()
        {
            RegH = Swap(RegH);
        }

        [CbOp(0x35, 8, "Swap L")]
        void SWAP_L()
        {
            RegL = Swap(RegL);
        }

        // This commands appears to do too much for 16 cycles. Check that this is as specified.
        [CbOp(0x36, 16, "Swap (HL)")]
        void SWAP_HL()
        {
            var value = Mmu.ReadByte(HL);
            var swappedValue = Swap(value);
            Mmu.WriteByte(HL, swappedValue);
        }

        #endregion

        #region DAA

        // This is proably wrong. Greater knowledge of BCD is needed.
        [Op(0x27, 4, "DAA")]
        void DAA()
        {
            RegA = AddBytes(RegA, 0x06);
            HalfCarryFlag = false;
        }

        #endregion

        #region CPL

        [Op(0x2F, 4, "CPL")]
        void CPL()
        {
            RegA = (byte)~RegA;
            OperationFlag = true;
            HalfCarryFlag = true;
        }

        #endregion

        #region CCF-SCF

        [Op(0x3F, 4, "CCF")]
        void CCF()
        {
            CarryFlag = !CarryFlag;
            OperationFlag = false;
            HalfCarryFlag = false;
        }

        [Op(0x37, 4, "SCF")]
        void SCF()
        {
            CarryFlag = true;
            OperationFlag = false;
            HalfCarryFlag = false;
        }

        #endregion

        #region POWER

        [Op(0x00, 4, "NOP")]
        void NOP()
        {
            // no operation
        }

        [Op(0x76, 4, "HALT")]
        void HALT()
        {
            // pause execution until an interupt occurs.
            Halted = true;
        }

        [Op(0x10, 4, "STOP")]
        void STOP()
        {
            // pause execution and screen refresh until a button is pressed.
            Stopped = true;
        }

        [Op(0xF3, 4, "DI")]
        void DI()
        {
            DisableInterupt = true;
        }

        [Op(0xFB, 4, "EI")]
        void EI()
        {
            DisableInterupt = true;
        }

        #endregion

        #region ROTATE

        [Op(0x07, 4, "RCLA")]
        void RCLA()
        {
            RLC_A();
        }

        [Op(0x17, 4, "RLA")]
        void RLA()
        {
            RL_A();
        }

        [Op(0x0F, 4, "RCRA")]
        void RCRA()
        {
            RRC_A();
        }

        [Op(0x1F, 4, "RRA")]
        void RRA()
        {
            RR_A();
        }

        [CbOp(0x07, 8, "RLC A")]
        void RLC_A()
        {
            RegA = RotateLeft(RegA);
        }

        [CbOp(0x00, 8, "RLC B")]
        void RLC_B()
        {
            RegB = RotateLeft(RegB);
        }

        [CbOp(0x01, 8, "RLC C")]
        void RLC_C()
        {
            RegC = RotateLeft(RegC);
        }

        [CbOp(0x02, 8, "RLC D")]
        void RLC_D()
        {
            RegD = RotateLeft(RegD);
        }

        [CbOp(0x03, 8, "RLC E")]
        void RLC_E()
        {
            RegE = RotateLeft(RegE);
        }

        [CbOp(0x04, 8, "RLC H")]
        void RLC_H()
        {
            RegH = RotateLeft(RegH);
        }

        [CbOp(0x05, 8, "RLC L")]
        void RLC_L()
        {
            RegL = RotateLeft(RegL);
        }

        [CbOp(0x06, 16, "RLC A")]
        void RLC_HL()
        {
            var result = RotateLeft(Mmu.ReadByte(HL));
            Mmu.WriteByte(HL, result);
        }

        [CbOp(0x17, 8, "RL A")]
        void RL_A()
        {
            RegA = RotateLeftThrough(RegA);
        }

        [CbOp(0x10, 8, "RL B")]
        void RL_B()
        {
            RegB = RotateLeftThrough(RegB);
        }

        [CbOp(0x11, 8, "RL C")]
        void RL_C()
        {
            RegC = RotateLeftThrough(RegC);
        }

        [CbOp(0x12, 8, "RL D")]
        void RL_D()
        {
            RegD = RotateLeftThrough(RegD);
        }

        [CbOp(0x13, 8, "RL E")]
        void RL_E()
        {
            RegE = RotateLeftThrough(RegE);
        }

        [CbOp(0x14, 8, "RL H")]
        void RL_H()
        {
            RegH = RotateLeftThrough(RegH);
        }

        [CbOp(0x15, 8, "RL L")]
        void RL_L()
        {
            RegL = RotateLeftThrough(RegL);
        }

        [CbOp(0x16, 16, "RL A")]
        void RL_HL()
        {
            var result = RotateLeftThrough(Mmu.ReadByte(HL));
            Mmu.WriteByte(HL, result);
        }

        [CbOp(0x0F, 8, "RRC A")]
        void RRC_A()
        {
            RegA = RotateRight(RegA);
        }

        [CbOp(0x08, 8, "RRC B")]
        void RRC_B()
        {
            RegB = RotateRight(RegB);
        }

        [CbOp(0x09, 8, "RRC C")]
        void RRC_C()
        {
            RegC = RotateRight(RegC);
        }

        [CbOp(0x0A, 8, "RRC D")]
        void RRC_D()
        {
            RegD = RotateRight(RegD);
        }

        [CbOp(0x0B, 8, "RRC E")]
        void RRC_E()
        {
            RegE = RotateRight(RegE);
        }

        [CbOp(0x0C, 8, "RRC H")]
        void RRC_H()
        {
            RegH = RotateRight(RegH);
        }

        [CbOp(0x0D, 8, "RRC L")]
        void RRC_L()
        {
            RegL = RotateRight(RegL);
        }

        [CbOp(0x0E, 16, "RRC A")]
        void RRC_HL()
        {
            var result = RotateRight(Mmu.ReadByte(HL));
            Mmu.WriteByte(HL, result);
        }

        [CbOp(0x1F, 8, "RR A")]
        void RR_A()
        {
            RegA = RotateRightThrough(RegA);
        }

        [CbOp(0x18, 8, "RR B")]
        void RR_B()
        {
            RegB = RotateRightThrough(RegB);
        }

        [CbOp(0x19, 8, "RR C")]
        void RR_C()
        {
            RegC = RotateRightThrough(RegC);
        }

        [CbOp(0x1A, 8, "RR D")]
        void RR_D()
        {
            RegD = RotateRightThrough(RegD);
        }

        [CbOp(0x1B, 8, "RR E")]
        void RR_E()
        {
            RegE = RotateRightThrough(RegE);
        }

        [CbOp(0x1C, 8, "RR H")]
        void RR_H()
        {
            RegH = RotateRightThrough(RegH);
        }

        [CbOp(0x1D, 8, "RR L")]
        void RR_L()
        {
            RegL = RotateRightThrough(RegL);
        }

        [CbOp(0x1E, 16, "RR A")]
        void RR_HL()
        {
            var result = RotateRightThrough(Mmu.ReadByte(HL));
            Mmu.WriteByte(HL, result);
        }

        #endregion

        #region SHIFT

        [CbOp(0x27, 8, "SLA A")]
        void SLA_A()
        {
            RegA = ShiftLeft(RegA);
        }

        [CbOp(0x20, 8, "SLA B")]
        void SLA_B()
        {
            RegB = ShiftLeft(RegB);
        }

        [CbOp(0x21, 8, "SLA C")]
        void SLA_C()
        {
            RegC = ShiftLeft(RegC);
        }

        [CbOp(0x22, 8, "SLA D")]
        void SLA_D()
        {
            RegD = ShiftLeft(RegD);
        }

        [CbOp(0x23, 8, "SLA E")]
        void SLA_E()
        {
            RegE = ShiftLeft(RegE);
        }

        [CbOp(0x24, 8, "SLA H")]
        void SLA_H()
        {
            RegH = ShiftLeft(RegH);
        }

        [CbOp(0x25, 8, "SLA L")]
        void SLA_L()
        {
            RegL = ShiftLeft(RegL);
        }

        [CbOp(0x26, 16, "SLA (HL)")]
        void SLA_HL()
        {
            var value = Mmu.ReadByte(HL);
            var result = ShiftLeft(value);
            Mmu.WriteByte(HL, result);
        }

        [CbOp(0x2F, 8, "SRA A")]
        void SRA_A()
        {
            RegA = ShiftRightArithmetic(RegA);
        }

        [CbOp(0x28, 8, "SRA B")]
        void SRA_B()
        {
            RegB = ShiftRightArithmetic(RegB);
        }

        [CbOp(0x29, 8, "SRA C")]
        void SRA_C()
        {
            RegC = ShiftRightArithmetic(RegC);
        }

        [CbOp(0x2A, 8, "SRA D")]
        void SRA_D()
        {
            RegD = ShiftRightArithmetic(RegD);
        }

        [CbOp(0x2B, 8, "SRA E")]
        void SRA_E()
        {
            RegE = ShiftRightArithmetic(RegE);
        }

        [CbOp(0x2C, 8, "SRA H")]
        void SRA_H()
        {
            RegH = ShiftRightArithmetic(RegH);
        }

        [CbOp(0x2D, 8, "SRA L")]
        void SRA_L()
        {
            RegL = ShiftRightArithmetic(RegL);
        }

        [CbOp(0x2E, 16, "SRA (HL)")]
        void SRA_HL()
        {
            var value = Mmu.ReadByte(HL);
            var result = ShiftRightArithmetic(value);
            Mmu.WriteByte(HL, result);
        }

        [CbOp(0x3F, 8, "SRL A")]
        void SRL_A()
        {
            RegA = ShiftRight(RegA);
        }

        [CbOp(0x38, 8, "SRL B")]
        void SRL_B()
        {
            RegB = ShiftRight(RegB);
        }

        [CbOp(0x39, 8, "SRL C")]
        void SRL_C()
        {
            RegC = ShiftRight(RegC);
        }

        [CbOp(0x3A, 8, "SRL D")]
        void SRL_D()
        {
            RegD = ShiftRight(RegD);
        }

        [CbOp(0x3B, 8, "SRL E")]
        void SRL_E()
        {
            RegE = ShiftRight(RegE);
        }

        [CbOp(0x3C, 8, "SRL H")]
        void SRL_H()
        {
            RegH = ShiftRight(RegH);
        }

        [CbOp(0x3D, 8, "SRL L")]
        void SRL_L()
        {
            RegL = ShiftRight(RegL);
        }

        [CbOp(0x3E, 16, "SRL (HL)")]
        void SRL_HL()
        {
            var value = Mmu.ReadByte(HL);
            var result = ShiftRight(value);
            Mmu.WriteByte(HL, result);
        }


        #endregion

        #region BIT OPS

        [CbOp(0x47, 8, "BIT {0} A")]
        void BIT_bA()
        {
            var bit = ReadByte();
            TestBit(bit, RegA);
        }

        [CbOp(0x40, 8, "BIT {0} B")]
        void BIT_bB()
        {
            var bit = ReadByte();
            TestBit(bit, RegB);
        }

        [CbOp(0x41, 8, "BIT {0} C")]
        void BIT_bC()
        {
            var bit = ReadByte();
            TestBit(bit, RegC);
        }

        [CbOp(0x42, 8, "BIT {0} D")]
        void BIT_bD()
        {
            var bit = ReadByte();
            TestBit(bit, RegD);
        }

        [CbOp(0x43, 8, "BIT {0} E")]
        void BIT_bE()
        {
            var bit = ReadByte();
            TestBit(bit, RegE);
        }

        [CbOp(0x44, 8, "BIT {0} H")]
        void BIT_bH()
        {
            var bit = ReadByte();
            TestBit(bit, RegH);
        }

        [CbOp(0x45, 8, "BIT {0} L")]
        void BIT_bL()
        {
            var bit = ReadByte();
            TestBit(bit, RegL);
        }

        [CbOp(0x46, 16, "BIT {0} (HL)")]
        void BIT_bHL()
        {
            var value = Mmu.ReadByte(HL);
            var bit = ReadByte();
            TestBit(bit, value);
        }

        [CbOp(0xC7, 8, "SET {0} A")]
        void SET_bA()
        {
            var bit = ReadByte();
            SetBit(bit, RegA);
        }

        [CbOp(0xC0, 8, "SET {0} B")]
        void SET_bB()
        {
            var bit = ReadByte();
            SetBit(bit, RegB);
        }

        [CbOp(0xC1, 8, "SET {0} C ")]
        void SET_bC()
        {
            var bit = ReadByte();
            SetBit(bit, RegC);
        }

        [CbOp(0xC2, 8, "SET {0} D")]
        void SET_bD()
        {
            var bit = ReadByte();
            SetBit(bit, RegD);
        }

        [CbOp(0xC3, 8, "SET {0} E")]
        void SET_bE()
        {
            var bit = ReadByte();
            SetBit(bit, RegE);
        }

        [CbOp(0xC4, 8, "SET {0} H")]
        void SET_bH()
        {
            var bit = ReadByte();
            SetBit(bit, RegH);
        }

        [CbOp(0xC5, 8, "SET {0} L")]
        void SET_bL()
        {
            var bit = ReadByte();
            SetBit(bit, RegL);
        }

        [CbOp(0xC6, 16, "SET {0} (HL)")]
        void SET_bHL()
        {
            var bit = ReadByte();
            var value = Mmu.ReadByte(HL);
            SetBit(bit, value);
        }

        [CbOp(0x87, 8, "RES {0} A")]
        void RES_bA()
        {
            var bit = ReadByte();
            ResetBit(bit, RegA);
        }

        [CbOp(0x80, 8, "RES {0} B")]
        void RES_bB()
        {
            var bit = ReadByte();
            ResetBit(bit, RegB);
        }

        [CbOp(0x81, 8, "RES {0} C ")]
        void RES_bC()
        {
            var bit = ReadByte();
            ResetBit(bit, RegC);
        }

        [CbOp(0x82, 8, "RES {0} D")]
        void RES_bD()
        {
            var bit = ReadByte();
            ResetBit(bit, RegD);
        }

        [CbOp(0x83, 8, "RES {0} E")]
        void RES_bE()
        {
            var bit = ReadByte();
            ResetBit(bit, RegE);
        }

        [CbOp(0x84, 8, "RES {0} H")]
        void RES_bH()
        {
            var bit = ReadByte();
            ResetBit(bit, RegH);
        }

        [CbOp(0x85, 8, "RES {0} L")]
        void RES_bL()
        {
            var bit = ReadByte();
            ResetBit(bit, RegL);
        }

        [CbOp(0x86, 16, "RES {0} (HL)")]
        void RES_bHL()
        {
            var bit = ReadByte();
            var value = Mmu.ReadByte(HL);
            ResetBit(bit, value);
        }

        #endregion

        #region JUMPS

        [Op(0xC3, 12, "JP")]
        void JP()
        {
            var value = ReadWord();
            PC = value;
        }

        [Op(0xC2, 12, "JP NZ")]
        void JP_NZ()
        {
            if (!ZeroFlag)
                JP();
        }

        [Op(0xCA, 12, "JP Z")]
        void JP_Z()
        {
            if (ZeroFlag)
                JP();
        }

        [Op(0xD2, 12, "JP NC")]
        void JP_NC()
        {
            if (!CarryFlag)
                JP();
        }

        [Op(0xDA, 12, "JP C")]
        void JP_C()
        {
            if (CarryFlag)
                JP();
        }

        [Op(0xE9, 4, "JP (HL)")]
        void JP_HL()
        {
            var address = Mmu.ReadWord(HL);
            PC = address;
        }

        [Op(0x18, 8, "JR n")]
        void JR_n()
        {
            var value = ReadByte();
            PC += value;
        }

        [Op(0x20, 12, "JR NZ")]
        void JR_NZ()
        {
            if (!ZeroFlag)
                JR_n();
        }

        [Op(0x28, 12, "JR Z")]
        void JR_Z()
        {
            if (ZeroFlag)
                JR_n();
        }

        [Op(0x30, 12, "JR NC")]
        void JR_NC()
        {
            if (!CarryFlag)
                JR_n();
        }

        [Op(0x38, 12, "JR C")]
        void JR_C()
        {
            if (CarryFlag)
                JR_n();
        }

        #endregion

        #region CALLS

        [Op(0xCD, 12, "CALL")]
        void CALL()
        {
            var address = ReadWord();
            Mmu.WriteWord(SP, PC);
            SP -= 2;
            PC = address;
        }

        [Op(0xC4, 12, "CALL NZ")]
        void CALL_NZ()
        {
            if (!ZeroFlag)
                CALL();
            else
                PC += 2;
        }

        [Op(0xCC, 12, "CALL Z")]
        void CALL_Z()
        {
            if (ZeroFlag)
                CALL();
            else
                PC += 2;
        }

        [Op(0xD4, 12, "CALL NC")]
        void CALL_NC()
        {
            if (!CarryFlag)
                CALL();
            else
                PC += 2;
        }

        [Op(0xDC, 12, "CALL C")]
        void CALL_C()
        {
            if (CarryFlag)
                CALL();
            else
                PC += 2;
        }

        #endregion

        #region RESTART

        [Op(0xC7, 32, "RST 00")]
        void RST_00()
        {
            PushWord(PC);
            PC = 0x00;
        }

        [Op(0xCF, 32, "RST 08")]
        void RST_08()
        {
            PushWord(PC);
            PC = 0x08;
        }

        [Op(0xD7, 32, "RST 10")]
        void RST_10()
        {
            PushWord(PC);
            PC = 0x10;
        }

        [Op(0xDF, 32, "RST 18")]
        void RST_18()
        {
            PushWord(PC);
            PC = 0x18;
        }

        [Op(0xE7, 32, "RST 20")]
        void RST_20()
        {
            PushWord(PC);
            PC = 0x20;
        }

        [Op(0xEF, 32, "RST 28")]
        void RST_28()
        {
            PushWord(PC);
            PC = 0x28;
        }

        [Op(0xF7, 32, "RST 30")]
        void RST_30()
        {
            PushWord(PC);
            PC = 0x30;
        }

        [Op(0xFF, 32, "RST 38")]
        void RST_38()
        {
            PushWord(PC);
            PC = 0x38;
        }

        [Op(0xC9, 8, "RET")]
        void RET()
        {
            var address = PopWord();
            PC = address;
        }

        [Op(0xC0, 8, "RET NZ")]
        void RET_NZ()
        {
            if (!ZeroFlag)
                RET();
        }

        [Op(0xC8, 8, "RET Z")]
        void RET_Z()
        {
            if (ZeroFlag)
                RET();
        }

        [Op(0xD0, 8, "RET NC")]
        void RET_NC()
        {
            if (!CarryFlag)
                RET();
        }

        [Op(0xD8, 8, "RET C")]
        void RET_C()
        {
            if (CarryFlag)
                RET();
        }

        [Op(0xD9, 8, "RETI")]
        void RETI()
        {
            var address = PopWord();
            PC = address;
            EnableInterupt = true;
        }

        #endregion

        #endregion

    }
}
