using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Cdp1802.Core;

/// <summary>
/// Remote debugger for CDP1802.
/// Exposes debugger commands over TCP socket.
/// </summary>
public class RemoteDebugger : IDisposable
{
    private readonly Core.Cdp1802 _cpu;
    private readonly Debugger _debugger;
    private TcpListener? _listener;
    private TcpClient? _client;
    private NetworkStream? _stream;
    private bool _running;
    private readonly StringBuilder _output = new();

    public int Port { get; }
    public bool IsConnected => _client?.Connected ?? false;
    public bool IsRunning => _running;

    public RemoteDebugger(Core.Cdp1802 cpu, int port = 6180)
    {
        _cpu = cpu;
        _debugger = new Debugger(cpu);
        Port = port;
    }

    /// <summary>
    /// Start listening for connections.
    /// </summary>
    public void Start()
    {
        _listener = new TcpListener(IPAddress.Any, Port);
        _listener.Start();
        _running = true;

        Task.Run(AcceptLoop);
    }

    /// <summary>
    /// Stop the server.
    /// </summary>
    public void Stop()
    {
        _running = false;
        _client?.Close();
        _listener?.Stop();
    }

    private async Task AcceptLoop()
    {
        while (_running)
        {
            try
            {
                _client = await _listener!.AcceptTcpClientAsync();
                _stream = _client.GetStream();
                await HandleClient();
            }
            catch { }
        }
    }

    private async Task HandleClient()
    {
        byte[] buffer = new byte[1024];
        await Send("CDP1802 Remote Debugger v1.0\r\n");

        while (_client?.Connected == true && _running)
        {
            try
            {
                int bytesRead = await _stream!.ReadAsync(buffer, 0, buffer.Length);
                if (bytesRead == 0) break;

                string command = Encoding.ASCII.GetString(buffer, 0, bytesRead).Trim();
                string response = ProcessCommand(command);
                await Send(response);
            }
            catch { break; }
        }
    }

    private async Task Send(string text)
    {
        if (_stream == null) return;
        byte[] data = Encoding.ASCII.GetBytes(text);
        await _stream.WriteAsync(data, 0, data.Length);
    }

    /// <summary>
    /// Process a debugger command.
    /// </summary>
    public string ProcessCommand(string command)
    {
        _output.Clear();
        string[] parts = command.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0) return "ERR: Empty command\r\n";

        string cmd = parts[0].ToLower();
        switch (cmd)
        {
            case "step":
            case "s":
                _debugger.Step();
                _output.AppendLine($"OK PC={_cpu.R[_cpu.P]:X4} D={_cpu.D:X2}");
                break;

            case "run":
                int count = parts.Length > 1 ? int.Parse(parts[1]) : 100;
                _debugger.Run(count);
                _output.AppendLine($"OK PC={_cpu.R[_cpu.P]:X4} cycles={_cpu.TotalCycles}");
                break;

            case "regs":
                _output.AppendLine($"D={_cpu.D:X2} DF={(_cpu.DF?1:0)} P={_cpu.P} X={_cpu.X}");
                for (int i = 0; i < 16; i += 4)
                    _output.AppendLine($"R{i:X}=0x{_cpu.R[i]:X4} R{i+1:X}=0x{_cpu.R[i+1]:X4} R{i+2:X}=0x{_cpu.R[i+2]:X4} R{i+3:X}=0x{_cpu.R[i+3]:X4}");
                break;

            case "mem":
                ushort addr = parts.Length > 1 ? Convert.ToUInt16(parts[1], 16) : _cpu.R[_cpu.P];
                int len = parts.Length > 2 ? int.Parse(parts[2]) : 16;
                for (int i = 0; i < len; i += 16)
                {
                    _output.Append($"{addr + i:X4}: ");
                    for (int j = 0; j < 16 && i + j < len; j++)
                        _output.Append($"{_cpu.Memory[addr + i + j]:X2} ");
                    _output.AppendLine();
                }
                break;

            case "break":
                if (parts.Length > 1)
                {
                    ushort bp = Convert.ToUInt16(parts[1], 16);
                    if (_debugger.HasBreakpoint(bp))
                    {
                        _debugger.RemoveBreakpoint(bp);
                        _output.AppendLine($"BREAK Removed 0x{bp:X4}");
                    }
                    else
                    {
                        _debugger.AddBreakpoint(bp);
                        _output.AppendLine($"BREAK Added 0x{bp:X4}");
                    }
                }
                else
                {
                    foreach (var b in _debugger.Breakpoints)
                        _output.AppendLine($"  0x{b:X4}");
                }
                break;

            case "write":
                if (parts.Length >= 3)
                {
                    ushort wAddr = Convert.ToUInt16(parts[1], 16);
                    byte wVal = Convert.ToByte(parts[2], 16);
                    _cpu.Memory[wAddr] = wVal;
                    _output.AppendLine($"OK Write 0x{wVal:X2} to 0x{wAddr:X4}");
                }
                break;

            case "reset":
                _cpu.Reset();
                _output.AppendLine("OK Reset");
                break;

            case "quit":
            case "q":
                _running = false;
                _output.AppendLine("OK Bye");
                break;

            default:
                _output.AppendLine("ERR: Unknown command");
                _output.AppendLine("Commands: step [n], run [n], regs, mem [addr] [len], break [addr], write [addr] [val], reset, quit");
                break;
        }

        return _output.ToString();
    }

    public void Dispose()
    {
        Stop();
        _client?.Dispose();
        _stream?.Dispose();
    }
}
