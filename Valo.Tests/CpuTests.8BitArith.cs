namespace Valo.Tests;

public partial class CpuTests
{
    #region ADD instruction
    [Test]
    [Sequential]
    public void Add(
        [ValueSource(nameof(StdRegister8))] Register8 src,
        [Random(count: 7, Distinct = true)] byte operand
    )
    {
        var opcode = (byte)(0b10_000_000 | EncodeStdRegister8(src));
        var sut = new Cpu(
            new RegisterFile { A = 0x42, [src] = operand },
            new Rom([opcode, 0])
        );

        var a = sut.Registers.A;
        var cycles = sut.Step();

        Assert.Multiple(() => {
            Assert.That(sut.Registers.A, Is.EqualTo((byte)(a + operand)));
            Assert.That(cycles, Is.EqualTo(1));
        });
    }

    [TestCase(0x01, 0x01, 0)]
    [TestCase(0x00, 0x00, FlagsBit.Z)]
    [TestCase(0x0F, 0x01, FlagsBit.H)]
    [TestCase(0xF0, 0x20, FlagsBit.C)]
    [TestCase(0xFF, 0x02, FlagsBit.C | FlagsBit.H)]
    [TestCase(0x80, 0x80, FlagsBit.Z | FlagsBit.C)]
    [TestCase(0x88, 0x78, FlagsBit.Z | FlagsBit.C | FlagsBit.H)]
    public void AddFlags(byte a, byte b, FlagsBit flags)
    {
        byte opcode = 0b10_000_000;
        var sut = new Cpu(
            new RegisterFile { A = a, B = b },
            new Rom([opcode, 0])
        );

        var cycles = sut.Step();

        Assert.Multiple(() => {
            Assert.That(sut.Registers.A, Is.EqualTo((byte)(a + b)));
            Assert.That(sut.Registers.Flags.Value, Is.EqualTo(flags));
            Assert.That(cycles, Is.EqualTo(1));
        });
    }

    [Test]
    public void AddHL()
    {
        byte opcode = 0b10_000_110;
        var sut = new Cpu(
            new RegisterFile { A = 0x42, HL = 0x0002 },
            new Rom([opcode, 0, 0x25])
        );

        var cycles = sut.Step();

        Assert.Multiple(() => {
            Assert.That(sut.Registers.A, Is.EqualTo(0x67));
            Assert.That(cycles, Is.EqualTo(2));
        });
    }

    [Test]
    public void AddImmediate()
    {
        byte opcode = 0b11_000_110;
        var sut = new Cpu(
            new RegisterFile { A = 0x42 },
            new Rom([opcode, 0x25, 0])
        );

        var cycles = sut.Step();

        Assert.Multiple(() => {
            Assert.That(sut.Registers.A, Is.EqualTo(0x67));
            Assert.That(cycles, Is.EqualTo(2));
        });
    }
    #endregion ADD instruction

    #region ADC instruction
    [Test]
    [Sequential]
    public void Adc(
        [ValueSource(nameof(StdRegister8))] Register8 src,
        [Random(count: 7)] byte operand,
        [Random(min: 0, max: 2, count: 7)] int carry
    )
    {
        var opcode = (byte)(0b10_001_000 | EncodeStdRegister8(src));
        var sut = new Cpu(
            new RegisterFile {
                A = 0x42,
                F = (byte)(carry == 1 ? FlagsBit.C : 0),
                [src] = operand,
            },
            new Rom([opcode, 0])
        );

        var a = sut.Registers.A;
        var cycles = sut.Step();

        Assert.Multiple(() => {
            Assert.That(sut.Registers.A, Is.EqualTo((byte)(a + operand + carry)));
            Assert.That(cycles, Is.EqualTo(1));
        });
    }

    [TestCase(0x01, 0x01, false, 0)]
    [TestCase(0x00, 0x00, false, FlagsBit.Z)]
    [TestCase(0x0F, 0x00, true,  FlagsBit.H)]
    [TestCase(0xF0, 0x20, false, FlagsBit.C)]
    [TestCase(0xFE, 0x02, true,  FlagsBit.C | FlagsBit.H)]
    [TestCase(0x80, 0x80, false, FlagsBit.Z | FlagsBit.C)]
    [TestCase(0x87, 0x78, true,  FlagsBit.Z | FlagsBit.H | FlagsBit.C)]
    public void AdcFlags(byte a, byte b, bool carry, FlagsBit flags)
    {
        byte opcode = 0b10_001_000;
        var sut = new Cpu(
            new RegisterFile { A = a, B = b, F = (byte)(carry ? FlagsBit.C : 0) },
            new Rom([opcode, 0])
        );

        var cycles = sut.Step();

        Assert.Multiple(() => {
            Assert.That(sut.Registers.A, Is.EqualTo((byte)(a + b + (carry ? 1 : 0))));
            Assert.That(sut.Registers.Flags.Value, Is.EqualTo(flags));
            Assert.That(cycles, Is.EqualTo(1));
        });
    }

    [Test]
    public void AdcHL([Values] bool carry)
    {
        byte opcode = 0b10_001_110;
        var sut = new Cpu(
            new RegisterFile { A = 0x42, HL = 0x0002, F = (byte)(carry ? FlagsBit.C : 0) },
            new Rom([opcode, 0, 0x25])
        );

        var cycles = sut.Step();

        Assert.Multiple(() => {
            Assert.That(sut.Registers.A, Is.EqualTo(0x67 + (carry ? 1 : 0)));
            Assert.That(cycles, Is.EqualTo(2));
        });
    }

    [Test]
    public void AdcImmediate([Values] bool carry)
    {
        byte opcode = 0b11_001_110;
        var sut = new Cpu(
            new RegisterFile { A = 0x42, F = (byte)(carry ? FlagsBit.C : 0) },
            new Rom([opcode, 0x25, 0])
        );

        var cycles = sut.Step();

        Assert.Multiple(() => {
            Assert.That(sut.Registers.A, Is.EqualTo(0x67 + (carry ? 1 : 0)));
            Assert.That(cycles, Is.EqualTo(2));
        });
    }
    #endregion ADC instruction

    #region SUB instruction
    [Test]
    [Sequential]
    public void Sub(
        [ValueSource(nameof(StdRegister8))] Register8 src,
        [Random(count: 7, Distinct = true)] byte operand
    )
    {
        var opcode = (byte)(0b10_010_000 | EncodeStdRegister8(src));
        var sut = new Cpu(
            new RegisterFile { A = 0x42, [src] = operand },
            new Rom([opcode, 0])
        );

        var a = sut.Registers.A;
        var cycles = sut.Step();

        Assert.Multiple(() => {
            Assert.That(sut.Registers.A, Is.EqualTo((byte)(a - operand)));
            Assert.That(cycles, Is.EqualTo(1));
        });
    }

    [TestCase(0x02, 0x01, FlagsBit.N)]
    [TestCase(0x00, 0x00, FlagsBit.Z | FlagsBit.N)]
    [TestCase(0x10, 0x01, FlagsBit.H | FlagsBit.N)]
    [TestCase(0x10, 0x20, FlagsBit.C | FlagsBit.N)]
    [TestCase(0x10, 0x12, FlagsBit.H | FlagsBit.C | FlagsBit.N)]
    public void SubFlags(byte a, byte b, FlagsBit flags)
    {
        byte opcode = 0b10_010_000;
        var sut = new Cpu(
            new RegisterFile { A = a, B = b },
            new Rom([opcode, 0])
        );

        var cycles = sut.Step();

        Assert.Multiple(() => {
            Assert.That(sut.Registers.A, Is.EqualTo((byte)(a - b)));
            Assert.That(sut.Registers.Flags.Value, Is.EqualTo(flags));
            Assert.That(cycles, Is.EqualTo(1));
        });
    }

    [Test]
    public void SubHL()
    {
        byte opcode = 0b10_010_110;
        var sut = new Cpu(
            new RegisterFile { A = 0x42, HL = 0x0002 },
            new Rom([opcode, 0, 0x25])
        );

        var cycles = sut.Step();

        Assert.Multiple(() => {
            Assert.That(sut.Registers.A, Is.EqualTo(0x1D));
            Assert.That(cycles, Is.EqualTo(2));
        });
    }

    [Test]
    public void SubImmediate()
    {
        byte opcode = 0b11_010_110;
        var sut = new Cpu(
            new RegisterFile { A = 0x42 },
            new Rom([opcode, 0x25, 0])
        );

        var cycles = sut.Step();

        Assert.Multiple(() => {
            Assert.That(sut.Registers.A, Is.EqualTo(0x1D));
            Assert.That(cycles, Is.EqualTo(2));
        });
    }
    #endregion SUB instruction

    #region SBC instruction
    [Test]
    [Sequential]
    public void Sbc(
        [ValueSource(nameof(StdRegister8))] Register8 src,
        [Random(count: 7)] byte operand,
        [Random(min: 0, max: 2, count: 7)] int carry
    )
    {
        var opcode = (byte)(0b10_011_000 | EncodeStdRegister8(src));
        var sut = new Cpu(
            new RegisterFile {
                A = 0x42,
                F = (byte)(carry == 1 ? FlagsBit.C : 0),
                [src] = operand,
            },
            new Rom([opcode, 0])
        );

        var a = sut.Registers.A;
        var cycles = sut.Step();

        Assert.Multiple(() => {
            Assert.That(sut.Registers.A, Is.EqualTo((byte)(a - operand - carry)));
            Assert.That(cycles, Is.EqualTo(1));
        });
    }

    [TestCase(0x02, 0x01, false, FlagsBit.N)]
    [TestCase(0x00, 0x00, false, FlagsBit.Z | FlagsBit.N)]
    [TestCase(0x10, 0x0F, true,  FlagsBit.Z | FlagsBit.H | FlagsBit.N)]
    [TestCase(0x00, 0xFF, true,  FlagsBit.Z | FlagsBit.H | FlagsBit.C | FlagsBit.N)]
    [TestCase(0x11, 0x01, true,  FlagsBit.H | FlagsBit.N)]
    [TestCase(0x10, 0x20, false, FlagsBit.C | FlagsBit.N)]
    [TestCase(0x10, 0x10, true,  FlagsBit.H | FlagsBit.C | FlagsBit.N)]
    public void SbcFlags(byte a, byte b, bool carry, FlagsBit flags)
    {
        byte opcode = 0b10_011_000;
        var sut = new Cpu(
            new RegisterFile { A = a, B = b, F = (byte)(carry ? FlagsBit.C : 0) },
            new Rom([opcode, 0])
        );

        var cycles = sut.Step();

        Assert.Multiple(() => {
            Assert.That(sut.Registers.A, Is.EqualTo((byte)(a - b - (carry ? 1 : 0))));
            Assert.That(sut.Registers.Flags.Value, Is.EqualTo(flags));
            Assert.That(cycles, Is.EqualTo(1));
        });
    }

    [Test]
    public void SbcHL([Values] bool carry)
    {
        byte opcode = 0b10_011_110;
        var sut = new Cpu(
            new RegisterFile {
                A = 0x42,
                HL = 0x0002,
                F = (byte)(carry ? FlagsBit.C : 0),
            },
            new Rom([opcode, 0, 0x25])
        );

        var cycles = sut.Step();

        Assert.Multiple(() => {
            Assert.That(sut.Registers.A, Is.EqualTo(0x1D - (carry ? 1 : 0)));
            Assert.That(cycles, Is.EqualTo(2));
        });
    }

    [Test]
    public void SbcImmediate([Values] bool carry)
    {
        byte opcode = 0b11_011_110;
        var sut = new Cpu(
            new RegisterFile { A = 0x42, F = (byte)(carry ? FlagsBit.C : 0) },
            new Rom([opcode, 0x25, 0])
        );

        var cycles = sut.Step();

        Assert.Multiple(() => {
            Assert.That(sut.Registers.A, Is.EqualTo(0x1D - (carry ? 1 : 0)));
            Assert.That(cycles, Is.EqualTo(2));
        });
    }
    #endregion SBC instruction
}
