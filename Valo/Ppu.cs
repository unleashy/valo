using System.Drawing;

namespace Valo;

public sealed class Ppu(ILcd lcd, IMemory vram, IMemory oam)
{
    public PpuControl Control { get; set; }
    public PpuInterrupts Interrupts { get; set; }
    public PpuMode Mode { get; private set; } = PpuMode.OamRead;

    public byte CurrentLine { get; private set; }
    public byte LineTarget { get; set; }
    public bool IsLineOnTarget => CurrentLine == LineTarget;

    public Point ViewportPos { get; set; }

    private const uint MaxDot = 456;
    private const uint MaxOamReadDot = 80;
    private const uint MinRenderDot = 172;

    private const uint MaxLine = 154;
    private const uint VBlankLine = 144;

    private uint _currentDot;

    public void Cycle()
    {
        if (Control.HasFlag(PpuControl.Enable)) {
            if (_currentDot == 0) {
                Mode = PpuMode.OamRead;
            }

            Mode = Mode switch {
                PpuMode.OamRead => CycleOamRead(),
                PpuMode.Render  => CycleRender(),
                PpuMode.HBlank  => CycleHBlank(),
                PpuMode.VBlank  => CycleVBlank(),
            };

            UpdateLocations();
        }
        else {
            Reset();
        }
    }

    private PpuMode CycleOamRead()
    {
        return _currentDot < MaxOamReadDot ? PpuMode.OamRead : PpuMode.Render;
    }

    private PpuMode CycleRender()
    {
        return _currentDot < MinRenderDot ? PpuMode.Render : PpuMode.HBlank;
    }

    private PpuMode CycleHBlank()
    {
        if (_currentDot < MaxDot) {
            return PpuMode.HBlank;
        }
        else if (CurrentLine >= VBlankLine - 1) {
            return PpuMode.VBlank;
        }
        else {
            return PpuMode.OamRead;
        }
    }

    private PpuMode CycleVBlank()
    {
        return CurrentLine < MaxLine ? PpuMode.VBlank : PpuMode.OamRead;
    }

    private void Reset()
    {
        _currentDot = 0;
        CurrentLine = 0;
        Mode = PpuMode.HBlank;
    }

    private void UpdateLocations()
    {
        ++_currentDot;

        if (_currentDot >= MaxDot) {
            ++CurrentLine;
        }

        if (CurrentLine >= MaxLine) {
            CurrentLine = 0;
        }
    }

    public IEnumerable<LocatedMemory> MemoryLayout() => [
        AccessorMemory.Located(0xFF40, () => (byte)Control, it => Control = (PpuControl)it),
        AccessorMemory.Located(
            0xFF41,
            () => (byte)((byte)Interrupts | (IsLineOnTarget ? 0b100 : 0) | (byte)Mode),
            it => Interrupts = (PpuInterrupts)(it & ~0b111)
        ),
        AccessorMemory.Located(
            0xFF42,
            () => (byte)ViewportPos.X,
            it => ViewportPos = ViewportPos with { X = it }
        ),
        AccessorMemory.Located(
            0xFF43,
            () => (byte)ViewportPos.Y,
            it => ViewportPos = ViewportPos with { Y = it }
        ),
        AccessorMemory.Located(0xFF44, () => CurrentLine, _ => {}),
        AccessorMemory.Located(0xFF45, () => LineTarget, it => LineTarget = it),
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
    IntLyc   = 0b0100_0000,
    IntMode2 = 0b0010_0000,
    IntMode1 = 0b0001_0000,
    IntMode0 = 0b0000_1000,
}

public enum PpuMode : byte
{
    HBlank  = 0,
    VBlank  = 1,
    OamRead = 2,
    Render  = 3,
}
