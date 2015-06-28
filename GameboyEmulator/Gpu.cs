﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameboyEmulator
{
    public class Gpu
    {
        public Cpu Cpu { get; private set; }
        public byte[] Vram { get; private set; }

        public int Mode { get; private set; }
        public int ModeClock { get; private set; }
        public byte Line { get; private set; }
        public byte[][][] Tileset { get; private set; }

        public Pixel[] Screen { get; set; }
        public bool BgSwitch { get; private set; }
        public bool BgMap { get; private set; }
        public bool BgTile { get; private set; }
        public bool LcdSwitch { get; private set; }

        public byte ScreenY { get; private set; }
        public byte ScreenX { get; private set; }

        public byte[][] Palette { get; private set; }

        public delegate void DrawEventHandler(Gpu sender, Pixel[] pixels, EventArgs e);
        public event DrawEventHandler DrawEvent;

        public Gpu(Cpu cpu)
        {
            Cpu = cpu;
            Vram = Cpu.Mmu.Vram;
            Palette = new byte[4][];

            Reset();
        }

        public void Step()
        {

            ModeClock += Cpu.Timer;

            switch (Mode)
            {
                // OAM read mode, scanline active
                case 2:
                    if (ModeClock >= 80)
                    {
                        // Enter scanline mode 3
                        ModeClock = 0;
                        Mode = 3;
                    }
                    break;

                // VRAM read mode, scanline active
                // Treat end of mode 3 as end of scanline
                case 3:
                    if (ModeClock >= 172)
                    {
                        // Enter hblank
                        ModeClock = 0;
                        Mode = 0;

                        // Write a scanline to the framebuffer
                        RenderScan();
                    }
                    break;

                // Hblank
                // After the last hblank, push the screen data to canvas
                case 0:
                    if (ModeClock >= 204)
                    {
                        ModeClock = 0;
                        Line++;

                        if (Line == 143)
                        {
                            // Enter vblank
                            Mode = 1;
                            // draw
                            //GPU._canvas.putImageData(GPU._scrn, 0, 0);
                        }
                        else
                        {
                            Mode = 2;
                        }
                    }
                    break;

                // Vblank (10 lines)
                case 1:
                    if (ModeClock >= 456)
                    {
                        ModeClock = 0;
                        Line++;

                        if (Line > 153)
                        {
                            // Restart scanning modes
                            Mode = 2;
                            Line = 0;
                        }
                    }
                    break;
            }
        }

        private void RenderScan()
        {
            // VRAM offset for the tile map
            var mapoffs = BgMap ? 0x1C00 : 0x1800;

            // Which line of tiles to use in the map
            mapoffs += ((Line + ScreenY) & 255) >> 3;

            // Which tile to start with in the map line
            var lineoffs = (ScreenX >> 3);

            // Which line of pixels to use in the tiles
            var y = (Line + ScreenY) & 7;

            // Where in the tileline to start
            var x = ScreenX & 7;

            // Where to render on the canvas
            var canvasoffs = Line * 160;

            // Read tile index from the background map
            var tile = Vram[mapoffs + lineoffs];

            // If the tile data set in use is #1, the
            // indices are signed; calculate a real tile offset
            // huh?
            // if (_bgtile && tile < 128) tile += 256;

            for (var i = 0; i < 160; i++)
            {
                // Re-map the tile pixel through the palette
                var colour = Palette[Tileset[tile][y][x]];

                // Plot the pixel to canvas
                var pixel = Screen[canvasoffs];
                pixel.R = colour[0];
                pixel.G = colour[1];
                pixel.B = colour[2];
                pixel.A = colour[3];
                canvasoffs++;

                // When this tile ends, read another
                x++;
                if (x == 8)
                {
                    x = 0;
                    lineoffs = (lineoffs + 1) & 31;
                    tile = Vram[mapoffs + lineoffs];
                    // huh?
                    // if (_bgtile && tile < 128) tile += 256;
                }
            }
        }



        private void Reset()
        {
            //         var c = document.getElementById('screen');
            //         if (c && c.getContext)
            //         {
            //             GPU._canvas = c.getContext('2d');
            //             if (GPU._canvas)
            //             {
            //                 if (GPU._canvas.createImageData)
            //                     GPU._scrn = GPU._canvas.createImageData(160, 144);

            //                 else if (GPU._canvas.getImageData)
            //                     GPU._scrn = GPU._canvas.getImageData(0, 0, 160, 144);

            //                 else
            //                     GPU._scrn = {
            //                     'width': 160,
            //'height': 144,
            //'data': new Array(160 * 144 * 4)
            //                     };

            //                 // Initialise canvas to white
            //                 for (var i = 0; i < 160 * 144 * 4; i++)
            //                     GPU._scrn.data[i] = 255;

            //                 GPU._canvas.putImageData(GPU._scrn, 0, 0);
            //             }
            //         }

            Screen = new Pixel[160 * 144];
            for(int i = 0; i < 160 * 144; i++)
            {
                Screen[i] = new Pixel();
            }
            if (DrawEvent != null)
                DrawEvent.Invoke(this, Screen, new EventArgs());

            Tileset = new byte[384][][];
            for (var i = 0; i < 384; i++)
            {
                Tileset[i] = new byte[8][];
                for (var j = 0; j < 8; j++)
                {
                    Tileset[i][j] = new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 };
                }
            }
        }

        // Takes a value written to VRAM, and updates the
        // internal tile data set
        public void UpdateTile(int addr, byte val)
        {
            // Get the "base address" for this tile row
            addr &= 0x1FFE;

            // Work out which tile and row was updated
            var tile = (addr >> 4) & 511;
            var y = (addr >> 1) & 7;

            // may not be good
            if(tile >= 384)
            {
                return;
            }
            int sx;
            for (var x = 0; x < 8; x++)
            {
                // Find bit index for this pixel
                sx = 1 << (7 - x);

                var v = (((Vram[addr] & sx) == 1) ? 1 : 0) + (((Vram[addr + 1] & sx) == 1) ? 2 : 0);
                // Update tile set
                Tileset[tile][y][x] = (byte)v;
            }
        }

        public byte Read(int addr)
        {
            switch (addr)
            {
                // LCD Control
                case 0xFF40:
                    var r = (BgSwitch ? 0x01 : 0x00) |
                       (BgMap ? 0x08 : 0x00) |
                       (BgTile ? 0x10 : 0x00) |
                       (LcdSwitch ? 0x80 : 0x00);
                    return (byte)r;

                // Scroll Y
                case 0xFF42:
                    return ScreenY;

                // Scroll X
                case 0xFF43:
                    return ScreenX;

                // Current scanline
                case 0xFF44:
                    return Line;

                default:
                    throw new Exception("Unknown address.");
            }
        }

        public void Write(int addr, byte val)
        {
            switch (addr)
            {
                // LCD Control
                case 0xFF40:
                    BgSwitch = (val & 0x01) == 1;
                    BgMap = (val & 0x08) == 1;
                    BgTile = (val & 0x10) == 1;
                    LcdSwitch = (val & 0x80) == 1;
                    break;

                // Scroll Y
                case 0xFF42:
                    ScreenY = val;
                    break;

                // Scroll X
                case 0xFF43:
                    ScreenX = val;
                    break;

                // Background palette
                case 0xFF47:
                    for (var i = 0; i < 4; i++)
                    {
                        switch ((val >> (i * 2)) & 3)
                        {
                            case 0: Palette[i] = new byte[] { 255, 255, 255, 255 }; break;
                            case 1: Palette[i] = new byte[] { 192, 192, 192, 255 }; break;
                            case 2: Palette[i] = new byte[] { 96, 96, 96, 255 }; break;
                            case 3: Palette[i] = new byte[] { 0, 0, 0, 255 }; break;
                        }
                    }
                    break;
            }
        }

    }
}
