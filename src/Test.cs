using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace CODE_DMG;

public class JsonTest
{
    public class ProcessorState
    {
        public int Pc { get; set; }
        public int Sp { get; set; }
        public int A { get; set; }
        public int B { get; set; }
        public int C { get; set; }
        public int D { get; set; }
        public int E { get; set; }
        public int F { get; set; }
        public int H { get; set; }
        public int L { get; set; }
        public int Ime { get; set; }
        public int Ei { get; set; }

        public List<List<int>> Ram { get; set; } = [];
    }

    public class Test
    {
        public string Name { get; set; } = string.Empty;
        public ProcessorState Initial { get; set; } = new();
        public ProcessorState Final { get; set; } = new();
    }

    private readonly Mmu mmu;
    private readonly Cpu cpu;

    public JsonTest()
    {
        mmu = new Mmu(new byte[32768], new byte[1], true);
        cpu = new Cpu(mmu);
    }

    [Fact]
    public void Run()
    {
        var fileFolder = Path.Combine("test", "v1");
        Directory.Exists(fileFolder);
        var fileEntries = Directory.GetFiles(fileFolder);
        if (!fileEntries.Any(f => f.EndsWith(".json"))) return;
        var json = File.ReadAllText(fileEntries.FirstOrDefault(f => f.EndsWith(".json")));
        var tests = JsonSerializer.Deserialize<List<Test>>(json) ?? [];
        foreach (var test in tests)
        {
            Console.WriteLine(test.Name);

            cpu.Pc = (ushort)test.Initial.Pc;
            cpu.Sp = (ushort)test.Initial.Sp;
            cpu.A = (byte)test.Initial.A;
            cpu.B = (byte)test.Initial.B;
            cpu.C = (byte)test.Initial.C;
            cpu.D = (byte)test.Initial.D;
            cpu.E = (byte)test.Initial.E;
            cpu.F = (byte)test.Initial.F;
            cpu.UpdateFlagsFromF();
            cpu.H = (byte)test.Initial.H;
            cpu.L = (byte)test.Initial.L;

            var initCpu16Reg = $"PC: {cpu.Pc}, SP: {cpu.Sp}";
            var initCpuReg =
                $"A: {cpu.A}, B: {cpu.B}, C: {cpu.C}, D: {cpu.D}, E: {cpu.E}, F: {cpu.F}, H: {cpu.H}, L: {cpu.L}";
            var initRam = string.Empty;

            foreach (var entry in test.Initial.Ram)
            {
                mmu.Write((ushort)entry[0], (byte)entry[1]);
                initRam += $"Address: {entry[0]}, Value: {entry[1]}\n";
            }

            cpu.ExecuteInstruction();

            var finalCpu16Reg = $"PC: {cpu.Pc}, SP: {cpu.Sp}";
            var finalCpuReg =
                $"A: {cpu.A}, B: {cpu.B}, C: {cpu.C}, D: {cpu.D}, E: {cpu.E}, F: {cpu.F}, H: {cpu.H}, L: {cpu.L}";
            var finalRam = string.Empty;

            var isMismatch = false;
            if (cpu.A != test.Final.A)
            {
                Console.WriteLine($"Mismatch in A: Expected {test.Final.A}, Found {cpu.A}");
                isMismatch = true;
            }

            if (cpu.B != test.Final.B)
            {
                Console.WriteLine($"Mismatch in B: Expected {test.Final.B}, Found {cpu.B}");
                isMismatch = true;
            }

            if (cpu.C != test.Final.C)
            {
                Console.WriteLine($"Mismatch in C: Expected {test.Final.C}, Found {cpu.C}");
                isMismatch = true;
            }

            if (cpu.D != test.Final.D)
            {
                Console.WriteLine($"Mismatch in D: Expected {test.Final.D}, Found {cpu.D}");
                isMismatch = true;
            }

            if (cpu.E != test.Final.E)
            {
                Console.WriteLine($"Mismatch in E: Expected {test.Final.E}, Found {cpu.E}");
                isMismatch = true;
            }

            if (cpu.F != test.Final.F)
            {
                Console.WriteLine($"Mismatch in F: Expected {test.Final.F}, Found {cpu.F}");
                isMismatch = true;
            }

            if (cpu.H != test.Final.H)
            {
                Console.WriteLine($"Mismatch in H: Expected {test.Final.H}, Found {cpu.H}");
                isMismatch = true;
            }

            if (cpu.L != test.Final.L)
            {
                Console.WriteLine($"Mismatch in L: Expected {test.Final.L}, Found {cpu.L}");
                isMismatch = true;
            }

            if (cpu.Pc != test.Final.Pc)
            {
                Console.WriteLine($"Mismatch in Pc: Expected {test.Final.Pc}, Found {cpu.Pc}");
                isMismatch = true;
            }

            if (cpu.Sp != test.Final.Sp)
            {
                Console.WriteLine($"Mismatch in Sp: Expected {test.Final.Sp}, Found {cpu.Sp}");
                isMismatch = true;
            }

            foreach (var entry in test.Final.Ram)
            {
                int valueInMmu = mmu.Read((ushort)entry[0]);
                finalRam += $"Address: {entry[0]}, Value: {entry[1]}\n";
                if (valueInMmu != entry[1]) isMismatch = true;
                valueInMmu.Should().Be(entry[1]);
            }

            if (!isMismatch) continue;
            //To compare init and final values to JSON for full detail if init properly or anyother
            Console.WriteLine("\nCPU and RAM init:");
            Console.WriteLine(initCpu16Reg);
            Console.WriteLine(initCpuReg);
            Console.WriteLine(initRam);

            Console.WriteLine("CPU and RAM final:");
            Console.WriteLine(finalCpu16Reg);
            Console.WriteLine(finalCpuReg);
            Console.WriteLine(finalRam);

            Console.WriteLine("JSON Test:");
            var testJson = JsonSerializer.Serialize(test);
            Console.WriteLine(testJson);

            return;
        }

        Console.WriteLine("All tests passed!");
    }
}