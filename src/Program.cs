namespace CODE_DMG;

public static class Program
{
    public static void Main(string[] args)
    {
        Console.WriteLine("CODE-DMG");
        Helper.Flags(args);
        var dmg = new Dmg(Helper.Rom, Helper.Bootrom);
        dmg.Run();
    }
}