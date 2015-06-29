using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameboyEmulator
{
    public class SpriteObjData
    {
        public int Y { get; set; }
        public int X { get; set; }
        public int Tile { get; set; }
        /// <summary>
        /// Palette 0 when false, palette 1 when true.
        /// </summary>
        public bool Palette { get; set; }
        /// <summary>
        /// Flipped when true.
        /// </summary>
        public bool XFlip { get; set; }
        /// <summary>
        /// Flipped when true.
        /// </summary>
        public bool YFlip { get; set; }
        /// <summary>
        /// Below background when true.
        /// </summary>
        public bool Prio { get; set; }
        public int Num { get; set; }
        
    }
}

