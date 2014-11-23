using PCLStorage;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameboyEmulator
{
    public class Mmu
    {
        public bool InBios { get; private set; }

        byte[] Bios = {
            0x31, 0xFE, 0xFF, 0xAF, 0x21, 0xFF, 0x9F, 0x32, 0xCB, 0x7C, 0x20, 0xFB, 0x21, 0x26, 0xFF, 0x0E,
            0x11, 0x3E, 0x80, 0x32, 0xE2, 0x0C, 0x3E, 0xF3, 0xE2, 0x32, 0x3E, 0x77, 0x77, 0x3E, 0xFC, 0xE0,
            0x47, 0x11, 0x04, 0x01, 0x21, 0x10, 0x80, 0x1A, 0xCD, 0x95, 0x00, 0xCD, 0x96, 0x00, 0x13, 0x7B,
            0xFE, 0x34, 0x20, 0xF3, 0x11, 0xD8, 0x00, 0x06, 0x08, 0x1A, 0x13, 0x22, 0x23, 0x05, 0x20, 0xF9,
            0x3E, 0x19, 0xEA, 0x10, 0x99, 0x21, 0x2F, 0x99, 0x0E, 0x0C, 0x3D, 0x28, 0x08, 0x32, 0x0D, 0x20,
            0xF9, 0x2E, 0x0F, 0x18, 0xF3, 0x67, 0x3E, 0x64, 0x57, 0xE0, 0x42, 0x3E, 0x91, 0xE0, 0x40, 0x04,
            0x1E, 0x02, 0x0E, 0x0C, 0xF0, 0x44, 0xFE, 0x90, 0x20, 0xFA, 0x0D, 0x20, 0xF7, 0x1D, 0x20, 0xF2,
            0x0E, 0x13, 0x24, 0x7C, 0x1E, 0x83, 0xFE, 0x62, 0x28, 0x06, 0x1E, 0xC1, 0xFE, 0x64, 0x20, 0x06,
            0x7B, 0xE2, 0x0C, 0x3E, 0x87, 0xF2, 0xF0, 0x42, 0x90, 0xE0, 0x42, 0x15, 0x20, 0xD2, 0x05, 0x20,
            0x4F, 0x16, 0x20, 0x18, 0xCB, 0x4F, 0x06, 0x04, 0xC5, 0xCB, 0x11, 0x17, 0xC1, 0xCB, 0x11, 0x17,
            0x05, 0x20, 0xF5, 0x22, 0x23, 0x22, 0x23, 0xC9, 0xCE, 0xED, 0x66, 0x66, 0xCC, 0x0D, 0x00, 0x0B,
            0x03, 0x73, 0x00, 0x83, 0x00, 0x0C, 0x00, 0x0D, 0x00, 0x08, 0x11, 0x1F, 0x88, 0x89, 0x00, 0x0E,
            0xDC, 0xCC, 0x6E, 0xE6, 0xDD, 0xDD, 0xD9, 0x99, 0xBB, 0xBB, 0x67, 0x63, 0x6E, 0x0E, 0xEC, 0xCC,
            0xDD, 0xDC, 0x99, 0x9F, 0xBB, 0xB9, 0x33, 0x3E, 0x3c, 0x42, 0xB9, 0xA5, 0xB9, 0xA5, 0x42, 0x4C,
            0x21, 0x04, 0x01, 0x11, 0xA8, 0x00, 0x1A, 0x13, 0xBE, 0x20, 0xFE, 0x23, 0x7D, 0xFE, 0x34, 0x20,
            0xF5, 0x06, 0x19, 0x78, 0x86, 0x23, 0x05, 0x20, 0xFB, 0x86, 0x20, 0xFE, 0x3E, 0x01, 0xE0, 0x50
        };

        public byte[] Rom { get; private set; }
        public byte[] Vram { get; private set; }
        public byte[] CartRam { get; private set; }
        public byte[] Ram { get; private set; }
        public byte[] Zram { get; private set; }

        public Mmu()
        {
            InBios = true;
            Vram = new byte[0x2000];
            CartRam = new byte[0x2000];
            Ram = new byte[0x8000];
            Zram = new byte[0x80];
            
        }

        public async void ReadRom(string filePath)
        {
            var file = await FileSystem.Current.GetFileFromPathAsync(filePath);
            using (var stream = await file.OpenAsync(FileAccess.Read))
            using (var memoryStream = new MemoryStream())
            {
                await stream.CopyToAsync(memoryStream);
                Rom = memoryStream.ToArray();
            }
        }

        /// <summary>
        /// Changes array to the memory zone being mapped.
        /// Returns the element index the address referred to.
        /// </summary>
        /// <param name="array"></param>
        /// <returns></returns>
        int resolve(ushort address, ref byte[] array)
        {
            if (Rom == null)
                throw new Exception("No ROM has been loaded.");

            int result = address;
            // Game ROM and bios. This is needs more work for bank switching.
            if (address < 0x8000)
            {
                if (address < 0x00FF && InBios)
                {
                    array = Bios;
                }
                else
                {
                    InBios = false;
                    array = Rom;
                }
            }
            // Graphics RAM
            else if (address >= 0x8000 && address < 0xA000)
            {
                array = Vram;
                result = address - 0x8000;
            }
            // Cartridge RAM. Some carts may have extra RAM that may also be battery backed.
            else if (address >= 0xA000 && address < 0xC000)
            {
                array = CartRam;
                result = address - 0xA000;
            }
            // Main working RAM.
            else if (address >= 0xC000 && address < 0xE000)
            {
                array = Ram;
                result = address - 0xC000;
            }
            // Shadow RAM.
            else if (address >= 0xE000 && address < 0xFE00)
            {
                array = Ram;
                result = address - 0xE000;
            }
            // Zero page RAM. Faster acting RAM.
            else if (address >= 0xFF80 && address <= 0xFFFF)
            {
                array = Zram;
                result = address - 0xFF80;
            }
            else
            {
                throw new Exception("Unimplemented address accessed.");
            }

            return result;
        }

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
