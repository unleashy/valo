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

    public int Step()
    {
        var cycleCount = 1;
        while (!Cycle()) cycleCount++;
        return cycleCount;
    }

    public bool Cycle()
    {
        _executor.MoveNext();
        return _executor.Current;
    }

    private IEnumerator<bool> Executor()
    {
        // Technically, the first instruction executed by the CPU is a NOP (that falls through to
        // a generic fetch) but this would be annoying to work around in when using Step() right
        // after building a new CPU instance so this (irrelevant) behaviour is not emulated.
        // Therefore, this memory read is not considered a cycle per se and does not `yield`.
        _reg[Register8.IR] = _mem.Read(_reg[Register16.PC]++);

        while (true) {
            var instr = Instruction.Decode(_reg[Register8.IR]);

            switch (instr.Op) {
                case Op.NoOp: break;

                case Op.LoadReg8: {
                    _reg[(Register8)instr.Dst] = _reg[(Register8)instr.Src];
                    break;
                }

                case Op.LoadImm8: {
                    _reg[Register8.Z] = _mem.Read(_reg[Register16.PC]++);
                    yield return false;

                    _reg[(Register8)instr.Dst] = _reg[Register8.Z];
                    break;
                }

                default: {
                    throw new NotImplementedException(
                        $"Missing implementation for operation {instr.Op}"
                    );
                }
            }

            _reg[Register8.IR] = _mem.Read(_reg[Register16.PC]++);
            yield return true;
        }
    }

    public RegisterFile Registers => _reg;
    public IMemory Memory => _mem;
}

file enum Op
{
    NoOp,
    LoadReg8,
    LoadImm8,
}

file readonly record struct Instruction(Op Op, byte Dst, byte Src)
{
    public static Instruction Decode(byte opcode)
    {
        var block = (byte)(opcode >> 6);

        switch (block) {
            case 0b00: {
                var subblock = (byte)(opcode & 0b111);

                switch (subblock) {
                    case 0b000: {
                        var operand = (byte)((opcode >> 3) & 0b111);
                        if (operand == 0b000) {
                            // No operation
                            // 7 6 5 4 3 2 1 0
                            // 0 0 0 0 0 0 0 0
                            return new Instruction(Op.NoOp, 0, 0);
                        }

                        break;
                    }

                    case 0b110: {
                        // Load register8 from immediate8
                        // 7 6  5 4 3  2 1 0
                        // 0 0  [dst]  1 1 0
                        var dst = (byte)((opcode >> 3) & 0b111);

                        return new Instruction(Op.LoadImm8, ToReg8(dst), 0);
                    }

                    default: break;
                }

                break;
            }

            case 0b01: {
                // Load register8 from register8
                // 7 6  5 4 3  2 1 0
                // 0 1  [dst]  [src]
                var dst = (byte)((opcode >> 3) & 0b111);
                var src = (byte)(opcode & 0b111);

                return new Instruction(Op.LoadReg8, ToReg8(dst), ToReg8(src));
            }

            default: break;
        }

        throw new NotImplementedException($"Missing decoder for opcode 0x{opcode:X2}");

        static byte ToReg8(byte placeholder) =>
            placeholder switch {
                0 => (byte) Register8.B,
                1 => (byte) Register8.C,
                2 => (byte) Register8.D,
                3 => (byte) Register8.E,
                4 => (byte) Register8.H,
                5 => (byte) Register8.L,
                7 => (byte) Register8.A,
                _ => throw new ArgumentException($"Not a Register8 placeholder: {placeholder}"),
            };
    }
}
