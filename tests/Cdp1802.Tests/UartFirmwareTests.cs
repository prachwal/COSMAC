using Cdp1802.Core;
using Timer = Cdp1802.Core.Timer;

namespace Cdp1802.Tests;

public class UartFirmwareTests
{
    [Fact]
    public void UartEchoCmd_AssemblesSuccessfully()
    {
        string asmPath = FindRepoFile("examples", "asm", "UART_ECHO_CMD.asm");
        string source = File.ReadAllText(asmPath);

        AssemblerResult result = Assembler.Assemble(source);

        Assert.True(result.Success, string.Join("; ", result.Errors));
        Assert.True(result.Binary.Length > 0);
    }

    [Fact]
    public void UartEchoCmd_BootsAndHandlesUptimeCommand()
    {
        string binPath = FindRepoFile("examples", "UART_ECHO_CMD.bin");
        byte[] firmware = File.ReadAllBytes(binPath);

        var cpu = new Core.Cdp1802();
        var uart = new Uart();
        var timer = new Timer(prescaler: 1);
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

        Assert.Contains("UART Ready", uart.TransmittedString);

        foreach (char ch in "!e\n")
        {
            uart.Receive((byte)ch);
            for (int i = 0; i < 100_000; i++)
            {
                cpu.Step();
                timer.Tick();
            }
        }

        string output = uart.TransmittedString;
        Assert.Contains("Uptime:", output);
        Assert.Contains("ticks", output);
    }

    [Fact]
    public void UartEchoCmd_EchoesPlainText()
    {
        string binPath = FindRepoFile("examples", "UART_ECHO_CMD.bin");
        byte[] firmware = File.ReadAllBytes(binPath);

        var cpu = new Core.Cdp1802();
        var uart = new Uart();
        var timer = new Timer(prescaler: 1);
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

        foreach (char ch in "abc\n")
        {
            uart.Receive((byte)ch);
            for (int i = 0; i < 10_000; i++)
            {
                cpu.Step();
                timer.Tick();
            }
        }

        Assert.Contains("abc", uart.TransmittedString);
    }

    private static string FindRepoFile(params string[] parts)
    {
        string dir = AppContext.BaseDirectory;
        for (int i = 0; i < 8; i++)
        {
            string candidate = Path.Combine([dir, .. parts]);
            if (File.Exists(candidate))
                return candidate;

            string? parent = Directory.GetParent(dir)?.FullName;
            if (parent is null)
                break;
            dir = parent;
        }

        throw new FileNotFoundException(string.Join('/', parts));
    }
}