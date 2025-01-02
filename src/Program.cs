class Program {
    public static void Main(string[] args) {
        Console.WriteLine("CODE-DMG");

        Helper.Flags(args);

        if (Helper.mode == 0) {
            DMG dmg = new DMG(Helper.rom, Helper.bootrom);

            dmg.Run();
        } else if (Helper.mode == 1) {
            JSONTest jsonTest = new JSONTest();

            jsonTest.Run(Helper.jsonPath);
        }
    }
}