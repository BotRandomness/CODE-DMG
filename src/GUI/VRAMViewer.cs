using Raylib_cs;
using rlImGui_cs;
using ImGuiNET;
using System.Numerics;

class VRAMViewer {
    const int TileWidth = 8;
    const int TileHeight = 8;
    const int MapWidth = 32;
    const int MapHeight = 32;
    const int TilesPerRow = 16;
    const int TileDataLength = 16;
    const int TotalDMGTiles = 384;
    const int VRAMSize = 8192;

    public int scale = 1;

    public byte[] vram = new byte[VRAMSize];

    const int spritesPerRow = 8;
    const int spriteSpacing = 16;
    public int spriteSize;
    const int OAMSize = 160;

    public byte[] oam = new byte[OAMSize];

    public bool use8800Mode = false;

    public Color[] colorPalette = new Color[4] { Color.Black, Color.DarkGray, Color.LightGray, Color.White };
    public Color[] backgroundPalette = new Color[4];
    public Color[] objectPalette0 = new Color[4];
    public Color[] objectPalette1 = new Color[4];
    
    public bool useobp1 = false;

    public Image vramTileImage;
    public Texture2D vramTileTexture;
    public Image vramTileMapImage;
    public Texture2D vramTileMapTexture;
    public Image spriteTileImage;
    public Texture2D spriteTileTexture;

    public VRAMViewer() {
        vramTileImage = Raylib.GenImageColor(128, 192, Color.Black);
        vramTileTexture = Raylib.LoadTextureFromImage(vramTileImage);
        vramTileMapImage = Raylib.GenImageColor(256, 256, Color.Black);
        vramTileMapTexture = Raylib.LoadTextureFromImage(vramTileMapImage);
        spriteTileImage = Raylib.GenImageColor(256, 256, Color.White);
        spriteTileTexture = Raylib.LoadTextureFromImage(spriteTileImage);

        spriteSize = 8 * scale;
    }

    public void Render(ref DMG dmg) {
        vram = dmg.mmu.vram;
        oam = dmg.mmu.oam;
        SetPalette(dmg.mmu.BGP, dmg.mmu.OBP0, dmg.mmu.OBP1);

        colorPalette = Helper.palettes[Helper.paletteName];

        if (ImGui.Begin("VRAM", ImGuiWindowFlags.HorizontalScrollbar)) {
            if (ImGui.BeginTabBar("TabsVRAM")) {
                if (ImGui.BeginTabItem("Tiles")) {
                    DrawTilesToImage(vramTileImage);
                    unsafe {
                        Raylib.UpdateTexture(vramTileTexture, vramTileImage.Data);
                    }
                    rlImGui.ImageSize(vramTileTexture, new System.Numerics.Vector2(128 * scale, 192 * scale));

                    ImGui.Spacing(); ImGui.Spacing(); ImGui.Spacing(); ImGui.Spacing(); ImGui.Spacing(); ImGui.Spacing();

                    ImGui.SliderInt("Scale", ref scale, 1, 10);
                    ImGui.EndTabItem();
                }

                if (ImGui.BeginTabItem("0x9800")) {
                    DrawTileMapToImage(vramTileMapImage, 0x9800);
                    unsafe {
                        Raylib.UpdateTexture(vramTileMapTexture, vramTileMapImage.Data);
                    }
                    rlImGui.ImageSize(vramTileMapTexture, new System.Numerics.Vector2(256 * scale, 256 * scale));

                    ImGui.TextUnformatted("Map showing: " + (use8800Mode ? "0x8800" : "0x8000"));
                    if (ImGui.Button(use8800Mode ? "Show 0x8000" : "Show 0x8800")) {
                        use8800Mode = !use8800Mode;
                    }

                    ImGui.SliderInt("Scale", ref scale, 1, 10);

                    ImGui.EndTabItem();
                }

                if (ImGui.BeginTabItem("0x9C00")) {
                    DrawTileMapToImage(vramTileMapImage, 0x9C00);
                    unsafe {
                        Raylib.UpdateTexture(vramTileMapTexture, vramTileMapImage.Data);
                    }
                    rlImGui.ImageSize(vramTileMapTexture, new System.Numerics.Vector2(256 * scale, 256 * scale));

                    ImGui.TextUnformatted("Map showing: " + (use8800Mode ? "0x8800" : "0x8000"));
                    if (ImGui.Button(use8800Mode ? "Show 0x8000" : "Show 0x8800")) {
                        use8800Mode = !use8800Mode;
                    }

                    ImGui.SliderInt("Scale", ref scale, 1, 10);

                    ImGui.EndTabItem();
                }

                if (ImGui.BeginTabItem("Sprites")) {
                    DrawOAMSpritesToImage(spriteTileImage);
                    unsafe {
                        Raylib.UpdateTexture(spriteTileTexture, spriteTileImage.Data);
                    }
                    rlImGui.ImageSize(spriteTileTexture, new System.Numerics.Vector2(256 * scale, 256 * scale));

                    ImGui.TextUnformatted("Sprite palette showing: " + (useobp1 ? "OBP1" : "OBP0"));
                    if (ImGui.Button(useobp1 ? "Show OBP0" : "Show OBP1")) {
                        useobp1 = !useobp1;
                    }
                    
                    ImGui.SliderInt("Scale", ref scale, 1, 10);

                    ImGui.EndTabItem();
                }
                if (ImGui.BeginTabItem("Palette")) {
                    ImGui.Text("Background Palette (BGP):");
                    for (int i = 0; i < 4; i++) {
                        Vector4 colorVec = ConvertToVector4(backgroundPalette[i]);

                        if (ImGui.ColorButton($"##bgColor{i}", colorVec)) {

                        }
                        ImGui.SameLine();
                    }
                    ImGui.NewLine();

                    ImGui.Text("Object Palette 0 (OBP0):");
                    for (int i = 0; i < 4; i++) {
                        Vector4 colorVec = ConvertToVector4(objectPalette0[i]);

                        if (ImGui.ColorButton($"##obp0Color{i}", colorVec)) {
                            Console.WriteLine($"Object Palette 0 Color {i} clicked!");
                        }
                        ImGui.SameLine();
                    }
                    ImGui.NewLine();

                    ImGui.Text("Object Palette 1 (OBP1):");
                    for (int i = 0; i < 4; i++) {
                        Vector4 colorVec = ConvertToVector4(objectPalette1[i]);

                        if (ImGui.ColorButton($"##obp1Color{i}", colorVec)) {

                        }
                        ImGui.SameLine();
                    }

                    ImGui.EndTabItem();
                }
                ImGui.EndTabBar();
            }
            ImGui.End();
        }

    }

    public Vector4 ConvertToVector4(Color color) {
        return new Vector4(color.R / 255f, color.G / 255f, color.B / 255f, color.A / 255f);
    }

    public void SetPalette(byte BGP, byte OBP0, byte OBP1) {
        //BGP - Background Palette
        for (int i = 0; i < 4; i++) {
            int colorIndex = (BGP >> (i * 2)) & 0x03;
            backgroundPalette[i] = colorPalette[colorIndex];
        }

        //OBP0 - Object Palette 0
        for (int i = 0; i < 4; i++) {
            int colorIndex = (OBP0 >> (i * 2)) & 0x03;
            objectPalette0[i] = colorPalette[colorIndex];
        }

        //OBP1 - Object Palette 1
        for (int i = 0; i < 4; i++) {
            int colorIndex = (OBP1 >> (i * 2)) & 0x03;
            objectPalette1[i] = colorPalette[colorIndex];
        }
    }

    public void DrawTileToImage(Image image, int tileIndex, int x, int y) {
        int tileStart = tileIndex * TileDataLength;

        for (int row = 0; row < TileHeight; row++) {
            byte byte1 = vram[tileStart + row * 2];
            byte byte2 = vram[tileStart + row * 2 + 1];

            for (int col = 0; col < TileWidth; col++) {
                int colorIndex = ((byte1 >> (7 - col)) & 1) | (((byte2 >> (7 - col)) & 1) << 1);
                Color pixelColor = backgroundPalette[colorIndex];

                Raylib.ImageDrawPixel(ref image, x + col, y + row, pixelColor);
            }
        }
    }

    public void DrawTilesToImage(Image image) {
        for (int tileIndex = 0; tileIndex < TotalDMGTiles; tileIndex++) {
            int tileX = tileIndex % TilesPerRow * TileWidth;
            int tileY = tileIndex / TilesPerRow * TileHeight;

            DrawTileToImage(image, tileIndex, tileX, tileY);
        }
    }

    public void DrawTileMapToImage(Image image, int tileMapAddress) {
        for (int row = 0; row < MapHeight; row++) {
            for (int col = 0; col < MapWidth; col++) {
                int mapIndex = tileMapAddress - 0x8000 + row * MapWidth + col;
                int tileIndex = vram[mapIndex];

                if (use8800Mode) {
                    if (tileIndex < 128) {
                        tileIndex += 256;
                    } else {
                        //tileIndex -= 128; 
                    }
                }

                int tileX = col * TileWidth;
                int tileY = row * TileHeight;

                DrawTileToImage(image, tileIndex, tileX, tileY);
            }
        }
    }

    public void DrawSpriteTileToImage(Image image, int tileIndex, int x, int y, bool flipX = false, bool flipY = false) {
        int tileStart = tileIndex * TileDataLength;

        for (int row = 0; row < TileHeight; row++) {
            int adjustedRow = flipY ? (TileHeight - 1 - row) : row;
            byte byte1 = vram[tileStart + adjustedRow * 2];
            byte byte2 = vram[tileStart + adjustedRow * 2 + 1];

            for (int col = 0; col < TileWidth; col++) {
                int adjustedCol = flipX ? (TileWidth - 1 - col) : col;
                int colorIndex = ((byte1 >> (7 - col)) & 1) | (((byte2 >> (7 - col)) & 1) << 1);
                Color pixelColor = objectPalette0[colorIndex];
                if (useobp1) {
                    pixelColor = objectPalette1[colorIndex];
                }

                Raylib.ImageDrawPixel(ref image, x + adjustedCol, y + row, pixelColor);
            }
        }
    }

    public void DrawOAMSpritesToImage(Image image) {
        for (int i = 0; i < 40; i++) {
            int row = i / spritesPerRow;
            int col = i % spritesPerRow;

            int xPos = col * (spriteSize + spriteSpacing);
            int yPos = row * (spriteSize + spriteSpacing);

            int tileIndex = oam[i * 4 + 2];
            byte attributes = oam[i * 4 + 3];

            bool flipX = (attributes & 0x20) != 0;
            bool flipY = (attributes & 0x40) != 0;

            DrawSpriteTileToImage(image, tileIndex, xPos, yPos, flipX, flipY);
        }
    }
}