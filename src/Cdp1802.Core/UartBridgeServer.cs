using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;

namespace Cdp1802.Core;

public sealed class UartBridgeServer : IDisposable
{
    public const byte ShutdownSignal = 0xFF;
    public const string DefaultSocketPath = "/tmp/cdp1802-uart.sock";
    public const string DefaultPidFile = "/tmp/cdp1802-uart.pid";

    private readonly Cdp1802System _system;
    private readonly Uart _uart;
    private readonly Timer _timer;
    private readonly string _socketPath;
    private readonly string _pidFile;

    private Socket? _listener;
    private Socket? _client;
    private CancellationTokenSource? _cts;
    private Task? _emulationTask;
    private Task? _acceptTask;
    private volatile bool _running;

    public bool IsRunning => _running;
    public string SocketPath => _socketPath;

    public UartBridgeServer(string? socketPath = null, string? pidFile = null)
    {
        _socketPath = socketPath ?? DefaultSocketPath;
        _pidFile = pidFile ?? DefaultPidFile;
        _system = new Cdp1802System();
        _uart = new Uart();
        _timer = new Timer(prescaler: 2000);
        _system.RegisterPeripheral(_uart);
        _system.RegisterPeripheral(_timer);
    }

    public static void Daemonize()
    {
        if (!OperatingSystem.IsLinux())
            return;

        int pid = fork();
        if (pid > 0)
            Environment.Exit(0);

        if (pid < 0)
            throw new InvalidOperationException("fork() failed — cannot daemonize");
    }

    public void Start(string firmwarePath)
    {
        if (_running)
            throw new InvalidOperationException("UART bridge server is already running");

        LoadFirmware(firmwarePath);
        WritePidFile();

        if (File.Exists(_socketPath))
            File.Delete(_socketPath);

        _listener = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
        _listener.Bind(new UnixDomainSocketEndPoint(_socketPath));
        _listener.Listen(1);

        _cts = new CancellationTokenSource();
        _running = true;
        _emulationTask = Task.Run(EmulationLoop);
        _acceptTask = Task.Run(AcceptLoop);
    }

    public async Task RunUntilStoppedAsync()
    {
        if (_acceptTask is null)
            throw new InvalidOperationException("Server not started");

        try
        {
            await _acceptTask.ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
        }
    }

    public void RequestShutdown()
    {
        _cts?.Cancel();
    }

    public void Dispose()
    {
        _running = false;
        _cts?.Cancel();

        try { _client?.Shutdown(SocketShutdown.Both); } catch { }
        _client?.Dispose();
        _client = null;

        try { _listener?.Close(); } catch { }
        _listener?.Dispose();
        _listener = null;

        if (File.Exists(_socketPath))
        {
            try { File.Delete(_socketPath); } catch { }
        }

        RemovePidFile();
        _cts?.Dispose();
    }

    private void LoadFirmware(string firmwarePath)
    {
        if (!File.Exists(firmwarePath))
            throw new FileNotFoundException($"UART firmware not found: {firmwarePath}");

        _system.LoadBin(firmwarePath);
    }

    private void WritePidFile()
    {
        File.WriteAllText(_pidFile, Environment.ProcessId.ToString());
    }

    private void RemovePidFile()
    {
        if (!File.Exists(_pidFile))
            return;

        try
        {
            string pidText = File.ReadAllText(_pidFile).Trim();
            if (pidText == Environment.ProcessId.ToString())
                File.Delete(_pidFile);
        }
        catch
        {
        }
    }

    private async Task AcceptLoop()
    {
        CancellationToken token = _cts!.Token;

        while (!token.IsCancellationRequested)
        {
            Socket client;
            try
            {
                client = await _listener!.AcceptAsync(token).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            _client = client;
            try
            {
                await HandleClientAsync(client, token).ConfigureAwait(false);
            }
            finally
            {
                try { client.Shutdown(SocketShutdown.Both); } catch { }
                client.Dispose();
                if (ReferenceEquals(_client, client))
                    _client = null;
            }
        }
    }

    private async Task HandleClientAsync(Socket client, CancellationToken token)
    {
        byte[] rxBuffer = new byte[256];

        while (!token.IsCancellationRequested && client.Connected)
        {
            FlushTxToClient(client);

            if (!client.Poll(10_000, SelectMode.SelectRead))
            {
                await Task.Delay(1, token).ConfigureAwait(false);
                continue;
            }

            int received;
            try
            {
                received = client.Receive(rxBuffer, SocketFlags.None);
            }
            catch (SocketException)
            {
                break;
            }

            if (received == 0)
                break;

            for (int i = 0; i < received; i++)
            {
                byte b = rxBuffer[i];

                if (b == ShutdownSignal)
                {
                    await SendToClientAsync(client, "Zakonczono.\r\n").ConfigureAwait(false);
                    _cts!.Cancel();
                    return;
                }

                if (b == '\r')
                    continue;

                _uart.Receive(b);
            }
        }
    }

    private void EmulationLoop()
    {
        CancellationToken token = _cts!.Token;

        while (!token.IsCancellationRequested)
        {
            _system.Step();
            _timer.Tick();

            if (_uart.RxPending == 0)
                Thread.Sleep(0);
        }
    }

    private void FlushTxToClient(Socket client)
    {
        string tx = _uart.DrainTxOutput();
        if (tx.Length == 0)
            return;

        byte[] data = Encoding.ASCII.GetBytes(tx);
        try
        {
            client.Send(data);
        }
        catch (SocketException)
        {
            _cts?.Cancel();
        }
    }

    private static async Task SendToClientAsync(Socket client, string text)
    {
        byte[] data = Encoding.ASCII.GetBytes(text);
        await client.SendAsync(data, SocketFlags.None).ConfigureAwait(false);
    }

    [DllImport("libc", SetLastError = true)]
    private static extern int fork();
}