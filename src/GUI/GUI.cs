using Raylib_cs;
using rlImGui_cs;
using ImGuiNET;

class GUI {
	public DMG dmg;

    public bool mode = true;
    public bool pause = false;
    public bool insertingRom = false;

    Debug debug;
    Game game;

	public GUI() {
        dmg = new DMG(Helper.rom, Helper.bootrom);

        debug = new Debug(dmg);
        game = new Game(dmg);

        dmg.Load();
	}

    public void Run() {
        while (!Raylib.WindowShouldClose()) {
            Raylib.BeginDrawing();
            Raylib.ClearBackground(Color.DarkGray);
            rlImGui.Begin();

            if (insertingRom) {
                dmg = new DMG(Helper.rom, Helper.bootrom);
                debug = new Debug(dmg);
                game = new Game(dmg);
                dmg.Load();
                insertingRom = false;
            }

            if (!pause) {
                dmg.Run();
            }

            if (mode == true) {
                debug.Run(ref mode, ref insertingRom, ref pause);
            } else if (mode == false) {
                game.Run(ref mode, ref insertingRom);
            }

            rlImGui.End();
            Raylib.EndDrawing();
        }

        dmg.Save();
    }
}