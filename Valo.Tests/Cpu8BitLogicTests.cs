namespace Valo.Tests;

public class Cpu8BitLogicTests : CpuTestsBase
{
    #region AND instruction
    [Test]
    public void And(
        [ValueSource(nameof(StdRegister8))] Register8 src,
        [Values(0b0101_0101, 0)] byte operand
    )
    {
        var opcode = (byte)(0b10_100_000 | EncodeStdRegister8(src));
        var sut = new Cpu(
            new RegisterFile { A = 0b1110_0111, [src] = operand },
            new Rom([opcode, 0])
        );

        var cycles = sut.Step();

        Assert.Multiple(() => {
            Assert.That(sut.Registers.A, Is.EqualTo(sut.Registers.A & operand));
            Assert.That(sut.Registers.F, Is.EqualTo(FlagsBit.H | (operand == 0 ? FlagsBit.Z : 0)));
            Assert.That(cycles, Is.EqualTo(1));
        });
    }

    [Test]
    public void AndHL([Values(0b0101_0101, 0)] byte operand)
    {
        byte opcode = 0b10_100_110;
        var sut = new Cpu(
            new RegisterFile { A = 0b1110_0111, HL = 0x0002 },
            new Rom([opcode, 0, operand])
        );

        var cycles = sut.Step();

        Assert.Multiple(() => {
            Assert.That(sut.Registers.A, Is.EqualTo(sut.Registers.A & operand));
            Assert.That(sut.Registers.F, Is.EqualTo(FlagsBit.H | (operand == 0 ? FlagsBit.Z : 0)));
            Assert.That(cycles, Is.EqualTo(2));
        });
    }

    [Test]
    public void AndImm8([Values(0b0101_0101, 0)] byte operand)
    {
        byte opcode = 0b11_100_110;
        var sut = new Cpu(
            new RegisterFile { A = 0b1110_0111 },
            new Rom([opcode, operand, 0])
        );

        var cycles = sut.Step();

        Assert.Multiple(() => {
            Assert.That(sut.Registers.A, Is.EqualTo(sut.Registers.A & operand));
            Assert.That(sut.Registers.F, Is.EqualTo(FlagsBit.H | (operand == 0 ? FlagsBit.Z : 0)));
            Assert.That(cycles, Is.EqualTo(2));
        });
    }
    #endregion AND instruction

    #region OR instruction
    [Test]
    public void Or(
        [ValueSource(nameof(StdRegister8))] Register8 src,
        [Values(0b0101_0101, 0)] byte operand
    )
    {
        var opcode = (byte)(0b10_110_000 | EncodeStdRegister8(src));
        var sut = new Cpu(
            new RegisterFile { A = 0, [src] = operand },
            new Rom([opcode, 0])
        );

        var cycles = sut.Step();

        Assert.Multiple(() => {
            Assert.That(sut.Registers.A, Is.EqualTo(sut.Registers.A | operand));
            Assert.That(sut.Registers.F, Is.EqualTo(operand == 0 ? FlagsBit.Z : 0));
            Assert.That(cycles, Is.EqualTo(1));
        });
    }

    [Test]
    public void OrHL([Values(0b0101_0101, 0)] byte operand)
    {
        byte opcode = 0b10_110_110;
        var sut = new Cpu(
            new RegisterFile { A = 0, HL = 0x0002 },
            new Rom([opcode, 0, operand])
        );

        var cycles = sut.Step();

        Assert.Multiple(() => {
            Assert.That(sut.Registers.A, Is.EqualTo(sut.Registers.A | operand));
            Assert.That(sut.Registers.F, Is.EqualTo(operand == 0 ? FlagsBit.Z : 0));
            Assert.That(cycles, Is.EqualTo(2));
        });
    }

    [Test]
    public void OrImm8([Values(0b0101_0101, 0)] byte operand)
    {
        byte opcode = 0b11_110_110;
        var sut = new Cpu(
            new RegisterFile { A = 0 },
            new Rom([opcode, operand, 0])
        );

        var cycles = sut.Step();

        Assert.Multiple(() => {
            Assert.That(sut.Registers.A, Is.EqualTo(sut.Registers.A | operand));
            Assert.That(sut.Registers.F, Is.EqualTo(operand == 0 ? FlagsBit.Z : 0));
            Assert.That(cycles, Is.EqualTo(2));
        });
    }
    #endregion OR instruction

    #region XOR instruction
    [Test]
    public void Xor(
        [ValueSource(nameof(StdRegister8))] Register8 src,
        [Values(0b0101_0101, 0b1110_0111)] byte operand
    )
    {
        var opcode = (byte)(0b10_101_000 | EncodeStdRegister8(src));
        var sut = new Cpu(
            new RegisterFile { A = 0b1110_0111, [src] = operand },
            new Rom([opcode, 0])
        );

        var a = sut.Registers.A;
        var cycles = sut.Step();

        Assert.Multiple(() => {
            Assert.That(sut.Registers.A, Is.EqualTo(a ^ operand));
            Assert.That(sut.Registers.F, Is.EqualTo(a == operand ? FlagsBit.Z : 0));
            Assert.That(cycles, Is.EqualTo(1));
        });
    }

    [Test]
    public void XorHL([Values(0b0101_0101, 0b1110_0111)] byte operand)
    {
        byte opcode = 0b10_101_110;
        var sut = new Cpu(
            new RegisterFile { A = 0b1110_0111, HL = 0x0002 },
            new Rom([opcode, 0, operand])
        );

        var a = sut.Registers.A;
        var cycles = sut.Step();

        Assert.Multiple(() => {
            Assert.That(sut.Registers.A, Is.EqualTo(a ^ operand));
            Assert.That(sut.Registers.F, Is.EqualTo(a == operand ? FlagsBit.Z : 0));
            Assert.That(cycles, Is.EqualTo(2));
        });
    }

    [Test]
    public void XorImm8([Values(0b0101_0101, 0b1110_0111)] byte operand)
    {
        byte opcode = 0b11_101_110;
        var sut = new Cpu(
            new RegisterFile { A = 0b1110_0111 },
            new Rom([opcode, operand, 0])
        );

        var a = sut.Registers.A;
        var cycles = sut.Step();

        Assert.Multiple(() => {
            Assert.That(sut.Registers.A, Is.EqualTo(a ^ operand));
            Assert.That(sut.Registers.F, Is.EqualTo(a == operand ? FlagsBit.Z : 0));
            Assert.That(cycles, Is.EqualTo(2));
        });
    }
    #endregion XOR instruction

    #region RLCA instruction
    [TestCase(0b0110_0010, 0b1100_0100, 0, 0)]
    [TestCase(0b1000_0010, 0b0000_0101, 0, FlagsBit.C)]
    [TestCase(0b0000_0000, 0b0000_0000, FlagsBit.Z, 0)]
    [TestCase(0b0000_0001, 0b0000_0010, FlagsBit.C, 0)]
    public void Rlca(byte before, byte after, FlagsBit flagsBefore, FlagsBit flagsAfter)
    {
        byte opcode = 0b00_000_111;
        var sut = new Cpu(
            new RegisterFile { A = before, F = flagsBefore },
            new Rom([opcode, 0])
        );

        var cycles = sut.Step();

        Assert.Multiple(() => {
            Assert.That(sut.Registers.A, Is.EqualTo(after));
            Assert.That(sut.Registers.F, Is.EqualTo(flagsAfter));
            Assert.That(cycles, Is.EqualTo(1));
        });
    }
    #endregion RLCA instruction

    #region RRCA instruction
    [TestCase(0b0110_0100, 0b0011_0010, 0, 0)]
    [TestCase(0b1000_0001, 0b1100_0000, 0, FlagsBit.C)]
    [TestCase(0b0000_0000, 0b0000_0000, FlagsBit.Z, 0)]
    [TestCase(0b0000_0010, 0b0000_0001, FlagsBit.C, 0)]
    public void Rrca(byte before, byte after, FlagsBit flagsBefore, FlagsBit flagsAfter)
    {
        byte opcode = 0b00_001_111;
        var sut = new Cpu(
            new RegisterFile { A = before, F = flagsBefore },
            new Rom([opcode, 0])
        );

        var cycles = sut.Step();

        Assert.Multiple(() => {
            Assert.That(sut.Registers.A, Is.EqualTo(after));
            Assert.That(sut.Registers.F, Is.EqualTo(flagsAfter));
            Assert.That(cycles, Is.EqualTo(1));
        });
    }
    #endregion RRCA instruction

    #region RLA instruction
    [TestCase(0b0110_0100, 0b1100_1000, 0, 0)]
    [TestCase(0b1000_0001, 0b0000_0010, 0, FlagsBit.C)]
    [TestCase(0b0000_0000, 0b0000_0000, FlagsBit.Z, 0)]
    [TestCase(0b0000_0100, 0b0000_1001, FlagsBit.C, 0)]
    [TestCase(0b1000_0100, 0b0000_1001, FlagsBit.C, FlagsBit.C)]
    public void Rla(byte before, byte after, FlagsBit flagsBefore, FlagsBit flagsAfter)
    {
        byte opcode = 0b00_010_111;
        var sut = new Cpu(
            new RegisterFile { A = before, F = flagsBefore },
            new Rom([opcode, 0])
        );

        var cycles = sut.Step();

        Assert.Multiple(() => {
            Assert.That(sut.Registers.A, Is.EqualTo(after));
            Assert.That(sut.Registers.F, Is.EqualTo(flagsAfter));
            Assert.That(cycles, Is.EqualTo(1));
        });
    }
    #endregion RLA instruction

    #region RRA instruction
    [TestCase(0b0110_0100, 0b0011_0010, 0, 0)]
    [TestCase(0b1000_0001, 0b0100_0000, 0, FlagsBit.C)]
    [TestCase(0b0000_0000, 0b0000_0000, FlagsBit.Z, 0)]
    [TestCase(0b0000_0010, 0b1000_0001, FlagsBit.C, 0)]
    [TestCase(0b0000_0001, 0b1000_0000, FlagsBit.C, FlagsBit.C)]
    public void Rra(byte before, byte after, FlagsBit flagsBefore, FlagsBit flagsAfter)
    {
        byte opcode = 0b00_011_111;
        var sut = new Cpu(
            new RegisterFile { A = before, F = flagsBefore },
            new Rom([opcode, 0])
        );

        var cycles = sut.Step();

        Assert.Multiple(() => {
            Assert.That(sut.Registers.A, Is.EqualTo(after));
            Assert.That(sut.Registers.F, Is.EqualTo(flagsAfter));
            Assert.That(cycles, Is.EqualTo(1));
        });
    }
    #endregion RRA instruction

    #region RLC instruction
    [Test]
    public void Rlc([ValueSource(nameof(StdRegister8))] Register8 dst)
    {
        var opcode = (byte)(0b00_000_000 | EncodeStdRegister8(dst));
        var sut = new Cpu(
            new RegisterFile { [dst] = 0b1001_0101 },
            new Rom([0xCB, opcode, 0])
        );

        var cycles = sut.Step();

        Assert.Multiple(() => {
            Assert.That(sut.Registers[dst], Is.EqualTo(0b0010_1011));
            Assert.That(sut.Registers.F, Is.EqualTo(FlagsBit.C));
            Assert.That(cycles, Is.EqualTo(2));
        });
    }

    [Test]
    public void RlcHL()
    {
        byte opcode = 0b00_000_110;
        var sut = new Cpu(
            new RegisterFile { HL = 0x0003 },
            new Ram([0xCB, opcode, 0, 0b1001_0101])
        );

        var cycles = sut.Step();

        Assert.Multiple(() => {
            Assert.That(sut.Memory.Read(0x0003), Is.EqualTo(0b0010_1011));
            Assert.That(sut.Registers.F, Is.EqualTo(FlagsBit.C));
            Assert.That(cycles, Is.EqualTo(4));
        });
    }

    [TestCase(0x01, 0x02, 0)]
    [TestCase(0x00, 0x00, FlagsBit.Z)]
    [TestCase(0x80, 0x01, FlagsBit.C)]
    public void RlcFlags(byte before, byte after, FlagsBit flags)
    {
        byte opcode = 0b00_000_000;
        var sut = new Cpu(
            new RegisterFile { B = before },
            new Rom([0xCB, opcode, 0])
        );

        var cycles = sut.Step();

        Assert.Multiple(() => {
            Assert.That(sut.Registers.B, Is.EqualTo(after));
            Assert.That(sut.Registers.F, Is.EqualTo(flags));
            Assert.That(cycles, Is.EqualTo(2));
        });
    }
    #endregion RLC instruction

    #region RRC instruction
    [Test]
    public void Rrc([ValueSource(nameof(StdRegister8))] Register8 dst)
    {
        var opcode = (byte)(0b00_001_000 | EncodeStdRegister8(dst));
        var sut = new Cpu(
            new RegisterFile { [dst] = 0b1001_0101 },
            new Rom([0xCB, opcode, 0])
        );

        var cycles = sut.Step();

        Assert.Multiple(() => {
            Assert.That(sut.Registers[dst], Is.EqualTo(0b1100_1010));
            Assert.That(sut.Registers.F, Is.EqualTo(FlagsBit.C));
            Assert.That(cycles, Is.EqualTo(2));
        });
    }

    [Test]
    public void RrcHL()
    {
        byte opcode = 0b00_001_110;
        var sut = new Cpu(
            new RegisterFile { HL = 0x0003 },
            new Ram([0xCB, opcode, 0, 0b1001_0101])
        );

        var cycles = sut.Step();

        Assert.Multiple(() => {
            Assert.That(sut.Memory.Read(0x0003), Is.EqualTo(0b1100_1010));
            Assert.That(sut.Registers.F, Is.EqualTo(FlagsBit.C));
            Assert.That(cycles, Is.EqualTo(4));
        });
    }

    [TestCase(0x02, 0x01, 0)]
    [TestCase(0x00, 0x00, FlagsBit.Z)]
    [TestCase(0x01, 0x80, FlagsBit.C)]
    public void RrcFlags(byte before, byte after, FlagsBit flags)
    {
        byte opcode = 0b00_001_000;
        var sut = new Cpu(
            new RegisterFile { B = before },
            new Rom([0xCB, opcode, 0])
        );

        var cycles = sut.Step();

        Assert.Multiple(() => {
            Assert.That(sut.Registers.B, Is.EqualTo(after));
            Assert.That(sut.Registers.F, Is.EqualTo(flags));
            Assert.That(cycles, Is.EqualTo(2));
        });
    }
    #endregion RRC instruction

    #region RL instruction
    [Test]
    public void Rl([ValueSource(nameof(StdRegister8))] Register8 dst)
    {
        var opcode = (byte)(0b00_010_000 | EncodeStdRegister8(dst));
        var sut = new Cpu(
            new RegisterFile { [dst] = 0b1001_0101, F = FlagsBit.C },
            new Rom([0xCB, opcode, 0])
        );

        var cycles = sut.Step();

        Assert.Multiple(() => {
            Assert.That(sut.Registers[dst], Is.EqualTo(0b0010_1011));
            Assert.That(sut.Registers.F, Is.EqualTo(FlagsBit.C));
            Assert.That(cycles, Is.EqualTo(2));
        });
    }

    [Test]
    public void RlHL()
    {
        byte opcode = 0b00_010_110;
        var sut = new Cpu(
            new RegisterFile { HL = 0x0003, F = FlagsBit.C },
            new Ram([0xCB, opcode, 0, 0b1001_0101])
        );

        var cycles = sut.Step();

        Assert.Multiple(() => {
            Assert.That(sut.Memory.Read(0x0003), Is.EqualTo(0b0010_1011));
            Assert.That(sut.Registers.F, Is.EqualTo(FlagsBit.C));
            Assert.That(cycles, Is.EqualTo(4));
        });
    }

    [TestCase(0b0000_0001, 0b0000_0010, 0,          0)]
    [TestCase(0b0000_0001, 0b0000_0011, FlagsBit.C, 0)]
    [TestCase(0b0000_0000, 0b0000_0000, 0,          FlagsBit.Z)]
    [TestCase(0b1000_0000, 0b0000_0000, 0,          FlagsBit.Z | FlagsBit.C)]
    [TestCase(0b1000_0000, 0b0000_0001, FlagsBit.C, FlagsBit.C)]
    public void RlFlags(byte before, byte after, FlagsBit flagsBefore, FlagsBit flagsAfter)
    {
        byte opcode = 0b00_010_000;
        var sut = new Cpu(
            new RegisterFile { B = before, F = flagsBefore },
            new Rom([0xCB, opcode, 0])
        );

        var cycles = sut.Step();

        Assert.Multiple(() => {
            Assert.That(sut.Registers.B, Is.EqualTo(after));
            Assert.That(sut.Registers.F, Is.EqualTo(flagsAfter));
            Assert.That(cycles, Is.EqualTo(2));
        });
    }
    #endregion RL instruction

    #region RR instruction
    [Test]
    public void Rr([ValueSource(nameof(StdRegister8))] Register8 dst)
    {
        var opcode = (byte)(0b00_011_000 | EncodeStdRegister8(dst));
        var sut = new Cpu(
            new RegisterFile { [dst] = 0b1001_0101, F = FlagsBit.C },
            new Rom([0xCB, opcode, 0])
        );

        var cycles = sut.Step();

        Assert.Multiple(() => {
            Assert.That(sut.Registers[dst], Is.EqualTo(0b1100_1010));
            Assert.That(sut.Registers.F, Is.EqualTo(FlagsBit.C));
            Assert.That(cycles, Is.EqualTo(2));
        });
    }

    [Test]
    public void RrHL()
    {
        byte opcode = 0b00_011_110;
        var sut = new Cpu(
            new RegisterFile { HL = 0x0003, F = FlagsBit.C },
            new Ram([0xCB, opcode, 0, 0b1001_0101])
        );

        var cycles = sut.Step();

        Assert.Multiple(() => {
            Assert.That(sut.Memory.Read(0x0003), Is.EqualTo(0b1100_1010));
            Assert.That(sut.Registers.F, Is.EqualTo(FlagsBit.C));
            Assert.That(cycles, Is.EqualTo(4));
        });
    }

    [TestCase(0b0000_0010, 0, 0b0000_0001, 0)]
    [TestCase(0b0000_0010, FlagsBit.C, 0b1000_0001, 0)]
    [TestCase(0b0000_0000, 0, 0b0000_0000, FlagsBit.Z)]
    [TestCase(0b0000_0001, 0, 0b0000_0000, FlagsBit.Z | FlagsBit.C)]
    [TestCase(0b0000_0001, FlagsBit.C, 0b1000_0000, FlagsBit.C)]
    public void RrFlags(byte before, FlagsBit flagsBefore, byte after, FlagsBit flagsAfter)
    {
        byte opcode = 0b00_011_000;
        var sut = new Cpu(
            new RegisterFile { B = before, F = flagsBefore },
            new Rom([0xCB, opcode, 0])
        );

        var cycles = sut.Step();

        Assert.Multiple(() => {
            Assert.That(sut.Registers.B, Is.EqualTo(after));
            Assert.That(sut.Registers.F, Is.EqualTo(flagsAfter));
            Assert.That(cycles, Is.EqualTo(2));
        });
    }
    #endregion RR instruction

    #region SLA instruction
    [Test]
    public void Sla([ValueSource(nameof(StdRegister8))] Register8 dst)
    {
        var opcode = (byte)(0b00_100_000 | EncodeStdRegister8(dst));
        var sut = new Cpu(
            new RegisterFile { [dst] = 0b1001_0101 },
            new Rom([0xCB, opcode, 0])
        );

        var cycles = sut.Step();

        Assert.Multiple(() => {
            Assert.That(sut.Registers[dst], Is.EqualTo(0b0010_1010));
            Assert.That(sut.Registers.F, Is.EqualTo(FlagsBit.C));
            Assert.That(cycles, Is.EqualTo(2));
        });
    }

    [Test]
    public void SlaHL()
    {
        byte opcode = 0b00_100_110;
        var sut = new Cpu(
            new RegisterFile { HL = 0x0003 },
            new Ram([0xCB, opcode, 0, 0b1001_0101])
        );

        var cycles = sut.Step();

        Assert.Multiple(() => {
            Assert.That(sut.Memory.Read(0x0003), Is.EqualTo(0b0010_1010));
            Assert.That(sut.Registers.F, Is.EqualTo(FlagsBit.C));
            Assert.That(cycles, Is.EqualTo(4));
        });
    }

    [TestCase(0b0000_0001, 0b0000_0010, 0)]
    [TestCase(0b0000_0000, 0b0000_0000, FlagsBit.Z)]
    [TestCase(0b1000_0000, 0b0000_0000, FlagsBit.Z | FlagsBit.C)]
    [TestCase(0b1100_0000, 0b1000_0000, FlagsBit.C)]
    public void SlaFlags(byte before, byte after, FlagsBit flags)
    {
        byte opcode = 0b00_100_000;
        var sut = new Cpu(
            new RegisterFile { B = before },
            new Rom([0xCB, opcode, 0])
        );

        var cycles = sut.Step();

        Assert.Multiple(() => {
            Assert.That(sut.Registers.B, Is.EqualTo(after));
            Assert.That(sut.Registers.F, Is.EqualTo(flags));
            Assert.That(cycles, Is.EqualTo(2));
        });
    }
    #endregion SLA instruction

    #region SRA instruction
    [Test]
    public void Sra([ValueSource(nameof(StdRegister8))] Register8 dst)
    {
        var opcode = (byte)(0b00_101_000 | EncodeStdRegister8(dst));
        var sut = new Cpu(
            new RegisterFile { [dst] = 0b1001_0101 },
            new Rom([0xCB, opcode, 0])
        );

        var cycles = sut.Step();

        Assert.Multiple(() => {
            Assert.That(sut.Registers[dst], Is.EqualTo(0b1100_1010));
            Assert.That(sut.Registers.F, Is.EqualTo(FlagsBit.C));
            Assert.That(cycles, Is.EqualTo(2));
        });
    }

    [Test]
    public void SraHL()
    {
        byte opcode = 0b00_101_110;
        var sut = new Cpu(
            new RegisterFile { HL = 0x0003 },
            new Ram([0xCB, opcode, 0, 0b1001_0101])
        );

        var cycles = sut.Step();

        Assert.Multiple(() => {
            Assert.That(sut.Memory.Read(0x0003), Is.EqualTo(0b1100_1010));
            Assert.That(sut.Registers.F, Is.EqualTo(FlagsBit.C));
            Assert.That(cycles, Is.EqualTo(4));
        });
    }

    [TestCase(0b0000_0010, 0b0000_0001, 0)]
    [TestCase(0b1000_0010, 0b1100_0001, 0)]
    [TestCase(0b0000_0000, 0b0000_0000, FlagsBit.Z)]
    [TestCase(0b0000_0001, 0b0000_0000, FlagsBit.Z | FlagsBit.C)]
    [TestCase(0b1000_0001, 0b1100_0000, FlagsBit.C)]
    public void SraFlags(byte before, byte after, FlagsBit flags)
    {
        byte opcode = 0b00_101_000;
        var sut = new Cpu(
            new RegisterFile { B = before },
            new Rom([0xCB, opcode, 0])
        );

        var cycles = sut.Step();

        Assert.Multiple(() => {
            Assert.That(sut.Registers.B, Is.EqualTo(after));
            Assert.That(sut.Registers.F, Is.EqualTo(flags));
            Assert.That(cycles, Is.EqualTo(2));
        });
    }
    #endregion SRA instruction

    #region SRL instruction
    [Test]
    public void Srl([ValueSource(nameof(StdRegister8))] Register8 dst)
    {
        var opcode = (byte)(0b00_111_000 | EncodeStdRegister8(dst));
        var sut = new Cpu(
            new RegisterFile { [dst] = 0b1001_0101 },
            new Rom([0xCB, opcode, 0])
        );

        var cycles = sut.Step();

        Assert.Multiple(() => {
            Assert.That(sut.Registers[dst], Is.EqualTo(0b0100_1010));
            Assert.That(sut.Registers.F, Is.EqualTo(FlagsBit.C));
            Assert.That(cycles, Is.EqualTo(2));
        });
    }

    [Test]
    public void SrlHL()
    {
        byte opcode = 0b00_111_110;
        var sut = new Cpu(
            new RegisterFile { HL = 0x0003 },
            new Ram([0xCB, opcode, 0, 0b1001_0101])
        );

        var cycles = sut.Step();

        Assert.Multiple(() => {
            Assert.That(sut.Memory.Read(0x0003), Is.EqualTo(0b0100_1010));
            Assert.That(sut.Registers.F, Is.EqualTo(FlagsBit.C));
            Assert.That(cycles, Is.EqualTo(4));
        });
    }

    [TestCase(0b0000_0010, 0b0000_0001, 0)]
    [TestCase(0b1000_0010, 0b0100_0001, 0)]
    [TestCase(0b0000_0000, 0b0000_0000, FlagsBit.Z)]
    [TestCase(0b0000_0001, 0b0000_0000, FlagsBit.Z | FlagsBit.C)]
    [TestCase(0b1000_0001, 0b0100_0000, FlagsBit.C)]
    public void SrlFlags(byte before, byte after, FlagsBit flags)
    {
        byte opcode = 0b00_111_000;
        var sut = new Cpu(
            new RegisterFile { B = before },
            new Rom([0xCB, opcode, 0])
        );

        var cycles = sut.Step();

        Assert.Multiple(() => {
            Assert.That(sut.Registers.B, Is.EqualTo(after));
            Assert.That(sut.Registers.F, Is.EqualTo(flags));
            Assert.That(cycles, Is.EqualTo(2));
        });
    }
    #endregion SRL instruction

    #region SWAP instruction
    [Test]
    public void Swap([ValueSource(nameof(StdRegister8))] Register8 dst)
    {
        var opcode = (byte)(0b00_110_000 | EncodeStdRegister8(dst));
        var sut = new Cpu(
            new RegisterFile { [dst] = 0b1001_0101 },
            new Rom([0xCB, opcode, 0])
        );

        var cycles = sut.Step();

        Assert.Multiple(() => {
            Assert.That(sut.Registers[dst], Is.EqualTo(0b0101_1001));
            Assert.That(sut.Registers.F, Is.EqualTo((FlagsBit)0));
            Assert.That(cycles, Is.EqualTo(2));
        });
    }

    [Test]
    public void SwapHL()
    {
        byte opcode = 0b00_110_110;
        var sut = new Cpu(
            new RegisterFile { HL = 0x0003 },
            new Ram([0xCB, opcode, 0, 0b1001_0101])
        );

        var cycles = sut.Step();

        Assert.Multiple(() => {
            Assert.That(sut.Memory.Read(0x0003), Is.EqualTo(0b0101_1001));
            Assert.That(sut.Registers.F, Is.EqualTo((FlagsBit)0));
            Assert.That(cycles, Is.EqualTo(4));
        });
    }

    [TestCase(0b1010_0101, 0b0101_1010, 0)]
    [TestCase(0b0000_0000, 0b0000_0000, FlagsBit.Z)]
    [TestCase(0b1111_0000, 0b0000_1111, 0)]
    public void SwapFlags(byte before, byte after, FlagsBit flags)
    {
        byte opcode = 0b00_110_000;
        var sut = new Cpu(
            new RegisterFile { B = before, F = FlagsBit.C | FlagsBit.H | FlagsBit.N },
            new Rom([0xCB, opcode, 0])
        );

        var cycles = sut.Step();

        Assert.Multiple(() => {
            Assert.That(sut.Registers.B, Is.EqualTo(after));
            Assert.That(sut.Registers.F, Is.EqualTo(flags));
            Assert.That(cycles, Is.EqualTo(2));
        });
    }
    #endregion SWAP instruction

    #region BIT instruction
    [Test]
    [Sequential]
    public void BitSet(
        [ValueSource(nameof(StdRegister8))] Register8 src,
        [Values(0, 1, 2, 3, 4, 5, 6)] byte bit
    )
    {
        var operand = (byte)(1 << bit);
        var opcode = (byte)(0b01_000_000 | (bit << 3) | EncodeStdRegister8(src));
        var sut = new Cpu(
            new RegisterFile { [src] = operand, F = FlagsBit.C },
            new Rom([0xCB, opcode, 0])
        );

        var cycles = sut.Step();

        Assert.Multiple(() => {
            Assert.That(sut.Registers[src], Is.EqualTo(operand));
            Assert.That(sut.Registers.F, Is.EqualTo(FlagsBit.H | FlagsBit.C));
            Assert.That(cycles, Is.EqualTo(2));
        });
    }

    [Test]
    [Sequential]
    public void BitUnset(
        [ValueSource(nameof(StdRegister8))] Register8 src,
        [Values(0, 1, 2, 3, 4, 5, 6)] byte bit
    )
    {
        var operand = (byte)~(1 << bit);
        var opcode = (byte)(0b01_000_000 | (bit << 3) | EncodeStdRegister8(src));
        var sut = new Cpu(
            new RegisterFile { [src] = operand, F = FlagsBit.C },
            new Rom([0xCB, opcode, 0])
        );

        var cycles = sut.Step();

        Assert.Multiple(() => {
            Assert.That(sut.Registers[src], Is.EqualTo(operand));
            Assert.That(sut.Registers.F, Is.EqualTo(FlagsBit.Z | FlagsBit.H | FlagsBit.C));
            Assert.That(cycles, Is.EqualTo(2));
        });
    }

    [Test]
    public void BitHL([Values(0, 2, 5, 7)] byte bit)
    {
        var operand = (byte)(1 << bit);
        var opcode = (byte)(0b01_000_110 | (bit << 3));
        var sut = new Cpu(
            new RegisterFile { HL = 0x0003, F = FlagsBit.C },
            new Ram([0xCB, opcode, 0, operand])
        );

        var cycles = sut.Step();

        Assert.Multiple(() => {
            Assert.That(sut.Memory.Read(0x0003), Is.EqualTo(operand));
            Assert.That(sut.Registers.F, Is.EqualTo(FlagsBit.H | FlagsBit.C));
            Assert.That(cycles, Is.EqualTo(3));
        });
    }

    [Test]
    public void BitHLUnset([Values(1, 3, 4, 6)] byte bit)
    {
        var operand = (byte)~(1 << bit);
        var opcode = (byte)(0b01_000_110 | (bit << 3));
        var sut = new Cpu(
            new RegisterFile { HL = 0x0003, F = FlagsBit.C },
            new Ram([0xCB, opcode, 0, operand])
        );

        var cycles = sut.Step();

        Assert.Multiple(() => {
            Assert.That(sut.Memory.Read(0x0003), Is.EqualTo(operand));
            Assert.That(sut.Registers.F, Is.EqualTo(FlagsBit.Z | FlagsBit.H | FlagsBit.C));
            Assert.That(cycles, Is.EqualTo(3));
        });
    }
    #endregion BIT instruction

    #region RES instruction
    [Test]
    public void Res(
        [ValueSource(nameof(StdRegister8))] Register8 dst,
        [Values(0, 2, 5, 7)] byte bit
    )
    {
        var opcode = (byte)(0b10_000_000 | (bit << 3) | EncodeStdRegister8(dst));
        var sut = new Cpu(
            new RegisterFile {
                [dst] = 0b1111_1111,
                F = FlagsBit.Z | FlagsBit.N | FlagsBit.H | FlagsBit.C,
            },
            new Rom([0xCB, opcode, 0])
        );

        var cycles = sut.Step();

        Assert.Multiple(() => {
            Assert.That(sut.Registers[dst], Is.EqualTo((byte)~(1 << bit)));
            Assert.That(
                sut.Registers.F,
                Is.EqualTo(FlagsBit.Z | FlagsBit.N | FlagsBit.H | FlagsBit.C)
            );
            Assert.That(cycles, Is.EqualTo(2));
        });
    }

    [Test]
    public void ResAlreadyZero([Values(1, 3, 4, 6)] byte bit)
    {
        var opcode = (byte)(0b10_000_000 | (bit << 3));
        var sut = new Cpu(
            new RegisterFile { B = 0b0000_0000 },
            new Rom([0xCB, opcode, 0])
        );

        var cycles = sut.Step();

        Assert.Multiple(() => {
            Assert.That(sut.Registers.B, Is.EqualTo(0b0000_0000));
            Assert.That(sut.Registers.F, Is.EqualTo((FlagsBit)0));
            Assert.That(cycles, Is.EqualTo(2));
        });
    }

    [Test]
    public void ResHL([Values(0, 1, 3, 4, 5, 7)] byte bit)
    {
        var opcode = (byte)(0b10_000_110 | (bit << 3));
        var sut = new Cpu(
            new RegisterFile {
                HL = 0x0003,
                F = FlagsBit.Z | FlagsBit.N | FlagsBit.H | FlagsBit.C,
            },
            new Ram([0xCB, opcode, 0, 0b1111_1111])
        );

        var cycles = sut.Step();

        Assert.Multiple(() => {
            Assert.That(sut.Memory.Read(0x0003), Is.EqualTo((byte)~(1 << bit)));
            Assert.That(
                sut.Registers.F,
                Is.EqualTo(FlagsBit.Z | FlagsBit.N | FlagsBit.H | FlagsBit.C)
            );
            Assert.That(cycles, Is.EqualTo(4));
        });
    }
    #endregion RES instruction

    #region SET instruction
    [Test]
    public void Set(
        [ValueSource(nameof(StdRegister8))] Register8 dst,
        [Values(0, 2, 5, 7)] byte bit
    )
    {
        var opcode = (byte)(0b11_000_000 | (bit << 3) | EncodeStdRegister8(dst));
        var sut = new Cpu(
            new RegisterFile {
                [dst] = 0b0000_0000,
                F = FlagsBit.Z | FlagsBit.N | FlagsBit.H | FlagsBit.C,
            },
            new Rom([0xCB, opcode, 0])
        );

        var cycles = sut.Step();

        Assert.Multiple(() => {
            Assert.That(sut.Registers[dst], Is.EqualTo((byte)(1 << bit)));
            Assert.That(
                sut.Registers.F,
                Is.EqualTo(FlagsBit.Z | FlagsBit.N | FlagsBit.H | FlagsBit.C)
            );
            Assert.That(cycles, Is.EqualTo(2));
        });
    }

    [Test]
    public void SetAlreadyOne([Values(1, 3, 4, 6)] byte bit)
    {
        var opcode = (byte)(0b11_000_000 | (bit << 3));
        var sut = new Cpu(
            new RegisterFile { B = 0b1111_1111 },
            new Rom([0xCB, opcode, 0])
        );

        var cycles = sut.Step();

        Assert.Multiple(() => {
            Assert.That(sut.Registers.B, Is.EqualTo(0b1111_1111));
            Assert.That(sut.Registers.F, Is.EqualTo((FlagsBit)0));
            Assert.That(cycles, Is.EqualTo(2));
        });
    }

    [Test]
    public void SetHL([Values(0, 1, 3, 4, 5, 7)] byte bit)
    {
        var opcode = (byte)(0b11_000_110 | (bit << 3));
        var sut = new Cpu(
            new RegisterFile {
                HL = 0x0003,
                F = FlagsBit.Z | FlagsBit.N | FlagsBit.H | FlagsBit.C,
            },
            new Ram([0xCB, opcode, 0, 0b0000_0000])
        );

        var cycles = sut.Step();

        Assert.Multiple(() => {
            Assert.That(sut.Memory.Read(0x0003), Is.EqualTo((byte)(1 << bit)));
            Assert.That(
                sut.Registers.F,
                Is.EqualTo(FlagsBit.Z | FlagsBit.N | FlagsBit.H | FlagsBit.C)
            );
            Assert.That(cycles, Is.EqualTo(4));
        });
    }
    #endregion SET instruction
}
