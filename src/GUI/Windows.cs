using Raylib_cs;
using rlImGui_cs;
using ImGuiNET;

class Windows {
    public void DMGWindow(ref DMG dmg) {
        if (ImGui.Begin("DMG")) {
            ImGui.SetWindowSize(new System.Numerics.Vector2(160 * Helper.scale, 144 * Helper.scale) + new System.Numerics.Vector2(ImGui.GetStyle().WindowPadding.X * 2, ImGui.GetStyle().WindowPadding.Y * 2) + new System.Numerics.Vector2(0, ImGui.GetFrameHeight()));
            rlImGui.ImageSize(dmg.screenTexture, new System.Numerics.Vector2(160*Helper.scale, 144*Helper.scale));
        }
        ImGui.End();
    }

    public void ConfigWindow() {
        if (ImGui.Begin("Configuration")) {
            ImGui.SliderInt("Scale", ref Helper.scale, 1, 10);

            ImGui.Spacing(); ImGui.Spacing(); ImGui.Spacing(); ImGui.Spacing();

            string[] paletteNames = Helper.palettes.Keys.ToArray();
            int currentIndex = Array.IndexOf(paletteNames, Helper.paletteName);

            if (ImGui.Combo("Select Palette", ref currentIndex, paletteNames, paletteNames.Length)){
                Helper.paletteName = paletteNames[currentIndex];
            }

            Color[] selectedPalette = Helper.palettes[Helper.paletteName];

            System.Numerics.Vector4 selectedColor = new System.Numerics.Vector4(selectedPalette[0].R / 255f, selectedPalette[0].G / 255f, selectedPalette[0].B / 255f, selectedPalette[0].A / 255f);
            if (ImGui.ColorEdit4("Index 0 Color", ref selectedColor)) {
                selectedPalette[0] = new Color((byte)(selectedColor.X * 255), (byte)(selectedColor.Y * 255), (byte)(selectedColor.Z * 255), (byte)(selectedColor.W * 255));
            }
            selectedColor = new System.Numerics.Vector4(selectedPalette[1].R / 255f, selectedPalette[1].G / 255f, selectedPalette[1].B / 255f, selectedPalette[1].A / 255f);
            if (ImGui.ColorEdit4("Index 1 Color", ref selectedColor)) {
                selectedPalette[1] = new Color((byte)(selectedColor.X * 255), (byte)(selectedColor.Y * 255), (byte)(selectedColor.Z * 255), (byte)(selectedColor.W * 255));
            }
            selectedColor = new System.Numerics.Vector4(selectedPalette[2].R / 255f, selectedPalette[2].G / 255f, selectedPalette[2].B / 255f, selectedPalette[2].A / 255f);
            if (ImGui.ColorEdit4("Index 2 Color", ref selectedColor)) {
                selectedPalette[2] = new Color((byte)(selectedColor.X * 255), (byte)(selectedColor.Y * 255), (byte)(selectedColor.Z * 255), (byte)(selectedColor.W * 255));
            }
            selectedColor = new System.Numerics.Vector4(selectedPalette[3].R / 255f, selectedPalette[3].G / 255f, selectedPalette[3].B / 255f, selectedPalette[3].A / 255f);
            if (ImGui.ColorEdit4("Index 3 Color", ref selectedColor)) {
                selectedPalette[3] = new Color((byte)(selectedColor.X * 255), (byte)(selectedColor.Y * 255), (byte)(selectedColor.Z * 255), (byte)(selectedColor.W * 255));
            }
        }  
        ImGui.End();    
    }

    public void RomInfo(ref DMG dmg) {
        if (ImGui.Begin("ROM Infomation")) {
            ImGui.TextUnformatted(dmg.mmu.HeaderInfo());
        }
        ImGui.End();
    }

    public void ConfigRegWindow(ref bool configRegShow) {
        if (ImGui.Begin("Options", ImGuiWindowFlags.HorizontalScrollbar)) {
            ImGui.SliderInt("Scale", ref Helper.scale, 1, 10);

            ImGui.Spacing(); ImGui.Spacing(); ImGui.Spacing(); ImGui.Spacing();

            string[] paletteNames = Helper.palettes.Keys.ToArray();
            int currentIndex = Array.IndexOf(paletteNames, Helper.paletteName);

            if (ImGui.Combo("Select Palette", ref currentIndex, paletteNames, paletteNames.Length)){
                Helper.paletteName = paletteNames[currentIndex];
            }

            if (ImGui.Button("Cancel")) {
                configRegShow = false;
            }
        }
        ImGui.End();
    }

    public void RomInfoRegWindow(ref DMG dmg, ref bool romInfoRegShow) {
        if (ImGui.Begin("About ROM", ImGuiWindowFlags.HorizontalScrollbar)) {
            ImGui.TextUnformatted(dmg.mmu.HeaderInfo());
        }
        if (ImGui.Button("Cancel")) {
            romInfoRegShow = false;
        }
        ImGui.End();
    }

    public void RegisterInfoWindow(ref DMG dmg) {
        if (ImGui.Begin("CPU Registers")) {
            ImGui.PushStyleColor(ImGuiCol.Text, new System.Numerics.Vector4(1.0f, 1.0f, 1.0f, 1f));  
            ImGui.Text("A: ");
            ImGui.SameLine();
            ImGui.PushStyleColor(ImGuiCol.Text, new System.Numerics.Vector4(0.0f, 1.0f, 0.0f, 1f));  
            ImGui.Text(dmg.cpu.A.ToString("X2"));
            ImGui.PopStyleColor();
            
            ImGui.PushStyleColor(ImGuiCol.Text, new System.Numerics.Vector4(1.0f, 1.0f, 1.0f, 1f));  
            ImGui.Text("B: ");
            ImGui.SameLine();
            ImGui.PushStyleColor(ImGuiCol.Text, new System.Numerics.Vector4(0.0f, 1.0f, 0.0f, 1f));  
            ImGui.Text(dmg.cpu.B.ToString("X2"));
            ImGui.PopStyleColor();

            ImGui.PushStyleColor(ImGuiCol.Text, new System.Numerics.Vector4(1.0f, 1.0f, 1.0f, 1f));  
            ImGui.Text("C: ");
            ImGui.SameLine();
            ImGui.PushStyleColor(ImGuiCol.Text, new System.Numerics.Vector4(0.0f, 1.0f, 0.0f, 1f));  
            ImGui.Text(dmg.cpu.C.ToString("X2"));
            ImGui.PopStyleColor();

            ImGui.PushStyleColor(ImGuiCol.Text, new System.Numerics.Vector4(1.0f, 1.0f, 1.0f, 1f));
            ImGui.Text("D: ");
            ImGui.SameLine();
            ImGui.PushStyleColor(ImGuiCol.Text, new System.Numerics.Vector4(0.0f, 1.0f, 0.0f, 1f));  
            ImGui.Text(dmg.cpu.D.ToString("X2"));
            ImGui.PopStyleColor();

            ImGui.PushStyleColor(ImGuiCol.Text, new System.Numerics.Vector4(1.0f, 1.0f, 1.0f, 1f));  
            ImGui.Text("E: ");
            ImGui.SameLine();
            ImGui.PushStyleColor(ImGuiCol.Text, new System.Numerics.Vector4(0.0f, 1.0f, 0.0f, 1f));  
            ImGui.Text(dmg.cpu.E.ToString("X2"));
            ImGui.PopStyleColor();

            ImGui.PushStyleColor(ImGuiCol.Text, new System.Numerics.Vector4(1.0f, 1.0f, 1.0f, 1f));  
            ImGui.Text("F: ");
            ImGui.SameLine();
            ImGui.PushStyleColor(ImGuiCol.Text, new System.Numerics.Vector4(0.0f, 1.0f, 0.0f, 1f));  
            ImGui.Text(dmg.cpu.F.ToString("X2"));
            ImGui.PopStyleColor();

            ImGui.Spacing(); 
            ImGui.Spacing();

            ImGui.PushStyleColor(ImGuiCol.Text, new System.Numerics.Vector4(1.0f, 1.0f, 1.0f, 1f));  
            ImGui.Text("Zero: ");
            ImGui.SameLine();
            ImGui.PushStyleColor(ImGuiCol.Text, new System.Numerics.Vector4(0.0f, 1.0f, 0.0f, 1f));  
            ImGui.Text(dmg.cpu.zero ? "1" : "0");
            ImGui.PopStyleColor();
            
            ImGui.PushStyleColor(ImGuiCol.Text, new System.Numerics.Vector4(1.0f, 1.0f, 1.0f, 1f));  
            ImGui.Text("Negative: ");
            ImGui.SameLine();
            ImGui.PushStyleColor(ImGuiCol.Text, new System.Numerics.Vector4(0.0f, 1.0f, 0.0f, 1f));  
            ImGui.Text(dmg.cpu.negative ? "1" : "0");
            ImGui.PopStyleColor();

            ImGui.PushStyleColor(ImGuiCol.Text, new System.Numerics.Vector4(1.0f, 1.0f, 1.0f, 1f));  
            ImGui.Text("HalfCarry: ");
            ImGui.SameLine();
            ImGui.PushStyleColor(ImGuiCol.Text, new System.Numerics.Vector4(0.0f, 1.0f, 0.0f, 1f));  
            ImGui.Text(dmg.cpu.halfCarry ? "1" : "0");
            ImGui.PopStyleColor();

            ImGui.PushStyleColor(ImGuiCol.Text, new System.Numerics.Vector4(1.0f, 1.0f, 1.0f, 1f));  
            ImGui.Text("Carry: ");
            ImGui.SameLine();
            ImGui.PushStyleColor(ImGuiCol.Text, new System.Numerics.Vector4(0.0f, 1.0f, 0.0f, 1f));  
            ImGui.Text(dmg.cpu.carry ? "1" : "0");
            ImGui.PopStyleColor();

            ImGui.PushStyleColor(ImGuiCol.Text, new System.Numerics.Vector4(1.0f, 1.0f, 1.0f, 1f));  
            ImGui.Text("IME: ");
            ImGui.SameLine();
            ImGui.PushStyleColor(ImGuiCol.Text, new System.Numerics.Vector4(0.0f, 1.0f, 0.0f, 1f));  
            ImGui.Text(dmg.cpu.IME ? "Enabled" : "Disabled");
            ImGui.PopStyleColor();

            ImGui.PushStyleColor(ImGuiCol.Text, new System.Numerics.Vector4(1.0f, 1.0f, 1.0f, 1f));  
            ImGui.Text("Halted: ");
            ImGui.SameLine();
            ImGui.PushStyleColor(ImGuiCol.Text, new System.Numerics.Vector4(0.0f, 1.0f, 0.0f, 1f));  
            ImGui.Text(dmg.cpu.halted ? "Yes" : "No");
            ImGui.PopStyleColor();

            ImGui.PushStyleColor(ImGuiCol.Text, new System.Numerics.Vector4(1.0f, 1.0f, 1.0f, 1f));  
            ImGui.Text("PC: ");
            ImGui.SameLine();
            ImGui.PushStyleColor(ImGuiCol.Text, new System.Numerics.Vector4(0.0f, 1.0f, 0.0f, 1f));  
            ImGui.Text(dmg.cpu.PC.ToString("X4"));
            ImGui.PopStyleColor();

            ImGui.PushStyleColor(ImGuiCol.Text, new System.Numerics.Vector4(1.0f, 1.0f, 1.0f, 1f));  
            ImGui.Text("SP: ");
            ImGui.SameLine();
            ImGui.PushStyleColor(ImGuiCol.Text, new System.Numerics.Vector4(0.0f, 1.0f, 0.0f, 1f));  
            ImGui.Text(dmg.cpu.SP.ToString("X4"));
            ImGui.PopStyleColor();

            ImGui.End();
        }
    }

    public void PauseWindow(ref bool pause) {
        ImGui.Begin("Excution Flow");
        ImGui.TextUnformatted("Currently: " + (pause ? IconFonts.FontAwesome6.CirclePause : IconFonts.FontAwesome6.CirclePlay));
        if (ImGui.Button(pause ? "Resume" : "Pause"))
        {
            pause = !pause;
        }
        ImGui.End();
    }
}