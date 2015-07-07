using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameboyEmulator
{
    public class Palette
    {
        public byte[] bg { get; set; }
        public byte[] obj0 { get; set; }
        public byte[] obj1 { get; set; }

        public Palette()
        {
            bg = new byte[4];
            obj0 = new byte[4];
            obj1 = new byte[4]; 
        }
    }
}
