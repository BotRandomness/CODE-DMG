using Raylib_cs;

namespace CODE_DMG;
public static class Helper
{
    public static int Scale = 2;
    public static string Rom = "fall_back.gb";
    public static string Bootrom = "dmg_boot.bin";
    private static string _jsonPath = string.Empty;
    public static bool FpsEnable;
    public static string PaletteName = "dmg";
    public static bool RaylibLog;
    private static readonly Version Version = new(1, 0, 0);
    public static Dictionary<string, Color[]> Palettes = new() {
        {"dmg", [
            new Color(155, 188, 15, 255), //Lightest 
        new Color(139, 172, 15, 255), //Light 
        new Color(48, 98, 48, 255), //Dark  
        new Color(15, 56, 15, 255), //Darkest 
        new Color(255, 255, 255, 255)
            ]
        }, //Transparent
        {"cyber", [
            new Color(50, 153, 180, 255),
        new Color(46, 116, 134, 255),
        new Color(2, 70, 88, 255),
        new Color(2, 49, 61, 255),
        new Color(255, 255, 255, 255)
            ]
        },
        {"emu", [
            new Color(224, 248, 208, 255),
        new Color(136, 192, 112, 255),
        new Color(52, 104, 86, 255),
        new Color(8, 24, 32, 255),
        new Color(255, 255, 255, 255)
            ]
        },
        {"autumn", [
            new Color(255, 246, 211, 255),
        new Color(249, 168, 117, 255),
        new Color(235, 107, 111, 255),
        new Color(124, 63, 88, 255),
        new Color(255, 255, 255, 255)
            ]
        },
        {"paris", [
            new Color(218, 112, 214, 255),
        new Color(186, 85, 211, 255),
        new Color(153, 50, 204, 255),
        new Color(75, 0, 130, 255),
        new Color(255, 255, 255, 255)
            ]
        },
        {"grayscale", [
            Color.White,
        Color.LightGray,
        Color.DarkGray,
        Color.Black,
        new Color(255, 255, 255, 255)
            ]
        },
        {"early", [
            Color.Black,
        Color.DarkGray,
        Color.LightGray,
        Color.White,
        new Color(255, 255, 255, 255)
            ]
        },
        {"crow", [
            new Color(204, 61, 80, 255),
        new Color(153, 31, 39, 255),
        new Color(89, 22, 22, 255),
        new Color(38, 15, 13, 255),
        new Color(255, 255, 255, 255)
            ]
        },
        {"coffee", [
            new Color(204, 158, 122, 255),
        new Color(153, 116, 92, 255),
        new Color(115, 77, 69, 255),
        new Color(77, 48, 46, 255),
        new Color(255, 255, 255, 255)
            ]
        },
        {"winter", [
            new Color(159, 244, 229, 255),
        new Color(0, 185, 190, 255),
        new Color(0, 95, 140, 255),
        new Color(0, 43, 89, 255),
        new Color(255, 255, 255, 255)
            ]
        }
    };

    public static void Flags(string[] args)
    {
        if (args.Length >= 1)
        {
            for (var i = 0; i < args.Length; i++)
            {
                if (args[i] == "--dmg")
                {
                    if (args[i] != args[^1] && args[i + 1].IndexOf('-') != 0)
                    {
                        Rom = args[i + 1];
                    }

                    if (!File.Exists(Rom))
                    {
                        Console.WriteLine("ROM \"" + Rom + "\" not found!");
                        Console.WriteLine("Error: Provide ROM --dmg <string:rom>");
                        Environment.Exit(1);
                    }
                }
                if (args[i] == "--json")
                {
                    _jsonPath = Path.Combine("test", "v1", args[i + 1]);
                    if (!File.Exists(_jsonPath))
                    {
                        Console.WriteLine("JSON Test \"" + args[i + 1] + "\" not found!");
                        Console.WriteLine("Error: Provide Test --json <test>");
                        Environment.Exit(1);
                    }
                }
                if (args[i] == "-b" || args[i] == "--bootrom")
                {
                    Bootrom = args[i + 1];
                    if (!File.Exists(Bootrom))
                    {
                        Console.WriteLine("Error: Custom bootrom file \"" + Bootrom + "\" not found!");
                        Environment.Exit(1);
                    }
                }
                if (args[i] == "-s" || args[i] == "--scale")
                {
                    Scale = int.Parse(args[i + 1]);
                }
                if (args[i] == "-f" || args[i] == "--fps")
                {
                    FpsEnable = true;
                }
                if (args[i] == "-rl" || args[i] == "--raylib-log")
                {
                    RaylibLog = true;
                }
                if (args[i] == "-p" || args[i] == "--palette")
                {
                    foreach (var palette in Palettes)
                    {
                        if (args[i + 1].ToLower().Equals(palette.Key))
                        {
                            PaletteName = args[i + 1].ToLower();
                            break;
                        }
                    }
                }
                if (args[i] == "-v" || args[i] == "--version")
                {
                    Console.WriteLine(Version);
                    Console.WriteLine("Made by Bot Randomness :)");
                    ASCII_DMG();
                    Environment.Exit(1);
                }
                if (args[i] == ":)" || args[i] == "-a" || args[i] == "--about")
                {
                    Console.WriteLine("Made by Bot Randomness :)");
                    Console.WriteLine(Version);
                    ASCII_DMG();
                    Environment.Exit(1);
                }
                if (args[i] == "-h" || args[i] == "--help")
                {
                    Console.WriteLine("DMG Help:");
                    Console.WriteLine("--dmg <string:path>, --dmg: Starts up the emulator given a rom file (Default mode. No rom given, fall back is default)");
                    Console.WriteLine("--json <string>: Runs a CPU test for a instruction given a JSON file in test/v1");
                    Console.WriteLine("-b <string:path>, --bootrom <string:path>: Loads custom bootrom path than default. (dmg_boot.bin is default)");
                    Console.WriteLine("-s <int>, --scale <int>: Scale window size by factor (2 is default)");
                    Console.WriteLine("-f, --fps: Enables FPS counter (off is default)");
                    Console.WriteLine("-rl, --raylib-log: Enables Raylib logs (off is default)");
                    Console.WriteLine("-p <string>, --palette <string>: Changes the 2bpp palette given name (dmg is default)");
                    Console.WriteLine("-a, --about: Shows about");
                    Console.WriteLine("-v, --version: Shows version number");
                    Console.WriteLine("-h, --help: Shows help screen (What you are reading right now)\n");
                    Console.WriteLine("Pallette names: dmg, cyber, emu, autumn, paris, grayscale, early, crow, coffee, winter");
                    Console.WriteLine("Controls: (A)=Z, (B)=X, [START]=[ENTER], [SELECT]=[RSHIFT], D-Pad=ArrowKeys");
                    Console.WriteLine("Note: Keep bootrom file (if provided) and fallback rom must be by the excutable!");
                    Console.WriteLine("Your current working directory must be at the application location when using!");
                    Console.WriteLine("Bootrom is not needed, but it is recommended. It must be named \"dmg_boot.bin\" and be placed in root of executable");
                    return;
                }
            }
        }
        else
        {
            
            Console.WriteLine("Error: No mode passed in");
            Console.WriteLine("Mode: --dmg <string:romPath> <optional flags>, --json <xx.json>");
            Console.WriteLine("Use -h or --help to bring up help options. Run in terminal (CLI only for now).");
            if (!File.Exists(Rom)) return;
        }
        Console.WriteLine("");
    }

    private static void ASCII_DMG()
    {
        Console.WriteLine(" __________________ ");
        Console.WriteLine("|-|--------------|-|");
        Console.WriteLine("|  ______________  |");
        Console.WriteLine("| |  __________  | |");
        Console.WriteLine("| | |          | | |");
        Console.WriteLine("| |·|          | | |");
        Console.WriteLine("| | |          | | |");
        Console.WriteLine("| | |__________| | |");
        Console.WriteLine("| |_____________/  |");
        Console.WriteLine("|   _  GAMEBOY     |");
        Console.WriteLine("| _| |_         () |");
        Console.WriteLine("||_   _|       ()  |");
        Console.WriteLine("|  |_|             |");
        Console.WriteLine("|       / /   \\\\\\ / ");
        Console.WriteLine("|________________/  ");
    }
}
