using System.Buffers.Binary;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Valo;

[SuppressMessage("ReSharper", "InconsistentNaming")]
public enum Register8
{
    A = 1, F = 0,
    B = 3, C = 2,
    D = 5, E = 4,
    H = 7, L = 6,
    W = 9, Z = 8,
    IR = 10,
    IME = 11,
    SPH = 15, SPL = 14,
}

[SuppressMessage("ReSharper", "InconsistentNaming")]
public enum Register16
{
    AF = 0,
    BC = 2,
    DE = 4,
    HL = 6,
    WZ = 8,
    // note: IR and IME go here (bytes 10 and 11), but they aren't pairable as 16-bit
    PC = 12,
    SP = 14,
}

[Flags]
public enum FlagsBit
{
    Z = 0b1000_0000,
    N = 0b0100_0000,
    H = 0b0010_0000,
    C = 0b0001_0000,
}

public struct RegisterFile
{
    [InlineArray((int)Register16.SP + 2)]
    private struct Storage
    {
        private byte _slot;
    }

    private Storage _storage;

    public byte this[Register8 reg]
    {
        readonly get => _storage[(int)reg];
        set => _storage[(int)reg] = value;
    }

    public ushort this[Register16 reg]
    {
        readonly get {
            var start = (int)reg;
            var end = start + 2;

            return BinaryPrimitives.ReadUInt16LittleEndian(_storage[start .. end]);
        }

        set {
            var start = (int)reg;
            var end = start + 2;

            BinaryPrimitives.WriteUInt16LittleEndian(_storage[start .. end], value);
        }
    }

    public byte A { readonly get => this[Register8.A]; set => this[Register8.A] = value; }
    public byte F { readonly get => this[Register8.F]; set => this[Register8.F] = value; }
    public ushort AF { readonly get => this[Register16.AF]; set => this[Register16.AF] = value; }

    public byte B { readonly get => this[Register8.B]; set => this[Register8.B] = value; }
    public byte C { readonly get => this[Register8.C]; set => this[Register8.C] = value; }
    public ushort BC { readonly get => this[Register16.BC]; set => this[Register16.BC] = value; }

    public byte D { readonly get => this[Register8.D]; set => this[Register8.D] = value; }
    public byte E { readonly get => this[Register8.E]; set => this[Register8.E] = value; }
    public ushort DE { readonly get => this[Register16.DE]; set => this[Register16.DE] = value; }

    public byte H { readonly get => this[Register8.H]; set => this[Register8.H] = value; }
    public byte L { readonly get => this[Register8.L]; set => this[Register8.L] = value; }
    public ushort HL { readonly get => this[Register16.HL]; set => this[Register16.HL] = value; }

    public byte W { readonly get => this[Register8.W]; set => this[Register8.W] = value; }
    public byte Z { readonly get => this[Register8.Z]; set => this[Register8.Z] = value; }
    public ushort WZ { readonly get => this[Register16.WZ]; set => this[Register16.WZ] = value; }

    public byte IR { readonly get => this[Register8.IR]; set => this[Register8.IR] = value; }
    public byte IME { readonly get => this[Register8.IME]; set => this[Register8.IME] = value; }

    public ushort PC { readonly get => this[Register16.PC]; set => this[Register16.PC] = value; }

    public ushort SP { readonly get => this[Register16.SP]; set => this[Register16.SP] = value; }

    public readonly (byte Msb, byte Lsb) Split(Register16 register) =>
        #pragma warning disable CS8524 // The switch expression does not handle some values of its input type (it is not exhaustive) involving an unnamed enum value.
        register switch {
            Register16.AF => (A, F),
            Register16.BC => (B, C),
            Register16.DE => (D, E),
            Register16.HL => (H, L),
            Register16.WZ => (W, Z),
            Register16.PC => (_storage[(int)Register16.PC + 1], _storage[(int)Register16.PC]),
            Register16.SP => (_storage[(int)Register16.SP + 1], _storage[(int)Register16.SP]),
        };
        #pragma warning restore CS8524 // The switch expression does not handle some values of its input type (it is not exhaustive) involving an unnamed enum value.
}

public static class RegisterFileExtensions
{
    extension (ref RegisterFile self)
    {
        public FlagsRegister Flags => new(ref self);
    }
}

public readonly ref struct FlagsRegister
{
    private readonly ref RegisterFile _reg;

    public FlagsRegister(ref RegisterFile reg)
    {
        _reg = ref reg;
    }

    public FlagsBit Value => (FlagsBit)_reg.F;

    public bool IsSet(FlagsBit bit) => (_reg.F & (byte)bit) != 0;

    public void Apply(FlagsBit affected, FlagsBit bits)
    {
        _reg.F = (byte)(((FlagsBit)_reg.F & ~affected) | (bits & affected));
    }

    public void Replace(FlagsBit bits)
    {
        _reg.F = (byte)bits;
    }
}
