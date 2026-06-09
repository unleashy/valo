namespace Valo;

public sealed class Cpu
{
    private RegisterFile _reg;
    private readonly IMemory _mem;
    private readonly IEnumerator<bool> _executor;

    public Cpu(RegisterFile reg, IMemory mem)
    {
        _reg = reg;
        _mem = mem;
        _executor = Executor();
    }

    public void Step()
    {
        while (!Cycle());
    }

    public bool Cycle()
    {
        _executor.MoveNext();
        return _executor.Current;
    }

    private IEnumerator<bool> Executor()
    {
        _reg[Register8.IR] = 0x00;

        while (true) {
            switch (_reg[Register8.IR]) {
                // NOP
                case 0: break;

                default: {
                    throw new NotImplementedException();
                }
            }

            _reg[Register8.IR] = _mem.Read(_reg[Register16.PC]++);
            yield return true;
        }
    }
}
