using System.Diagnostics.CodeAnalysis;

namespace Valo.Tests;

public class CpuTests
{
    [Test]
    public void LoadRegisterRegister(
        [ValueSource(nameof(StdRegister8))] Register8 dst,
        [ValueSource(nameof(StdRegister8))] Register8 src
    )
    {
        var opcode =
            (byte)(0b01_000_000 | (EncodeStdRegister8(dst) << 3) | EncodeStdRegister8(src));
        var sut = new Cpu(new RegisterFile { [src] = 0x42 }, new Rom([opcode, 0]));

        var cycles = sut.Step();

        Assert.Multiple(() => {
            Assert.That(sut.Registers[dst], Is.EqualTo(sut.Registers[src]));
            Assert.That(cycles, Is.EqualTo(1));
        });
    }

    [Test]
    public void LoadRegisterImmediate([ValueSource(nameof(StdRegister8))] Register8 dst)
    {
        var opcode = (byte)(0b00_000_110 | (EncodeStdRegister8(dst) << 3));
        var sut = new Cpu(new RegisterFile(), new Rom([opcode, 0x67, 0]));

        var cycles = sut.Step();

        Assert.Multiple(() => {
            Assert.That(sut.Registers[dst], Is.EqualTo(sut.Memory.Read(1)));
            Assert.That(cycles, Is.EqualTo(2));
        });
    }

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
}
