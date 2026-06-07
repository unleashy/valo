using System.Buffers.Binary;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Valo;

public struct RegisterFile
{
    private RegisterArray _regs = new();

    public RegisterFile()
    {}

    public byte this[Register8 reg]
    {
        readonly get => _regs[(int)reg];
        set => _regs[(int)reg] = value;
    }

    public ushort this[Register16 reg]
    {
        readonly get {
            var start = 2 * (int)reg;
            var end = start + 2;

            return BinaryPrimitives.ReadUInt16LittleEndian(_regs[start .. end]);
        }

        set {
            var start = 2 * (int)reg;
            var end = start + 2;

            BinaryPrimitives.WriteUInt16LittleEndian(_regs[start .. end], value);
        }
    }

    public readonly bool GetFlag(FlagsBit bit)
    {
        var flags = this[Register8.F];

        return ((flags >> (7 - (int)bit)) & 1) != 0;
    }

    public void SetFlag(FlagsBit bit, bool value)
    {
        var flags = this[Register8.F];
        var mask = 1 << (7 - (int)bit);

        if (value) {
            flags = (byte)(flags | mask);
        }
        else {
            flags = (byte)(flags & ~mask);
        }

        this[Register8.F] = flags;
    }
}

public enum Register8
{
    F, A,
    C, B,
    E, D,
    L, H,
}

[SuppressMessage("ReSharper", "InconsistentNaming")]
public enum Register16
{
    AF,
    BC,
    DE,
    HL,
    PC,
    SP,
}

public enum FlagsBit
{
    Z,
    N,
    H,
    C,
}

[InlineArray(12)]
internal struct RegisterArray
{
    private byte _register;
}
