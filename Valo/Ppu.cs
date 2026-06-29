using System.Diagnostics;
using System.Drawing;
using System.Runtime.CompilerServices;

namespace Valo;

public sealed class Ppu(ILcd lcd, InterruptRequester interrupt)
{
    public ILcd Lcd { get; } = lcd;
    public Ram Vram { get; } = new(new byte[0xA000 - 0x8000]);
    public Ram Oam { get; } = new(new byte[0xFEA0 - 0xFE00]);

    private PpuMode _mode = PpuMode.OamRead;
    public PpuMode Mode {
        get => _mode;
        set {
            var prev = _mode;
            _mode = value;

            interrupt.RequestIf(
                _mode != prev && (
                    (Interrupts.HasFlag(PpuInterrupts.HBlank) && _mode == PpuMode.HBlank) ||
                    (Interrupts.HasFlag(PpuInterrupts.VBlank) && _mode == PpuMode.VBlank) ||
                    (Interrupts.HasFlag(PpuInterrupts.OamRead) && _mode == PpuMode.OamRead)
                )
            );
        }
    }

    public PpuControl Control { get; set; }
    public PpuInterrupts Interrupts { get; set; }

    public uint CurrentDot { get; set; }

    private byte _currentLine;
    public byte CurrentLine {
        get => _currentLine;
        set {
            _currentLine = value;

            if (IsLineOnTarget) {
                interrupt.RequestIf(Interrupts.HasFlag(PpuInterrupts.LineOnTarget));
            }
        }
    }

    public byte LineTarget { get; set; }
    public bool IsLineOnTarget => CurrentLine == LineTarget;

    public Point ScrollPos { get; set; }

    public Palette BgPalette { get; set; }
    public Palette Obj0Palette { get; set; }
    public Palette Obj1Palette { get; set; }

    private PpuRenderer _renderer;

    private static class Dots
    {
        public const uint Disabled = 4;
        public const uint OamRead  = 80;
        public const uint Max      = 456;
    }

    private static class Lines
    {
        public const uint VBlank = 144;
        public const uint Max    = 154;
    }

    public uint Step()
    {
        if (!Control.HasFlag(PpuControl.Enable)) {
            CurrentDot = 0;
            _currentLine = 0;
            _mode = PpuMode.HBlank;

            return Dots.Disabled;
        }

        if (CurrentDot == 0 && CurrentLine == 0) {
            Mode = PpuMode.OamRead;
        }

        return Mode switch {
            PpuMode.OamRead => OamRead(),
            PpuMode.Render  => Render(),
            PpuMode.HBlank  => HBlank(),
            PpuMode.VBlank  => VBlank(),
        };
    }

    private uint OamRead()
    {
        Debug.Assert(CurrentDot == 0);

        CurrentDot = Dots.OamRead;
        Mode = PpuMode.Render;
        _renderer = new PpuRenderer(this);

        return Dots.OamRead;
    }

    private uint Render()
    {
        var dots = _renderer.Step(out var done);
        if (done) {
            Mode = PpuMode.HBlank;
        }

        CurrentDot += dots;
        return dots;
    }

    private struct PpuRenderer(Ppu ppu)
    {
        private Point _currentPixel = new(0, ppu.CurrentLine);
        private Queue<byte> _bgFifo = [];

        private int _currentTileX = 0;
        private int _fetchStep = 0;
        private byte _fetchedRowLow = 0;
        private byte _fetchedRowHigh = 0;
        private int _toDiscard = 0;

        public uint Step(out bool done)
        {
            done = false;

            var cycles = Fetch();
            Debug.Assert(cycles > 0);

            if (_currentPixel.X == 0) {
                _toDiscard = ppu.ScrollPos.X % 8;
            }

            for (var i = 0; i < cycles; ++i) {
                if (_bgFifo.TryDequeue(out var colour)) {
                    if (_toDiscard > 0) {
                        --_toDiscard;
                        continue;
                    }

                    var shade =
                        ppu.Control.HasFlag(PpuControl.BgEnable)
                            ? ppu.BgPalette[colour]
                            : Shade.White;

                    ppu.Lcd.Poke(_currentPixel, shade);
                    ++_currentPixel.X;

                    if (_currentPixel.X == 160) {
                        done = true;
                        break;
                    }
                }
            }

            return cycles;
        }

        private uint Fetch()
        {
            switch (_fetchStep) {
                case 0: {
                    // Fetch tile
                    var tilemap = ppu.Control.HasFlag(PpuControl.BgMap) ? 0x1C00 : 0x1800;
                    var tileX = (ppu.ScrollPos.X / 8 + _currentTileX) & 0x1F;
                    var tileY = (ppu.CurrentLine + ppu.ScrollPos.Y) & 0xFF;

                    var address = tilemap + (32 * (tileY / 8) + tileX);
                    var id = ppu.Vram.Read((ushort)address);

                    var addressMode = ppu.Control.HasFlag(PpuControl.Blocks);
                    var tileAddress = addressMode ? 0x0000 : 0x1000;

                    var tileLow =
                        addressMode
                            ? tileAddress + 16 * id
                            : tileAddress + 16 * (sbyte)id;
                    tileLow += 2 * (tileY & 7);

                    var tileHigh = tileLow + 1;

                    _fetchedRowLow = ppu.Vram.Read((ushort)tileLow);
                    _fetchedRowHigh = ppu.Vram.Read((ushort)tileHigh);

                    if (TryEnqueue(_fetchedRowHigh, _fetchedRowLow)) {
                        ++_currentTileX;
                        _fetchStep = 0;
                        return 8;
                    }
                    else {
                        _fetchStep = 1;
                        return 6;
                    }
                }

                case 1: {
                    // Push row
                    if (TryEnqueue(_fetchedRowHigh, _fetchedRowLow)) {
                        ++_currentTileX;
                        _fetchStep = 0;
                    }

                    return 2;
                }

                default: throw new UnreachableException($"Invalid fetch step {_fetchStep}");
            }
        }

        private bool TryEnqueue(byte rowHigh, byte rowLow)
        {
            if (_bgFifo.Count == 0) {
                for (var i = 0; i < 8; ++i) {
                    var partHigh = rowHigh >> 7;
                    rowHigh <<= 1;
                    var partLow = rowLow >> 7;
                    rowLow <<= 1;

                    _bgFifo.Enqueue((byte)((partHigh << 1) | partLow));
                }

                return true;
            }
            else {
                return false;
            }
        }
    }

    private uint HBlank()
    {
        Debug.Assert(CurrentDot < Dots.Max);

        var cycleCount = Dots.Max - CurrentDot;

        CurrentDot = 0;
        ++CurrentLine;

        if (CurrentLine >= Lines.VBlank) {
            Mode = PpuMode.VBlank;
            Lcd.OnVBlank();
        }
        else {
            Mode = PpuMode.OamRead;
        }

        return cycleCount;
    }

    private uint VBlank()
    {
        Debug.Assert((uint)CurrentLine is >= Lines.VBlank and < Lines.Max);

        CurrentDot = 0;
        ++CurrentLine;

        if (CurrentLine == Lines.Max) {
            CurrentLine = 0;
            Mode = PpuMode.OamRead;
        }

        return Dots.Max;
    }

    public IEnumerable<LocatedMemory> MemoryLayout() => [
        new(0x8000, 0xA000, Vram),
        new(0xFE00, 0xFEA0, Oam),
        AccessorMemory.Located(0xFF40, () => (byte)Control, it => Control = (PpuControl)it),
        AccessorMemory.Located(
            0xFF41,
            () => (byte)((byte)Interrupts | (IsLineOnTarget ? 0b100 : 0) | (byte)Mode),
            it => Interrupts = (PpuInterrupts)(it & ~0b111)
        ),
        AccessorMemory.Located(
            0xFF42,
            () => (byte)ScrollPos.Y,
            it => ScrollPos = ScrollPos with { Y = it }
        ),
        AccessorMemory.Located(
            0xFF43,
            () => (byte)ScrollPos.X,
            it => ScrollPos = ScrollPos with { X = it }
        ),
        AccessorMemory.Located(0xFF44, () => CurrentLine, _ => {}),
        AccessorMemory.Located(0xFF45, () => LineTarget, it => LineTarget = it),
        AccessorMemory.Located(0xFF47, BgPalette.ToByte, it => BgPalette = Palette.FromByte(it)),
        AccessorMemory.Located(
            0xFF48,
            Obj0Palette.ToByte,
            it => Obj0Palette = Palette.FromByte(it)
        ),
        AccessorMemory.Located(
            0xFF49,
            Obj1Palette.ToByte,
            it => Obj1Palette = Palette.FromByte(it)
        ),
        AccessorMemory.Located(
            0xFF4A,
            () => 0,
            _ => {}
        ),
        AccessorMemory.Located(
            0xFF4B,
            () => 0,
            _ => {}
        ),
    ];
}

[Flags]
public enum PpuControl : byte
{
    Enable    = 0b1000_0000,
    WinMap    = 0b0100_0000,
    WinEnable = 0b0010_0000,
    Blocks    = 0b0001_0000,
    BgMap     = 0b0000_1000,
    ObjSize   = 0b0000_0100,
    ObjEnable = 0b0000_0010,
    BgEnable  = 0b0000_0001,
}

[Flags]
public enum PpuInterrupts : byte
{
    LineOnTarget = 0b0100_0000,
    OamRead      = 0b0010_0000,
    VBlank       = 0b0001_0000,
    HBlank       = 0b0000_1000,
}

public enum PpuMode : byte
{
    HBlank  = 0,
    VBlank  = 1,
    OamRead = 2,
    Render  = 3,
}

[InlineArray(4)]
public struct Palette
{
    private Shade _slot;

    public static Palette FromByte(byte code)
    {
        var palette = new Palette();

        palette[0] = (Shade)(code >> 0 & 0b11);
        palette[1] = (Shade)(code >> 2 & 0b11);
        palette[2] = (Shade)(code >> 4 & 0b11);
        palette[3] = (Shade)(code >> 6 & 0b11);

        return palette;
    }

    public byte ToByte() =>
        (byte)(
            (byte)this[0] << 0 |
            (byte)this[1] << 2 |
            (byte)this[2] << 4 |
            (byte)this[3] << 6
        );
}
