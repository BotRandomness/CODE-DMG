public class MMU {
    public byte[] rom; //ROM
    public byte[] wram; //Work RAM
    public byte[] vram; //Video RAM
    public byte[] oam; //Object Attribute Memory
    public byte[] hram; //High RAM
    public byte[] io; //I/O Registers
    private byte[] bootRom; //Boot ROM
    private bool bootEnabled; //Boot ROM enabled flag

    private MBC mbc;

    public const int ROM_SIZE = 0x8000; //32KB
    public const int BOOT_ROM_SIZE = 0x0100; //256 bytes
    public const int WRAM_SIZE = 0x2000; //8KB
    public const int VRAM_SIZE = 0x2000; //8KB
    public const int OAM_SIZE = 0x00A0; //160 bytes
    public const int HRAM_SIZE = 0x007F; //127 bytes
    public const int IO_SIZE = 0x0080; //128 bytes

    public byte IE; //0xFFFF
    public byte IF; //0xFF0F
    public byte JOYP; //0xFF00
    public byte DIV; //0xFF04
    public byte TIMA; //0xFF05
    public byte TMA; //0xFF06
    public byte TAC; //0xFF07
    public byte LCDC; //0xFF40
    public byte STAT; //0xFF41
    public byte SCY; //0xFF42
    public byte SCX; //0xFF43
    public byte LY; //0xFF44
    public byte LYC; //0xFF54
    public byte BGP; //0xFF47
    public byte OBP0; //0xFF48
    public byte OBP1; //0xFF49
    public byte WY; //0xFF4A
    public byte WX; //0xFF4B

    public byte joypadState = 0xFF; //Raw inputs

    public byte[] ram; //64 KB RAM
    public bool mode;

    public MMU(byte[] gameRom, byte[] bootRomData, bool mode)
    {
        rom = gameRom;
        bootRom = bootRomData;
        wram = new byte[WRAM_SIZE];
        vram = new byte[VRAM_SIZE];
        oam = new byte[OAM_SIZE];
        hram = new byte[HRAM_SIZE];
        io = new byte[IO_SIZE];
        bootEnabled = true;

        ram = new byte[65536];
        this.mode = mode;

        mbc = new MBC(rom);

        Console.WriteLine("MMU init");
    }

    public void Save(string path) {
        if (mbc.mbcType != 0) {
            Console.WriteLine("Writing to save to: " + path);
            File.WriteAllBytes(path, mbc.ramBanks);
        }
    }

    public void Load(string path) {
       if (File.Exists(path) && mbc.mbcType != 0) {
            Console.WriteLine("Loading save: " + path);
            mbc.ramBanks = File.ReadAllBytes(path);
        } else if (mbc.mbcType != 0) {
            Console.WriteLine("Save not found at: " + path);
        }
    }

    public string HeaderInfo() {
        return mbc.GetTitle() + "\n" + mbc.GetCartridgeType() + "\n" + mbc.GetRomSize() + "\n" + mbc.GetRamSize() + "\n" + mbc.GetChecksum();
    }

    public byte Read(ushort address) {
        if (mode == false) {
            return Read1(address);
        } else if (mode == true) {
            return Read2(address);
        }
        return 0xFF;
    }
    public void Write(ushort address, byte value) {
        if (mode == false) {
            Write1(address, value);
        } else if (mode == true) {
            Write2(address, value);
        }
    }

    public void Write2(ushort address, byte value) {
        ram[address] = value;
    }
    public byte Read2(ushort address) {
        return ram[address];
    }

    public byte Read1(ushort address) {
        if (bootEnabled && address < BOOT_ROM_SIZE) {
            return bootRom[address]; //Boot ROM
        }

        if (address < 0x8000 || (address >= 0xA000 && address < 0xC000)) {
            return mbc.Read(address); //Delegate to MBC
        }

        switch (address) {
            case 0xFF00:
                //if action or direction buttons are selected
                if ((JOYP & 0x10) == 0) { //Action buttons selected
                    return (byte)((joypadState >> 4) | 0x20);
                }
                else if ((JOYP & 0x20) == 0) { //Direction buttons selected
                    return (byte)((joypadState & 0x0F) | 0x10);
                }
                return (byte)(JOYP | 0xFF);
            case 0xFF04:
                return DIV;    
            case 0xFF40:
                return LCDC;
            case 0xFF41:
                return STAT;
            case 0xFF42:
                return SCY;
            case 0xFF43:
                return SCX;
            case 0xFF44:
                return LY;
            case 0xFF45:
                return LYC;
            case 0xFF47:
                return BGP;
            case 0xFF48:
                return OBP0;
            case 0xFF49:
                return OBP1;
            case 0xFF4A:
                return WY;
            case 0xFF4B:
                return WX;
            case 0xFF0F:
                return IF;
            case 0xFFFF:
                return IE;
        }

        /*
        if (address < ROM_SIZE) {
            return rom[address];
        }
        */
        if (address >= 0xC000 && address < 0xE000) {
            return wram[address - 0xC000];
        }
        else if (address >= 0x8000 && address < 0xA000) {
            return vram[address - 0x8000];
        }
        else if (address >= 0xFE00 && address < 0xFEA0) {
            return oam[address - 0xFE00];
        }
        else if (address >= 0xFF80 && address < 0xFFFF) {
            return hram[address - 0xFF80];
        }
        else if (address >= 0xFF00 && address < 0xFF80) {
            return io[address - 0xFF00]; //Mostly as a fallback
        }
        return 0xFF; //Default values of unknown reads
    }

    public void Write1(ushort address, byte value) {
        if (address == 0xFF50) {
            //Disable boot ROM if written to
            bootEnabled = false;
            return;
        }

        if (address < 0x8000 || (address >= 0xA000 && address < 0xC000)) {
            mbc.Write(address, value); //Delegate to MBC
            return;
        }

        switch (address) {
            case 0xFF00:
                JOYP = (byte)(value & 0x30);
                break;
            case 0xFF04:
                DIV = value;
                break;
            case 0xFF40:
                LCDC = value;
                if ((value & 0x80) == 0) {
                    STAT &= 0x7C;
                    LY = 0x00;
                }
                break;
            case 0xFF46: //DMA
                ushort sourceAddress = (ushort)(value << 8);
                for (ushort i = 0; i < 0xA0; i++)
                {
                    Write((ushort)(0xFE00 + i), Read((ushort)(sourceAddress + i)));
                }
                break;
            case 0xFF41:
                STAT = value;
                break;
            case 0xFF42:
                SCY = value;
                break;
            case 0xFF43:
                SCX = value;
                break;
            case 0xFF44:
                LY = value;
                break;
            case 0xFF45:
                LYC = value;
                break;
            case 0xFF47:
                BGP = value;
                break;
            case 0xFF48:
                OBP0 = value;
                break;
            case 0xFF49:
                OBP1 = value;
                break;
            case 0xFF4A:
                WY = value;
                break;
            case 0xFF4B:
                WX = value;
                break;
            case 0xFF0F:
                IF = value;
                break;
            case 0xFFFF:
                IE = value;
                break;
        }

        if (address >= 0xC000 && address < 0xE000) {
            wram[address - 0xC000] = value;
        }
        else if (address >= 0x8000 && address < 0xA000) {
            vram[address - 0x8000] = value;
        }
        else if (address >= 0xFE00 && address < 0xFEA0) {
            oam[address - 0xFE00] = value;
        }
        else if (address >= 0xFF80 && address < 0xFFFF) {
            hram[address - 0xFF80] = value;
        }
        else if (address >= 0xFF00 && address < 0xFF80) {
            io[address - 0xFF00] = value; //Mostly as a fallback
        }
        else if (address == 0xFFFF) {
            //IE accounted for in switch statement, else if here to prevement "OUT OF RANGE" message 
        }
        else {
            Console.WriteLine(address.ToString("X4") + " - OUT OF RANGE WRITE");
        }
    }
}