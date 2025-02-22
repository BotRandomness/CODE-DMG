using Raylib_cs;
using ImGuiNET;

class Game {
    DMG dmg;

    private FileDialog fileDialog;
    private string selectedFilePath = "";

    private FileDialog fileDialogBootRom;
    private string selectedFilePathBootRom = "";

    private bool configRegShow;
    private bool romInfoRegShow;

    Windows windows;

    public Game(DMG dmg) {
        this.dmg = dmg;

        fileDialog = new FileDialog(Directory.GetCurrentDirectory());
        fileDialogBootRom = new FileDialog(Directory.GetCurrentDirectory());
        
        windows = new Windows();

        configRegShow = false;
        romInfoRegShow = false;
    }

    public void Run(ref bool mode, ref bool insertingRom) {
        Raylib.SetWindowSize(160 * Helper.scale, (144 * Helper.scale)+19);

        if (ImGui.BeginMainMenuBar()) {
            ImGui.Text("CODE-DMG");

            ImGui.Separator();

            if (ImGui.BeginMenu("File")) {
                if (ImGui.MenuItem("Open ROM")) {
                        fileDialog.Open();
                }
                if (ImGui.MenuItem("Select BOOTROM")) {
                    fileDialogBootRom.Open();
                }
                 ImGui.EndMenu();
            }
            if (ImGui.BeginMenu("View")) {
                if (ImGui.MenuItem("Debug")) {
                    mode = !mode;
                    Helper.fpsEnable = true;
                    Helper.scale = 3;

                    Raylib.SetWindowSize(1280, 800);

                    int screenWidth = Raylib.GetMonitorWidth(0);
                    int screenHeight = Raylib.GetMonitorHeight(0);
                    int windowWidth = Raylib.GetScreenWidth();
                    int windowHeight = Raylib.GetScreenHeight();
                    int posX = (screenWidth - windowWidth) / 2;
                    int posY = (screenHeight - windowHeight) / 2;

                    Raylib.SetWindowPosition(posX, posY);
                }
                if (ImGui.MenuItem("FPS Enable", null, Helper.fpsEnable)) {
                    Helper.fpsEnable = !Helper.fpsEnable;
                }
                if (ImGui.MenuItem("Options", null, configRegShow)) {
                    configRegShow = !configRegShow;
                }
                if (ImGui.MenuItem("About ROM", null, romInfoRegShow)) {
                    romInfoRegShow = !romInfoRegShow;
                }
                    ImGui.EndMenu();
            }

                ImGui.EndMainMenuBar();
        }

        Raylib.DrawTextureEx(dmg.screenTexture, new System.Numerics.Vector2(0, 19), 0.0f, Helper.scale, Color.White);
        if (configRegShow) {
            ImGui.SetNextWindowPos(new System.Numerics.Vector2(0, 0), ImGuiCond.Appearing);
            ImGui.SetNextWindowSize(new System.Numerics.Vector2(160 * Helper.scale, (144 * Helper.scale)+19), ImGuiCond.Appearing);
            windows.ConfigRegWindow(ref configRegShow);
        }
        if (romInfoRegShow) {
            ImGui.SetNextWindowPos(new System.Numerics.Vector2(0, 0), ImGuiCond.Appearing);
            ImGui.SetNextWindowSize(new System.Numerics.Vector2(320, 135), ImGuiCond.Appearing);
            windows.RomInfoRegWindow(ref dmg, ref romInfoRegShow);
        }

        ImGui.SetNextWindowPos(new System.Numerics.Vector2(0, 0), ImGuiCond.Appearing);
        ImGui.SetNextWindowSize(new System.Numerics.Vector2(160 * Helper.scale, (144 * Helper.scale)+19), ImGuiCond.Appearing);
        if (fileDialog.Show(ref selectedFilePath)) {
            Helper.rom = selectedFilePath;
            insertingRom = true;
        }
        ImGui.SetNextWindowPos(new System.Numerics.Vector2(0, 0), ImGuiCond.Appearing);
        ImGui.SetNextWindowSize(new System.Numerics.Vector2(160 * Helper.scale, (144 * Helper.scale)+19), ImGuiCond.Appearing);
        if (fileDialogBootRom.Show(ref selectedFilePathBootRom)) {
            Helper.bootrom = selectedFilePathBootRom;
            insertingRom = true;
        }

        if (Helper.fpsEnable) Raylib.DrawFPS(1, 17);
    }
}