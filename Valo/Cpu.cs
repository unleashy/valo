using System.Diagnostics;

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

    public ref RegisterFile Registers => ref _reg;
    public IMemory Memory => _mem;

    private IEnumerator<bool> Executor()
    {
        // Technically, the first instruction executed by the CPU is a NOP (that falls through to
        // a generic fetch) but this would be annoying to work around in when using Step() right
        // after building a new CPU instance so this (irrelevant) behaviour is not emulated.
        // Therefore, this memory read is not considered a cycle per se and does not `yield`.
        _reg.IR = _mem.Read(_reg.PC++);

        while (true) {
            var instr = Instruction.Decode(_reg.IR);

            switch (instr.Op) {
                case Op.NoOp: break;

                case Op.LoadReg8: {
                    _reg[(Register8)instr.Dst] = _reg[(Register8)instr.Src];
                    break;
                }

                case Op.LoadImm8: {
                    _reg.Z = _mem.Read(_reg.PC++);
                    yield return false;

                    _reg[(Register8)instr.Dst] = _reg.Z;
                    break;
                }

                case Op.LoadReg8IndHL: {
                    _reg.Z = _mem.Read(_reg.HL);
                    yield return false;

                    _reg[(Register8)instr.Dst] = _reg.Z;
                    break;
                }

                case Op.LoadIndHLReg8: {
                    _mem.Write(_reg.HL, _reg[(Register8)instr.Src]);
                    yield return false;

                    break;
                }

                case Op.LoadIndHLImm8: {
                    _reg.Z = _mem.Read(_reg.PC++);
                    yield return false;

                    _mem.Write(_reg.HL, _reg.Z);
                    yield return false;

                    break;
                }

                case Op.LoadAInd16: {
                    _reg.Z = _mem.Read(_reg[(Register16)instr.Src]);
                    yield return false;

                    _reg.A = _reg.Z;
                    break;
                }

                case Op.LoadInd16A: {
                    _mem.Write(_reg[(Register16)instr.Dst], _reg.A);
                    yield return false;

                    break;
                }

                case Op.LoadDir16A: {
                    _reg.Z = _mem.Read(_reg.PC++);
                    yield return false;

                    _reg.W = _mem.Read(_reg.PC++);
                    yield return false;

                    _mem.Write(_reg.WZ, _reg.A);
                    yield return false;

                    break;
                }

                case Op.LoadADir16: {
                    _reg.Z = _mem.Read(_reg.PC++);
                    yield return false;

                    _reg.W = _mem.Read(_reg.PC++);
                    yield return false;

                    _reg.Z = _mem.Read(_reg.WZ);
                    yield return false;

                    _reg.A = _reg.Z;
                    break;
                }

                case Op.LoadAIndHigh: {
                    _reg.Z = _mem.Read((ushort)(0xFF00 | _reg.C));
                    yield return false;

                    _reg.A = _reg.Z;
                    break;
                }

                case Op.LoadIndHighA: {
                    _mem.Write((ushort)(0xFF00 | _reg.C), _reg.A);
                    yield return false;

                    break;
                }

                case Op.LoadADirHigh: {
                    _reg.Z = _mem.Read(_reg.PC++);
                    yield return false;

                    _reg.Z = _mem.Read((ushort)(0xFF00 | _reg.Z));
                    yield return false;

                    _reg.A = _reg.Z;
                    break;
                }

                case Op.LoadDirHighA: {
                    _reg.Z = _mem.Read(_reg.PC++);
                    yield return false;

                    _mem.Write((ushort)(0xFF00 | _reg.Z), _reg.A);
                    yield return false;
                    break;
                }

                case Op.LoadAIncHL: {
                    _reg.Z = _mem.Read(_reg.HL++);
                    yield return false;

                    _reg.A = _reg.Z;
                    break;
                }

                case Op.LoadADecHL: {
                    _reg.Z = _mem.Read(_reg.HL--);
                    yield return false;

                    _reg.A = _reg.Z;
                    break;
                }

                case Op.LoadIncHLA: {
                    _mem.Write(_reg.HL++, _reg.A);
                    yield return false;

                    break;
                }

                case Op.LoadDecHLA: {
                    _mem.Write(_reg.HL--, _reg.A);
                    yield return false;

                    break;
                }

                case Op.LoadImm16: {
                    _reg.Z = _mem.Read(_reg.PC++);
                    yield return false;

                    _reg.W = _mem.Read(_reg.PC++);
                    yield return false;

                    _reg[(Register16)instr.Dst] = _reg.WZ;
                    break;
                }

                case Op.LoadInd16SP: {
                    _reg.Z = _mem.Read(_reg.PC++);
                    yield return false;

                    _reg.W = _mem.Read(_reg.PC++);
                    yield return false;

                    _mem.Write(_reg.WZ++, _reg.SPL);
                    yield return false;

                    _mem.Write(_reg.WZ, _reg.SPH);
                    yield return false;
                    break;
                }

                case Op.LoadHLAdjustedSP: {
                    _reg.Z = _mem.Read(_reg.PC++);
                    yield return false;

                    _reg.L = Add(_reg.SPL, _reg.Z, out var carry, out var halfCarry);
                    _reg.Flags.Set(FlagsBit.Z | FlagsBit.N, false);
                    _reg.Flags.Set(FlagsBit.H, halfCarry);
                    _reg.Flags.Set(FlagsBit.C, carry);
                    yield return false;

                    var adj = _reg.Z > sbyte.MaxValue ? byte.MaxValue : 0;
                    _reg.H = (byte)(_reg.SPH + adj + (carry ? 1 : 0));

                    break;
                }

                case Op.LoadSPHL: {
                    _reg.SP = _reg.HL;
                    yield return false;

                    break;
                }

                default: {
                    throw new NotImplementedException(
                        $"Missing implementation for operation {instr.Op}"
                    );
                }
            }

            _reg.IR = _mem.Read(_reg.PC++);
            yield return true;
        }
    }

    private static byte Add(byte a, byte b, out bool carry, out bool halfCarry)
    {
        var result = a + b;
        halfCarry = ((a ^ b ^ result) & 0x10) == 0x10;
        carry = result > byte.MaxValue;

        return (byte)result;
    }
}

file enum Op
{
    NoOp,
    LoadReg8,
    LoadImm8,
    LoadReg8IndHL,
    LoadIndHLReg8,
    LoadIndHLImm8,
    LoadAInd16,
    LoadInd16A,
    LoadDir16A,
    LoadADir16,
    LoadAIndHigh,
    LoadIndHighA,
    LoadADirHigh,
    LoadDirHighA,
    LoadAIncHL,
    LoadIncHLA,
    LoadADecHL,
    LoadDecHLA,
    LoadImm16,
    LoadInd16SP,
    LoadHLAdjustedSP,
    LoadSPHL,
}

file readonly record struct Instruction(Op Op, byte Dst = byte.MaxValue, byte Src = byte.MaxValue)
{
    public static Instruction Decode(byte opcode)
    {
        var block = (byte)(opcode >> 6);

        switch (block) {
            case 0b00: {
                var subblock = (byte)(opcode & 0b1111);

                switch (subblock) {
                    case 0b0000: {
                        // No operation
                        // 7 6 5 4 3 2 1 0
                        // 0 0 0 0 0 0 0 0
                        return new Instruction(Op.NoOp);
                    }

                    case 0b0001: {
                        // Load register16 from immediate16
                        // 7 6   5 4   3 2 1 0
                        // 0 0  [dst]  0 0 0 1
                        var dst = (byte)((opcode >> 4) & 0b11);

                        return new Instruction(Op.LoadImm16, ToReg16(dst));
                    }

                    case 0b0010: {
                        // Load indirect16 from A
                        // 7 6  5 4    3 2 1 0
                        // 0 0  [dst]  0 0 1 0
                        var operand = (byte)((opcode >> 4) & 0b11);

                        return operand switch {
                            0b00 => new Instruction(Op.LoadInd16A, Dst: (byte) Register16.BC),
                            0b01 => new Instruction(Op.LoadInd16A, Dst: (byte) Register16.DE),
                            0b10 => new Instruction(Op.LoadIncHLA),
                            0b11 => new Instruction(Op.LoadDecHLA),
                            _    => throw new UnreachableException(),
                        };
                    }

                    case 0b1000: {
                        // Load SP from indirect immediate16
                        // 7 6  5 4  3 2 1 0
                        // 0 0  0 0  1 0 0 0
                        return new Instruction(Op.LoadInd16SP);
                    }

                    case 0b1010: {
                        // Load A from indirect16
                        // 7 6  5 4    3 2 1 0
                        // 0 0  [src]  1 0 1 0
                        var operand = (byte)((opcode >> 4) & 0b11);

                        return operand switch {
                            0b00 => new Instruction(Op.LoadAInd16, Src: (byte) Register16.BC),
                            0b01 => new Instruction(Op.LoadAInd16, Src: (byte) Register16.DE),
                            0b10 => new Instruction(Op.LoadAIncHL),
                            0b11 => new Instruction(Op.LoadADecHL),
                            _    => throw new UnreachableException(),
                        };
                    }

                    case 0b0110:
                    case 0b1110: {
                        var dst = (byte)((opcode >> 3) & 0b111);

                        if (dst == 0b110) {
                            // Load indirect HL from immediate8
                            // 7 6  5 4 3  2 1 0
                            // 0 0  1 1 0  1 1 0
                            return new Instruction(Op.LoadIndHLImm8);
                        }
                        else {
                            // Load register8 from immediate8
                            // 7 6  5 4 3  2 1 0
                            // 0 0  [dst]  1 1 0
                            return new Instruction(Op.LoadImm8, ToReg8(dst));
                        }
                    }

                    default: break;
                }

                break;
            }

            case 0b01: {
                var dst = (byte)((opcode >> 3) & 0b111);
                var src = (byte)(opcode & 0b111);

                if (src == 0b110) {
                    // Load register8 from indirect HL
                    // 7 6  5 4 3  2 1 0
                    // 0 1  [dst]  1 1 0
                    return new Instruction(Op.LoadReg8IndHL, ToReg8(dst));
                }
                else if (dst == 0b110) {
                    // Load indirect HL from register8
                    // 7 6  5 4 3  2 1 0
                    // 0 1  1 1 0  [src]
                    return new Instruction(Op.LoadIndHLReg8, Src: ToReg8(src));
                }
                else {
                    // Load register8 from register8
                    // 7 6  5 4 3  2 1 0
                    // 0 1  [dst]  [src]
                    return new Instruction(Op.LoadReg8, ToReg8(dst), ToReg8(src));
                }
            }

            case 0b11: {
                switch (opcode & 0b111111) {
                    case 0b101010: {
                        // Load direct immediate16 from A
                        // 7 6  5 4 3 2 1 0
                        // 1 1  1 0 1 0 1 0
                        return new Instruction(Op.LoadDir16A);
                    }

                    case 0b111000: {
                        // Load HL from adjusted SP
                        // 7 6  5 4 3 2 1 0
                        // 1 1  1 1 1 0 0 0
                        return new Instruction(Op.LoadHLAdjustedSP);
                    }

                    case 0b111001: {
                        // Load SP from HL
                        // 7 6  5 4 3 2 1 0
                        // 1 1  1 1 1 0 0 1
                        return new Instruction(Op.LoadSPHL);
                    }

                    case 0b111010: {
                        // Load A from direct immediate16
                        // 7 6  5 4 3 2 1 0
                        // 1 1  1 1 1 0 1 0
                        return new Instruction(Op.LoadADir16);
                    }

                    case 0b110010: {
                        // Load A from indirect $FF00 + C
                        // 7 6  5 4 3 2 1 0
                        // 1 1  1 1 0 0 1 0
                        return new Instruction(Op.LoadAIndHigh);
                    }

                    case 0b100010: {
                        // Load indirect $FF00 + C from A
                        // 7 6  5 4 3 2 1 0
                        // 1 1  1 0 0 0 1 0
                        return new Instruction(Op.LoadIndHighA);
                    }

                    case 0b110000: {
                        // Load A from indirect $FF00 + immediate
                        // 7 6  5 4 3 2 1 0
                        // 1 1  1 1 0 0 0 0
                        return new Instruction(Op.LoadADirHigh);
                    }

                    case 0b100000: {
                        // Load indirect $FF00 + immediate from A
                        // 7 6  5 4 3 2 1 0
                        // 1 1  1 0 0 0 0 0
                        return new Instruction(Op.LoadDirHighA);
                    }

                    default: break;
                }

                break;
            }

            default: break;
        }

        throw new NotImplementedException($"Missing decoder for opcode 0x{opcode:X2}");

        static byte ToReg8(byte placeholder) =>
            placeholder switch {
                0 => (byte)Register8.B,
                1 => (byte)Register8.C,
                2 => (byte)Register8.D,
                3 => (byte)Register8.E,
                4 => (byte)Register8.H,
                5 => (byte)Register8.L,
                7 => (byte)Register8.A,
                _ => throw new ArgumentException($"Not a Register8 placeholder: {placeholder}"),
            };

        static byte ToReg16(byte placeholder) =>
            placeholder switch {
                0 => (byte)Register16.BC,
                1 => (byte)Register16.DE,
                2 => (byte)Register16.HL,
                3 => (byte)Register16.SP,
                _ => throw new ArgumentException($"Not a Register16 placeholder: {placeholder}"),
            };
    }
}
