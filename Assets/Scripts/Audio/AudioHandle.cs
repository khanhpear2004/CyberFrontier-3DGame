public readonly struct AudioHandle
{
    internal AudioHandle(int slotIndex, int generation)
    {
        SlotIndex = slotIndex;
        Generation = generation;
    }

    internal int SlotIndex { get; }
    internal int Generation { get; }
    public bool IsValid => SlotIndex >= 0 && Generation > 0;

    public static AudioHandle Invalid => new AudioHandle(-1, -1);
}
