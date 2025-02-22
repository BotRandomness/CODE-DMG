using Raylib_cs;

class Joypad {
    private MMU mmu;

    public Joypad(MMU mmu)
    {
        this.mmu = mmu;
    }

    private void SetButton(uint button, bool pressed) {
        //0 = pressed, 1 = released
        if (pressed) {
            mmu.joypadState &= (byte)~button;
        } else {
            mmu.joypadState |= (byte)button;
        }
    }

    public void HandleInput() {
        //Action buttons
        SetButton(0x01, Raylib.IsKeyDown(KeyboardKey.Z)); //A
        SetButton(0x02, Raylib.IsKeyDown(KeyboardKey.X)); //B
        SetButton(0x04, Raylib.IsKeyDown(KeyboardKey.RightShift)); //Select
        SetButton(0x08, Raylib.IsKeyDown(KeyboardKey.Enter)); //Start

        //D-Pad buttons
        SetButton(0x10, Raylib.IsKeyDown(KeyboardKey.Right)); //Right
        SetButton(0x20, Raylib.IsKeyDown(KeyboardKey.Left)); //Left
        SetButton(0x40, Raylib.IsKeyDown(KeyboardKey.Up)); //Up
        SetButton(0x80, Raylib.IsKeyDown(KeyboardKey.Down)); //Down

        //Console.WriteLine(mmu.Read(0xFF00).ToString("X4"));
    }
}
