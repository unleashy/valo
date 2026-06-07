namespace Valo.Tests;

public class RegisterFileTests
{
    [Test]
    public void InitialState()
    {
        var sut = new RegisterFile();

        Assert.Multiple(() => {
           Assert.That(sut[Register8.A], Is.Zero);
           Assert.That(sut[Register8.F], Is.Zero);
           Assert.That(sut[Register8.B], Is.Zero);
           Assert.That(sut[Register8.C], Is.Zero);
           Assert.That(sut[Register8.D], Is.Zero);
           Assert.That(sut[Register8.E], Is.Zero);
           Assert.That(sut[Register8.H], Is.Zero);
           Assert.That(sut[Register8.L], Is.Zero);

           Assert.That(sut[Register16.AF], Is.Zero);
           Assert.That(sut[Register16.BC], Is.Zero);
           Assert.That(sut[Register16.DE], Is.Zero);
           Assert.That(sut[Register16.HL], Is.Zero);
           Assert.That(sut[Register16.PC], Is.Zero);
           Assert.That(sut[Register16.SP], Is.Zero);
        });
    }

    [Test]
    public void SetRegister8()
    {
        var sut = new RegisterFile();

        sut[Register8.A] = 0x12;
        sut[Register8.F] = 0x34;
        sut[Register8.B] = 0x56;
        sut[Register8.C] = 0x78;
        sut[Register8.D] = 0x9A;
        sut[Register8.E] = 0xBC;
        sut[Register8.H] = 0xDE;
        sut[Register8.L] = 0xF0;

        Assert.Multiple(() => {
            Assert.That(sut[Register8.A], Is.EqualTo(0x12));
            Assert.That(sut[Register8.F], Is.EqualTo(0x34));
            Assert.That(sut[Register8.B], Is.EqualTo(0x56));
            Assert.That(sut[Register8.C], Is.EqualTo(0x78));
            Assert.That(sut[Register8.D], Is.EqualTo(0x9A));
            Assert.That(sut[Register8.E], Is.EqualTo(0xBC));
            Assert.That(sut[Register8.H], Is.EqualTo(0xDE));
            Assert.That(sut[Register8.L], Is.EqualTo(0xF0));

            Assert.That(sut[Register16.AF], Is.EqualTo(0x1234));
            Assert.That(sut[Register16.BC], Is.EqualTo(0x5678));
            Assert.That(sut[Register16.DE], Is.EqualTo(0x9ABC));
            Assert.That(sut[Register16.HL], Is.EqualTo(0xDEF0));
        });
    }

    [Test]
    public void SetRegister16()
    {
        var sut = new RegisterFile();

        sut[Register16.AF] = 0x1122;
        sut[Register16.BC] = 0x3344;
        sut[Register16.DE] = 0x5566;
        sut[Register16.HL] = 0x7788;
        sut[Register16.PC] = 0x99AA;
        sut[Register16.SP] = 0xBBCC;

        Assert.Multiple(() => {
            Assert.That(sut[Register16.AF], Is.EqualTo(0x1122));
            Assert.That(sut[Register16.BC], Is.EqualTo(0x3344));
            Assert.That(sut[Register16.DE], Is.EqualTo(0x5566));
            Assert.That(sut[Register16.HL], Is.EqualTo(0x7788));
            Assert.That(sut[Register16.PC], Is.EqualTo(0x99AA));
            Assert.That(sut[Register16.SP], Is.EqualTo(0xBBCC));

            Assert.That(sut[Register8.A], Is.EqualTo(0x11));
            Assert.That(sut[Register8.F], Is.EqualTo(0x22));
            Assert.That(sut[Register8.B], Is.EqualTo(0x33));
            Assert.That(sut[Register8.C], Is.EqualTo(0x44));
            Assert.That(sut[Register8.D], Is.EqualTo(0x55));
            Assert.That(sut[Register8.E], Is.EqualTo(0x66));
            Assert.That(sut[Register8.H], Is.EqualTo(0x77));
            Assert.That(sut[Register8.L], Is.EqualTo(0x88));
        });
    }

    [Test]
    public void InitialFlags()
    {
        var sut = new RegisterFile();

        Assert.Multiple(() => {
            Assert.That(sut.GetFlag(FlagsBit.Z), Is.False);
            Assert.That(sut.GetFlag(FlagsBit.N), Is.False);
            Assert.That(sut.GetFlag(FlagsBit.H), Is.False);
            Assert.That(sut.GetFlag(FlagsBit.C), Is.False);
        });
    }

    [Test]
    public void SetAllFlags()
    {
        var sut = new RegisterFile();

        sut.SetFlag(FlagsBit.Z, true);
        sut.SetFlag(FlagsBit.N, true);
        sut.SetFlag(FlagsBit.H, true);
        sut.SetFlag(FlagsBit.C, true);

        Assert.Multiple(() => {
            Assert.That(sut.GetFlag(FlagsBit.Z), Is.True);
            Assert.That(sut.GetFlag(FlagsBit.N), Is.True);
            Assert.That(sut.GetFlag(FlagsBit.H), Is.True);
            Assert.That(sut.GetFlag(FlagsBit.C), Is.True);

            Assert.That(sut[Register8.F], Is.EqualTo(0b1111_0000));
        });
    }

    [Test]
    public void SetSomeFlags()
    {
        var sut = new RegisterFile();

        sut.SetFlag(FlagsBit.Z, false);
        sut.SetFlag(FlagsBit.N, true);
        sut.SetFlag(FlagsBit.H, false);
        sut.SetFlag(FlagsBit.C, true);

        Assert.Multiple(() => {
            Assert.That(sut.GetFlag(FlagsBit.Z), Is.False);
            Assert.That(sut.GetFlag(FlagsBit.N), Is.True);
            Assert.That(sut.GetFlag(FlagsBit.H), Is.False);
            Assert.That(sut.GetFlag(FlagsBit.C), Is.True);

            Assert.That(sut[Register8.F], Is.EqualTo(0b0101_0000));
        });
    }
}
