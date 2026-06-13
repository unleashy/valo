using System.Diagnostics.CodeAnalysis;

namespace Valo.Tests;

public partial class CpuTests
{
    public static Register8[] StdRegister8 => [
        Register8.A,
        Register8.B,
        Register8.C,
        Register8.D,
        Register8.E,
        Register8.H,
        Register8.L,
    ];

    [SuppressMessage("ReSharper", "SwitchExpressionHandlesSomeKnownEnumValuesWithExceptionInDefault")]
    private static byte EncodeStdRegister8(Register8 reg) =>
        reg switch {
            Register8.A => 0b111,
            Register8.B => 0b000,
            Register8.C => 0b001,
            Register8.D => 0b010,
            Register8.E => 0b011,
            Register8.H => 0b100,
            Register8.L => 0b101,
            _ => throw new ArgumentOutOfRangeException(nameof(reg)),
        };

    public static Register16[] StdRegister16 => [
        Register16.BC,
        Register16.DE,
        Register16.HL,
        Register16.SP,
    ];

    [SuppressMessage("ReSharper", "SwitchExpressionHandlesSomeKnownEnumValuesWithExceptionInDefault")]
    private static byte EncodeStdRegister16(Register16 reg) =>
        reg switch {
            Register16.BC => 0b00,
            Register16.DE => 0b01,
            Register16.HL => 0b10,
            Register16.SP => 0b11,
            _ => throw new ArgumentOutOfRangeException(nameof(reg)),
        };

    public static Register16[] StackRegister16 => [
        Register16.BC,
        Register16.DE,
        Register16.HL,
        Register16.AF,
    ];

    [SuppressMessage("ReSharper", "SwitchExpressionHandlesSomeKnownEnumValuesWithExceptionInDefault")]
    private static byte EncodeStackRegister16(Register16 reg) =>
        reg switch {
            Register16.BC => 0b00,
            Register16.DE => 0b01,
            Register16.HL => 0b10,
            Register16.AF => 0b11,
            _             => throw new ArgumentOutOfRangeException(nameof(reg)),
        };
}
