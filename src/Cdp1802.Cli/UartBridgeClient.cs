using System.Net.Sockets;
using System.Text;
using Cdp1802.Core;

namespace Cdp1802.Cli;

public static class UartBridgeClient
{
    public static async Task RunAsync(string? socketPath = null)
    {
        string path = socketPath ?? UartBridgeServer.DefaultSocketPath;

        if (!File.Exists(path))
        {
            Console.WriteLine($"Serwer UART nie dziala (brak {path}).");
            Console.WriteLine("Uruchom: dotnet run --project src/Cdp1802.Cli -- --uart-server");
            return;
        }

        using var client = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
        client.Connect(new UnixDomainSocketEndPoint(path));

        Console.WriteLine("Polaczono z emulatorem UART. Ctrl+C zamyka serwer w tle.");
        Console.WriteLine("Polecenia (firmware 1802): !e = uptime, pozostale — echo.");
        Console.WriteLine();

        using var cts = new CancellationTokenSource();
        bool shutdownRequested = false;

        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            if (!shutdownRequested)
            {
                shutdownRequested = true;
                cts.Cancel();
            }
        };

        Task readTask = Task.Run(() => ReadFromServerAsync(client, cts.Token), cts.Token);
        Task writeTask = Task.Run(() => WriteToServer(client, cts.Token), cts.Token);

        try
        {
            await Task.WhenAny(readTask, writeTask).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
        }

        if (shutdownRequested)
        {
            try
            {
                client.Send([UartBridgeServer.ShutdownSignal]);
                await WaitForShutdownAckAsync(client).ConfigureAwait(false);
            }
            catch (SocketException)
            {
            }
        }

        cts.Cancel();

        try { await readTask.ConfigureAwait(false); } catch { }
        try { await writeTask.ConfigureAwait(false); } catch { }

        if (shutdownRequested)
            Console.WriteLine("\nZakonczono.");
    }

    private static async Task ReadFromServerAsync(Socket client, CancellationToken token)
    {
        byte[] buffer = new byte[1024];

        while (!token.IsCancellationRequested && client.Connected)
        {
            if (!client.Poll(50_000, SelectMode.SelectRead))
                continue;

            int received;
            try
            {
                received = await client.ReceiveAsync(buffer, SocketFlags.None, token).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (SocketException)
            {
                break;
            }

            if (received == 0)
                break;

            Console.Write(Encoding.ASCII.GetString(buffer, 0, received));
        }
    }

    private static void WriteToServer(Socket client, CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            string? line = Console.ReadLine();
            if (line is null)
                break;

            byte[] payload = Encoding.ASCII.GetBytes(line + "\n");
            try
            {
                client.Send(payload);
            }
            catch (SocketException)
            {
                break;
            }
        }
    }

    private static async Task WaitForShutdownAckAsync(Socket client)
    {
        byte[] buffer = new byte[256];
        var deadline = DateTime.UtcNow.AddSeconds(2);

        while (DateTime.UtcNow < deadline)
        {
            if (!client.Poll(100_000, SelectMode.SelectRead))
            {
                await Task.Delay(50).ConfigureAwait(false);
                continue;
            }

            int received = client.Receive(buffer, SocketFlags.None);
            if (received <= 0)
                break;

            string text = Encoding.ASCII.GetString(buffer, 0, received);
            Console.Write(text);
            if (text.Contains("Zakonczono", StringComparison.Ordinal))
                return;
        }
    }
}