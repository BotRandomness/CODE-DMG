using Raylib_cs;
using ImGuiNET;

class Debug {
    DMG dmg;

    private FileDialog fileDialog;
    private string selectedFilePath = "";

    private FileDialog fileDialogBootRom;
    private string selectedFilePathBootRom = "";

    private bool dmgShow;
    private bool configShow;
    private bool romInfoShow;
    private bool memoryShow;
    private bool regInfoShow;
    private bool pauseShow;
    private bool vramViewShow;

    Windows windows;

    MemoryEditor memoryEditorRom;
    MemoryEditor memoryEditorWram;
    MemoryEditor memoryEditorVram;
    MemoryEditor memoryEditorOam;
    MemoryEditor memoryEditorHram;
    MemoryEditor memoryEditorIo;

    VRAMViewer view;

    public Debug(DMG dmg) {
        this.dmg = dmg;

        fileDialog = new FileDialog(Directory.GetCurrentDirectory());
        fileDialogBootRom = new FileDialog(Directory.GetCurrentDirectory());

        memoryEditorRom = new MemoryEditor();
        memoryEditorRom.Data = new byte[dmg.mmu.rom.Length];
        memoryEditorRom.base_display_addr = 0x0000;

        memoryEditorWram = new MemoryEditor();
        memoryEditorWram.Data = new byte[dmg.mmu.wram.Length];
        memoryEditorWram.base_display_addr = 0xC000;

        memoryEditorVram = new MemoryEditor();
        memoryEditorVram.Data = new byte[dmg.mmu.vram.Length];
        memoryEditorVram.base_display_addr = 0x8000;

        memoryEditorOam = new MemoryEditor();
        memoryEditorOam.Data = new byte[dmg.mmu.oam.Length];
        memoryEditorOam.base_display_addr = 0xFE00;

        memoryEditorHram = new MemoryEditor();
        memoryEditorHram.Data = new byte[dmg.mmu.hram.Length];
        memoryEditorHram.base_display_addr = 0xFF80;

        memoryEditorIo = new MemoryEditor();
        memoryEditorIo.Data = new byte[dmg.mmu.io.Length];
        memoryEditorIo.base_display_addr = 0xFF00;
        view = new VRAMViewer();

        windows = new Windows();

        dmgShow = true;
        configShow = true;
        romInfoShow = true;
        memoryShow = true;
        pauseShow = true;
        vramViewShow = true;
        regInfoShow = true;
    }

    public void Run(ref bool mode, ref bool insertingRom, ref bool pause) {
        if (ImGui.BeginMainMenuBar()) {
            ImGui.Text("CODE-DMG - Dear ImGui Version");

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
                if (ImGui.MenuItem("Game")) {
                    mode = !mode;
                    Helper.fpsEnable = false;
                    Helper.scale = 2;

                    Raylib.SetWindowSize(160 * Helper.scale, (144 * Helper.scale)+19);

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
                if (ImGui.MenuItem("DMG", null, dmgShow)) {
                    dmgShow = !dmgShow;
                }
                if (ImGui.MenuItem("ROM Info", null, romInfoShow)) {
                    romInfoShow = !romInfoShow;
                }
                if (ImGui.MenuItem("Configuration", null, configShow)) {
                    configShow = !configShow;
                }
                if (ImGui.MenuItem("Memory Viewer", null, memoryShow)) {
                    memoryShow = !memoryShow;
                }
                if (ImGui.MenuItem("VRAM Viewer", null, vramViewShow)) {
                    vramViewShow = !vramViewShow;
                }
                if (ImGui.MenuItem("Registers State", null, regInfoShow)) {
                    regInfoShow = !regInfoShow;
                }
                if (ImGui.MenuItem("Pause Window", null, pauseShow)) {
                    pauseShow = !pauseShow;
                }
                ImGui.EndMenu();
            }

            ImGui.EndMainMenuBar();
        }


        if (dmgShow) {
            ImGui.SetNextWindowPos(new System.Numerics.Vector2(15, 50), ImGuiCond.Appearing);
            windows.DMGWindow(ref dmg);
        }
        if (romInfoShow) {
            ImGui.SetNextWindowPos(new System.Numerics.Vector2(15, 670), ImGuiCond.Appearing);
            ImGui.SetNextWindowSize(new System.Numerics.Vector2(310, 110), ImGuiCond.Appearing);
            windows.RomInfo(ref dmg);
        }
        if (configShow) {
            ImGui.SetNextWindowPos(new System.Numerics.Vector2(860, 50), ImGuiCond.Appearing);
            ImGui.SetNextWindowSize(new System.Numerics.Vector2(390, 190), ImGuiCond.Appearing);
            windows.ConfigWindow();
        }

        ImGui.SetNextWindowPos(new System.Numerics.Vector2((Raylib.GetScreenWidth()-580)/2, (Raylib.GetScreenHeight()-310)/2), ImGuiCond.Appearing);
        ImGui.SetNextWindowSize(new System.Numerics.Vector2(580, 310), ImGuiCond.Appearing);
        if (fileDialog.Show(ref selectedFilePath)) {
            Helper.rom = selectedFilePath;
            insertingRom = true;
        }
        ImGui.SetNextWindowPos(new System.Numerics.Vector2((Raylib.GetScreenWidth()-580)/2, (Raylib.GetScreenHeight()-310)/2), ImGuiCond.Appearing);
        ImGui.SetNextWindowSize(new System.Numerics.Vector2(580, 310), ImGuiCond.Appearing);
        if (fileDialogBootRom.Show(ref selectedFilePathBootRom)) {
            Helper.bootrom = selectedFilePathBootRom;
            insertingRom = true;
        }

        ImGui.SetNextWindowPos(new System.Numerics.Vector2(680, 470), ImGuiCond.Appearing);
        ImGui.SetNextWindowSize(new System.Numerics.Vector2(580, 310), ImGuiCond.Appearing);
        if (memoryShow) {
            if (ImGui.Begin("Memory")) {
                if (ImGui.BeginTabBar("Tabs")) {
                    if (ImGui.BeginTabItem("ROM Memory")) {
                        memoryEditorRom.Data = dmg.mmu.rom;
                        memoryEditorRom.RenderContent();
                        ImGui.EndTabItem();
                    }

                    if (ImGui.BeginTabItem("VRAM Memory")) {
                        memoryEditorVram.Data = dmg.mmu.vram;
                        memoryEditorVram.RenderContent();
                        ImGui.EndTabItem();
                    }

                    if (ImGui.BeginTabItem("WRAM Memory")) {
                        memoryEditorWram.Data = dmg.mmu.wram;
                        memoryEditorWram.RenderContent();
                        ImGui.EndTabItem();
                    }

                    if (ImGui.BeginTabItem("OAM Memory")) {
                        memoryEditorOam.Data = dmg.mmu.oam;
                        memoryEditorOam.RenderContent();
                        ImGui.EndTabItem();
                    }

                    if (ImGui.BeginTabItem("IO Memory")) {
                        memoryEditorIo.Data = dmg.mmu.io;
                        memoryEditorIo.RenderContent();
                        ImGui.EndTabItem();
                    }

                    if (ImGui.BeginTabItem("HRAM Memory")) {
                        memoryEditorHram.Data = dmg.mmu.hram;
                        memoryEditorHram.RenderContent();
                        ImGui.EndTabItem();
                    }

                    ImGui.EndTabBar();
                }

                ImGui.End();
            }
        }

        if (regInfoShow) {
            ImGui.SetNextWindowPos(new System.Numerics.Vector2(530, 470), ImGuiCond.Appearing);
            ImGui.SetNextWindowSize(new System.Numerics.Vector2(130, 305), ImGuiCond.Appearing);
            windows.RegisterInfoWindow(ref dmg);
        }

        if (pauseShow) {
            ImGui.SetNextWindowPos(new System.Numerics.Vector2(355, 680), ImGuiCond.Appearing);
            ImGui.SetNextWindowSize(new System.Numerics.Vector2(150, 78), ImGuiCond.Appearing);
            windows.PauseWindow(ref pause);
        }

        if (vramViewShow) {
            ImGui.SetNextWindowPos(new System.Numerics.Vector2(525, 50), ImGuiCond.Appearing);
            ImGui.SetNextWindowSize(new System.Numerics.Vector2(320, 380), ImGuiCond.Appearing);
            view.Render(ref dmg);
        }

        if (Helper.fpsEnable) Raylib.DrawFPS(5, 20);
    }
}