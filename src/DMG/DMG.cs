using Raylib_cs;
using rlImGui_cs;
using ImGuiNET;

class DMG {
    public byte[] gameRom;
    public byte[] bootRom;

    public MMU mmu;
    public CPU cpu;
    public PPU ppu;
    public Joypad joypad;
    public Timer timer;

    public Image screenImage;
    public Texture2D screenTexture;

    public DMG(string gameRomPath, string bootRomDataPath) {
        if (File.Exists(gameRomPath)) {
            gameRom = File.ReadAllBytes(gameRomPath);
        } else {
            gameRom = new byte[32768];
        }
        if (File.Exists(bootRomDataPath)){
            bootRom = File.ReadAllBytes(bootRomDataPath);
        } else {
            bootRom = new byte[256];
        }
        
        screenImage = Raylib.GenImageColor(160, 144, Color.Black);
        screenTexture = Raylib.LoadTextureFromImage(screenImage);

        mmu = new MMU(gameRom, bootRom, false);
        cpu = new CPU(mmu);
        ppu = new PPU(mmu, screenImage, screenTexture);
        joypad = new Joypad(mmu);
        timer = new Timer(mmu);

        if (!File.Exists(bootRomDataPath)) cpu.Reset();

        Console.WriteLine("DMG");
    }

    public void Run() {
        if (gameRom.SequenceEqual(new byte[32768])) {
            return;
        }

        int cycles = 0;
        int cycle = 0;

        joypad.HandleInput();

        while (cycles < 70224) {
            cycle = cpu.ExecuteInstruction();
            cycles += cycle;
            ppu.Step(cycle);
            timer.Step(cycle);

            if (cpu.PC == 0x100) {
                Console.WriteLine("Made it to PC: 0x100");
            }
        }
    }

    public void Load() {
        Console.WriteLine("\n" + mmu.HeaderInfo() + "\n");
        mmu.Load(Path.Combine(Path.GetDirectoryName(Helper.rom) ?? string.Empty, Path.GetFileNameWithoutExtension(Helper.rom) + ".sav"));
    }

    public void Save() {
        mmu.Save(Path.Combine(Path.GetDirectoryName(Helper.rom) ?? string.Empty, Path.GetFileNameWithoutExtension(Helper.rom) + ".sav"));
        Raylib.UnloadImage(screenImage);
        Raylib.UnloadTexture(screenTexture);
    }
}