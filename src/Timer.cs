namespace CODE_DMG;

public class Timer(Mmu mmu)
{
    private readonly Mmu mmu = mmu;

    private int cycles;

    public void Step(int elapsedCycles)
    {
        cycles += elapsedCycles;
        if (cycles < 256) return;
        cycles -= 256;
        mmu.Div++;
    }
}