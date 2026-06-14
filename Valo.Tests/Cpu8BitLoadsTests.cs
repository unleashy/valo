namespace Valo.Tests;

public class Cpu8BitLoadsTests : CpuTestsBase
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
            Assert.That(sut.Registers[dst], Is.EqualTo(0x67));
            Assert.That(cycles, Is.EqualTo(2));
        });
    }

    [Test]
    public void LoadRegisterIndirectHL([ValueSource(nameof(StdRegister8))] Register8 dst)
    {
        var opcode = (byte)(0b01_000_110 | (EncodeStdRegister8(dst) << 3));
        var sut = new Cpu(
            new RegisterFile { HL = 0x0002 },
            new Rom([opcode, 0, 0x42])
        );

        var cycles = sut.Step();

        Assert.Multiple(() => {
            Assert.That(sut.Registers[dst], Is.EqualTo(0x42));
            Assert.That(cycles, Is.EqualTo(2));
        });
    }

    [Test]
    public void LoadIndirectHLRegister([ValueSource(nameof(StdRegister8))] Register8 src)
    {
        var opcode = (byte)(0b01_110_000 | EncodeStdRegister8(src));
        var sut = new Cpu(
            new RegisterFile { [src] = 0x42, HL = 0x0002 },
            new Ram([opcode, 0, 0xFF])
        );

        var cycles = sut.Step();

        Assert.Multiple(() => {
            Assert.That(sut.Memory.Read(0x0002), Is.EqualTo(sut.Registers[src]));
            Assert.That(cycles, Is.EqualTo(2));
        });
    }

    [Test]
    public void LoadIndirectHLImmediate([ValueSource(nameof(StdRegister8))] Register8 src)
    {
        byte opcode = 0b00_110_110;
        var sut = new Cpu(
            new RegisterFile { HL = 0x0003 },
            new Ram([opcode, 0x42, 0, 0xFF])
        );

        var cycles = sut.Step();

        Assert.Multiple(() => {
            Assert.That(sut.Memory.Read(0x0003), Is.EqualTo(0x42));
            Assert.That(cycles, Is.EqualTo(3));
        });
    }

    [Test]
    public void LoadAIndirect16([Values(Register16.BC, Register16.DE)] Register16 src)
    {
        var opcode = (byte)(src == Register16.BC ? 0b00_00_1010 : 0b00_01_1010);
        var sut = new Cpu(
            new RegisterFile { [src] = 0x0002 },
            new Rom([opcode, 0, 0x42])
        );

        var cycles = sut.Step();

        Assert.Multiple(() => {
            Assert.That(sut.Registers.A, Is.EqualTo(0x42));
            Assert.That(cycles, Is.EqualTo(2));
        });
    }

    [Test]
    public void LoadIndirect16A([Values(Register16.BC, Register16.DE)] Register16 dst)
    {
        var opcode = (byte)(dst == Register16.BC ? 0b00_00_0010 : 0b00_01_0010);
        var sut = new Cpu(
            new RegisterFile { A = 0x42, [dst] = 0x0002 },
            new Ram([opcode, 0, 0xFF])
        );

        var cycles = sut.Step();

        Assert.Multiple(() => {
            Assert.That(sut.Memory.Read(0x0002), Is.EqualTo(0x42));
            Assert.That(cycles, Is.EqualTo(2));
        });
    }

    [Test]
    public void LoadADirect16()
    {
        byte opcode = 0b11_111010;
        var sut = new Cpu(
            new RegisterFile(),
            new Rom([opcode, 0x04, 0x00, 0, 0x42])
        );

        var cycles = sut.Step();

        Assert.Multiple(() => {
            Assert.That(sut.Registers.A, Is.EqualTo(0x42));
            Assert.That(cycles, Is.EqualTo(4));
        });
    }

    [Test]
    public void LoadDirect16A()
    {
        byte opcode = 0b11_101010;
        var sut = new Cpu(
            new RegisterFile { A = 0x42 },
            new Ram([opcode, 0x04, 0x00, 0, 0xFF])
        );

        var cycles = sut.Step();

        Assert.Multiple(() => {
            Assert.That(sut.Memory.Read(0x0004), Is.EqualTo(0x42));
            Assert.That(cycles, Is.EqualTo(4));
        });
    }

    [Test]
    public void LoadAHighC()
    {
        byte opcode = 0b11_110010;
        var sut = new Cpu(
            new RegisterFile { C = 0xF1 },
            new MappedMemory.Builder()
                .Map(0x0000, 0x0002, new Rom([opcode, 0]))
                .Map(0xFFF1, 0xFFF2, new Rom([0x42]))
                .Build()
        );

        var cycles = sut.Step();

        Assert.Multiple(() => {
            Assert.That(sut.Registers.A, Is.EqualTo(0x42));
            Assert.That(cycles, Is.EqualTo(2));
        });
    }

    [Test]
    public void LoadHighCA()
    {
        byte opcode = 0b11_100010;
        var sut = new Cpu(
            new RegisterFile { C = 0xF1, A = 0x42 },
            new MappedMemory.Builder()
                .Map(0x0000, new Rom([opcode, 0]))
                .Map(0xFFF1, new Ram([0xFF]))
                .Build()
        );

        var cycles = sut.Step();

        Assert.Multiple(() => {
            Assert.That(sut.Memory.Read(0xFFF1), Is.EqualTo(0x42));
            Assert.That(cycles, Is.EqualTo(2));
        });
    }

    [Test]
    public void LoadAHighImmediate()
    {
        byte opcode = 0b11_110000;
        var sut = new Cpu(
            new RegisterFile(),
            new MappedMemory.Builder()
                .Map(0x0000, new Rom([opcode, 0xF1, 0]))
                .Map(0xFFF1, new Rom([0x42]))
                .Build()
        );

        var cycles = sut.Step();

        Assert.Multiple(() => {
            Assert.That(sut.Registers.A, Is.EqualTo(0x42));
            Assert.That(cycles, Is.EqualTo(3));
        });
    }

    [Test]
    public void LoadHighCImmediate()
    {
        byte opcode = 0b11_100000;
        var sut = new Cpu(
            new RegisterFile { A = 0x42 },
            new MappedMemory.Builder()
                .Map(0x0000, new Rom([opcode, 0xF1, 0]))
                .Map(0xFFF1, new Ram([0xFF]))
                .Build()
        );

        var cycles = sut.Step();

        Assert.Multiple(() => {
            Assert.That(sut.Memory.Read(0xFFF1), Is.EqualTo(0x42));
            Assert.That(cycles, Is.EqualTo(3));
        });
    }

    [Test]
    public void LoadAIncHL()
    {
        byte opcode = 0b00_10_1010;
        var sut = new Cpu(
            new RegisterFile { HL = 0x0002 },
            new Rom([opcode, 0, 0x42])
        );

        var cycles = sut.Step();

        Assert.Multiple(() => {
            Assert.That(sut.Registers.A, Is.EqualTo(0x42));
            Assert.That(sut.Registers.HL, Is.EqualTo(0x0003));
            Assert.That(cycles, Is.EqualTo(2));
        });
    }

    [Test]
    public void LoadADecHL()
    {
        byte opcode = 0b00_11_1010;
        var sut = new Cpu(
            new RegisterFile { HL = 0x0002 },
            new Rom([opcode, 0, 0x42])
        );

        var cycles = sut.Step();

        Assert.Multiple(() => {
            Assert.That(sut.Registers.A, Is.EqualTo(0x42));
            Assert.That(sut.Registers.HL, Is.EqualTo(0x0001));
            Assert.That(cycles, Is.EqualTo(2));
        });
    }

    [Test]
    public void LoadIncHLA()
    {
        byte opcode = 0b00_10_0010;
        var sut = new Cpu(
            new RegisterFile { A = 0x42, HL = 0x0002 },
            new Ram([opcode, 0, 0x42])
        );

        var cycles = sut.Step();

        Assert.Multiple(() => {
            Assert.That(sut.Memory.Read(0x0002), Is.EqualTo(0x42));
            Assert.That(sut.Registers.HL, Is.EqualTo(0x0003));
            Assert.That(cycles, Is.EqualTo(2));
        });
    }

    [Test]
    public void LoadDecHLA()
    {
        byte opcode = 0b00_11_0010;
        var sut = new Cpu(
            new RegisterFile { A = 0x42, HL = 0x0002 },
            new Ram([opcode, 0, 0xFF])
        );

        var cycles = sut.Step();

        Assert.Multiple(() => {
            Assert.That(sut.Memory.Read(0x0002), Is.EqualTo(0x42));
            Assert.That(sut.Registers.HL, Is.EqualTo(0x0001));
            Assert.That(cycles, Is.EqualTo(2));
        });
    }
}
