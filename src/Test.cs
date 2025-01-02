using Newtonsoft.Json;

class JSONTest {
    public class ProcessorState {
        public int pc { get; set; }
        public int sp { get; set; }
        public int a { get; set; }
        public int b { get; set; }
        public int c { get; set; }
        public int d { get; set; }
        public int e { get; set; }
        public int f { get; set; }
        public int h { get; set; }
        public int l { get; set; }
        public int ime { get; set; }
        public int ei { get; set; }

        public List<List<int>> ram { get; set; } = new List<List<int>>();
    }
    public class Test {
        public string name { get; set; } = "";
        public ProcessorState initial { get; set; } = new ProcessorState();
        public ProcessorState final { get; set; } = new ProcessorState();
    }

    public MMU mmu;
    public CPU cpu;

    public JSONTest() {
        mmu = new MMU(new byte[32768], new byte[1], true);
        cpu = new CPU(mmu);
    }

    public void Run(string jsonPath) {
        string filePath = jsonPath;
        
        var json = File.ReadAllText(filePath);
        var tests = JsonConvert.DeserializeObject<List<Test>>(json) ?? new List<Test>();
        foreach (var test in tests) {
            Console.WriteLine(test.name);

            cpu.PC = (ushort)test.initial.pc;
            cpu.SP = (ushort)test.initial.sp;
            cpu.A = (byte)test.initial.a;
            cpu.B = (byte)test.initial.b;
            cpu.C = (byte)test.initial.c;
            cpu.D = (byte)test.initial.d;
            cpu.E = (byte)test.initial.e;
            cpu.F = (byte)test.initial.f;
            cpu.UpdateFlagsFromF();
            cpu.H = (byte)test.initial.h;
            cpu.L = (byte)test.initial.l;

            string initCPU16Reg = $"PC: {cpu.PC}, SP: {cpu.SP}";
            string initCPUReg = $"A: {cpu.A}, B: {cpu.B}, C: {cpu.C}, D: {cpu.D}, E: {cpu.E}, F: {cpu.F}, H: {cpu.H}, L: {cpu.L}";
            string initRAM = "";

            foreach (var entry in test.initial.ram) {
                mmu.Write((ushort)entry[0], (byte)entry[1]);
                initRAM += $"Address: {entry[0]}, Value: {entry[1]}\n";
            }

            cpu.ExecuteInstruction();

            string finalCPU16Reg = $"PC: {cpu.PC}, SP: {cpu.SP}";
            string finalCPUReg = $"A: {cpu.A}, B: {cpu.B}, C: {cpu.C}, D: {cpu.D}, E: {cpu.E}, F: {cpu.F}, H: {cpu.H}, L: {cpu.L}";
            string finalRAM = "";

            bool isMismatch = false;
            if (cpu.A != test.final.a) { Console.WriteLine($"Mismatch in A: Expected {test.final.a}, Found {cpu.A}"); isMismatch = true; }
            if (cpu.B != test.final.b) { Console.WriteLine($"Mismatch in B: Expected {test.final.b}, Found {cpu.B}"); isMismatch = true; }
            if (cpu.C != test.final.c) { Console.WriteLine($"Mismatch in C: Expected {test.final.c}, Found {cpu.C}"); isMismatch = true; }
            if (cpu.D != test.final.d) { Console.WriteLine($"Mismatch in D: Expected {test.final.d}, Found {cpu.D}"); isMismatch = true; }
            if (cpu.E != test.final.e) { Console.WriteLine($"Mismatch in E: Expected {test.final.e}, Found {cpu.E}"); isMismatch = true; }
            if (cpu.F != test.final.f) { Console.WriteLine($"Mismatch in F: Expected {test.final.f}, Found {cpu.F}"); isMismatch = true; }
            if (cpu.H != test.final.h) { Console.WriteLine($"Mismatch in H: Expected {test.final.h}, Found {cpu.H}"); isMismatch = true; }
            if (cpu.L != test.final.l) { Console.WriteLine($"Mismatch in L: Expected {test.final.l}, Found {cpu.L}"); isMismatch = true; }
            if (cpu.PC != test.final.pc) { Console.WriteLine($"Mismatch in Pc: Expected {test.final.pc}, Found {cpu.PC}"); isMismatch = true; }
            if (cpu.SP != test.final.sp) { Console.WriteLine($"Mismatch in Sp: Expected {test.final.sp}, Found {cpu.SP}"); isMismatch = true; }
            
            foreach (var entry in test.final.ram) {
                int valueInMMU = mmu.Read((ushort)entry[0]);
                finalRAM += $"Address: {entry[0]}, Value: {entry[1]}\n";

                if (valueInMMU != entry[1]) {
                    Console.WriteLine($"Mismatch in MMU at Address {entry[0]}: Expected {entry[1]}, Found {valueInMMU}");
                    isMismatch = true;
                }
            }

            if (isMismatch) {
                //To compare init and final values to JSON for full detail if init properly or anyother
                Console.WriteLine("\nCPU and RAM init:");
                Console.WriteLine(initCPU16Reg);
                Console.WriteLine(initCPUReg);
                Console.WriteLine(initRAM);

                Console.WriteLine("CPU and RAM final:");
                Console.WriteLine(finalCPU16Reg);
                Console.WriteLine(finalCPUReg);
                Console.WriteLine(finalRAM);
                
                Console.WriteLine("JSON Test:");
                string testJson = JsonConvert.SerializeObject(test, Formatting.Indented);
                Console.WriteLine(testJson);

                Environment.Exit(1);
            }
        }

        Console.WriteLine("All tests passed!");
    }
}