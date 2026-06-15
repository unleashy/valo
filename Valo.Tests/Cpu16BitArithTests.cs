namespace Valo.Tests;

public class Cpu16BitArithTests : CpuTestsBase
{
    [Test]
    public void Inc([ValueSource(nameof(StdRegister16))] Register16 reg)
    {
        var opcode = (byte)(0b00_00_0011 | (EncodeStdRegister16(reg) << 4));
        var sut = new Cpu(
            new RegisterFile { [reg] = 0x00FF },
            new Rom([opcode, 0])
        );

        var cycles = sut.Step();

        Assert.Multiple(() => {
            Assert.That(sut.Registers[reg], Is.EqualTo(0x0100));
            Assert.That(cycles, Is.EqualTo(2));
        });
    }

    [Test]
    public void Dec([ValueSource(nameof(StdRegister16))] Register16 reg)
    {
        var opcode = (byte)(0b00_00_1011 | (EncodeStdRegister16(reg) << 4));
        var sut = new Cpu(
            new RegisterFile { [reg] = 0x0100 },
            new Rom([opcode, 0])
        );

        var cycles = sut.Step();

        Assert.Multiple(() => {
            Assert.That(sut.Registers[reg], Is.EqualTo(0x00FF));
            Assert.That(cycles, Is.EqualTo(2));
        });
    }

    [Test]
    public void AddHL16([ValueSource(nameof(StdRegister16))] Register16 src)
    {
        var opcode = (byte)(0b00_00_1001 | (EncodeStdRegister16(src) << 4));
        var sut = new Cpu(
            new RegisterFile { HL = 0x1234, [src] = 0x5678 },
            new Rom([opcode, 0])
        );

        var hl = sut.Registers.HL;
        var operand = sut.Registers[src];
        var cycles = sut.Step();

        Assert.Multiple(() => {
            Assert.That(sut.Registers.HL, Is.EqualTo((ushort)(hl + operand)));
            Assert.That(sut.Registers.F, Is.EqualTo((FlagsBit)0));
            Assert.That(cycles, Is.EqualTo(2));
        });
    }

    [TestCase(0x0000, 0x0000, 0x0000, 0)]
    [TestCase(0x0FFF, 0x0001, 0x1000, FlagsBit.H)]
    [TestCase(0xF000, 0x1000, 0x0000, FlagsBit.C)]
    [TestCase(0x0FFF, 0xF001, 0x0000, FlagsBit.H | FlagsBit.C)]
    public void AddHL16Flags(int hl, int bc, int expected, FlagsBit flags)
    {
        byte opcode = 0b00_00_1001;
        var sut = new Cpu(
            new RegisterFile { HL = (ushort)hl, BC = (ushort)bc, F = FlagsBit.Z },
            new Rom([opcode, 0])
        );

        var cycles = sut.Step();

        Assert.Multiple(() => {
            Assert.That(sut.Registers.HL, Is.EqualTo(expected));
            Assert.That(sut.Registers.F, Is.EqualTo(FlagsBit.Z | flags));
            Assert.That(cycles, Is.EqualTo(2));
        });
    }

    [TestCase(0x0000, +0x01, 0x0001, 0)]
    [TestCase(0x000F, +0x01, 0x0010, FlagsBit.H)]
    [TestCase(0x00FF, +0x01, 0x0100, FlagsBit.C | FlagsBit.H)]
    [TestCase(0x00F0, +0x20, 0x0110, FlagsBit.C)]
    [TestCase(0x00FF, -0x7F, 0x0080, FlagsBit.C | FlagsBit.H)]
    [TestCase(0x0010, -0x01, 0x000F, FlagsBit.C)]
    public void AddSPImm8(int sp, sbyte offset, int expected, FlagsBit flags)
    {
        byte opcode = 0b11_101_000;
        var sut = new Cpu(
            new RegisterFile { SP = (ushort)sp, F = FlagsBit.Z | FlagsBit.N },
            new Rom([opcode, (byte)offset, 0])
        );

        var cycles = sut.Step();

        Assert.Multiple(() => {
            Assert.That(sut.Registers.SP, Is.EqualTo((ushort)expected));
            Assert.That(sut.Registers.F, Is.EqualTo(flags));
            Assert.That(cycles, Is.EqualTo(4));
        });
    }
}
