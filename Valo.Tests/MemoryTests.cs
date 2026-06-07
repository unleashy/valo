using System.Collections.Immutable;

namespace Valo.Tests;

public class MemoryTests
{
    [Test]
    public void RomRead()
    {
        ImmutableArray<byte> bytes = [0, 1, 2];
        var sut = new Rom(bytes);

        Assert.Multiple(() => {
            Assert.That(sut.Read(0), Is.EqualTo(bytes[0]));
            Assert.That(sut.Read(1), Is.EqualTo(bytes[1]));
            Assert.That(sut.Read(2), Is.EqualTo(bytes[2]));
        });
    }

    [Test]
    public void RomWriteFails()
    {
        ImmutableArray<byte> bytes = [0, 1, 2];
        var sut = new Rom(bytes);

        Assert.Throws<InvalidOperationException>(() => sut.Write(0, 255));
    }

    [Test]
    public void RamReadWrite()
    {
        byte[] bytes = [0, 1, 2];
        var sut = new Ram(bytes);

        Assert.Multiple(() => {
            Assert.That(sut.Read(0), Is.EqualTo(bytes[0]));
            Assert.That(sut.Read(1), Is.EqualTo(bytes[1]));
            Assert.That(sut.Read(2), Is.EqualTo(bytes[2]));
        });

        sut.Write(0, 255);

        Assert.Multiple(() => {
            Assert.That(sut.Read(0), Is.EqualTo(255));
            Assert.That(sut.Read(1), Is.EqualTo(bytes[1]));
            Assert.That(sut.Read(2), Is.EqualTo(bytes[2]));
        });
    }

    [Test]
    public void MappedReadWrite()
    {
        byte[] ram = [1, 2, 3];
        ImmutableArray<byte> rom = [6, 7, 8];
        var sut = new MappedMemory.Builder()
            .Map(0, 3, new Ram(ram))
            .Map(3, 6, new Rom(rom))
            .Build();

        Assert.Multiple(() => {
            Assert.That(sut.Read(0), Is.EqualTo(ram[0]));
            Assert.That(sut.Read(1), Is.EqualTo(ram[1]));
            Assert.That(sut.Read(2), Is.EqualTo(ram[2]));

            Assert.That(sut.Read(3), Is.EqualTo(rom[0]));
            Assert.That(sut.Read(4), Is.EqualTo(rom[1]));
            Assert.That(sut.Read(5), Is.EqualTo(rom[2]));
        });

        sut.Write(0, 255);

        Assert.Multiple(() => {
            Assert.That(sut.Read(0), Is.EqualTo(255));
            Assert.That(sut.Read(1), Is.EqualTo(ram[1]));
            Assert.That(sut.Read(2), Is.EqualTo(ram[2]));

            Assert.That(sut.Read(3), Is.EqualTo(rom[0]));
            Assert.That(sut.Read(4), Is.EqualTo(rom[1]));
            Assert.That(sut.Read(5), Is.EqualTo(rom[2]));
        });

        Assert.Throws<InvalidOperationException>(() => sut.Write(3, 255));
    }
}
