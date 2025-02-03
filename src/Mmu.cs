namespace CODE_DMG;
public class Mmu
{
    private readonly byte[] wram; //Work RAM
    private readonly byte[] vram; //Video RAM
    private readonly byte[] oam; //Object Attribute Memory
    private readonly byte[] hram; //High RAM
    private readonly byte[] io; //I/O Registers
    private readonly byte[] bootRom; //Boot ROM
    private bool bootEnabled; //Boot ROM enabled flag

    private readonly Mbc mbc;

    private const int BootRomSize = 0x0100; //256 bytes
    private const int WramSize = 0x2000; //8KB
    private const int VramSize = 0x2000; //8KB
    private const int OamSize = 0x00A0; //160 bytes
    private const int HramSize = 0x007F; //127 bytes
    private const int IoSize = 0x0080; //128 bytes

    private byte ie; //0xFFFF
    public byte If; //0xFF0F
    public byte Joyp; //0xFF00
    public byte Div; //0xFF04
    public byte Lcdc; //0xFF40
    public byte Stat; //0xFF41
    public byte Scy; //0xFF42
    public byte Scx; //0xFF43
    public byte Ly; //0xFF44
    public byte Lyc; //0xFF54
    public byte Bgp; //0xFF47
    public byte Obp0; //0xFF48
    public byte Obp1; //0xFF49
    public byte Wy; //0xFF4A
    public byte Wx; //0xFF4B

    public byte JoypadState = 0xFF; //Raw inputs

    private readonly byte[] ram; //64 KB RAM
    private readonly bool mode;

    public Mmu(byte[] gameRom, byte[] bootRomData, bool mode)
    {
        bootRom = bootRomData;
        wram = new byte[WramSize];
        vram = new byte[VramSize];
        oam = new byte[OamSize];
        hram = new byte[HramSize];
        io = new byte[IoSize];
        bootEnabled = true;

        ram = new byte[65536];
        this.mode = mode;

        mbc = new Mbc(gameRom);

        Console.WriteLine("MMU init");
    }

    public void Save(string path)
    {
        if (mbc.MbcType == 0) return;
        Console.WriteLine("Writing to save to: " + path);
        File.WriteAllBytes(path, mbc.RamBanks);
    }

    public void Load(string path)
    {
        if (File.Exists(path) && mbc.MbcType != 0)
        {
            Console.WriteLine("Loading save: " + path);
            mbc.RamBanks = File.ReadAllBytes(path);
        }
        else if (mbc.MbcType != 0)
        {
            Console.WriteLine("Save not found at: " + path);
        }
    }

    public string HeaderInfo()
    {
        return mbc.GetTitle() + "\n" + mbc.GetCartridgeType() + "\n" + mbc.GetRomSize() + "\n" + mbc.GetRamSize() + "\n" + mbc.GetChecksum();
    }

    public byte Read(ushort address)
    {
        return mode ? Read2(address) : Read1(address);
    }
    public void Write(ushort address, byte value)
    {
        switch (mode)
        {
            case true: 
                Write2(address, value);
                break;
            case false:
                Write1(address, value);
                break;
        }
    }

    private void Write2(ushort address, byte value)
    {
        ram[address] = value;
    }

    private byte Read2(ushort address)
    {
        return ram[address];
    }

    private byte Read1(ushort address)
    {
        if (bootEnabled && address < BootRomSize)
        {
            return bootRom[address]; //Boot ROM
        }

        if (address is < 0x8000 or >= 0xA000 and < 0xC000)
        {
            return mbc.Read(address); //Delegate to MBC
        }

        switch (address)
        {
            case 0xFF00:
                //if action or direction buttons are selected
                if ((Joyp & 0x10) == 0)
                { //Action buttons selected
                    return (byte)((JoypadState >> 4) | 0x20);
                }
                else if ((Joyp & 0x20) == 0)
                { //Direction buttons selected
                    return (byte)((JoypadState & 0x0F) | 0x10);
                }
                return (byte)(Joyp | 0xFF);
            case 0xFF04:
                return Div;
            case 0xFF40:
                return Lcdc;
            case 0xFF41:
                return Stat;
            case 0xFF42:
                return Scy;
            case 0xFF43:
                return Scx;
            case 0xFF44:
                return Ly;
            case 0xFF45:
                return Lyc;
            case 0xFF47:
                return Bgp;
            case 0xFF48:
                return Obp0;
            case 0xFF49:
                return Obp1;
            case 0xFF4A:
                return Wy;
            case 0xFF4B:
                return Wx;
            case 0xFF0F:
                return If;
            case 0xFFFF:
                return ie;
        }

        return address switch
        {
            >= 0xC000 and < 0xE000 => wram[address - 0xC000],
            >= 0x8000 and < 0xA000 => vram[address - 0x8000],
            >= 0xFE00 and < 0xFEA0 => oam[address - 0xFE00],
            >= 0xFF80 and < 0xFFFF => hram[address - 0xFF80],
            >= 0xFF00 and < 0xFF80 => io[address - 0xFF00],
            _ => 0xFF
        };
    }

    private void Write1(ushort address, byte value)
    {
        if (address == 0xFF50)
        {
            //Disable boot ROM if written to
            bootEnabled = false;
            return;
        }

        if (address is < 0x8000 or >= 0xA000 and < 0xC000)
        {
            mbc.Write(address, value); //Delegate to MBC
            return;
        }

        switch (address)
        {
            case 0xFF00:
                Joyp = (byte)(value & 0x30);
                break;
            case 0xFF04:
                Div = value;
                break;
            case 0xFF40:
                Lcdc = value;
                if ((value & 0x80) == 0)
                {
                    Stat &= 0x7C;
                    Ly = 0x00;
                }
                break;
            case 0xFF46: //DMA
                var sourceAddress = (ushort)(value << 8);
                for (ushort i = 0; i < 0xA0; i++)
                {
                    Write((ushort)(0xFE00 + i), Read((ushort)(sourceAddress + i)));
                }
                break;
            case 0xFF41:
                Stat = value;
                break;
            case 0xFF42:
                Scy = value;
                break;
            case 0xFF43:
                Scx = value;
                break;
            case 0xFF44:
                Ly = value;
                break;
            case 0xFF45:
                Lyc = value;
                break;
            case 0xFF47:
                Bgp = value;
                break;
            case 0xFF48:
                Obp0 = value;
                break;
            case 0xFF49:
                Obp1 = value;
                break;
            case 0xFF4A:
                Wy = value;
                break;
            case 0xFF4B:
                Wx = value;
                break;
            case 0xFF0F:
                If = value;
                break;
            case 0xFFFF:
                ie = value;
                break;
        }

        switch (address)
        {
            case >= 0xC000 and < 0xE000:
                wram[address - 0xC000] = value;
                break;
            case >= 0x8000 and < 0xA000:
                vram[address - 0x8000] = value;
                break;
            case >= 0xFE00 and < 0xFEA0:
                oam[address - 0xFE00] = value;
                break;
            case >= 0xFF80 and < 0xFFFF:
                hram[address - 0xFF80] = value;
                break;
            case >= 0xFF00 and < 0xFF80:
                io[address - 0xFF00] = value; //Mostly as a fallback
                break;
            case 0xFFFF:
                //IE accounted for in switch statement, else if here to prevement "OUT OF RANGE" message 
                break;
            default:
                Console.WriteLine(address.ToString("X4") + " - OUT OF RANGE WRITE");
                break;
        }
    }
}