using Raylib_cs;

class DMG {
    public byte[] gameRom;
    public byte[] bootRom;

    public MMU mmu;
    public CPU cpu;
    public PPU ppu;
    public Joypad joypad;
    public Timer timer;

    Image screenImage;
    Texture2D screenTexture;

    Image icon;

    public DMG(string gameRomPath, string bootRomDataPath) {
        gameRom = File.ReadAllBytes(gameRomPath);
        if (File.Exists(bootRomDataPath)){
            bootRom = File.ReadAllBytes(bootRomDataPath);
        } else {
            bootRom = new byte[256];
        }
        

        if (!Helper.raylibLog) Raylib.SetTraceLogLevel(TraceLogLevel.None);

        Raylib.InitWindow(160*Helper.scale, 144*Helper.scale, "DMG");
        Raylib.SetTargetFPS(60);
        
        icon = Raylib.LoadImage("icon.png");
        Raylib.SetWindowIcon(icon);

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
        Console.WriteLine("\n" + mmu.HeaderInfo() + "\n");
        mmu.Load(Path.Combine(Path.GetDirectoryName(Helper.rom) ?? string.Empty, Path.GetFileNameWithoutExtension(Helper.rom) + ".sav"));

        while (!Raylib.WindowShouldClose()) {
            Raylib.BeginDrawing();
            //Raylib.ClearBackground(Color.White);

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

            if (Helper.fpsEnable) Raylib.DrawFPS(0, 0);  

            Raylib.EndDrawing();
        }

        mmu.Save(Path.Combine(Path.GetDirectoryName(Helper.rom) ?? string.Empty, Path.GetFileNameWithoutExtension(Helper.rom) + ".sav"));
        Console.Write("Closing Window\n");
        
        Raylib.UnloadImage(screenImage);
        Raylib.UnloadTexture(screenTexture);
        Raylib.UnloadImage(icon);
        Raylib.CloseWindow();
    }
}