using Raylib_cs;

namespace CODE_DMG;
public class Ppu
{
    private const int Hblank = 0;
    private const int Vblank = 1;
    private const int Oam = 2;
    private const int Vram = 3;

    private const int ScanlineCycles = 456;

    private int mode;
    private int cycles;

    private readonly Mmu mmu;

    private readonly Color[] frameBuffer;
    private readonly Color[] scanlineBuffer = new Color[ScreenWidth];
    private const int ScreenWidth = 160;
    private const int ScreenHeight = 144;

    private Image screenImage;
    private readonly Texture2D screenTexture;

    private bool vblankTriggered;
    private int windowLineCounter;
    private bool lcdPreviouslyOff;

    public Ppu(Mmu mmu, Image screenImage, Texture2D screenTexture)
    {
        this.mmu = mmu;
        mode = Oam;
        cycles = 0;
        frameBuffer = new Color[ScreenWidth * ScreenHeight];
        this.screenImage = screenImage;
        this.screenTexture = screenTexture;

        Console.WriteLine("PPU init");
    }

    public void Step(int elapsedCycles)
    {
        cycles += elapsedCycles;

        if ((mmu.Lcdc & 0x80) != 0 && lcdPreviouslyOff)
        {
            cycles = 0;
            lcdPreviouslyOff = false;
        }
        else if ((mmu.Lcdc & 0x80) == 0)
        {
            lcdPreviouslyOff = true;
            mode = Hblank;
            return;
        }

        switch (mode)
        {
            case Oam:
                if (cycles >= 80)
                {
                    cycles -= 80;
                    mode = Vram;

                    mmu.Stat = (byte)((mmu.Stat & 0xFC) | mode);
                }
                break;

            case Vram:
                if (cycles >= 172)
                {
                    cycles -= 172;
                    mode = Hblank;

                    mmu.Stat = (byte)((mmu.Stat & 0xFC) | mode);

                    if (mmu.Ly < 144)
                    {
                        RenderScanline();
                    }

                    if ((mmu.Stat & 0x08) != 0)
                    {
                        mmu.If = (byte)(mmu.If | 0x02);
                    }
                }
                break;

            case Hblank:
                if (cycles >= 204)
                {
                    cycles -= 204;
                    mmu.Ly++;

                    SetLycFlag();

                    if (mmu.Ly == 144)
                    {
                        mode = Vblank;
                        vblankTriggered = false;
                        DrawFrame(ref screenImage);
                        if ((mmu.Stat & 0x10) != 0)
                        {
                            mmu.If = (byte)(mmu.If | 0x02);
                        }
                    }
                    else
                    {
                        if ((mmu.Stat & 0x20) != 0)
                        {
                            mmu.If = (byte)(mmu.If | 0x02);
                        }
                        mode = Oam;
                    }
                    mmu.Stat = (byte)((mmu.Stat & 0xFC) | mode);
                }
                break;

            case Vblank:
                if (!vblankTriggered && mmu.Ly == 144 && (mmu.Lcdc & 0x80) != 0)
                {
                    mmu.If = (byte)(mmu.If | 0x01);
                    vblankTriggered = true;
                }

                if (cycles >= ScanlineCycles)
                {
                    cycles -= ScanlineCycles;
                    mmu.Ly++;

                    SetLycFlag();

                    if (mmu.Ly == 153)
                    {
                        mmu.Ly = 0;
                        mode = Oam;
                        vblankTriggered = false;

                        mmu.Stat = (byte)((mmu.Stat & 0xFC) | mode);

                        if ((mmu.Stat & 0x20) != 0)
                        {
                            mmu.If = (byte)(mmu.If | 0x02);
                        }
                    }
                }
                break;
        }
    }

    private void RenderScanline()
    {
        RenderBackground();

        RenderWindow();

        RenderSprites();

        Array.Copy(scanlineBuffer, 0, frameBuffer, mmu.Ly * ScreenWidth, ScreenWidth);
    }

    private void RenderBackground()
    {
        var currentScanline = mmu.Ly;
        var scrollX = mmu.Scx;
        var scrollY = mmu.Scy;

        if ((mmu.Lcdc & 0x01) == 0) return;

        for (var x = 0; x < ScreenWidth; x++)
        {
            var bgX = (scrollX + x) % 256;
            var bgY = (scrollY + currentScanline) % 256;

            var tileX = bgX / 8;
            var tileY = bgY / 8;

            var tileIndex = tileY * 32 + tileX;

            var tileMapBase = (mmu.Lcdc & 0x08) != 0 ? (ushort)0x9C00 : (ushort)0x9800;
            var tileNumber = mmu.Read((ushort)(tileMapBase + tileIndex));

            var tileDataBase = (mmu.Lcdc & 0x10) != 0 || tileNumber >= 128 ? (ushort)0x8000 : (ushort)0x9000;
            var tileAddress = (ushort)(tileDataBase + tileNumber * 16);

            var lineInTile = bgY % 8;

            var tileLow = mmu.Read((ushort)(tileAddress + lineInTile * 2));
            var tileHigh = mmu.Read((ushort)(tileAddress + lineInTile * 2 + 1));

            var bitIndex = 7 - (bgX % 8);
            var colorBit = ((tileHigh >> bitIndex) & 0b1) << 1 | ((tileLow >> bitIndex) & 0b1);

            var bgp = mmu.Bgp;
            var paletteShift = colorBit * 2;
            var paletteColor = (bgp >> paletteShift) & 0b11;

            scanlineBuffer[x] = ConvertPaletteColor(paletteColor);
        }
    }


    private void RenderWindow()
    {
        if ((mmu.Lcdc & (1 << 5)) == 0) return;

        var currentScanline = mmu.Ly;
        var windowX = mmu.Wx - 7;
        var windowY = mmu.Wy;

        if (currentScanline < windowY) return;

        if (currentScanline == windowY)
        {
            windowLineCounter = 0;
        }

        var tileMapBase = (mmu.Lcdc & (1 << 6)) != 0 ? (ushort)0x9C00 : (ushort)0x9800;

        var windowRendered = false;

        for (var x = 0; x < ScreenWidth; x++)
        {
            if (x < windowX) continue;

            windowRendered = true;

            var windowColumn = x - windowX;

            var tileX = windowColumn / 8;
            var tileY = windowLineCounter / 8;

            var tileIndex = tileY * 32 + tileX;

            var tileNumber = mmu.Read((ushort)(tileMapBase + tileIndex));

            var tileDataBase = (mmu.Lcdc & (1 << 4)) != 0 || tileNumber >= 128 ? (ushort)0x8000 : (ushort)0x9000;
            var tileAddress = (ushort)(tileDataBase + tileNumber * 16);

            var lineInTile = windowLineCounter % 8;

            var tileLow = mmu.Read((ushort)(tileAddress + lineInTile * 2));
            var tileHigh = mmu.Read((ushort)(tileAddress + lineInTile * 2 + 1));

            var bitIndex = 7 - (windowColumn % 8);
            var colorBit = ((tileHigh >> bitIndex) & 1) << 1 | ((tileLow >> bitIndex) & 1);

            var bgp = mmu.Bgp;
            var paletteShift = colorBit * 2;
            var paletteColor = (bgp >> paletteShift) & 0b11;
            scanlineBuffer[x] = ConvertPaletteColor(paletteColor);
        }

        if (windowRendered)
        {
            windowLineCounter++;
        }
    }

    private void RenderSprites()
    {
        var currentScanline = mmu.Ly;
        if ((mmu.Lcdc & (1 << 1)) == 0) return;

        var renderedSprites = 0;
        var pixelOwner = new int[ScreenWidth];
        Array.Fill(pixelOwner, -1);

        for (var i = 0; i < 40; i++)
        {
            if (renderedSprites >= 10) break;

            var spriteIndex = i * 4;
            var yPos = mmu.Read((ushort)(0xFE00 + spriteIndex)) - 16;
            var xPos = mmu.Read((ushort)(0xFE00 + spriteIndex + 1)) - 8;
            var tileIndex = mmu.Read((ushort)(0xFE00 + spriteIndex + 2));
            var attributes = mmu.Read((ushort)(0xFE00 + spriteIndex + 3));

            var spriteHeight = (mmu.Lcdc & (1 << 2)) != 0 ? 16 : 8;
            if (currentScanline < yPos || currentScanline >= yPos + spriteHeight)
            {
                continue;
            }

            var lineInSprite = currentScanline - yPos;
            if ((attributes & (1 << 6)) != 0)
            {
                lineInSprite = spriteHeight - 1 - lineInSprite;
            }

            if (spriteHeight == 16)
            {
                tileIndex &= 0xFE;
                if (lineInSprite >= 8)
                {
                    tileIndex += 1;
                    lineInSprite -= 8;
                }
            }

            var tileAddress = (ushort)(0x8000 + tileIndex * 16 + lineInSprite * 2);
            var tileLow = mmu.Read(tileAddress);
            var tileHigh = mmu.Read((ushort)(tileAddress + 1));

            for (var x = 0; x < 8; x++)
            {
                var bitIndex = (attributes & (1 << 5)) != 0 ? x : 7 - x;
                var colorBit = ((tileHigh >> bitIndex) & 1) << 1 | ((tileLow >> bitIndex) & 1);

                if (colorBit == 0) continue;

                var screenX = xPos + x;
                if (screenX is < 0 or >= ScreenWidth) continue;

                var bgOverObj = (attributes & (1 << 7)) != 0;
                if (bgOverObj && !scanlineBuffer[screenX].Equals(ConvertPaletteColor(0)))
                {
                    continue;
                }

                if (pixelOwner[screenX] != -1 && xPos >= pixelOwner[screenX]) continue;
                pixelOwner[screenX] = xPos;
                var isSpritePalette1 = (attributes & (1 << 4)) != 0;

                var spritePalette = isSpritePalette1 ? mmu.Obp1 : mmu.Obp0;
                var paletteShift = colorBit * 2;
                var paletteColor = (spritePalette >> paletteShift) & 0b11;

                scanlineBuffer[screenX] = ConvertPaletteColor(paletteColor);
            }
            renderedSprites++;
        }
    }

    private static Color ConvertPaletteColor(int paletteColor)
    {
        return Helper.Palettes[Helper.PaletteName][paletteColor];
    }

    private void SetLycFlag()
    {
        if (mmu.Ly == mmu.Lyc)
        {
            mmu.Stat = (byte)(mmu.Stat | 0x04);
            if ((mmu.Stat & 0x40) != 0)
            {
                //If the LYC=LY interrupt is enabled set the flag in the IF registers
                mmu.If = (byte)(mmu.If | 0x02);
            }
        }
        else
        {
            mmu.Stat = (byte)(mmu.Stat & 0xFB); //Clear the LYC=LY flag
        }
    }

    private void DrawFrame(ref Image image)
    {
        for (var y = 0; y < ScreenHeight; y++)
        {
            for (var x = 0; x < ScreenWidth; x++)
            {
                var color = frameBuffer[y * ScreenWidth + x];
                Raylib.ImageDrawPixel(ref image, x, y, color);
            }
        }

        unsafe
        {
            Raylib.UpdateTexture(screenTexture, screenImage.Data);
        }
        Raylib.DrawTextureEx(screenTexture, new System.Numerics.Vector2(0, 0), 0.0f, Helper.Scale, Color.White);
    }
}