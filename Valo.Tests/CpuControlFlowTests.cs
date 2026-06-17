namespace Valo.Tests;

public class CpuControlFlowTests : CpuTestsBase
{
    [Test]
    public void Jp()
    {
        byte opcode = 0b11_000011;
        var sut = new Cpu(
            new RegisterFile(),
            new MappedMemory.Builder()
                .Map(0x0000, new Rom([opcode, 0x34, 0x12]))
                .Map(0x1234, new Rom([0]))
                .Build()
        );

        var cycles = sut.Step();

        Assert.Multiple(() => {
            // The fetch happens at 0x1234, but PC is always incremented by one
            Assert.That(sut.Registers.PC, Is.EqualTo(0x1234 + 1));
            Assert.That(cycles, Is.EqualTo(4));
        });
    }

    [Test]
    public void JpHL()
    {
        byte opcode = 0b11_101001;
        var sut = new Cpu(
            new RegisterFile { HL = 0x1234 },
            new MappedMemory.Builder()
                .Map(0x0000, new Rom([opcode]))
                .Map(0x1234, new Rom([0]))
                .Build()
        );

        var cycles = sut.Step();

        Assert.Multiple(() => {
            Assert.That(sut.Registers.PC, Is.EqualTo(0x1234 + 1));
            Assert.That(cycles, Is.EqualTo(1));
        });
    }

    [TestCase(0b00, 0,          true)]
    [TestCase(0b00, FlagsBit.Z, false)]
    [TestCase(0b01, FlagsBit.Z, true)]
    [TestCase(0b01, 0,          false)]
    [TestCase(0b10, 0,          true)]
    [TestCase(0b10, FlagsBit.C, false)]
    [TestCase(0b11, FlagsBit.C, true)]
    [TestCase(0b11, 0,          false)]
    public void JpCond(int condition, FlagsBit flags, bool doesJump)
    {
        var opcode = (byte)(0b110_00_010 | (condition << 3));
        var sut = new Cpu(
            new RegisterFile { HL = 0x1234, F = flags },
            new MappedMemory.Builder()
                .Map(0x0000, new Rom([opcode, 0x34, 0x12, 0]))
                .Map(0x1234, new Rom([0]))
                .Build()
        );

        var cycles = sut.Step();

        Assert.Multiple(() => {
            if (doesJump) {
                Assert.That(sut.Registers.PC, Is.EqualTo(0x1235));
                Assert.That(cycles, Is.EqualTo(4));
            }
            else {
                Assert.That(sut.Registers.PC, Is.EqualTo(0x0004));
                Assert.That(cycles, Is.EqualTo(3));
            }
        });
    }

    [TestCase(+0x10)]
    [TestCase(-0x10)]
    [TestCase(+0x7F)]
    [TestCase(-0x7F)]
    public void Jr(sbyte offset)
    {
        byte opcode = 0b00_011000;
        var sut = new Cpu(
            new RegisterFile { PC = 0x0100 },
            new MappedMemory.Builder()
                .Map(0x0100, new Rom([opcode, (byte)offset]))
                // The offset is added to PC *after* the immediate, so add a bias of 2
                .Map((ushort)(0x0100 + offset + 2), new Rom([0]))
                .Build()
        );

        var cycles = sut.Step();

        Assert.Multiple(() => {
            Assert.That(sut.Registers.PC, Is.EqualTo((ushort)(0x0100 + offset + 3)));
            Assert.That(cycles, Is.EqualTo(3));
        });
    }

    [TestCase(0b00, 0,          true)]
    [TestCase(0b00, FlagsBit.Z, false)]
    [TestCase(0b01, FlagsBit.Z, true)]
    [TestCase(0b01, 0,          false)]
    [TestCase(0b10, 0,          true)]
    [TestCase(0b10, FlagsBit.C, false)]
    [TestCase(0b11, FlagsBit.C, true)]
    [TestCase(0b11, 0,          false)]
    public void JrCond(int condition, FlagsBit flags, bool doesJump)
    {
        var opcode = (byte)(0b001_00_000 | (condition << 3));
        var sut = new Cpu(
            new RegisterFile { F = flags },
            new MappedMemory.Builder()
                .Map(0x0000, new Rom([opcode, 0x08, 0]))
                .Map(0x000A, new Rom([0]))
                .Build()
        );

        var cycles = sut.Step();

        Assert.Multiple(() => {
            if (doesJump) {
                Assert.That(sut.Registers.PC, Is.EqualTo(0x000B));
                Assert.That(cycles, Is.EqualTo(3));
            }
            else {
                Assert.That(sut.Registers.PC, Is.EqualTo(0x0003));
                Assert.That(cycles, Is.EqualTo(2));
            }
        });
    }

    [Test]
    public void Call()
    {
        byte opcode = 0b11_001101;
        var sut = new Cpu(
            new RegisterFile { PC = 0x1000, SP = 0xA002 },
            new MappedMemory.Builder()
                .Map(0x1000, new Rom([opcode, 0x78, 0x56]))
                .Map(0x5678, new Rom([0]))
                .Map(0xA000, new Ram([0, 0]))
                .Build()
        );

        var cycles = sut.Step();

        Assert.Multiple(() => {
            Assert.That(sut.Registers.PC, Is.EqualTo(0x5679));
            Assert.That(sut.Registers.SP, Is.EqualTo(0xA000));
            Assert.That(sut.Memory.Read(0xA000), Is.EqualTo(0x03));
            Assert.That(sut.Memory.Read(0xA001), Is.EqualTo(0x10));
            Assert.That(cycles, Is.EqualTo(6));
        });
    }

    [TestCase(0b00, 0,          true)]
    [TestCase(0b00, FlagsBit.Z, false)]
    [TestCase(0b01, FlagsBit.Z, true)]
    [TestCase(0b01, 0,          false)]
    [TestCase(0b10, 0,          true)]
    [TestCase(0b10, FlagsBit.C, false)]
    [TestCase(0b11, FlagsBit.C, true)]
    [TestCase(0b11, 0,          false)]
    public void CallCond(int condition, FlagsBit flags, bool doesCall)
    {
        var opcode = (byte)(0b110_00_100 | (condition << 3));
        var sut = new Cpu(
            new RegisterFile { PC = 0x1000, SP = 0xA002, F = flags },
            new MappedMemory.Builder()
                .Map(0x1000, new Rom([opcode, 0x78, 0x56, 0]))
                .Map(0x5678, new Rom([0]))
                .Map(0xA000, new Ram([0, 0]))
                .Build()
        );

        var cycles = sut.Step();

        Assert.Multiple(() => {
            if (doesCall) {
                Assert.That(sut.Registers.PC, Is.EqualTo(0x5679));
                Assert.That(sut.Registers.SP, Is.EqualTo(0xA000));
                Assert.That(sut.Memory.Read(0xA000), Is.EqualTo(0x03));
                Assert.That(sut.Memory.Read(0xA001), Is.EqualTo(0x10));
                Assert.That(cycles, Is.EqualTo(6));
            }
            else {
                Assert.That(sut.Registers.PC, Is.EqualTo(0x1004));
                Assert.That(sut.Registers.SP, Is.EqualTo(0xA002));
                Assert.That(sut.Memory.Read(0xA000), Is.EqualTo(0x00));
                Assert.That(sut.Memory.Read(0xA001), Is.EqualTo(0x00));
                Assert.That(cycles, Is.EqualTo(3));
            }
        });
    }

    [Test]
    public void Ret()
    {
        byte opcode = 0b11_001001;
        var sut = new Cpu(
            new RegisterFile { PC = 0x5678, SP = 0xA000 },
            new MappedMemory.Builder()
                .Map(0x1000, new Rom([0]))
                .Map(0x5678, new Rom([opcode]))
                .Map(0xA000, new Rom([0x00, 0x10]))
                .Build()
        );

        var cycles = sut.Step();

        Assert.Multiple(() => {
            Assert.That(sut.Registers.PC, Is.EqualTo(0x1001));
            Assert.That(sut.Registers.SP, Is.EqualTo(0xA002));
            Assert.That(cycles, Is.EqualTo(4));
        });
    }

    [TestCase(0b00, 0,          true)]
    [TestCase(0b00, FlagsBit.Z, false)]
    [TestCase(0b01, FlagsBit.Z, true)]
    [TestCase(0b01, 0,          false)]
    [TestCase(0b10, 0,          true)]
    [TestCase(0b10, FlagsBit.C, false)]
    [TestCase(0b11, FlagsBit.C, true)]
    [TestCase(0b11, 0,          false)]
    public void RetCond(int condition, FlagsBit flags, bool doesReturn)
    {
        var opcode = (byte)(0b110_00_000 | (condition << 3));
        var sut = new Cpu(
            new RegisterFile { PC = 0x5678, SP = 0xA000, F = flags },
            new MappedMemory.Builder()
                .Map(0x1000, new Rom([0]))
                .Map(0x5678, new Rom([opcode, 0]))
                .Map(0xA000, new Rom([0x00, 0x10]))
                .Build()
        );

        var cycles = sut.Step();

        Assert.Multiple(() => {
            if (doesReturn) {
                Assert.That(sut.Registers.PC, Is.EqualTo(0x1001));
                Assert.That(sut.Registers.SP, Is.EqualTo(0xA002));
                Assert.That(cycles, Is.EqualTo(5));
            }
            else {
                Assert.That(sut.Registers.PC, Is.EqualTo(0x567A));
                Assert.That(sut.Registers.SP, Is.EqualTo(0xA000));
                Assert.That(cycles, Is.EqualTo(2));
            }
        });
    }

    [Test]
    public void Reti()
    {
        byte opcode = 0b11_011001;
        var sut = new Cpu(
            new RegisterFile { PC = 0x5678, SP = 0xA000 },
            new MappedMemory.Builder()
                .Map(0x1000, new Rom([0]))
                .Map(0x5678, new Rom([opcode]))
                .Map(0xA000, new Rom([0x00, 0x10]))
                .Map(0xFF0F, new Rom([0]))
                .Map(0xFFFF, new Rom([0]))
                .Build()
        );

        var cycles = sut.Step();

        Assert.Multiple(() => {
            Assert.That(sut.Registers.PC, Is.EqualTo(0x1001));
            Assert.That(sut.Registers.SP, Is.EqualTo(0xA002));
            Assert.That(sut.Registers.IME, Is.True);
            Assert.That(cycles, Is.EqualTo(4));
        });
    }

    [TestCase(0, 0x0000)]
    public void Rst(int vector, int target)
    {
        var opcode = (byte)(0b11_000_111 | (vector << 3));
        var sut = new Cpu(
            new RegisterFile { PC = 0x1000, SP = 0xA002 },
            new MappedMemory.Builder()
                .Map((ushort)target, new Rom([0]))
                .Map(0x1000, new Rom([opcode, 0]))
                .Map(0xA000, new Ram([0, 0]))
                .Build()
        );

        var cycles = sut.Step();

        Assert.Multiple(() => {
            Assert.That(sut.Registers.PC, Is.EqualTo(target + 1));
            Assert.That(sut.Registers.SP, Is.EqualTo(0xA000));
            Assert.That(sut.Memory.Read(0xA000), Is.EqualTo(0x01));
            Assert.That(sut.Memory.Read(0xA001), Is.EqualTo(0x10));
            Assert.That(cycles, Is.EqualTo(4));
        });
    }
}
