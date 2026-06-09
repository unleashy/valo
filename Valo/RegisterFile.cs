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

public enum FlagsBit
{
    Z,
    N,
    H,
    C,
}

public sealed class RegisterFile
{
    [InlineArray((int)Register16.SP + 2)]
    private struct Storage
    {
        private byte _slot;
    }

    private Storage _storage;

    public byte this[Register8 reg]
    {
        get => _storage[(int)reg];
        set => _storage[(int)reg] = value;
    }

    public ushort this[Register16 reg]
    {
        get {
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

    public bool GetFlag(FlagsBit bit)
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
