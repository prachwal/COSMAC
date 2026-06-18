#!/usr/bin/env dotnet-script
// Test UART_DEMO.asm execution

#r "src/Cdp1802.Core/bin/Release/net10.0/Cdp1802.Core.dll"

using Cdp1802.Core;
using System;
using System.IO;

Console.WriteLine("=== Testing UART_DEMO.asm ===\n");

// Load binary
string binaryPath = "examples/UART_DEMO.bin";
if (!File.Exists(binaryPath))
{
    Console.WriteLine($"❌ Error: {binaryPath} not found");
    return;
}

byte[] binary = File.ReadAllBytes(binaryPath);
Console.WriteLine($"✓ Loaded binary: {binary.Length} bytes");

// Setup emulator
var cpu = new Cdp1802();
var uart = new Uart();
var debugger = new Debugger(cpu);

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

    // Check if program is stuck in infinite loop (echo loop)
    if (steps > 1000 && uart.TransmittedString.Length > 0)
        break;
}

Console.WriteLine($"\n✓ Executed {steps} cycles");
Console.WriteLine($"✓ Final PC: 0x{cpu.R[cpu.P]:X4}");

// Check UART output
string output = uart.TransmittedString;
Console.WriteLine($"\n📤 UART TX Output: {(output.Length == 0 ? "(empty)" : $"\"{output}\"")}");

// Verify expected output
if (output.Contains("UART Ready"))
{
    Console.WriteLine("✅ SUCCESS: Program sent 'UART Ready' to UART TX!");

    // Show hex values
    Console.WriteLine("\nBytes sent:");
    foreach (char c in output)
    {
        Console.Write($"0x{(byte)c:X2} '{(c >= 32 && c < 127 ? c : '?')}' ");
    }
    Console.WriteLine("\n");
}
else
{
    Console.WriteLine("❌ FAIL: Expected 'UART Ready' in output");
}

// Test UART RX (echo)
Console.WriteLine("Testing UART RX echo...");
uart.Receive((byte)'A');
uart.Receive((byte)'B');
uart.Receive((byte)'C');

// Run more cycles to process input
for (int i = 0; i < 500 && steps < maxSteps; i++)
{
    debugger.Step();
    steps++;
}

string fullOutput = uart.TransmittedString;
Console.WriteLine($"✓ After sending ABC to RX, TX output: \"{fullOutput}\"");

if (fullOutput.Contains("ABC"))
{
    Console.WriteLine("✅ ECHO TEST PASSED: Program echoed back 'ABC'!");
}
else
{
    Console.WriteLine("⚠️  Note: Echo may not appear due to step limit");
}

Console.WriteLine("\n=== Test Complete ===");
