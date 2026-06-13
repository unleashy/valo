namespace Valo.Tests;

public partial class CpuTests
{
    [Test]
    public void LoadImmediate16([ValueSource(nameof(StdRegister16))] Register16 dst)
    {
        var opcode = (byte) (0b00_00_0001 | (EncodeStdRegister16(dst) << 4));
        var sut = new Cpu(
            new RegisterFile(),
            new Ram([opcode, 0xFE, 0xCA, 0])
        );

        var cycles = sut.Step();

        Assert.Multiple(() => {
            Assert.That(sut.Registers[dst], Is.EqualTo(0xCAFE));
            Assert.That(cycles, Is.EqualTo(3));
        });
    }

    [Test]
    public void LoadIndirectImmediateSP()
    {
        byte opcode = 0b00_001000;
        var sut = new Cpu(
            new RegisterFile { SP = 0xCAFE },
            new Ram([opcode, 0x04, 0x00, 0, 0xFF, 0xFF])
        );

        var cycles = sut.Step();

        Assert.Multiple(() => {
            Assert.That(sut.Memory.Read(0x0004), Is.EqualTo(0xFE));
            Assert.That(sut.Memory.Read(0x0005), Is.EqualTo(0xCA));
            Assert.That(cycles, Is.EqualTo(5));
        });
    }

    [Test]
    public void LoadSPHL()
    {
        byte opcode = 0b11_111001;
        var sut = new Cpu(
            new RegisterFile { HL = 0x4267 },
            new Rom([opcode, 0])
        );

        var cycles = sut.Step();

        Assert.Multiple(() => {
            Assert.That(sut.Registers.SP, Is.EqualTo(0x4267));
            Assert.That(cycles, Is.EqualTo(2));
        });
    }

    [TestCase(0xFF00, +0x42, 0)]
    [TestCase(0xFF00, -0x42, 0)]
    [TestCase(0xFFC0, +0x40, FlagsBit.C)]
    [TestCase(0x0008, +0x08, FlagsBit.H)]
    [TestCase(0x00FF, -0x7F, FlagsBit.C | FlagsBit.H)]
    public void LoadHLAdjustedSP(int sp, sbyte offset, FlagsBit flags)
    {
        byte opcode = 0b11_111000;
        var sut = new Cpu(
            new RegisterFile { SP = (ushort) sp },
            new Rom([opcode, (byte) offset, 0])
        );

        var cycles = sut.Step();

        Assert.Multiple(() => {
            Assert.That(sut.Registers.HL, Is.EqualTo((ushort) (sp + offset)));
            Assert.That(sut.Registers.Flags.Value, Is.EqualTo(flags));
            Assert.That(cycles, Is.EqualTo(3));
        });
    }

    [Test]
    public void Push([ValueSource(nameof(StackRegister16))] Register16 src)
    {
        var opcode = (byte) (0b11_00_0101 | (EncodeStackRegister16(src) << 4));
        var sut = new Cpu(
            new RegisterFile { SP = 0x0004, [src] = 0xCAFE },
            new Ram([opcode, 0, 0, 0])
        );

        var cycles = sut.Step();

        Assert.Multiple(() => {
            Assert.That(sut.Registers.SP, Is.EqualTo(0x0002));
            Assert.That(sut.Memory.Read(0x0002), Is.EqualTo(0xFE));
            Assert.That(sut.Memory.Read(0x0003), Is.EqualTo(0xCA));
            Assert.That(cycles, Is.EqualTo(4));
        });
    }

    [Test]
    public void Pop([ValueSource(nameof(StackRegister16))] Register16 dst)
    {
        var opcode = (byte) (0b11_00_0001 | (EncodeStackRegister16(dst) << 4));
        var sut = new Cpu(
            new RegisterFile { SP = 0x0002 },
            new Rom([opcode, 0, 0xFE, 0xCA])
        );

        var cycles = sut.Step();

        Assert.Multiple(() => {
            Assert.That(sut.Registers.SP, Is.EqualTo(0x0004));
            Assert.That(sut.Registers[dst], Is.EqualTo(0xCAFE));
            Assert.That(cycles, Is.EqualTo(3));
        });
    }
}
