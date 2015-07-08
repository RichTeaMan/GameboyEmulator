using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameboyEmulator
{
    public class Gpu
    {
        public Cpu Cpu { get; private set; }
        public byte[] Vram { get; private set; }

        public byte Mode { get; private set; } = 2;
        public int ModeClock { get; private set; }
        public byte[][][] Tileset { get; private set; }
        
        public bool BgSwitch { get; private set; }
        public bool ObjSwitch { get; private set; }
        public bool _winon { get; private set; }
        public bool LcdSwitch { get; private set; }

        public Palette Palette { get; private set; }

        public byte[] _oam { get; private set; }
        public SpriteObjData[] _objdata { get; private set; }
        public SpriteObjData[] _objdatasorted { get; private set; }

        public delegate void DrawEventHandler(Gpu sender, byte[] screenData, EventArgs e);
        public event DrawEventHandler DrawEvent;

        public Gpu(Cpu cpu)
        {
            Cpu = cpu;
            Vram = Cpu.Mmu.Vram;
            Palette = new Palette();
            
            Reset();
        }

        byte _curline = 0;
        int _curscan = 0;
        byte _raster = 0;

        byte _yscrl= 0;
        byte _xscrl = 0;
        byte _winy = 0;
        byte _winx = 0;

        int _objsize = 0;

        int _bgtilebase = 0x0000;
        int _bgmapbase = 0x1800;
        int _winmapbase = 0x1800;

        byte[] _scanrow;

        byte[] _scrndata;

        byte ints = 0;
        byte intfired = 0;

        public void Step(int cycles)
        {
            ModeClock += cycles;
            switch (Mode)
            {
                // In hblank
                case 0:
                    if (ModeClock >= 51)
                    {
                        // End of hblank for last scanline; render screen
                        if (_curline == 143)
                        {
                            Mode = 1;
                            if (DrawEvent != null)
                            {
                                DrawEvent.Invoke(this, _scrndata, new EventArgs());
                            }
                            Cpu.Mmu.VblankIntFlag = true;
                            if ((ints & 2) > 0)
                            {
                                intfired |= 2;
                                Cpu.Mmu.LcdStatIntFlag = true;
                            }
                        }
                        else
                        {
                            Mode = 2;
                            if ((ints & 4)  > 0)
                            {
                                intfired |= 4;
                                Cpu.Mmu.LcdStatIntFlag = true;
                            }
                        }
                        _curline++;
                        if (_curline == _raster)
                        {
                            if ((ints & 8) > 0)
                            {
                                intfired |= 8;
                                Cpu.Mmu.LcdStatIntFlag = true;
                            }
                        }
                        _curscan += 640;
                        ModeClock = 0;
                    }
                    break;

                // In vblank
                case 1:
                    if (ModeClock >= 114)
                    {
                        ModeClock = 0;
                        _curline++;
                        if (_curline > 153)
                        {
                            _curline = 0;
                            _curscan = 0;
                            Mode = 2;
                        }
                        if ((ints & 4) > 0)
                        {
                            intfired |= 4;
                            Cpu.Mmu.LcdStatIntFlag = true;
                        }
                    }
                    break;

                // In OAM-read mode
                case 2:
                    if (ModeClock >= 20)
                    {
                        ModeClock = 0;
                        Mode = 3;
                    }
                    break;

                // In VRAM-read mode
                case 3:
                    // Render scanline at end of allotted time
                    if (ModeClock >= 43)
                    {
                        ModeClock = 0;
                        Mode = 0;
                        if ((ints & 1) > 0)
                        {
                            intfired |= 1;
                            Cpu.Mmu.LcdStatIntFlag = true;
                        }

                        if (LcdSwitch)
                        {
                            if (BgSwitch)
                            {
                                var linebase = _curscan;
                                var mapbase = _bgmapbase + ((((_curline + _yscrl) & 255) >> 3) << 5);
                                var y = (_curline + _yscrl) & 7;
                                var x = _xscrl & 7;
                                var t = (_xscrl >> 3) & 31;
                                var w = 160;

                                if (BgSwitch)
                                {
                                    int tile = Vram[mapbase + t];
                                    //if (tile < 128) tile = 256 + tile;
                                    var tilerow = Tileset[tile][y];
                                    do
                                    {
                                        _scanrow[159 - x] = tilerow[x];
                                        var b = Palette.bg[tilerow[x]];
                                        _scrndata[linebase + 3] = b;
                                        x++;
                                        if (x == 8)
                                        {
                                            t = (t + 1) & 31;
                                            x = 0;
                                            tile = Vram[mapbase + t];
                                            //if (tile < 128) tile = 256 + tile;
                                            tilerow = Tileset[tile][y];
                                        }
                                        linebase += 4;
                                    } while (--w > 0);
                                }
                                else
                                {
                                    var tilerow = Tileset[Vram[mapbase + t]][y];
                                    do
                                    {
                                        _scanrow[159 - x] = tilerow[x];
                                        var b = Palette.bg[tilerow[x]];
                                        _scrndata[linebase + 3] = b;
                                        x++;
                                        if (x == 8) { t = (t + 1) & 31; x = 0; tilerow = Tileset[Vram[mapbase + t]][y]; }
                                        linebase += 4;
                                    } while (--w > 0);
                                }
                            }
                            if (ObjSwitch)
                            {
                                var cnt = 0;
                                if (_objsize > 0)
                                {
                                    for (var i = 0; i < 40; i++)
                                    {
                                    }
                                }
                                else
                                {
                                    var linebase = _curscan;
                                    for (var i = 0; i < 40; i++)
                                    {
                                        var obj = _objdatasorted[i];
                                        if (obj.Y <= _curline && (obj.Y + 8) > _curline)
                                        {
                                            byte[] tilerow;
                                            if (obj.YFlip)
                                                tilerow = Tileset[obj.Tile][7 - (_curline - obj.Y)];
                                            else
                                                tilerow = Tileset[obj.Tile][_curline - obj.Y];

                                            byte[] pal;
                                            if (obj.Palette)
                                            {
                                                pal = Palette.obj1;
                                            }
                                            else
                                            {
                                                pal = Palette.obj0;
                                            }

                                            linebase = (_curline * 160 + obj.X) * 4;
                                            if (obj.XFlip)
                                            {
                                                for (int x = 0; x < 8; x++)
                                                {
                                                    if (obj.X + x >= 0 && obj.X + x < 160)
                                                    {
                                                        if (tilerow[7 - x] > 0 && (obj.Prio || _scanrow[x] == 0))
                                                        {
                                                            _scrndata[linebase + 3] = pal[tilerow[7 - x]];
                                                        }
                                                    }
                                                    linebase += 4;
                                                }
                                            }
                                            else
                                            {
                                                for (int x = 0; x < 8; x++)
                                                {
                                                    if (obj.X + x >= 0 && obj.X + x < 160)
                                                    {
                                                        if (tilerow[x] > 0 && (obj.Prio || _scanrow[x] == 0))
                                                        {
                                                            _scrndata[linebase + 3] = pal[tilerow[x]];
                                                        }
                                                    }
                                                    linebase += 4;
                                                }
                                            }
                                            cnt++; if (cnt > 10) break;
                                        }
                                    }
                                }
                            }
                        }
                    }
                    break;
            }
        }

        private void Reset()
        {
            _scanrow = new byte[160];
            _scrndata = new byte[160 * 144 * 4];

            for(int i = 0; i < _scrndata.Length; i++)
            {
                _scrndata[i] = 255;
            }

            if (DrawEvent != null)
            {
                DrawEvent.Invoke(this, _scrndata, new EventArgs());
            }

            for (int i = 0; i < 4; i++)
            {
                Palette.bg[i] = 255;
                Palette.obj0[i] = 255;
                Palette.obj1[i] = 255;
            }

            Tileset = new byte[512][][];
            for (var i = 0; i < 512; i++)
            {
                Tileset[i] = new byte[8][];
                for (var j = 0; j < 8; j++)
                {
                    Tileset[i][j] = new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 };
                }
            }

            _oam = new byte[40 * 4];
            _objdata = new SpriteObjData[40];
            for (int i = 0, n = 0; i < 40; i++, n += 4)
            {
                _oam[n + 0] = 0;
                _oam[n + 1] = 0;
                _oam[n + 2] = 0;
                _oam[n + 3] = 0;
                _objdata[i] = new SpriteObjData()
                {
                    Y = -16,
                    X = -8,
                    Tile = 0,
                    Palette = false,
                    XFlip = false,
                    YFlip = false,
                    Prio = false,
                    Num = i
                };
            }
        }

        public void buildobjdata(ushort addr, byte val)
        {
            var obj = addr >> 2;
            if (obj < 40)
            {
                switch (addr & 3)
                {
                    // Y-coordinate
                    case 0: _objdata[obj].Y = val - 16; break;

                    // X-coordinate
                    case 1: _objdata[obj].X = val - 8; break;

                    // Data tile
                    case 2: _objdata[obj].Tile = val; break;

                    // Options
                    case 3:
                        _objdata[obj].Palette = (val & 0x10) > 0;
                        _objdata[obj].XFlip = (val & 0x20) > 0;
                        _objdata[obj].YFlip = (val & 0x40) > 0;
                        _objdata[obj].Prio = (val & 0x80) > 0;
                        break;
                }
            }
        }

        private void UpdateOam(ushort addr, byte val)
        {
            addr -= 0xFE00;
            var obj = addr >> 2;
            if (obj < 40)
            {
                switch (addr & 3)
                {
                    case 0: _objdata[obj].Y = val - 16; break;
                    case 1: _objdata[obj].X = val - 8; break;
                    case 2:
                        if (_objsize > 0)
                        {
                            _objdata[obj].Tile = (val & 0xFE);
                        }
                        else
                        {
                            _objdata[obj].Tile = val;
                        }
                        break;
                    case 3:
                        _objdata[obj].Palette = (val & 0x10) > 0;
                        _objdata[obj].XFlip = (val & 0x20) > 0;
                        _objdata[obj].YFlip = (val & 0x40) > 0;
                        _objdata[obj].Prio = (val & 0x80) > 0;
                        break;
                }
            }
            _objdatasorted = _objdata.OrderByDescending(o => o.X).ThenByDescending(o => o.Num).ToArray();
            // not exactly equivalent
            //_objdatasorted.sort(function(a, b){
            //    if (a.x > b.x) return -1;
            //    if (a.num > b.num) return -1;
            //});
        }

        public void UpdateTile(int addr, byte val)
        {
            var saddr = addr;
            if ((addr & 1) > 0)
            {
                saddr--;
                addr--;
            }
            var tile = (addr >> 4) & 511;
            var y = (addr >> 1) & 7;
            for (var x = 0; x < 8; x++)
            {
                var sx = 1 << (7 - x);
                Tileset[tile][y][x] = (byte)(((Vram[saddr] & sx) > 0 ? 1 : 0) | ((Vram[saddr + 1] & sx) > 0 ? 2 : 0));
            }
        }

        public byte Read(int addr)
        {
            switch (addr)
            {
                // LCD Control
                case 0xFF40:
                    var r = ( LcdSwitch ? 0x80 : 0) |
                   ((_winmapbase == 0x1C00) ? 0x40 : 0) |
                   (_winon ? 0x20 : 0) |
                    ((_bgtilebase == 0x0000) ? 0x10 : 0) |
                    ((_bgmapbase == 0x1C00) ? 0x08 : 0) |
                    (_objsize > 0 ? 0x04 : 0) |
                    (ObjSwitch ? 0x02 : 0) |
                    (BgSwitch ? 0x01 : 0);
                    return (byte)r;

                case 0xFF41:
                    var intf = intfired;
                    intfired = 0;
                    return (byte)((intf << 3) | (_curline == _raster ? 4 : 0) | Mode);

                // Scroll Y
                case 0xFF42:
                    return _yscrl;

                // Scroll X
                case 0xFF43:
                    return _xscrl;

                // Current scanline
                case 0xFF44:
                    return _curline;

                case 0xFF45:
                    return _raster;

                case 0xFF4A:
                    return _winy;

                case 0xFF4B:
                    return (byte)(_winx + 7);

                default:
                    Debug.WriteLine("Unknown address read from GPU: {0:X4}", addr);
                    return 0;
            }
        }

        public void Write(int addr, byte val)
        {
            switch (addr)
            {
                // LCD Control
                case 0xFF40:
                    BgSwitch = val.IsBitSet(0);
                    ObjSwitch = val.IsBitSet(1);
                    _objsize = val.IsBitSet(2) ? 1 : 0;
                    _bgmapbase = val.IsBitSet(3) ? 0x1C00 : 0x1800;
                    _bgtilebase = val.IsBitSet(4) ? 0x0000 : 0x0800;
                    _winon = val.IsBitSet(5);
                    _winmapbase = val.IsBitSet(6) ? 0x1C00 : 0x1800;
                    LcdSwitch = val.IsBitSet(7);
                    break;

                case 0xFF41:
                    ints = (byte)((val >> 3) & 15);
                    break;

                // Scroll Y
                case 0xFF42:
                    _yscrl = val;
                    break;

                // Scroll X
                case 0xFF43:
                    _xscrl = val;
                    break;

                case 0xFF45:
                    _raster = val;
                    break;
                    
                // OAM DMA
                case 0xFF46:
                    for (var i = 0; i < 160; i++)
                    {
                        var v = Cpu.Mmu.ReadByte((val << 8) + i);
                        _oam[i] = v;
                        UpdateOam((ushort)(0xFE00 + i), v);
                    }
                    break;

                // Background palette
                case 0xFF47:
                    for (var i = 0; i < 4; i++)
                    {
                        switch ((val >> (i * 2)) & 3)
                        {
                            case 0: Palette.bg[i] = 255; break;
                            case 1: Palette.bg[i] = 192; break;
                            case 2: Palette.bg[i] = 96; break;
                            case 3: Palette.bg[i] = 0; break;
                        }
                    }
                    break;

                // Object palettes
                case 0xFF48:
                    for (var i = 0; i < 4; i++)
                    {
                        switch ((val >> (i * 2)) & 3)
                        {
                            case 0: Palette.obj0[i] = 255; break;
                            case 1: Palette.obj0[i] = 192; break;
                            case 2: Palette.obj0[i] = 96; break;
                            case 3: Palette.obj0[i] = 0; break;
                        }
                    }
                    break;

                case 0xFF49:
                    for (var i = 0; i < 4; i++)
                    {
                        switch ((val >> (i * 2)) & 3)
                        {
                            case 0: Palette.obj1[i] = 255; break;
                            case 1: Palette.obj1[i] = 192; break;
                            case 2: Palette.obj1[i] = 96; break;
                            case 3: Palette.obj1[i] = 0; break;
                        }
                    }
                    break;

                case 0xFF4A:
                    _winy = val;
                    break;

                case 0xFF4B:
                    _winx = (byte)(val - 7);
                    break;

                default:
                    Debug.WriteLine("Unknown address written tp GPU: {0:X4} - {1:X2}", addr, val);
                    break;
            }
        }

    }
}
