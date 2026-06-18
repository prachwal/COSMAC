namespace Cdp1802.Cli;

public static class UartCmdTest
{
    public static void Run()
    {
        string binaryPath = Path.Combine(FindRepoRoot(), "examples", "UART_ECHO_CMD.bin");
        if (!File.Exists(binaryPath))
        {
            Console.WriteLine($"Missing {binaryPath}");
            return;
        }

        byte[] firmware = File.ReadAllBytes(binaryPath);
        var cpu = new Core.Cdp1802();
        var uart = new Core.Uart();
        var timer = new Core.Timer(prescaler: 1);
        cpu.RegisterPeripheral(uart);
        cpu.RegisterPeripheral(timer);

        for (int i = 0; i < firmware.Length; i++)
            cpu.Memory[i] = firmware[i];

        for (int i = 0; i < 50_000; i++)
        {
            cpu.Step();
            timer.Tick();
            if (uart.TransmittedString.Contains("UART Ready"))
                break;
        }

        Console.WriteLine($"Boot TX: '{uart.TransmittedString}'");

        foreach (char ch in "!e\n")
        {
            uart.Receive((byte)ch);
            for (int i = 0; i < 200_000; i++)
            {
                cpu.Step();
                timer.Tick();
            }
        }

        Console.WriteLine($"Final TX: '{uart.TransmittedString}'");
    }

    private static string FindRepoRoot()
    {
        string dir = AppContext.BaseDirectory;
        for (int i = 0; i < 8; i++)
        {
            if (File.Exists(Path.Combine(dir, "Cdp1802.sln")))
                return dir;
            string? parent = Directory.GetParent(dir)?.FullName;
            if (parent is null) break;
            dir = parent;
        }
        return Directory.GetCurrentDirectory();
    }
}