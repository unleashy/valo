using System.Collections.Immutable;

namespace Valo;

public partial class MappedMemory
{
    public sealed class Builder
    {
        private readonly ImmutableArray<LocatedMemory>.Builder _map =
            ImmutableArray.CreateBuilder<LocatedMemory>();

        public Builder Map(uint start, ISizedMemory memory) =>
            Map(start, start + memory.Size, memory);

        public Builder Map(uint start, uint end, IMemory memory) =>
            Map(new LocatedMemory(start, end, memory));

        public Builder Map(LocatedMemory locatedMemory)
        {
            Validate(locatedMemory);
            _map.Add(locatedMemory);
            return this;
        }

        public Builder Map(params IEnumerable<LocatedMemory> layout)
        {
            foreach (var mem in layout) Map(mem);
            return this;
        }

        public MappedMemory Build()
        {
            _map.Sort((a, b) => (int)(a.Start - b.Start));
            return new MappedMemory(_map.DrainToImmutable());
        }

        private void Validate(LocatedMemory mem)
        {
            if (mem.Start >= mem.End) {
                throw new ArgumentException(
                    $"Start address ${mem.Start:X4} must be less than end address ${mem.End:X4}"
                );
            }

            if (HasAllocation(mem)) {
                throw new ArgumentException(
                    $"Address range ${mem.Start:X4}..${mem.End:X4} is already mapped"
                );
            }
        }

        private bool HasAllocation(LocatedMemory locatedMemory) =>
            _map.Any(it => locatedMemory.Start < it.End && it.Start < locatedMemory.End);
    }
}
