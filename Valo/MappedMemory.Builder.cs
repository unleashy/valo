using System.Collections.Immutable;

namespace Valo;

public partial class MappedMemory
{
    public sealed class Builder
    {
        private ImmutableArray<Allocation>.Builder _map =
            ImmutableArray.CreateBuilder<Allocation>();

        public Builder Map(ushort start, ISizedMemory memory)
        {
            return Map(start, (ushort)(start + memory.Size), memory);
        }

        public Builder Map(ushort start, ushort end, IMemory memory)
        {
            if (start >= end) {
                throw new ArgumentException(
                    $"Start address ${start:X4} must be less than end address ${end:X4}"
                );
            }

            if (HasAllocation(start, end)) {
                throw new ArgumentException(
                    $"Address range ${start:X4}..${end:X4} is already mapped"
                );
            }

            _map.Add(new Allocation(start, end, memory));

            return this;
        }

        public MappedMemory Build()
        {
            return new MappedMemory(_map.DrainToImmutable());
        }

        private bool HasAllocation(ushort start, ushort end) =>
            _map.Any(it => start < it.End && it.Start < end);
    }
}
