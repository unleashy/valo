namespace Valo;

public readonly record struct LocatedMemory(uint Start, uint End, IMemory Memory);
