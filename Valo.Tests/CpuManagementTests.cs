namespace Valo.Tests;

public class CpuManagementTests : CpuTestsBase
{
    [Test]
    public void Nop()
    {
        byte opcode = 0b00000000;
        var registers = new RegisterFile();
        var sut = new Cpu(
            registers,
            new Rom([opcode, 0])
        );

        var cycles = sut.Step();

        Assert.Multiple(() => {
            Assert.That(sut.Registers, Is.EqualTo(registers with { PC = 0x0002 }));
            Assert.That(cycles, Is.EqualTo(1));
        });
    }

    [Test]
    public void Ei()
    {
        byte opcode = 0b11111011;
        var sut = new Cpu(
            new RegisterFile(),
            new Rom([opcode, 0])
        );

        var cycles = sut.Step();

        Assert.Multiple(() => {
            Assert.That(sut.Registers.IME, Is.True);
            Assert.That(cycles, Is.EqualTo(1));
        });
    }

    [Test]
    public void Di()
    {
        byte opcode = 0b11110011;
        var sut = new Cpu(
            new RegisterFile { IME = true },
            new Rom([opcode, 0])
        );

        var cycles = sut.Step();

        Assert.Multiple(() => {
            Assert.That(sut.Registers.IME, Is.False);
            Assert.That(cycles, Is.EqualTo(1));
        });
    }
}
