using System.Diagnostics;
using Cdp1802.Core;

namespace Cdp1802.Cli;

public static class UartBridgeHost
{
    public static void StartBackground(string firmwarePath, string? socketPath = null)
    {
        string pidFile = UartBridgeServer.DefaultPidFile;

        if (TryReadRunningPid(pidFile, out int existingPid))
        {
            Console.WriteLine($"Serwer UART juz dziala (PID {existingPid}).");
            Console.WriteLine("Polacz: dotnet run --project src/Cdp1802.Cli -- --uart-client");
            return;
        }

        string projectPath = FindProjectPath();
        string args = $"run --project \"{projectPath}\" -- --uart-server --foreground --firmware \"{firmwarePath}\"";
        if (socketPath is not null)
            args += $" --socket \"{socketPath}\"";

        var psi = new ProcessStartInfo("dotnet", args)
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = FindRepoRoot()
        };

        Process? process = Process.Start(psi);
        if (process is null)
        {
            Console.WriteLine("Nie udalo sie uruchomic serwera UART w tle.");
            return;
        }

        Thread.Sleep(500);

        if (TryReadRunningPid(pidFile, out int pid))
        {
            Console.WriteLine($"Serwer UART uruchomiony w tle (PID {pid}).");
            Console.WriteLine($"Socket: {socketPath ?? UartBridgeServer.DefaultSocketPath}");
            Console.WriteLine("Polacz klientem: dotnet run --project src/Cdp1802.Cli -- --uart-client");
        }
        else
        {
            Console.WriteLine("Serwer UART startuje... sprawdz logi lub poczekaj chwile.");
            Console.WriteLine("Polacz klientem: dotnet run --project src/Cdp1802.Cli -- --uart-client");
        }
    }

    public static async Task RunServerAsync(bool foreground, string firmwarePath, string? socketPath)
    {
        if (!foreground)
            UartBridgeServer.Daemonize();

        using var server = new UartBridgeServer(socketPath);
        server.Start(firmwarePath);

        if (foreground)
        {
            Console.WriteLine($"UART bridge PID {Environment.ProcessId}");
            Console.WriteLine($"Socket: {server.SocketPath}");
            Console.WriteLine("Oczekiwanie na klienta...");
        }

        await server.RunUntilStoppedAsync().ConfigureAwait(false);
    }

    private static bool TryReadRunningPid(string pidFile, out int pid)
    {
        pid = 0;
        if (!File.Exists(pidFile))
            return false;

        if (!int.TryParse(File.ReadAllText(pidFile).Trim(), out pid))
            return false;

        try
        {
            Process.GetProcessById(pid);
            return true;
        }
        catch (ArgumentException)
        {
            File.Delete(pidFile);
            return false;
        }
    }

    private static string FindRepoRoot()
    {
        string dir = AppContext.BaseDirectory;
        for (int i = 0; i < 8; i++)
        {
            if (File.Exists(Path.Combine(dir, "Cdp1802.sln")))
                return dir;

            string? parent = Directory.GetParent(dir)?.FullName;
            if (parent is null)
                break;
            dir = parent;
        }

        return Directory.GetCurrentDirectory();
    }

    private static string FindProjectPath()
    {
        string root = FindRepoRoot();
        return Path.Combine(root, "src", "Cdp1802.Cli", "Cdp1802.Cli.csproj");
    }
}