namespace Valo;

public class InterruptController
{
    public bool MasterEnabled { get; set; }
    public Interrupt Enabled { get; set; }
    public Interrupt Requested { get; set; }

    public InterruptRequester RequesterFor(Interrupt flag) => new(this, flag);

    public void Request(Interrupt flag)
    {
        Requested |= flag;
    }

    public bool TryAcknowledge(out ushort targetVector)
    {
        if (MasterEnabled && (Enabled & Requested) != 0) {
            var which = byte.TrailingZeroCount((byte)Requested);
            targetVector = (ushort)(0x40 + 8 * which);

            MasterEnabled = false;
            Requested &= Requested - 1;

            return true;
        }
        else {
            targetVector = ushort.MaxValue;
            return false;
        }
    }

    public IEnumerable<LocatedMemory> MemoryLayout() => [
        AccessorMemory.Located(0xFF0F, () => (byte)Requested, it => Requested = (Interrupt)it),
        AccessorMemory.Located(0xFFFF, () => (byte)Enabled, it => Enabled = (Interrupt)it),
    ];
}

public class InterruptRequester(InterruptController controller, Interrupt flag)
{
    public void Request()
    {
        controller.Request(flag);
    }

    public void RequestIf(bool shouldRequest)
    {
        if (shouldRequest) {
            Request();
        }
    }
}

[Flags]
public enum Interrupt : byte
{
    VBlank = 0b0000_0001,
    Lcd    = 0b0000_0010,
    Timer  = 0b0000_0100,
    Serial = 0b0000_1000,
    Joypad = 0b0001_0000,
}
