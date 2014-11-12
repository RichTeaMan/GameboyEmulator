using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GameboyEmulator
{
    public class Cpu
    {
        const ushort P1 = 0xFF00;

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
    
        public Cpu()
        {
            PC = 0;
            SP = 0;
        }

        #region Methods

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
            var value = a + b;
            CarryFlag = false;
            while (value > byte.MaxValue)
            {
                CarryFlag = true;
                value -= byte.MaxValue;
            }

            // calculate half carry
            var lowNibbles = a.LowNibble() + b.LowNibble();
            HalfCarryFlag = lowNibbles > 15;
            return (byte)value;
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
                value -= ushort.MaxValue;
            }

            // calculate half carry
            // this is almost definitely wrong
            HalfCarryFlag = value > 2048;
            return (ushort)value;
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

        [Op(0x16, 8, "LD H {0}")]
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
            RegD =  RegB;
        }

        [Op(0x51, 4, "LD D C")]
        void LD_DC()
        {
            RegD =  RegC;
        }

        [Op(0x52, 4, "LD D D")]
        void LD_DD()
        {
            RegD =  RegD;
        }

        [Op(0x53, 4, "LD D E")]
        void LD_DE()
        {
            RegD =  RegE;
        }

        [Op(0x54, 4, "LD D H")]
        void LD_DH()
        {
            RegD =  RegH;
        }

        [Op(0x55, 4, "LD D L")]
        void LD_DL()
        {
            RegD =  RegL;
        }

        [Op(0x56, 8, "LD D HL")]
        void LD_DHL()
        {
            var value = Mmu.ReadByte(HL);
            RegD =  value;
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

        [Op(0x21, 12, "LD SP nn")]
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

        #endregion

    }
}
