namespace CODE_DMG;
public class Mbc
{
    //MBC is currently only experimental
    //MBC0/ROM Only should be good
    //MBC1, MBC3, MBC5 are currently basic and experimental
    private readonly byte[] rom; //Game ROM
    public byte[] RamBanks; //Cartridge RAM
    private int romBank = 1; //Current ROM bank
    private int ramBank; //Current RAM bank
    private bool ramEnabled; //RAM enable flag
    public readonly int MbcType; //MBC type
    private readonly int romBankCount;
    private readonly int ramBankCount;

    public Mbc(byte[] romData)
    {
        int ramSize1;
        var romSize1 = CalculateRomSize(romData[0x0148]);
        romBankCount = romSize1 / (16 * 1024);
        rom = romData;

        switch (rom[0x0147])
        {
            case 0x00:
                MbcType = 0;
                break;
            case 0x01:
            case 0x02:
            case 0x03:
                MbcType = 1;
                break;
            case 0x0F:
            case 0x10:
            case 0x11:
            case 0x12:
            case 0x13:
                MbcType = 3;
                break;
            case 0x19:
            case 0x1A:
            case 0x1B:
            case 0x1C:
            case 0x1D:
            case 0x1E:
                MbcType = 5;
                break;
            default:
                MbcType = 0;
                Console.WriteLine("Error: Unknown/Unsupported MBC, using MBC0/ROM Only");
                break;
        }

        switch (rom[0x0149])
        {
            case 0x01:
                ramSize1 = 2 * 1024; //2 KB (Unofficial?)
                ramBankCount = 1;
                break;
            case 0x02:
                ramSize1 = 8 * 1024; //8 KB
                ramBankCount = 1;
                break;
            case 0x03:
                ramSize1 = 32 * 1024; //32 KB
                ramBankCount = 4;
                break;
            case 0x04:
                ramSize1 = 128 * 1024; //128 KB
                ramBankCount = 16;
                break;
            case 0x05:
                ramSize1 = 64 * 1024; //64 KB
                ramBankCount = 8;
                break;
            default:
                ramSize1 = 0; //No RAM
                ramBankCount = 0;
                break;
        }

        RamBanks = new byte[ramSize1];
    }

    private static int CalculateRomSize(byte headerValue)
    {
        return 32 * 1024 * (1 << headerValue); //32 KiB × (1 << <value>)
    }

    public string GetTitle()
    {
        var titleBytes = new byte[16];
        Array.Copy(rom, 0x0134, titleBytes, 0, 16);

        var title = System.Text.Encoding.ASCII.GetString(titleBytes).TrimEnd(null);

        if (title.Length > 15)
        {
            title = title[..15];
        }

        return "Title: " + title;
    }

    public string GetCartridgeType()
    {
        var cartridgeType = rom[0x0147] switch
        {
            0x00 => "MBC0/ROM ONLY",
            0x01 => "MBC1",
            0x02 => "MBC1+RAM",
            0x03 => "MBC1+RAM+BATTERY",
            0x05 => "MBC2",
            0x06 => "MBC2+BATTERY",
            0x08 => "ROM+RAM",
            0x09 => "ROM+RAM+BATTERY",
            0x0B => "MMM01",
            0x0C => "MMM01+RAM",
            0x0D => "MMM01+RAM+BATTERY",
            0x0F => "MBC3+TIMER+BATTERY",
            0x10 => "MBC3+TIMER+RAM+BATTERY",
            0x11 => "MBC3",
            0x12 => "MBC3+RAM",
            0x13 => "MBC3+RAM+BATTERY",
            0x19 => "MBC5",
            0x1A => "MBC5+RAM",
            0x1B => "MBC5+RAM+BATTERY",
            0x1C => "MBC5+RUMBLE",
            0x1D => "MBC5+RUMBLE+RAM",
            0x1E => "MBC5+RUMBLE+RAM+BATTERY",
            0x20 => "MBC6",
            0x22 => "MBC7+SENSOR+RUMBLE+RAM+BATTERY",
            0xFC => "POCKET CAMERA",
            0xFD => "BANDAI TAMA5",
            0xFE => "HuC3",
            0xFF => "HuC1+RAM+BATTERY",
            _ => "Unknown cartridge type",
        };
        return "Cartridge Type: " + cartridgeType;
    }

    public string GetRomSize()
    {
        var romSizeName = rom[0x0148] switch
        {
            0x00 => "32 KiB (2 ROM banks, No Banking)",
            0x01 => "64 KiB (4 ROM banks)",
            0x02 => "128 KiB (8 ROM banks)",
            0x03 => "256 KiB (16 ROM banks)",
            0x04 => "512 KiB (32 ROM banks)",
            0x05 => "1 MiB (64 ROM banks)",
            0x06 => "2 MiB (128 ROM banks)",
            0x07 => "4 MiB (256 ROM banks)",
            0x08 => "8 MiB (512 ROM banks)",
            _ => "Unknown ROM size",
        };
        return "ROM Size: " + romSizeName;
    }

    public string GetRamSize()
    {
        var ramSizeName = rom[0x0149] switch
        {
            0x00 => "No RAM",
            0x01 => "Unused (2 KB?)",
            0x02 => "8 KiB (1 bank)",
            0x03 => "32 KiB (4 banks of 8 KiB each)",
            0x04 => "128 KiB (16 banks of 8 KiB each)",
            0x05 => "64 KiB (8 banks of 8 KiB each)",
            _ => "Unknown RAM size",
        };
        return "RAM Size: " + ramSizeName;
    }

    public string GetChecksum()
    {
        return "Checksum: " + rom[0x014D].ToString("X2");
    }

    public byte Read(ushort address)
    {
        switch (address)
        {
            case < 0x4000:
                //Fixed ROM Bank 0 (common)
                return rom[address];
            case < 0x8000:
            {
                //Switchable ROM Bank (common)
                var bankOffset = (romBank % romBankCount) * 0x4000;
                return rom[bankOffset + (address - 0x4000)];
            }
            //RAM Access (common)
            case >= 0xA000 and < 0xC000 when ramEnabled:
            {
                var ramOffset = (ramBank % ramBankCount) * 0x2000;
                return RamBanks[ramOffset + (address - 0xA000)];
            }
            default:
                return 0xFF;
        }
    }

    public void Write(ushort address, byte value)
    {
        switch (address)
        {
            case < 0x2000:
                //Enable or disable RAM (common)
                ramEnabled = (value & 0x0F) == 0x0A;
                break;
            //ROM Bank Switching
            case < 0x4000 when MbcType == 1:
            {
                romBank = value & 0x1F;
                if (romBank == 0) romBank = 1;
                break;
            }
            case < 0x4000 when MbcType == 3:
            {
                romBank = value & 0x7F;
                if (romBank == 0) romBank = 1;
                break;
            }
            case < 0x4000:
            {
                if (MbcType == 5)
                {
                    if (address < 0x3000)
                    {
                        romBank = (romBank & 0x100) | value;
                    }
                    else
                    {
                        romBank = (romBank & 0xFF) | ((value & 0x01) << 8);
                    }
                }

                break;
            }
            //RAM Bank Switching
            case < 0x6000 when MbcType == 1:
                ramBank = value & 0x03;
                break;
            //No banking mode for MBC1
            case < 0x6000:
            {
                if (MbcType is 5 or 3)
                {
                    //ramBank = value & 0x03; MBC3 2 bits for no RTC
                    ramBank = value & 0x0F;
                }

                break;
            }
            case >= 0xA000 and < 0xC000:
            {
                //RAM Write (common)
                if (ramEnabled)
                {
                    var ramOffset = (ramBank % ramBankCount) * 0x2000;
                    RamBanks[ramOffset + (address - 0xA000)] = value;
                }

                break;
            }
        }
    }
}
