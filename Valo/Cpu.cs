using System.Diagnostics;

namespace Valo;

public enum CycleStatus
{
    Executing,
    Servicing,
    Fetched,
}

public sealed class Cpu
{
    private RegisterFile _reg;
    private readonly IEnumerator<CycleStatus> _executor;

    public ref RegisterFile Registers => ref _reg;
    public IMemory Memory { get; set; }
    public InterruptController Interrupts { get; }

    public Cpu(RegisterFile registers, IMemory memory)
        : this(registers, memory, new InterruptController())
    {}

    public Cpu(
        RegisterFile registers,
        IMemory memory,
        InterruptController interrupts
    )
    {
        _reg = registers;
        Memory = memory;
        Interrupts = interrupts;

        _executor = Executor();
    }

    public int Step()
    {
        var cycleCount = 1;
        while (Cycle() != CycleStatus.Fetched) cycleCount++;
        return cycleCount;
    }

    public CycleStatus Cycle()
    {
        _executor.MoveNext();
        return _executor.Current;
    }

    private IEnumerator<CycleStatus> Executor()
    {
        // Technically, the first instruction executed by the CPU is a NOP (that falls through to
        // a generic fetch) but this would be annoying to work around in when using Step() right
        // after building a new CPU instance so this (irrelevant) behaviour is not emulated.
        // Therefore, this memory read is not considered a cycle per se and does not `yield`.
        _reg.IR = Memory.Read(_reg.PC++);

        while (true) {
            var instr = Instruction.Decode(_reg.IR);

            switch (instr.Op) {
                #region Management instructions
                case Op.Nop: break;

                case Op.Di: {
                    _reg.IR = Memory.Read(_reg.PC++);
                    Interrupts.MasterEnabled = false;
                    yield return CycleStatus.Fetched;
                    continue;
                }

                case Op.Ei: {
                    _reg.IR = Memory.Read(_reg.PC++);
                    Interrupts.MasterEnabled = true;
                    yield return CycleStatus.Fetched;
                    continue;
                }

                case Op.Halt: {
                    _reg.PC--;
                    break;
                }

                case Op.Stop: {
                    throw new NotSupportedException("STOP instruction is not supported");
                }
                #endregion Management instructions

                #region 8-bit load instructions
                case Op.LoadReg8: {
                    _reg[(Register8)instr.Dst] = _reg[(Register8)instr.Src];
                    break;
                }

                case Op.LoadImm8: {
                    _reg.Z = Memory.Read(_reg.PC++);
                    yield return CycleStatus.Executing;

                    _reg[(Register8)instr.Dst] = _reg.Z;
                    break;
                }

                case Op.LoadReg8IndHL: {
                    _reg.Z = Memory.Read(_reg.HL);
                    yield return CycleStatus.Executing;

                    _reg[(Register8)instr.Dst] = _reg.Z;
                    break;
                }

                case Op.LoadIndHLReg8: {
                    Memory.Write(_reg.HL, _reg[(Register8)instr.Src]);
                    yield return CycleStatus.Executing;

                    break;
                }

                case Op.LoadIndHLImm8: {
                    _reg.Z = Memory.Read(_reg.PC++);
                    yield return CycleStatus.Executing;

                    Memory.Write(_reg.HL, _reg.Z);
                    yield return CycleStatus.Executing;

                    break;
                }

                case Op.LoadAInd16: {
                    _reg.Z = Memory.Read(_reg[(Register16)instr.Src]);
                    yield return CycleStatus.Executing;

                    _reg.A = _reg.Z;
                    break;
                }

                case Op.LoadInd16A: {
                    Memory.Write(_reg[(Register16)instr.Dst], _reg.A);
                    yield return CycleStatus.Executing;

                    break;
                }

                case Op.LoadDir16A: {
                    _reg.Z = Memory.Read(_reg.PC++);
                    yield return CycleStatus.Executing;

                    _reg.W = Memory.Read(_reg.PC++);
                    yield return CycleStatus.Executing;

                    Memory.Write(_reg.WZ, _reg.A);
                    yield return CycleStatus.Executing;

                    break;
                }

                case Op.LoadADir16: {
                    _reg.Z = Memory.Read(_reg.PC++);
                    yield return CycleStatus.Executing;

                    _reg.W = Memory.Read(_reg.PC++);
                    yield return CycleStatus.Executing;

                    _reg.Z = Memory.Read(_reg.WZ);
                    yield return CycleStatus.Executing;

                    _reg.A = _reg.Z;
                    break;
                }

                case Op.LoadAIndHigh: {
                    _reg.Z = Memory.Read((ushort)(HighRam | _reg.C));
                    yield return CycleStatus.Executing;

                    _reg.A = _reg.Z;
                    break;
                }

                case Op.LoadIndHighA: {
                    Memory.Write((ushort)(HighRam | _reg.C), _reg.A);
                    yield return CycleStatus.Executing;

                    break;
                }

                case Op.LoadADirHigh: {
                    _reg.Z = Memory.Read(_reg.PC++);
                    yield return CycleStatus.Executing;

                    _reg.Z = Memory.Read((ushort)(HighRam | _reg.Z));
                    yield return CycleStatus.Executing;

                    _reg.A = _reg.Z;
                    break;
                }

                case Op.LoadDirHighA: {
                    _reg.Z = Memory.Read(_reg.PC++);
                    yield return CycleStatus.Executing;

                    Memory.Write((ushort)(HighRam | _reg.Z), _reg.A);
                    yield return CycleStatus.Executing;
                    break;
                }

                case Op.LoadAIncHL: {
                    _reg.Z = Memory.Read(_reg.HL++);
                    yield return CycleStatus.Executing;

                    _reg.A = _reg.Z;
                    break;
                }

                case Op.LoadADecHL: {
                    _reg.Z = Memory.Read(_reg.HL--);
                    yield return CycleStatus.Executing;

                    _reg.A = _reg.Z;
                    break;
                }

                case Op.LoadIncHLA: {
                    Memory.Write(_reg.HL++, _reg.A);
                    yield return CycleStatus.Executing;

                    break;
                }

                case Op.LoadDecHLA: {
                    Memory.Write(_reg.HL--, _reg.A);
                    yield return CycleStatus.Executing;

                    break;
                }
                #endregion 8-bit load instructions

                #region 16-bit load instructions
                case Op.LoadImm16: {
                    _reg.Z = Memory.Read(_reg.PC++);
                    yield return CycleStatus.Executing;

                    _reg.W = Memory.Read(_reg.PC++);
                    yield return CycleStatus.Executing;

                    _reg[(Register16)instr.Dst] = _reg.WZ;
                    break;
                }

                case Op.LoadInd16SP: {
                    _reg.Z = Memory.Read(_reg.PC++);
                    yield return CycleStatus.Executing;

                    _reg.W = Memory.Read(_reg.PC++);
                    yield return CycleStatus.Executing;

                    var (sph, spl) = _reg.Split(Register16.SP);

                    Memory.Write(_reg.WZ++, spl);
                    yield return CycleStatus.Executing;

                    Memory.Write(_reg.WZ, sph);
                    yield return CycleStatus.Executing;
                    break;
                }

                case Op.LoadHLAdjustedSP: {
                    _reg.Z = Memory.Read(_reg.PC++);
                    yield return CycleStatus.Executing;

                    var (sph, spl) = _reg.Split(Register16.SP);

                    _reg.L = Add(spl, _reg.Z, out var flags);
                    _reg.Flags.Replace(flags);
                    _reg.Flags.Reset(FlagsBit.Z);
                    yield return CycleStatus.Executing;

                    var adj = _reg.Z > sbyte.MaxValue ? byte.MaxValue : 0;
                    _reg.H = (byte)(sph + adj + (_reg.Flags.C ? 1 : 0));

                    break;
                }

                case Op.LoadSPHL: {
                    _reg.SP = _reg.HL;
                    yield return CycleStatus.Executing;

                    break;
                }

                case Op.Push: {
                    _reg.SP--;
                    yield return CycleStatus.Executing;

                    var (msb, lsb) = _reg.Split((Register16)instr.Src);

                    Memory.Write(_reg.SP--, msb);
                    yield return CycleStatus.Executing;

                    Memory.Write(_reg.SP, lsb);
                    yield return CycleStatus.Executing;

                    break;
                }

                case Op.Pop: {
                    _reg.Z = Memory.Read(_reg.SP++);
                    yield return CycleStatus.Executing;

                    _reg.W = Memory.Read(_reg.SP++);
                    yield return CycleStatus.Executing;

                    _reg[(Register16)instr.Dst] = _reg.WZ;
                    break;
                }
                #endregion 16-bit load instructions

                #region 8-bit arithmetic instructions
                case Op.AddHL: {
                    _reg[(Register8)instr.Src] = Memory.Read(_reg.HL);
                    yield return CycleStatus.Executing;

                    goto case Op.AddReg8;
                }

                case Op.AddImm8: {
                    _reg[(Register8)instr.Src] = Memory.Read(_reg.PC++);
                    yield return CycleStatus.Executing;

                    goto case Op.AddReg8;
                }

                case Op.AddReg8: {
                    _reg.A = Add(_reg.A, _reg[(Register8)instr.Src], out var flags);
                    _reg.Flags.Replace(flags);

                    break;
                }

                case Op.AdcHL: {
                    _reg[(Register8)instr.Src] = Memory.Read(_reg.HL);
                    yield return CycleStatus.Executing;

                    goto case Op.AdcReg8;
                }

                case Op.AdcImm8: {
                    _reg[(Register8)instr.Src] = Memory.Read(_reg.PC++);
                    yield return CycleStatus.Executing;

                    goto case Op.AdcReg8;
                }

                case Op.AdcReg8: {
                    _reg.A =
                        Adc(
                            _reg.A,
                            _reg[(Register8)instr.Src],
                            carry: _reg.Flags.C,
                            out var flags
                        );
                    _reg.Flags.Replace(flags);

                    break;
                }

                case Op.SubHL: {
                    _reg[(Register8)instr.Src] = Memory.Read(_reg.HL);
                    yield return CycleStatus.Executing;

                    goto case Op.SubReg8;
                }

                case Op.SubImm8: {
                    _reg[(Register8)instr.Src] = Memory.Read(_reg.PC++);
                    yield return CycleStatus.Executing;

                    goto case Op.SubReg8;
                }

                case Op.SubReg8: {
                    _reg.A = Sub(_reg.A, _reg[(Register8)instr.Src], out var flags);
                    _reg.Flags.Replace(flags);

                    break;
                }

                case Op.SbcHL: {
                    _reg[(Register8)instr.Src] = Memory.Read(_reg.HL);
                    yield return CycleStatus.Executing;

                    goto case Op.SbcReg8;
                }

                case Op.SbcImm8: {
                    _reg[(Register8)instr.Src] = Memory.Read(_reg.PC++);
                    yield return CycleStatus.Executing;

                    goto case Op.SbcReg8;
                }

                case Op.SbcReg8: {
                    _reg.A = Sbc(
                        _reg.A,
                        _reg[(Register8)instr.Src],
                        carry: _reg.Flags.C,
                        out var flags
                    );
                    _reg.Flags.Replace(flags);

                    break;
                }

                case Op.CpHL: {
                    _reg[(Register8)instr.Src] = Memory.Read(_reg.HL);
                    yield return CycleStatus.Executing;

                    goto case Op.CpReg8;
                }

                case Op.CpImm8: {
                    _reg[(Register8)instr.Src] = Memory.Read(_reg.PC++);
                    yield return CycleStatus.Executing;

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
                    yield return CycleStatus.Executing;

                    var result = Add(_reg.Z, 1, out var flags);
                    _reg.Flags.Apply(FlagsBit.Z | FlagsBit.N | FlagsBit.H, flags);

                    Memory.Write(_reg.HL, result);
                    yield return CycleStatus.Executing;
                    break;
                }

                case Op.DecReg8: {
                    _reg[(Register8)instr.Dst] = Sub(_reg[(Register8)instr.Dst], 1, out var flags);
                    _reg.Flags.Apply(FlagsBit.Z | FlagsBit.N | FlagsBit.H, flags);

                    break;
                }

                case Op.DecHL: {
                    _reg.Z = Memory.Read(_reg.HL);
                    yield return CycleStatus.Executing;

                    var result = Sub(_reg.Z, 1, out var flags);
                    _reg.Flags.Apply(FlagsBit.Z | FlagsBit.N | FlagsBit.H, flags);

                    Memory.Write(_reg.HL, result);
                    yield return CycleStatus.Executing;
                    break;
                }

                case Op.Ccf: {
                    _reg.Flags.Apply(
                        FlagsBit.N | FlagsBit.H | FlagsBit.C,
                        _reg.Flags.C ? 0 : FlagsBit.C
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

                case Op.Daa: {
                    var negative = _reg.Flags.N;
                    var halfcarry = _reg.Flags.H;
                    var carry = _reg.Flags.C;

                    // Adapted from https://www.reddit.com/r/EmuDev/comments/4ycoix/a_guide_to_the_gameboys_halfcarry_flag/d6p619w/
                    var adjustment = 0;
                    if (halfcarry || (!negative && (_reg.A & 0x0F) > 0x09)) {
                        adjustment = 0x06;
                    }

                    if (carry || (!negative && _reg.A > 0x99)) {
                        adjustment |= 0x60;
                        carry = true;
                    }

                    _reg.A += (byte)(negative ? -adjustment : adjustment);
                    _reg.Flags.Apply(
                        FlagsBit.Z | FlagsBit.H | FlagsBit.C,
                        (_reg.A == 0 ? FlagsBit.Z : 0) | (carry ? FlagsBit.C : 0)
                    );

                    break;
                }
                #endregion 8-bit arithmetic instructions

                #region 8-bit logical instructions
                case Op.AndHL: {
                    _reg[(Register8)instr.Src] = Memory.Read(_reg.HL);
                    yield return CycleStatus.Executing;

                    goto case Op.AndReg8;
                }

                case Op.AndImm8: {
                    _reg[(Register8)instr.Src] = Memory.Read(_reg.PC++);
                    yield return CycleStatus.Executing;

                    goto case Op.AndReg8;
                }

                case Op.AndReg8: {
                    _reg.A &= _reg[(Register8)instr.Src];
                    _reg.Flags.Replace((_reg.A == 0 ? FlagsBit.Z : 0) | FlagsBit.H);
                    break;
                }

                case Op.OrHL: {
                    _reg[(Register8)instr.Src] = Memory.Read(_reg.HL);
                    yield return CycleStatus.Executing;

                    goto case Op.OrReg8;
                }

                case Op.OrImm8: {
                    _reg[(Register8)instr.Src] = Memory.Read(_reg.PC++);
                    yield return CycleStatus.Executing;

                    goto case Op.OrReg8;
                }

                case Op.OrReg8: {
                    _reg.A |= _reg[(Register8)instr.Src];
                    _reg.Flags.Replace(_reg.A == 0 ? FlagsBit.Z : 0);
                    break;
                }

                case Op.XorHL: {
                    _reg[(Register8)instr.Src] = Memory.Read(_reg.HL);
                    yield return CycleStatus.Executing;

                    goto case Op.XorReg8;
                }

                case Op.XorImm8: {
                    _reg[(Register8)instr.Src] = Memory.Read(_reg.PC++);
                    yield return CycleStatus.Executing;

                    goto case Op.XorReg8;
                }

                case Op.XorReg8: {
                    _reg.A ^= _reg[(Register8)instr.Src];
                    _reg.Flags.Replace(_reg.A == 0 ? FlagsBit.Z : 0);
                    break;
                }

                case Op.Rlca: {
                    _reg.A = byte.RotateLeft(_reg.A, 1);
                    _reg.Flags.Replace((_reg.A & 1) == 1 ? FlagsBit.C : 0);
                    break;
                }

                case Op.Rrca: {
                    _reg.A = byte.RotateRight(_reg.A, 1);
                    _reg.Flags.Replace(_reg.A >> 7 == 1 ? FlagsBit.C : 0);
                    break;
                }

                case Op.Rla: {
                    var before = _reg.A;
                    _reg.A <<= 1;
                    _reg.A = (byte)(_reg.Flags.C ? _reg.A | 1 : _reg.A & ~1);
                    _reg.Flags.Replace(before >> 7 == 1 ? FlagsBit.C : 0);
                    break;
                }

                case Op.Rra: {
                    var before = _reg.A;
                    _reg.A >>= 1;
                    _reg.A = (byte)(_reg.Flags.C ? _reg.A | 0x80 : _reg.A & ~0x80);
                    _reg.Flags.Replace((before & 1) == 1 ? FlagsBit.C : 0);
                    break;
                }

                case Op.CbPrefix: {
                    foreach (var r in ExecuteCb()) {
                        yield return r;
                    }

                    break;
                }
                #endregion 8-bit logical instructions

                #region 16-bit arithmetic instructions
                case Op.AddHLReg16: {
                    var (msb, lsb) = _reg.Split((Register16)instr.Src);

                    _reg.L = Add(_reg.L, lsb, out var flags);
                    _reg.Flags.Apply(FlagsBit.N | FlagsBit.H | FlagsBit.C, flags);
                    yield return CycleStatus.Executing;

                    _reg.H = Adc(_reg.H, msb, _reg.Flags.C, out flags);
                    _reg.Flags.Apply(FlagsBit.H | FlagsBit.C, flags);

                    break;
                }


                case Op.IncReg16: {
                    ++_reg[(Register16)instr.Dst];
                    yield return CycleStatus.Executing;

                    break;
                }

                case Op.DecReg16: {
                    --_reg[(Register16)instr.Dst];
                    yield return CycleStatus.Executing;

                    break;
                }

                case Op.AddSPImm8: {
                    _reg.Z = Memory.Read(_reg.PC++);
                    var adj = _reg.Z > sbyte.MaxValue ? byte.MaxValue : 0;
                    yield return CycleStatus.Executing;

                    var (sph, spl) = _reg.Split(Register16.SP);

                    _reg.Z = Add(spl, _reg.Z, out var flags);
                    _reg.Flags.Replace(flags);
                    _reg.Flags.Reset(FlagsBit.Z);
                    yield return CycleStatus.Executing;

                    _reg.W = (byte)(sph + adj + (_reg.Flags.C ? 1 : 0));
                    yield return CycleStatus.Executing;

                    _reg.SP = _reg.WZ;

                    break;
                }
                #endregion

                #region Control flow instructions
                case Op.Jp: {
                    _reg.Z = Memory.Read(_reg.PC++);
                    yield return CycleStatus.Executing;

                    _reg.W = Memory.Read(_reg.PC++);
                    yield return CycleStatus.Executing;

                    _reg.PC = _reg.WZ;
                    yield return CycleStatus.Executing;
                    break;
                }

                case Op.JpHL: {
                    _reg.PC = _reg.HL;
                    break;
                }

                case Op.JpCond: {
                    _reg.Z = Memory.Read(_reg.PC++);
                    yield return CycleStatus.Executing;

                    _reg.W = Memory.Read(_reg.PC++);
                    yield return CycleStatus.Executing;

                    if (HasJumpCondition((Condition)instr.Src)) {
                        _reg.PC = _reg.WZ;
                        yield return CycleStatus.Executing;
                    }

                    break;
                }

                case Op.Jr: {
                    _reg.Z = Memory.Read(_reg.PC++);
                    yield return CycleStatus.Executing;

                    _reg.WZ = (ushort)(_reg.PC + (sbyte)_reg.Z);
                    yield return CycleStatus.Executing;

                    _reg.PC = _reg.WZ;
                    break;
                }

                case Op.JrCond: {
                    _reg.Z = Memory.Read(_reg.PC++);
                    yield return CycleStatus.Executing;

                    if (HasJumpCondition((Condition)instr.Src)) {
                        _reg.WZ = (ushort)(_reg.PC + (sbyte)_reg.Z);
                        yield return CycleStatus.Executing;

                        _reg.PC = _reg.WZ;
                    }

                    break;
                }

                case Op.Call: {
                    _reg.Z = Memory.Read(_reg.PC++);
                    yield return CycleStatus.Executing;

                    _reg.W = Memory.Read(_reg.PC++);
                    yield return CycleStatus.Executing;

                    _reg.SP--;
                    yield return CycleStatus.Executing;

                    var (pch, pcl) = _reg.Split(Register16.PC);
                    Memory.Write(_reg.SP--, pch);
                    yield return CycleStatus.Executing;

                    Memory.Write(_reg.SP, pcl);
                    _reg.PC = _reg.WZ;
                    yield return CycleStatus.Executing;

                    break;
                }

                case Op.CallCond: {
                    _reg.Z = Memory.Read(_reg.PC++);
                    yield return CycleStatus.Executing;

                    _reg.W = Memory.Read(_reg.PC++);
                    yield return CycleStatus.Executing;

                    if (HasJumpCondition((Condition)instr.Src)) {
                        _reg.SP--;
                        yield return CycleStatus.Executing;

                        var (pch, pcl) = _reg.Split(Register16.PC);
                        Memory.Write(_reg.SP--, pch);
                        yield return CycleStatus.Executing;

                        Memory.Write(_reg.SP, pcl);
                        _reg.PC = _reg.WZ;
                        yield return CycleStatus.Executing;
                    }

                    break;
                }

                case Op.Ret: {
                    _reg.Z = Memory.Read(_reg.SP++);
                    yield return CycleStatus.Executing;

                    _reg.W = Memory.Read(_reg.SP++);
                    yield return CycleStatus.Executing;

                    _reg.PC = _reg.WZ;
                    yield return CycleStatus.Executing;

                    break;
                }

                case Op.RetCond: {
                    var jump = HasJumpCondition((Condition)instr.Src);
                    yield return CycleStatus.Executing;

                    if (jump) {
                        _reg.Z = Memory.Read(_reg.SP++);
                        yield return CycleStatus.Executing;

                        _reg.W = Memory.Read(_reg.SP++);
                        yield return CycleStatus.Executing;

                        _reg.PC = _reg.WZ;
                        yield return CycleStatus.Executing;
                    }

                    break;
                }

                case Op.Reti: {
                    _reg.Z = Memory.Read(_reg.SP++);
                    yield return CycleStatus.Executing;

                    _reg.W = Memory.Read(_reg.SP++);
                    yield return CycleStatus.Executing;

                    _reg.PC = _reg.WZ;
                    Interrupts.MasterEnabled = true;
                    yield return CycleStatus.Executing;

                    break;
                }

                case Op.Rst: {
                    _reg.SP--;
                    yield return CycleStatus.Executing;

                    var (pch, pcl) = _reg.Split(Register16.PC);
                    Memory.Write(_reg.SP--, pch);
                    yield return CycleStatus.Executing;

                    Memory.Write(_reg.SP, pcl);
                    _reg.PC = instr.Dst;
                    yield return CycleStatus.Executing;

                    break;
                }
                #endregion

                default: {
                    throw new UnreachableException(
                        $"Missing implementation for operation {instr.Op}"
                    );
                }
            }

            _reg.IR = Memory.Read(_reg.PC++);

            if (Interrupts.TryAcknowledge(out var vector)) {
                yield return CycleStatus.Servicing;

                --_reg.PC;
                yield return CycleStatus.Servicing;

                --_reg.SP;
                yield return CycleStatus.Servicing;

                var (pch, pcl) = _reg.Split(Register16.PC);
                Memory.Write(_reg.SP--, pch);
                yield return CycleStatus.Servicing;

                Memory.Write(_reg.SP, pcl);

                _reg.PC = vector;
                yield return CycleStatus.Servicing;

                _reg.IR = Memory.Read(_reg.PC++);
            }

            yield return CycleStatus.Fetched;
        }
    }

    private IEnumerable<CycleStatus> ExecuteCb()
    {
        _reg.IR = Memory.Read(_reg.PC++);
        yield return CycleStatus.Executing;

        var instr = Instruction.DecodeCb(_reg.IR);
        switch (instr.Op) {
            case Op.RlcReg8: {
                var dst = (Register8)instr.Dst;
                _reg[dst] = Rlc(_reg[dst], out var flags);
                _reg.Flags.Replace(flags);
                break;
            }

            case Op.RlcHL: {
                _reg.Z = Memory.Read(_reg.HL);
                yield return CycleStatus.Executing;

                var result = Rlc(_reg.Z, out var flags);
                _reg.Flags.Replace(flags);

                Memory.Write(_reg.HL, result);
                yield return CycleStatus.Executing;
                break;
            }

            case Op.RrcReg8: {
                var dst = (Register8)instr.Dst;
                _reg[dst] = Rrc(_reg[dst], out var flags);
                _reg.Flags.Replace(flags);
                break;
            }

            case Op.RrcHL: {
                _reg.Z = Memory.Read(_reg.HL);
                yield return CycleStatus.Executing;

                var result = Rrc(_reg.Z, out var flags);
                _reg.Flags.Replace(flags);

                Memory.Write(_reg.HL, result);
                yield return CycleStatus.Executing;
                break;
            }

            case Op.RlReg8: {
                var dst = (Register8)instr.Dst;
                _reg[dst] = Rl(_reg[dst], _reg.Flags.C, out var flags);
                _reg.Flags.Replace(flags);
                break;
            }

            case Op.RlHL: {
                _reg.Z = Memory.Read(_reg.HL);
                yield return CycleStatus.Executing;

                var result = Rl(_reg.Z, _reg.Flags.C, out var flags);
                _reg.Flags.Replace(flags);

                Memory.Write(_reg.HL, result);
                yield return CycleStatus.Executing;
                break;
            }

            case Op.RrReg8: {
                var dst = (Register8)instr.Dst;
                _reg[dst] = Rr(_reg[dst], _reg.Flags.C, out var flags);
                _reg.Flags.Replace(flags);
                break;
            }

            case Op.RrHL: {
                _reg.Z = Memory.Read(_reg.HL);
                yield return CycleStatus.Executing;

                var result = Rr(_reg.Z, _reg.Flags.C, out var flags);
                _reg.Flags.Replace(flags);

                Memory.Write(_reg.HL, result);
                yield return CycleStatus.Executing;
                break;
            }

            case Op.SlaReg8: {
                var dst = (Register8)instr.Dst;
                _reg[dst] = Sla(_reg[dst], out var flags);
                _reg.Flags.Replace(flags);
                break;
            }

            case Op.SlaHL: {
                _reg.Z = Memory.Read(_reg.HL);
                yield return CycleStatus.Executing;

                var result = Sla(_reg.Z, out var flags);
                _reg.Flags.Replace(flags);

                Memory.Write(_reg.HL, result);
                yield return CycleStatus.Executing;
                break;
            }

            case Op.SraReg8: {
                var dst = (Register8)instr.Dst;
                _reg[dst] = Sra(_reg[dst], out var flags);
                _reg.Flags.Replace(flags);
                break;
            }

            case Op.SraHL: {
                _reg.Z = Memory.Read(_reg.HL);
                yield return CycleStatus.Executing;

                var result = Sra(_reg.Z, out var flags);
                _reg.Flags.Replace(flags);

                Memory.Write(_reg.HL, result);
                yield return CycleStatus.Executing;
                break;
            }

            case Op.SrlReg8: {
                var dst = (Register8)instr.Dst;
                _reg[dst] = Srl(_reg[dst], out var flags);
                _reg.Flags.Replace(flags);
                break;
            }

            case Op.SrlHL: {
                _reg.Z = Memory.Read(_reg.HL);
                yield return CycleStatus.Executing;

                var result = Srl(_reg.Z, out var flags);
                _reg.Flags.Replace(flags);

                Memory.Write(_reg.HL, result);
                yield return CycleStatus.Executing;
                break;
            }

            case Op.SwapReg8: {
                var dst = (Register8)instr.Dst;
                _reg[dst] = Swap(_reg[dst], out var flags);
                _reg.Flags.Replace(flags);
                break;
            }

            case Op.SwapHL: {
                _reg.Z = Memory.Read(_reg.HL);
                yield return CycleStatus.Executing;

                var result = Swap(_reg.Z, out var flags);
                _reg.Flags.Replace(flags);

                Memory.Write(_reg.HL, result);
                yield return CycleStatus.Executing;
                break;
            }

            case Op.BitReg8: {
                var value = _reg[(Register8)instr.Src];
                Bit(value, instr.Dst);

                break;
            }

            case Op.BitHL: {
                _reg.Z = Memory.Read(_reg.HL);
                yield return CycleStatus.Executing;

                Bit(_reg.Z, instr.Dst);
                break;
            }

            case Op.ResReg8: {
                var src = (Register8)instr.Src;
                _reg[src] = Res(_reg[src], instr.Dst);
                break;
            }

            case Op.ResHL: {
                _reg.Z = Memory.Read(_reg.HL);
                yield return CycleStatus.Executing;

                var result = Res(_reg.Z, instr.Dst);

                Memory.Write(_reg.HL, result);
                yield return CycleStatus.Executing;
                break;
            }

            case Op.SetReg8: {
                var src = (Register8)instr.Src;
                _reg[src] = Set(_reg[src], instr.Dst);
                break;
            }

            case Op.SetHL: {
                _reg.Z = Memory.Read(_reg.HL);
                yield return CycleStatus.Executing;

                var result = Set(_reg.Z, instr.Dst);

                Memory.Write(_reg.HL, result);
                yield return CycleStatus.Executing;
                break;
            }

            default: {
                throw new UnreachableException(
                    $"Missing implementation for CB-prefixed operation {instr.Op}"
                );
            }
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

    private static byte Rlc(byte value, out FlagsBit flags)
    {
        var result = byte.RotateLeft(value, 1);

        flags = 0;
        if ((result & 1) == 1) flags |= FlagsBit.C;
        if (result == 0) flags |= FlagsBit.Z;

        return result;
    }

    private static byte Rrc(byte value, out FlagsBit flags)
    {
        var result = byte.RotateRight(value, 1);

        flags = 0;
        if (result >> 7 == 1) flags |= FlagsBit.C;
        if (result == 0) flags |= FlagsBit.Z;

        return result;
    }

    private static byte Rl(byte value, bool carry, out FlagsBit flags)
    {
        var result = (byte)(value << 1);
        result = (byte)(carry ? result | 1 : result & ~1);

        flags = 0;
        if (value >> 7 == 1) flags |= FlagsBit.C;
        if (result == 0) flags |= FlagsBit.Z;

        return result;
    }

    private static byte Rr(byte value, bool carry, out FlagsBit flags)
    {
        var result = (byte)(value >> 1);
        result = (byte)(carry ? result | 0x80 : result & ~0x80);

        flags = 0;
        if ((value & 1) == 1) flags |= FlagsBit.C;
        if (result == 0) flags |= FlagsBit.Z;

        return result;
    }

    private static byte Sla(byte value, out FlagsBit flags)
    {
        var result = (byte)(value << 1);

        flags = 0;
        if (value >> 7 == 1) flags |= FlagsBit.C;
        if (result == 0) flags |= FlagsBit.Z;

        return result;
    }

    private static byte Sra(byte value, out FlagsBit flags)
    {
        var result = (byte)((sbyte)value >> 1);

        flags = 0;
        if ((value & 1) == 1) flags |= FlagsBit.C;
        if (result == 0) flags |= FlagsBit.Z;

        return result;
    }

    private static byte Srl(byte value, out FlagsBit flags)
    {
        var result = (byte)(value >> 1);

        flags = 0;
        if ((value & 1) == 1) flags |= FlagsBit.C;
        if (result == 0) flags |= FlagsBit.Z;

        return result;
    }

    private static byte Swap(byte value, out FlagsBit flags)
    {
        var result = byte.RotateLeft(value, 4);

        flags = result == 0 ? FlagsBit.Z : 0;

        return result;
    }

    private void Bit(byte value, byte bit)
    {
        var test = ((value >> bit) & 1) == 0;
        _reg.Flags.Apply(
            FlagsBit.Z | FlagsBit.N | FlagsBit.H,
            (test ? FlagsBit.Z : 0) | FlagsBit.H
        );
    }

    private static byte Res(byte value, byte bit) => (byte)(value & ~(1 << bit));

    private static byte Set(byte value, byte bit) => (byte)(value | (1 << bit));

    private bool HasJumpCondition(Condition condition) => condition switch {
        Condition.NotZero  => !_reg.Flags.Z,
        Condition.Zero     => _reg.Flags.Z,
        Condition.NotCarry => !_reg.Flags.C,
        Condition.Carry    => _reg.Flags.C,
    };

    private const ushort HighRam = 0xFF00;
}

internal enum Op
{
    Nop,
    Halt,
    Stop,
    Di,
    Ei,

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
    AddHLReg16,
    AddSPImm8,
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
    IncReg16,
    DecReg8,
    DecReg16,
    DecHL,
    Ccf,
    Scf,
    Cpl,
    Daa,

    AndReg8,
    AndHL,
    AndImm8,
    OrReg8,
    OrHL,
    OrImm8,
    XorReg8,
    XorHL,
    XorImm8,
    Rlca,
    Rrca,
    Rla,
    Rra,

    CbPrefix,
    RlcReg8,
    RlcHL,
    RrcReg8,
    RrcHL,
    RlReg8,
    RlHL,
    RrReg8,
    RrHL,
    SlaReg8,
    SlaHL,
    SraReg8,
    SraHL,
    SwapReg8,
    SwapHL,
    SrlReg8,
    SrlHL,
    BitReg8,
    BitHL,
    ResReg8,
    ResHL,
    SetReg8,
    SetHL,

    Jp,
    JpHL,
    JpCond,
    Jr,
    JrCond,
    Call,
    CallCond,
    Ret,
    RetCond,
    Reti,
    Rst,
}

internal enum Condition
{
    NotZero,
    Zero,
    NotCarry,
    Carry,
}

internal readonly record struct Instruction(Op Op, byte Dst = byte.MaxValue, byte Src = byte.MaxValue)
{
    public static Instruction Decode(byte opcode)
    {
        var x = (byte)((opcode >> 6) & 0b11);
        var y = (byte)((opcode >> 3) & 0b111);
        var z = (byte)((opcode >> 0) & 0b111);
        var p = (byte)((y >> 1) & 0b11);
        var q = (byte)((y >> 0) & 0b1);

        return (x, z, y, q, p) switch {
            #region x == 0
            (0, 0, 0,    _, _) => new Instruction(Op.Nop),
            (0, 0, 1,    _, _) => new Instruction(Op.LoadInd16SP),
            (0, 0, 2,    _, _) => new Instruction(Op.Stop),
            (0, 0, 3,    _, _) => new Instruction(Op.Jr),
            (0, 0, >= 4, _, _) => new Instruction(Op.JrCond, Src: ToCondition(y - 4)),

            (0, 1, _, 0, _) => new Instruction(Op.LoadImm16, Dst: ToReg16(p)),
            (0, 1, _, 1, _) => new Instruction(Op.AddHLReg16, Src: ToReg16(p)),

            (0, 2, _, 0, 0) => new Instruction(Op.LoadInd16A, Dst: (byte)Register16.BC),
            (0, 2, _, 0, 1) => new Instruction(Op.LoadInd16A, Dst: (byte)Register16.DE),
            (0, 2, _, 0, 2) => new Instruction(Op.LoadIncHLA),
            (0, 2, _, 0, 3) => new Instruction(Op.LoadDecHLA),
            (0, 2, _, 1, 0) => new Instruction(Op.LoadAInd16, Src: (byte)Register16.BC),
            (0, 2, _, 1, 1) => new Instruction(Op.LoadAInd16, Src: (byte)Register16.DE),
            (0, 2, _, 1, 2) => new Instruction(Op.LoadAIncHL),
            (0, 2, _, 1, 3) => new Instruction(Op.LoadADecHL),

            (0, 3, _, 0, _) => new Instruction(Op.IncReg16, Dst: ToReg16(p)),
            (0, 3, _, 1, _) => new Instruction(Op.DecReg16, Dst: ToReg16(p)),

            (0, 4, 6, _, _) => new Instruction(Op.IncHL),
            (0, 4, _, _, _) => new Instruction(Op.IncReg8, Dst: ToReg8(y)),
            (0, 5, 6, _, _) => new Instruction(Op.DecHL),
            (0, 5, _, _, _) => new Instruction(Op.DecReg8, Dst: ToReg8(y)),

            (0, 6, 6, _, _) => new Instruction(Op.LoadIndHLImm8),
            (0, 6, _, _, _) => new Instruction(Op.LoadImm8, Dst: ToReg8(y)),

            (0, 7, 0, _, _) => new Instruction(Op.Rlca),
            (0, 7, 1, _, _) => new Instruction(Op.Rrca),
            (0, 7, 2, _, _) => new Instruction(Op.Rla),
            (0, 7, 3, _, _) => new Instruction(Op.Rra),
            (0, 7, 4, _, _) => new Instruction(Op.Daa),
            (0, 7, 5, _, _) => new Instruction(Op.Cpl),
            (0, 7, 6, _, _) => new Instruction(Op.Scf),
            (0, 7, 7, _, _) => new Instruction(Op.Ccf),
            #endregion

            #region x == 1
            (1, 6, 6, _, _) => new Instruction(Op.Halt),
            (1, _, 6, _, _) => new Instruction(Op.LoadIndHLReg8, Src: ToReg8(z)),
            (1, 6, _, _, _) => new Instruction(Op.LoadReg8IndHL, Dst: ToReg8(y)),
            (1, _, _, _, _) => new Instruction(Op.LoadReg8, Dst: ToReg8(y), Src: ToReg8(z)),
            #endregion

            #region x == 2
            (2, 6, 0, _, _) => new Instruction(Op.AddHL, Src: (byte)Register8.Z),
            (2, _, 0, _, _) => new Instruction(Op.AddReg8, Src: ToReg8(z)),

            (2, 6, 1, _, _) => new Instruction(Op.AdcHL, Src: (byte)Register8.Z),
            (2, _, 1, _, _) => new Instruction(Op.AdcReg8, Src: ToReg8(z)),

            (2, 6, 2, _, _) => new Instruction(Op.SubHL, Src: (byte)Register8.Z),
            (2, _, 2, _, _) => new Instruction(Op.SubReg8, Src: ToReg8(z)),

            (2, 6, 3, _, _) => new Instruction(Op.SbcHL, Src: (byte)Register8.Z),
            (2, _, 3, _, _) => new Instruction(Op.SbcReg8, Src: ToReg8(z)),

            (2, 6, 4, _, _) => new Instruction(Op.AndHL, Src: (byte)Register8.Z),
            (2, _, 4, _, _) => new Instruction(Op.AndReg8, Src: ToReg8(z)),

            (2, 6, 5, _, _) => new Instruction(Op.XorHL, Src: (byte)Register8.Z),
            (2, _, 5, _, _) => new Instruction(Op.XorReg8, Src: ToReg8(z)),

            (2, 6, 6, _, _) => new Instruction(Op.OrHL, Src: (byte)Register8.Z),
            (2, _, 6, _, _) => new Instruction(Op.OrReg8, Src: ToReg8(z)),

            (2, 6, 7, _, _) => new Instruction(Op.CpHL, Src: (byte)Register8.Z),
            (2, _, 7, _, _) => new Instruction(Op.CpReg8, Src: ToReg8(z)),
            #endregion

            #region x == 3
            (3, 0, <= 3, _, _) => new Instruction(Op.RetCond, Src: ToCondition(y)),
            (3, 0, 4,    _, _) => new Instruction(Op.LoadDirHighA),
            (3, 0, 5,    _, _) => new Instruction(Op.AddSPImm8),
            (3, 0, 6,    _, _) => new Instruction(Op.LoadADirHigh),
            (3, 0, 7,    _, _) => new Instruction(Op.LoadHLAdjustedSP),

            (3, 1, _, 0, _) => new Instruction(Op.Pop, Dst: ToReg16Stack(p)),
            (3, 1, _, 1, 0) => new Instruction(Op.Ret),
            (3, 1, _, 1, 1) => new Instruction(Op.Reti),
            (3, 1, _, 1, 2) => new Instruction(Op.JpHL),
            (3, 1, _, 1, 3) => new Instruction(Op.LoadSPHL),

            (3, 2, <= 3, _, _) => new Instruction(Op.JpCond, Src: ToCondition(y)),
            (3, 2, 4,    _, _) => new Instruction(Op.LoadIndHighA),
            (3, 2, 5,    _, _) => new Instruction(Op.LoadDir16A),
            (3, 2, 6,    _, _) => new Instruction(Op.LoadAIndHigh),
            (3, 2, 7,    _, _) => new Instruction(Op.LoadADir16),

            (3, 3, 0, _, _) => new Instruction(Op.Jp),
            (3, 3, 1, _, _) => new Instruction(Op.CbPrefix),
            (3, 3, 6, _, _) => new Instruction(Op.Di),
            (3, 3, 7, _, _) => new Instruction(Op.Ei),

            (3, 4, <= 3, _, _) => new Instruction(Op.CallCond, Src: ToCondition(y)),

            (3, 5, _, 0, _) => new Instruction(Op.Push, Src: ToReg16Stack(p)),
            (3, 5, _, 1, 0) => new Instruction(Op.Call),

            (3, 6, 0, _, _) => new Instruction(Op.AddImm8, Src: (byte)Register8.Z),
            (3, 6, 1, _, _) => new Instruction(Op.AdcImm8, Src: (byte)Register8.Z),
            (3, 6, 2, _, _) => new Instruction(Op.SubImm8, Src: (byte)Register8.Z),
            (3, 6, 3, _, _) => new Instruction(Op.SbcImm8, Src: (byte)Register8.Z),
            (3, 6, 4, _, _) => new Instruction(Op.AndImm8, Src: (byte)Register8.Z),
            (3, 6, 5, _, _) => new Instruction(Op.XorImm8, Src: (byte)Register8.Z),
            (3, 6, 6, _, _) => new Instruction(Op.OrImm8, Src: (byte)Register8.Z),
            (3, 6, 7, _, _) => new Instruction(Op.CpImm8, Src: (byte)Register8.Z),

            (3, 7, _, _, _) => new Instruction(Op.Rst, Dst: (byte)(8 * y)),
            #endregion

            _ => throw new UnreachableException($"Missing decoder for opcode 0x{opcode:X2}"),
        };
    }

    public static Instruction DecodeCb(byte opcode)
    {
        var x = (byte)((opcode >> 6) & 0b11);
        var y = (byte)((opcode >> 3) & 0b111);
        var z = (byte)((opcode >> 0) & 0b111);

        return (x, y, z) switch {
            (0, 0, 6) => new Instruction(Op.RlcHL, Dst: (byte)Register8.Z),
            (0, 0, _) => new Instruction(Op.RlcReg8, Dst: ToReg8(z)),

            (0, 1, 6) => new Instruction(Op.RrcHL, Dst: (byte)Register8.Z),
            (0, 1, _) => new Instruction(Op.RrcReg8, Dst: ToReg8(z)),

            (0, 2, 6) => new Instruction(Op.RlHL, Dst: (byte)Register8.Z),
            (0, 2, _) => new Instruction(Op.RlReg8, Dst: ToReg8(z)),

            (0, 3, 6) => new Instruction(Op.RrHL, Dst: (byte)Register8.Z),
            (0, 3, _) => new Instruction(Op.RrReg8, Dst: ToReg8(z)),

            (0, 4, 6) => new Instruction(Op.SlaHL, Dst: (byte)Register8.Z),
            (0, 4, _) => new Instruction(Op.SlaReg8, Dst: ToReg8(z)),

            (0, 5, 6) => new Instruction(Op.SraHL, Dst: (byte)Register8.Z),
            (0, 5, _) => new Instruction(Op.SraReg8, Dst: ToReg8(z)),

            (0, 6, 6) => new Instruction(Op.SwapHL, Dst: (byte)Register8.Z),
            (0, 6, _) => new Instruction(Op.SwapReg8, Dst: ToReg8(z)),

            (0, 7, 6) => new Instruction(Op.SrlHL, Dst: (byte)Register8.Z),
            (0, 7, _) => new Instruction(Op.SrlReg8, Dst: ToReg8(z)),

            (1, _, 6) => new Instruction(Op.BitHL, Dst: y, Src: (byte)Register8.Z),
            (1, _, _) => new Instruction(Op.BitReg8, Dst: y, Src: ToReg8(z)),

            (2, _, 6) => new Instruction(Op.ResHL, Dst: y, Src: (byte)Register8.Z),
            (2, _, _) => new Instruction(Op.ResReg8, Dst: y, Src: ToReg8(z)),

            (3, _, 6) => new Instruction(Op.SetHL, Dst: y, Src: (byte)Register8.Z),
            (3, _, _) => new Instruction(Op.SetReg8, Dst: y, Src: ToReg8(z)),

            _ => throw new UnreachableException($"Missing decoder for opcode 0x{opcode:X2}"),
        };
    }

    private static byte ToReg8(byte placeholder) =>
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

    private static byte ToReg16(byte placeholder) =>
        placeholder switch {
            0 => (byte)Register16.BC,
            1 => (byte)Register16.DE,
            2 => (byte)Register16.HL,
            3 => (byte)Register16.SP,
            _ => throw new ArgumentException($"Not a Register16 placeholder: {placeholder}"),
        };

    private static byte ToReg16Stack(byte placeholder) =>
        placeholder switch {
            0 => (byte)Register16.BC,
            1 => (byte)Register16.DE,
            2 => (byte)Register16.HL,
            3 => (byte)Register16.AF,
            _ => throw new ArgumentException($"Not a Register16 placeholder: {placeholder}"),
        };

    private static byte ToCondition(int placeholder) =>
        placeholder switch {
            0 => (byte)Condition.NotZero,
            1 => (byte)Condition.Zero,
            2 => (byte)Condition.NotCarry,
            3 => (byte)Condition.Carry,
            _ => throw new ArgumentException($"Not a Condition placeholder: {placeholder}"),
        };
}
