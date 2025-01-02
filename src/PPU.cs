using Raylib_cs;

public class PPU {
    private const int HBLANK = 0;
    private const int VBLANK = 1;
    private const int OAM = 2;
    private const int VRAM = 3;

    private const int SCANLINE_CYCLES = 456;

    private int mode;
    private int cycles;

    private MMU mmu;

    private Color[] frameBuffer;
    private Color[] scanlineBuffer = new Color[ScreenWidth];
    private const int ScreenWidth = 160;
    private const int ScreenHeight = 144;

    Image screenImage;
    Texture2D screenTexture;

    private bool vblankTriggered = false;
    private int windowLineCounter = 0; 
    private bool lcdPreviouslyOff = false;

    public PPU(MMU mmu, Image screenImage, Texture2D screenTexture) {
        this.mmu = mmu;
        mode = OAM;
        cycles = 0;
        frameBuffer = new Color[ScreenWidth * ScreenHeight];
        this.screenImage = screenImage;
        this.screenTexture = screenTexture;

        Console.WriteLine("PPU init");
    }

    public void Step(int elapsedCycles) {
        cycles += elapsedCycles;

        if ((mmu.LCDC & 0x80) != 0 && lcdPreviouslyOff) {
            //mode = OAM;
            cycles = 0;
            lcdPreviouslyOff = false;
        } else if ((mmu.LCDC & 0x80) == 0) {
            lcdPreviouslyOff = true;
            //mmu.LY = 0;
            //mmu.STAT &= 0xFB;
            mode = HBLANK;
            return;
        }

        switch (mode) {
            case OAM:
                if (cycles >= 80) {
                    cycles -= 80;
                    mode = VRAM;

                    mmu.STAT = (byte)((mmu.STAT & 0xFC) | mode);
                }
                break;

            case VRAM:
                if (cycles >= 172) {
                    cycles -= 172;
                    mode = HBLANK;

                    mmu.STAT = (byte)((mmu.STAT & 0xFC) | mode);

                    if (mmu.LY < 144) {
                        RenderScanline();
                    }

                    if ((mmu.STAT & 0x08) != 0) {
                        mmu.IF = (byte)(mmu.IF | 0x02);
                    }
                }
                break;

            case HBLANK:
                if (cycles >= 204) {
                    cycles -= 204;
                    mmu.LY++;

                    setLYCFlag();

                    if (mmu.LY == 144) {
                        mode = VBLANK;
                        vblankTriggered = false;
                        DrawFrame(ref screenImage);
                        if ((mmu.STAT & 0x10) != 0) {
                            mmu.IF = (byte)(mmu.IF | 0x02);
                        }
                    } else {
                        if ((mmu.STAT & 0x20) != 0) {
                            mmu.IF = (byte)(mmu.IF | 0x02);
                        }
                        mode = OAM;
                    }
                    mmu.STAT = (byte)((mmu.STAT & 0xFC) | mode);
                }
                break;

            case VBLANK:
                if (!vblankTriggered && mmu.LY == 144)
                {
                    if ((mmu.LCDC & 0x80) != 0) {
                        mmu.IF = (byte)(mmu.IF | 0x01);
                        vblankTriggered = true; 
                    }
                }

                if (cycles >= SCANLINE_CYCLES) {
                    cycles -= SCANLINE_CYCLES;
                    mmu.LY++;

                    setLYCFlag();

                    if (mmu.LY == 153) {
                        mmu.LY = 0;  
                        mode = OAM;
                        vblankTriggered = false;

                        mmu.STAT = (byte)((mmu.STAT & 0xFC) | mode);

                        if ((mmu.STAT & 0x20) != 0) {
                            mmu.IF = (byte)(mmu.IF | 0x02);
                        }
                    }
                }
                break;
        }
    }

    private void RenderScanline() {
        RenderBackground();
        
        RenderWindow();

        RenderSprites();

        Array.Copy(scanlineBuffer, 0, frameBuffer, mmu.LY * ScreenWidth, ScreenWidth);
    }

    private void RenderBackground() {
        int currentScanline = mmu.LY;
        int scrollX = mmu.SCX;
        int scrollY = mmu.SCY;

        if ((mmu.LCDC & 0x01) == 0) return;

        for (int x = 0; x < ScreenWidth; x++) {
            int bgX = (scrollX + x) % 256;
            int bgY = (scrollY + currentScanline) % 256;

            int tileX = bgX / 8;
            int tileY = bgY / 8;

            int tileIndex = tileY * 32 + tileX;

            ushort tileMapBase = (mmu.LCDC & 0x08) != 0 ? (ushort)0x9C00 : (ushort)0x9800;
            byte tileNumber = mmu.Read((ushort)(tileMapBase + tileIndex));

            ushort tileDataBase = (mmu.LCDC & 0x10) != 0 || tileNumber >= 128 ? (ushort)0x8000 : (ushort)0x9000;
            ushort tileAddress = (ushort)(tileDataBase + tileNumber * 16);

            int lineInTile = bgY % 8;

            byte tileLow = mmu.Read((ushort)(tileAddress + lineInTile * 2));
            byte tileHigh = mmu.Read((ushort)(tileAddress + lineInTile * 2 + 1));

            int bitIndex = 7 - (bgX % 8);
            int colorBit = ((tileHigh >> bitIndex) & 0b1) << 1 | ((tileLow >> bitIndex) & 0b1);

            byte bgp = mmu.BGP;
            int paletteShift = colorBit * 2;
            int paletteColor = (bgp >> paletteShift) & 0b11;

            scanlineBuffer[x] = ConvertPaletteColor(paletteColor);
        }
    }

    
    private void RenderWindow() 
    {
        if ((mmu.LCDC & (1 << 5)) == 0) return;

        int currentScanline = mmu.LY;
        int windowX = mmu.WX - 7;
        int windowY = mmu.WY;

        if (currentScanline < windowY) return;

        if (currentScanline == windowY) {
            windowLineCounter = 0;
        }

        ushort tileMapBase = (mmu.LCDC & (1 << 6)) != 0 ? (ushort)0x9C00 : (ushort)0x9800;

        bool windowRendered = false;

        for (int x = 0; x < ScreenWidth; x++) 
        {
            if (x < windowX) continue;

            windowRendered = true;

            int windowColumn = x - windowX;

            int tileX = windowColumn / 8;
            int tileY = windowLineCounter / 8;

            int tileIndex = tileY * 32 + tileX;

            byte tileNumber = mmu.Read((ushort)(tileMapBase + tileIndex));

            ushort tileDataBase = (mmu.LCDC & (1 << 4)) != 0 || tileNumber >= 128 ? (ushort)0x8000 : (ushort)0x9000;
            ushort tileAddress = (ushort)(tileDataBase + tileNumber * 16);

            int lineInTile = windowLineCounter % 8;

            byte tileLow = mmu.Read((ushort)(tileAddress + lineInTile * 2));
            byte tileHigh = mmu.Read((ushort)(tileAddress + lineInTile * 2 + 1));

            int bitIndex = 7 - (windowColumn % 8);
            int colorBit = ((tileHigh >> bitIndex) & 1) << 1 | ((tileLow >> bitIndex) & 1);

            byte bgp = mmu.BGP;
            int paletteShift = colorBit * 2;
            int paletteColor = (bgp >> paletteShift) & 0b11;
            //return ConvertPaletteColor(paletteColor);

            scanlineBuffer[x] = ConvertPaletteColor(paletteColor);
        }

        if (windowRendered) {
            windowLineCounter++;
        }
    }

    private void RenderSprites() {
        int currentScanline = mmu.LY;
        if ((mmu.LCDC & (1 << 1)) == 0) return;

        int renderedSprites = 0;
        int[] pixelOwner = new int[ScreenWidth];
        Array.Fill(pixelOwner, -1);

        for (int i = 0; i < 40; i++) {
            if (renderedSprites >= 10) break;

            int spriteIndex = i * 4;
            int yPos = mmu.Read((ushort)(0xFE00 + spriteIndex)) - 16;
            int xPos = mmu.Read((ushort)(0xFE00 + spriteIndex + 1)) - 8;
            byte tileIndex = mmu.Read((ushort)(0xFE00 + spriteIndex + 2));
            byte attributes = mmu.Read((ushort)(0xFE00 + spriteIndex + 3));

            int spriteHeight = (mmu.LCDC & (1 << 2)) != 0 ? 16 : 8;
            if (currentScanline < yPos || currentScanline >= yPos + spriteHeight) {
                continue;
            }

            int lineInSprite = currentScanline - yPos;
            if ((attributes & (1 << 6)) != 0) {
                lineInSprite = spriteHeight - 1 - lineInSprite;
            }

            if (spriteHeight == 16) {
                tileIndex &= 0xFE;
                if (lineInSprite >= 8) {
                    tileIndex += 1;
                    lineInSprite -= 8;
                }
            }

            ushort tileAddress = (ushort)(0x8000 + tileIndex * 16 + lineInSprite * 2);
            byte tileLow = mmu.Read(tileAddress);
            byte tileHigh = mmu.Read((ushort)(tileAddress + 1));

            for (int x = 0; x < 8; x++) {
                int bitIndex = (attributes & (1 << 5)) != 0 ? x : 7 - x;
                int colorBit = ((tileHigh >> bitIndex) & 1) << 1 | ((tileLow >> bitIndex) & 1);

                if (colorBit == 0) continue;

                int screenX = xPos + x;
                if (screenX < 0 || screenX >= ScreenWidth) continue;

                bool bgOverObj = (attributes & (1 << 7)) != 0;
                if (bgOverObj && !scanlineBuffer[screenX].Equals(ConvertPaletteColor(0))) {
                    continue;
                }

                if (pixelOwner[screenX] == -1 || xPos < pixelOwner[screenX]) {
                    pixelOwner[screenX] = xPos;
                    bool isSpritePalette1 = (attributes & (1 << 4)) != 0;

                    byte spritePalette = isSpritePalette1 ? mmu.OBP1 : mmu.OBP0;
                    int paletteShift = colorBit * 2;
                    int paletteColor = (spritePalette >> paletteShift) & 0b11;

                    scanlineBuffer[screenX] = ConvertPaletteColor(paletteColor);
                }
            }
            renderedSprites++;
        }
    }

    private Color ConvertPaletteColor(int paletteColor) {
        return Helper.palettes[Helper.paletteName][paletteColor];
    }

    private void setLYCFlag() {
        if (mmu.LY == mmu.LYC) {
            //Console.WriteLine(mmu.LY);
            //Set the LYC=LY flag
            mmu.STAT = (byte)(mmu.STAT | 0x04);
            if ((mmu.STAT & 0x40) != 0) {//If the LYC=LY interrupt is enabled set the flag in the IF registers
                mmu.IF = (byte)(mmu.IF | 0x02);
            }
        } else {
            mmu.STAT = (byte)(mmu.STAT & 0xFB); //Clear the LYC=LY flag
        }
    }    

    public void DrawFrame(ref Image image) {
        for (int y = 0; y < ScreenHeight; y++) {
            for (int x = 0; x < ScreenWidth; x++) {
                Color color = frameBuffer[y * ScreenWidth + x];
                //Raylib.DrawRectangle(x*2, y*2, 1*2, 1*2, color);
                Raylib.ImageDrawPixel(ref image, x, y, color);
            }
        }

        unsafe {
            Raylib.UpdateTexture(screenTexture, screenImage.Data);
        }
        Raylib.DrawTextureEx(screenTexture, new System.Numerics.Vector2(0, 0), 0.0f, Helper.scale, Color.White);
    }
}