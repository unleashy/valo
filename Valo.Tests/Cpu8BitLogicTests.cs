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
}
