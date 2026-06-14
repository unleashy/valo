using System.Diagnostics;

namespace Valo;

public sealed class Cpu
{
    private RegisterFile _reg;
    private readonly IEnumerator<bool> _executor;

    public ref RegisterFile Registers => ref _reg;
    public IMemory Memory { get; }

    public Cpu(RegisterFile registers, IMemory memory)
    {
        _reg = registers;
        Memory = memory;
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
        _reg.IR = Memory.Read(_reg.PC++);

        while (true) {
            var instr = Instruction.Decode(_reg.IR);

            switch (instr.Op) {
                case Op.NoOp: break;

                case Op.LoadReg8: {
                    _reg[(Register8)instr.Dst] = _reg[(Register8)instr.Src];
                    break;
                }

                case Op.LoadImm8: {
                    _reg.Z = Memory.Read(_reg.PC++);
                    yield return false;

                    _reg[(Register8)instr.Dst] = _reg.Z;
                    break;
                }

                case Op.LoadReg8IndHL: {
                    _reg.Z = Memory.Read(_reg.HL);
                    yield return false;

                    _reg[(Register8)instr.Dst] = _reg.Z;
                    break;
                }

                case Op.LoadIndHLReg8: {
                    Memory.Write(_reg.HL, _reg[(Register8)instr.Src]);
                    yield return false;

                    break;
                }

                case Op.LoadIndHLImm8: {
                    _reg.Z = Memory.Read(_reg.PC++);
                    yield return false;

                    Memory.Write(_reg.HL, _reg.Z);
                    yield return false;

                    break;
                }

                case Op.LoadAInd16: {
                    _reg.Z = Memory.Read(_reg[(Register16)instr.Src]);
                    yield return false;

                    _reg.A = _reg.Z;
                    break;
                }

                case Op.LoadInd16A: {
                    Memory.Write(_reg[(Register16)instr.Dst], _reg.A);
                    yield return false;

                    break;
                }

                case Op.LoadDir16A: {
                    _reg.Z = Memory.Read(_reg.PC++);
                    yield return false;

                    _reg.W = Memory.Read(_reg.PC++);
                    yield return false;

                    Memory.Write(_reg.WZ, _reg.A);
                    yield return false;

                    break;
                }

                case Op.LoadADir16: {
                    _reg.Z = Memory.Read(_reg.PC++);
                    yield return false;

                    _reg.W = Memory.Read(_reg.PC++);
                    yield return false;

                    _reg.Z = Memory.Read(_reg.WZ);
                    yield return false;

                    _reg.A = _reg.Z;
                    break;
                }

                case Op.LoadAIndHigh: {
                    _reg.Z = Memory.Read((ushort)(0xFF00 | _reg.C));
                    yield return false;

                    _reg.A = _reg.Z;
                    break;
                }

                case Op.LoadIndHighA: {
                    Memory.Write((ushort)(0xFF00 | _reg.C), _reg.A);
                    yield return false;

                    break;
                }

                case Op.LoadADirHigh: {
                    _reg.Z = Memory.Read(_reg.PC++);
                    yield return false;

                    _reg.Z = Memory.Read((ushort)(0xFF00 | _reg.Z));
                    yield return false;

                    _reg.A = _reg.Z;
                    break;
                }

                case Op.LoadDirHighA: {
                    _reg.Z = Memory.Read(_reg.PC++);
                    yield return false;

                    Memory.Write((ushort)(0xFF00 | _reg.Z), _reg.A);
                    yield return false;
                    break;
                }

                case Op.LoadAIncHL: {
                    _reg.Z = Memory.Read(_reg.HL++);
                    yield return false;

                    _reg.A = _reg.Z;
                    break;
                }

                case Op.LoadADecHL: {
                    _reg.Z = Memory.Read(_reg.HL--);
                    yield return false;

                    _reg.A = _reg.Z;
                    break;
                }

                case Op.LoadIncHLA: {
                    Memory.Write(_reg.HL++, _reg.A);
                    yield return false;

                    break;
                }

                case Op.LoadDecHLA: {
                    Memory.Write(_reg.HL--, _reg.A);
                    yield return false;

                    break;
                }

                case Op.LoadImm16: {
                    _reg.Z = Memory.Read(_reg.PC++);
                    yield return false;

                    _reg.W = Memory.Read(_reg.PC++);
                    yield return false;

                    _reg[(Register16)instr.Dst] = _reg.WZ;
                    break;
                }

                case Op.LoadInd16SP: {
                    _reg.Z = Memory.Read(_reg.PC++);
                    yield return false;

                    _reg.W = Memory.Read(_reg.PC++);
                    yield return false;

                    var (sph, spl) = _reg.Split(Register16.SP);

                    Memory.Write(_reg.WZ++, spl);
                    yield return false;

                    Memory.Write(_reg.WZ, sph);
                    yield return false;
                    break;
                }

                case Op.LoadHLAdjustedSP: {
                    _reg.Z = Memory.Read(_reg.PC++);
                    yield return false;

                    var (sph, spl) = _reg.Split(Register16.SP);

                    _reg.L = Add(spl, _reg.Z, out var flags);
                    _reg.Flags.Replace(flags);
                    yield return false;

                    var adj = _reg.Z > sbyte.MaxValue ? byte.MaxValue : 0;
                    _reg.H = (byte)(sph + adj + (flags.HasFlag(FlagsBit.C) ? 1 : 0));

                    break;
                }

                case Op.LoadSPHL: {
                    _reg.SP = _reg.HL;
                    yield return false;

                    break;
                }

                case Op.Push: {
                    _reg.SP--;
                    yield return false;

                    var (msb, lsb) = _reg.Split((Register16)instr.Src);

                    Memory.Write(_reg.SP--, msb);
                    yield return false;

                    Memory.Write(_reg.SP, lsb);
                    yield return false;

                    break;
                }

                case Op.Pop: {
                    _reg.Z = Memory.Read(_reg.SP++);
                    yield return false;

                    _reg.W = Memory.Read(_reg.SP++);
                    yield return false;

                    _reg[(Register16)instr.Dst] = _reg.WZ;
                    break;
                }

                case Op.AddHL: {
                    _reg[(Register8)instr.Src] = Memory.Read(_reg.HL);
                    yield return false;

                    goto case Op.AddReg8;
                }

                case Op.AddImm8: {
                    _reg[(Register8)instr.Src] = Memory.Read(_reg.PC++);
                    yield return false;

                    goto case Op.AddReg8;
                }

                case Op.AddReg8: {
                    _reg.A = Add(_reg.A, _reg[(Register8)instr.Src], out var flags);
                    _reg.Flags.Replace(flags);

                    break;
                }

                case Op.AdcHL: {
                    _reg[(Register8)instr.Src] = Memory.Read(_reg.HL);
                    yield return false;

                    goto case Op.AdcReg8;
                }

                case Op.AdcImm8: {
                    _reg[(Register8)instr.Src] = Memory.Read(_reg.PC++);
                    yield return false;

                    goto case Op.AdcReg8;
                }

                case Op.AdcReg8: {
                    _reg.A =
                        Adc(
                            _reg.A,
                            _reg[(Register8)instr.Src],
                            carry: _reg.Flags.IsSet(FlagsBit.C),
                            out var flags
                        );
                    _reg.Flags.Replace(flags);

                    break;
                }

                case Op.SubHL: {
                    _reg[(Register8)instr.Src] = Memory.Read(_reg.HL);
                    yield return false;

                    goto case Op.SubReg8;
                }

                case Op.SubImm8: {
                    _reg[(Register8)instr.Src] = Memory.Read(_reg.PC++);
                    yield return false;

                    goto case Op.SubReg8;
                }

                case Op.SubReg8: {
                    _reg.A = Sub(_reg.A, _reg[(Register8)instr.Src], out var flags);
                    _reg.Flags.Replace(flags);

                    break;
                }

                case Op.SbcHL: {
                    _reg[(Register8)instr.Src] = Memory.Read(_reg.HL);
                    yield return false;

                    goto case Op.SbcReg8;
                }

                case Op.SbcImm8: {
                    _reg[(Register8)instr.Src] = Memory.Read(_reg.PC++);
                    yield return false;

                    goto case Op.SbcReg8;
                }

                case Op.SbcReg8: {
                    _reg.A = Sbc(
                        _reg.A,
                        _reg[(Register8)instr.Src],
                        carry: _reg.Flags.IsSet(FlagsBit.C),
                        out var flags
                    );
                    _reg.Flags.Replace(flags);

                    break;
                }

                case Op.CpHL: {
                    _reg[(Register8)instr.Src] = Memory.Read(_reg.HL);
                    yield return false;

                    goto case Op.CpReg8;
                }

                case Op.CpImm8: {
                    _reg[(Register8)instr.Src] = Memory.Read(_reg.PC++);
                    yield return false;

                    goto case Op.CpReg8;
                }

                case Op.CpReg8: {
                    _ = Sub(_reg.A, _reg[(Register8)instr.Src], out var flags);
                    _reg.Flags.Replace(flags);
                    break;
                }

                case Op.IncReg8: {
                    _reg[(Register8)instr.Dst] = Add(_reg[(Register8)instr.Dst], 1, out var flags);
                    _reg.Flags.Apply(FlagsBit.Z | FlagsBit.N | FlagsBit.H, flags);

                    break;
                }

                case Op.IncHL: {
                    _reg.Z = Memory.Read(_reg.HL);
                    yield return false;

                    var result = Add(_reg.Z, 1, out var flags);
                    _reg.Flags.Apply(FlagsBit.Z | FlagsBit.N | FlagsBit.H, flags);

                    Memory.Write(_reg.HL, result);
                    yield return false;
                    break;
                }

                case Op.DecReg8: {
                    _reg[(Register8)instr.Dst] = Sub(_reg[(Register8)instr.Dst], 1, out var flags);
                    _reg.Flags.Apply(FlagsBit.Z | FlagsBit.N | FlagsBit.H, flags);

                    break;
                }

                case Op.DecHL: {
                    _reg.Z = Memory.Read(_reg.HL);
                    yield return false;

                    var result = Sub(_reg.Z, 1, out var flags);
                    _reg.Flags.Apply(FlagsBit.Z | FlagsBit.N | FlagsBit.H, flags);

                    Memory.Write(_reg.HL, result);
                    yield return false;
                    break;
                }

                case Op.Ccf: {
                    _reg.Flags.Apply(
                        FlagsBit.N | FlagsBit.H | FlagsBit.C,
                        _reg.Flags.IsSet(FlagsBit.C) ? 0 : FlagsBit.C
                    );
                    break;
                }

                case Op.Scf: {
                    _reg.Flags.Apply(
                        FlagsBit.N | FlagsBit.H | FlagsBit.C,
                        FlagsBit.C
                    );
                    break;
                }

                case Op.Cpl: {
                    _reg.A = (byte)~_reg.A;
                    _reg.Flags.Set(FlagsBit.N | FlagsBit.H);
                    break;
                }

                default: {
                    throw new UnreachableException(
                        $"Missing implementation for operation {instr.Op}"
                    );
                }
            }

            _reg.IR = Memory.Read(_reg.PC++);
            yield return true;
        }
    }

    private static byte Add(byte a, byte b, out FlagsBit flags) =>
        Adc(a, b, carry: false, out flags);

    private static byte Adc(byte a, byte b, bool carry, out FlagsBit flags)
    {
        var c = carry ? 1 : 0;
        var result = a + b + c;

        flags = 0;
        if ((byte)result == 0) flags |= FlagsBit.Z;
        if (((a ^ b ^ c ^ result) & 0x10) == 0x10) flags |= FlagsBit.H;
        if (result > byte.MaxValue) flags |= FlagsBit.C;

        return (byte)result;
    }

    private static byte Sub(byte a, byte b, out FlagsBit flags) =>
        Sbc(a, b, carry: false, out flags);

    private static byte Sbc(byte a, byte b, bool carry, out FlagsBit flags)
    {
        var c = carry ? 1 : 0;
        var result = a - b - c;

        flags = FlagsBit.N;
        if ((byte)result == 0) flags |= FlagsBit.Z;
        if (((a ^ b ^ c ^ result) & 0x10) == 0x10) flags |= FlagsBit.H;
        if (result < 0) flags |= FlagsBit.C;

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
    Push,
    Pop,

    AddReg8,
    AddHL,
    AddImm8,
    AdcReg8,
    AdcHL,
    AdcImm8,
    SubReg8,
    SubHL,
    SubImm8,
    SbcReg8,
    SbcHL,
    SbcImm8,
    CpReg8,
    CpHL,
    CpImm8,
    IncReg8,
    IncHL,
    DecReg8,
    DecHL,
    Ccf,
    Scf,
    Cpl,
    Daa,
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

                    case 0b0100:
                    case 0b1100: {
                        var dst = (byte)((opcode >> 3) & 0b111);

                        if (dst == 0b110) {
                            // Increment indirect HL
                            // 7 6  5 4 3  2 1 0
                            // 0 0  1 1 0  1 0 0
                            return new Instruction(Op.IncHL);
                        }
                        else {
                            // Increment register8
                            // 7 6  5 4 3  2 1 0
                            // 0 0  [dst]  1 0 0
                            return new Instruction(Op.IncReg8, ToReg8(dst));
                        }
                    }

                    case 0b0101:
                    case 0b1101: {
                        var dst = (byte)((opcode >> 3) & 0b111);

                        if (dst == 0b110) {
                            // Decrement indirect HL
                            // 7 6  5 4 3  2 1 0
                            // 0 0  1 1 0  1 0 1
                            return new Instruction(Op.DecHL);
                        }
                        else {
                            // Decrement register8
                            // 7 6  5 4 3  2 1 0
                            // 0 0  [dst]  1 0 1
                            return new Instruction(Op.DecReg8, ToReg8(dst));
                        }
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

                    case 0b0111: {
                        var specifier = opcode >> 4;

                        switch (specifier) {
                            case 0b10: {
                                // Decimal adjust accumulator
                                // 7 6  5 4  3 2 1 0
                                // 0 0  1 0  0 1 1 1
                                return new Instruction(Op.Daa);
                            }

                            case 0b11: {
                                // Set carry flag
                                // 7 6  5 4  3 2 1 0
                                // 0 0  1 1  0 1 1 1
                                return new Instruction(Op.Scf);
                            }
                        }

                        break;
                    }

                    case 0b1111: {
                        var specifier = opcode >> 4;

                        switch (specifier) {
                            case 0b10: {
                                // Complement accumulator
                                // 7 6  5 4  3 2 1 0
                                // 0 0  1 0  1 1 1 1
                                return new Instruction(Op.Cpl);
                            }

                            case 0b11: {
                                // Complement carry flag
                                // 7 6  5 4  3 2 1 0
                                // 0 0  1 1  1 1 1 1
                                return new Instruction(Op.Ccf);
                            }
                        }

                        break;
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

            case 0b10: {
                var identifier = (byte)((opcode >> 3) & 0b111);
                var operand = (byte)(opcode & 0b111);

                switch (identifier) {
                    case 0b000: {
                        // Add register8
                        // 7 6  5 4 3  2 1 0
                        // 1 0  0 0 0  [src]
                        if (operand == 0b110) {
                            return new Instruction(Op.AddHL, Src: (byte)Register8.Z);
                        }
                        else {
                            return new Instruction(Op.AddReg8, Src: ToReg8(operand));
                        }
                    }

                    case 0b001: {
                        // Add register8 with carry
                        // 7 6  5 4 3  2 1 0
                        // 1 0  0 0 1  [src]
                        if (operand == 0b110) {
                            return new Instruction(Op.AdcHL, Src: (byte)Register8.Z);
                        }
                        else {
                            return new Instruction(Op.AdcReg8, Src: ToReg8(operand));
                        }
                    }

                    case 0b010: {
                        // Subtract from register8
                        // 7 6  5 4 3  2 1 0
                        // 1 0  0 1 0  [src]
                        if (operand == 0b110) {
                            return new Instruction(Op.SubHL, Src: (byte)Register8.Z);
                        }
                        else {
                            return new Instruction(Op.SubReg8, Src: ToReg8(operand));
                        }
                    }

                    case 0b011: {
                        // Subtract from register8 with carry
                        // 7 6  5 4 3  2 1 0
                        // 1 0  0 1 1  [src]
                        if (operand == 0b110) {
                            return new Instruction(Op.SbcHL, Src: (byte)Register8.Z);
                        }
                        else {
                            return new Instruction(Op.SbcReg8, Src: ToReg8(operand));
                        }
                    }

                    case 0b111: {
                        // Compare register8
                        // 7 6  5 4 3  2 1 0
                        // 1 0  1 1 1  [src]
                        if (operand == 0b110) {
                            return new Instruction(Op.CpHL, Src: (byte)Register8.Z);
                        }
                        else {
                            return new Instruction(Op.CpReg8, Src: ToReg8(operand));
                        }
                    }
                }

                break;
            }

            case 0b11: {
                switch (opcode & 0b111111) {
                    case 0b000110: {
                        // Add immediate8
                        // 7 6  5 4 3  2 1 0
                        // 1 1  0 0 0  1 1 0
                        return new Instruction(Op.AddImm8, Src: (byte)Register8.Z);
                    }

                    case 0b001110: {
                        // Add immediate8 with carry
                        // 7 6  5 4 3 2 1 0
                        // 1 1  0 0 1 1 1 0
                        return new Instruction(Op.AdcImm8, Src: (byte)Register8.Z);
                    }

                    case 0b010110: {
                        // Subtract from immediate8
                        // 7 6  5 4 3 2 1 0
                        // 1 1  0 1 0 1 1 0
                        return new Instruction(Op.SubImm8, Src: (byte)Register8.Z);
                    }

                    case 0b011110: {
                        // Subtract from immediate8 with carry
                        // 7 6  5 4 3 2 1 0
                        // 1 1  0 1 1 1 1 0
                        return new Instruction(Op.SbcImm8, Src: (byte)Register8.Z);
                    }

                    case 0b111110: {
                        // Compare immediate8
                        // 7 6  5 4 3 2 1 0
                        // 1 1  1 1 1 1 1 0
                        return new Instruction(Op.CpImm8, Src: (byte)Register8.Z);
                    }

                    case 0b000001:
                    case 0b010001:
                    case 0b100001:
                    case 0b110001: {
                        // Pop from stack to register16
                        // 7 6   5 4   3 2 1 0
                        // 1 1  [src]  0 1 0 1
                        var dst = (byte)((opcode >> 4) & 0b11);
                        return new Instruction(Op.Pop, Dst: ToReg16Stack(dst));
                    }

                    case 0b000101:
                    case 0b010101:
                    case 0b100101:
                    case 0b110101: {
                        // Push register16 to stack
                        // 7 6   5 4   3 2 1 0
                        // 1 1  [src]  0 1 0 1
                        var src = (byte)((opcode >> 4) & 0b11);
                        return new Instruction(Op.Push, Src: ToReg16Stack(src));
                    }

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

        throw new UnreachableException($"Missing decoder for opcode 0x{opcode:X2}");

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

        static byte ToReg16Stack(byte placeholder) =>
            placeholder switch {
                0 => (byte)Register16.BC,
                1 => (byte)Register16.DE,
                2 => (byte)Register16.HL,
                3 => (byte)Register16.AF,
                _ => throw new ArgumentException($"Not a Register16 placeholder: {placeholder}"),
            };
    }
}
