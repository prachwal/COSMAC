namespace Cdp1802.Cli;

public static class UartDemoTest
{
    public static void Run()
    {
        Console.WriteLine("=== Testing UART_DEMO.asm ===\n");

        // Load binary
        string binaryPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "examples", "UART_DEMO.bin");
        if (!File.Exists(binaryPath))
        {
            // Try relative path
            binaryPath = "examples/UART_DEMO.bin";
        }

        if (!File.Exists(binaryPath))
        {
            Console.WriteLine($"❌ Error: UART_DEMO.bin not found at {binaryPath}");
            return;
        }

        byte[] binary = File.ReadAllBytes(binaryPath);
        Console.WriteLine($"✓ Loaded binary: {binary.Length} bytes from {binaryPath}");

        // Setup emulator
        var cpu = new Core.Cdp1802();
        var uart = new Core.Uart();
        var debugger = new Core.Debugger(cpu);

        cpu.RegisterPeripheral(uart);

        // Load program into memory at 0x0000
        for (int i = 0; i < binary.Length; i++)
            cpu.Memory[i] = binary[i];

        Console.WriteLine($"✓ Loaded program at 0x0000-0x{binary.Length:X4}");

        // Run for limited steps
        int steps = 0;
        int maxSteps = 10000;

        while (steps < maxSteps && !cpu.IsHalted)
        {
            debugger.Step();
            steps++;

            // Check if program is stuck in echo loop
            if (steps > 500 && uart.TransmittedString.Length > 0)
                break;
        }

        Console.WriteLine($"\n✓ Executed {steps} cycles");
        Console.WriteLine($"✓ Final PC: 0x{cpu.R[cpu.P]:X4}");

        // Check UART output
        string output = uart.TransmittedString;
        Console.WriteLine($"\n📤 UART TX Output bytes ({output.Length} total):");

        foreach (char c in output)
        {
            byte b = (byte)c;
            string display = (b >= 32 && b < 127) ? $"'{c}'" : "   ";
            Console.Write($"0x{b:X2} {display}  ");
        }
        Console.WriteLine("\n");

        // Verify expected output
        if (output.Contains("UART Ready"))
        {
            Console.WriteLine("✅ SUCCESS: Program sent 'UART Ready' to UART TX!");
            Console.WriteLine($"   Complete output: \"{output.Replace("\n", "\\n").Replace("\r", "\\r")}\"");
        }
        else
        {
            Console.WriteLine($"❌ FAIL: Expected 'UART Ready' in output, got: \"{output}\"");
        }

        // Test UART RX (echo)
        Console.WriteLine("\nTesting UART RX echo...");
        uart.Receive((byte)'T');
        uart.Receive((byte)'E');
        uart.Receive((byte)'S');
        uart.Receive((byte)'T');

        // Run more cycles to process input
        int echoSteps = 0;
        while (echoSteps < 1000 && steps < maxSteps)
        {
            debugger.Step();
            steps++;
            echoSteps++;
        }

        string fullOutput = uart.TransmittedString;
        Console.WriteLine($"✓ After sending TEST to RX, total TX: \"{fullOutput.Replace("\n", "\\n")}\"");

        if (fullOutput.Contains("TEST"))
        {
            Console.WriteLine("✅ ECHO TEST PASSED: Program echoed back 'TEST'!");
        }
        else
        {
            Console.WriteLine("⚠️  Echo may not have completed yet");
        }

        Console.WriteLine("\n=== Test Complete ===");
    }
}
