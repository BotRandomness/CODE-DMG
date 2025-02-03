using Raylib_cs;

namespace CODE_DMG;
public class Dmg
{
    private readonly Mmu mmu;
    private readonly Cpu cpu;
    private readonly Ppu ppu;
    private readonly Joypad joypad;
    private readonly Timer timer;

    private readonly Image screenImage;
    private readonly Texture2D screenTexture;

    private readonly Image icon;

    public Dmg(string gameRomPath, string bootRomDataPath)
    {
        var gameRom = File.ReadAllBytes(gameRomPath);
        var bootRom = File.Exists(bootRomDataPath) ? File.ReadAllBytes(bootRomDataPath) : new byte[256];

        if (!Helper.RaylibLog) Raylib.SetTraceLogLevel(TraceLogLevel.None);

        Raylib.InitWindow(160 * Helper.Scale, 144 * Helper.Scale, "DMG");
        Raylib.SetTargetFPS(60);

        icon = Raylib.LoadImage("icon.png");
        Raylib.SetWindowIcon(icon);

        screenImage = Raylib.GenImageColor(160, 144, Color.Black);
        screenTexture = Raylib.LoadTextureFromImage(screenImage);

        mmu = new Mmu(gameRom, bootRom, false);
        cpu = new Cpu(mmu);
        ppu = new Ppu(mmu, screenImage, screenTexture);
        joypad = new Joypad(mmu);
        timer = new Timer(mmu);

        if (!File.Exists(bootRomDataPath)) cpu.Reset();

        Console.WriteLine("DMG");
    }

    public void Run()
    {
        Console.WriteLine("\n" + mmu.HeaderInfo() + "\n");
        mmu.Load(Path.Combine(Path.GetDirectoryName(Helper.Rom) ?? string.Empty, Path.GetFileNameWithoutExtension(Helper.Rom) + ".sav"));

        while (!Raylib.WindowShouldClose())
        {
            Raylib.BeginDrawing();
            joypad.HandleInput();

            var cycles = 0;
            while (cycles < 70224)
            {
                var cycle = cpu.ExecuteInstruction();
                cycles += cycle;
                ppu.Step(cycle);
                timer.Step(cycle);

                if (cpu.Pc == 0x100)
                {
                    Console.WriteLine("Made it to PC: 0x100");
                }
            }

            if (Helper.FpsEnable) Raylib.DrawFPS(0, 0);

            Raylib.EndDrawing();
        }

        mmu.Save(Path.Combine(Path.GetDirectoryName(Helper.Rom) ?? string.Empty, Path.GetFileNameWithoutExtension(Helper.Rom) + ".sav"));
        Console.Write("Closing Window\n");

        Raylib.UnloadImage(screenImage);
        Raylib.UnloadTexture(screenTexture);
        Raylib.UnloadImage(icon);
        Raylib.CloseWindow();
    }
}