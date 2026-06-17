using Cdp1802.Core;
using Xunit;
using Timer = Cdp1802.Core.Timer;
using Cdp1802Cpu = Cdp1802.Core.Cdp1802;

namespace Cdp1802.Tests;

public class PeripheralTests
{
    #region UART Tests

    [Fact]
    public void UART_TransmitSendsByte()
    {
        // Arrange
        var uart = new Uart();
        byte testByte = 0x41; // 'A'

        // Act
        uart.Write(0, testByte); // TX register

        // Assert
        Assert.Equal(testByte, uart.LastTransmittedByte);
        Assert.True(uart.HasTransmitted);
    }

    [Fact]
    public void UART_ReceiveGetsByte()
    {
        // Arrange
        var uart = new Uart();
        uart.Receive(0x42); // 'B'

        // Act
        byte result = uart.Read(1); // RX register

        // Assert
        Assert.Equal(0x42, result);
    }

    [Fact]
    public void UART_StatusShowsIdle()
    {
        // Arrange
        var uart = new Uart();

        // Act
        byte status = uart.Read(2);

        // Assert - No TX busy, no RX available
        Assert.Equal(0x00, status);
    }

    [Fact]
    public void UART_StatusShowsRXAvailable()
    {
        // Arrange
        var uart = new Uart();
        uart.Receive(0x42);

        // Act
        byte status = uart.Read(2);

        // Assert - RX available (bit 1)
        Assert.Equal(0x02, status);
    }

    [Fact]
    public void UART_ResetClearsState()
    {
        // Arrange
        var uart = new Uart();
        uart.Write(0, 0x41);
        uart.Receive(0x42);

        // Act
        uart.Reset();

        // Assert
        Assert.False(uart.HasTransmitted);
        Assert.Equal(0, uart.LastTransmittedByte);
    }

    #endregion

    #region Timer Tests

    [Fact]
    public void Timer_IncrementsOnTick()
    {
        // Arrange
        var timer = new Timer();

        // Act
        timer.Tick();
        timer.Tick();
        timer.Tick();

        // Assert
        Assert.Equal(3UL, timer.Counter);
    }

    [Fact]
    public void Timer_PrescalerDividesClock()
    {
        // Arrange
        var timer = new Timer(prescaler: 4);

        // Act
        for (int i = 0; i < 4; i++)
            timer.Tick();

        // Assert
        Assert.Equal(1UL, timer.Counter);
        Assert.False(timer.InterruptPending);
    }

    [Fact]
    public void Timer_InterruptOnCompare()
    {
        // Arrange
        var timer = new Timer();
        timer.CompareValue = 5;

        // Act
        for (int i = 0; i < 5; i++)
            timer.Tick();

        // Assert
        Assert.True(timer.InterruptPending);
    }

    [Fact]
    public void Timer_ClearOnReadControl()
    {
        // Arrange
        var timer = new Timer();
        timer.CompareValue = 3;
        for (int i = 0; i < 3; i++)
            timer.Tick();

        // Act
        timer.Read(2); // Read control clears interrupt

        // Assert
        Assert.False(timer.InterruptPending);
    }

    [Fact]
    public void Timer_WriteCompareValue()
    {
        // Arrange
        var timer = new Timer();

        // Act
        timer.Write(1, 0x0A); // Set compare value

        // Assert
        Assert.Equal(0x0A, timer.CompareValue);
    }

    [Fact]
    public void Timer_Reset()
    {
        // Arrange
        var timer = new Timer();
        timer.CompareValue = 5;
        for (int i = 0; i < 5; i++)
            timer.Tick();

        // Act
        timer.Reset();

        // Assert
        Assert.Equal(0UL, timer.Counter);
        Assert.Equal(0, timer.CompareValue);
        Assert.False(timer.InterruptPending);
    }

    #endregion

    #region GPIO Tests

    [Fact]
    public void GPIO_ReadPort()
    {
        // Arrange
        var gpio = new Gpio();
        gpio.SetInput(0x55);

        // Act
        byte result = gpio.Read(0); // Input port

        // Assert
        Assert.Equal(0x55, result);
    }

    [Fact]
    public void GPIO_WritePort()
    {
        // Arrange
        var gpio = new Gpio();

        // Act
        gpio.Write(1, 0xAA); // Output port (offset 1)

        // Assert
        Assert.Equal(0xAA, gpio.OutputValue);
    }

    [Fact]
    public void GPIO_DirectionRegister()
    {
        // Arrange
        var gpio = new Gpio();

        // Act
        gpio.Write(2, 0x0F); // Lower 4 bits as output

        // Assert
        Assert.Equal(0x0F, gpio.DirectionMask);
    }

    [Fact]
    public void GPIO_PinState()
    {
        // Arrange
        var gpio = new Gpio();
        gpio.SetInput(0xFF);

        // Act & Assert
        Assert.True(gpio.GetPin(0));
        Assert.True(gpio.GetPin(7));
        Assert.False(gpio.GetPin(8)); // Out of range
    }

    [Fact]
    public void GPIO_Reset()
    {
        // Arrange
        var gpio = new Gpio();
        gpio.Write(0, 0xAA);
        gpio.SetInput(0x55);

        // Act
        gpio.Reset();

        // Assert
        Assert.Equal(0, gpio.OutputValue);
        Assert.Equal(0, gpio.DirectionMask);
    }

    #endregion

    #region Processor + Peripheral Integration

    [Fact]
    public void Processor_CanRegisterPeripheral()
    {
        // Arrange
        var cpu = new Cdp1802Cpu();
        var uart = new Uart();

        // Act
        cpu.RegisterPeripheral(uart);

        // Assert - Write to UART TX (0x0100)
        cpu.WriteMemory(0x0100, 0x41);
        Assert.Equal(0x41, uart.LastTransmittedByte);
    }

    [Fact]
    public void Processor_PeripheralWriteDoesNotAffectRAM()
    {
        // Arrange
        var cpu = new Cdp1802Cpu();
        var gpio = new Gpio();
        cpu.RegisterPeripheral(gpio);

        // Act
        cpu.WriteMemory(0x0301, 0xAA); // GPIO output port (offset 1)

        // Assert
        Assert.Equal(0xAA, gpio.OutputValue);
        Assert.Equal(0, cpu.Memory[0x0300]); // RAM unchanged
    }

    [Fact]
    public void Processor_PeripheralReadInterceptsRAM()
    {
        // Arrange
        var cpu = new Cdp1802Cpu();
        var gpio = new Gpio();
        cpu.RegisterPeripheral(gpio);
        gpio.SetInput(0x55);

        // Act
        byte result = cpu.ReadMemory(0x0300); // GPIO input port

        // Assert
        Assert.Equal(0x55, result);
    }

    [Fact]
    public void Processor_RAMAccessUnaffectedByPeripherals()
    {
        // Arrange
        var cpu = new Cdp1802Cpu();
        var uart = new Uart();
        cpu.RegisterPeripheral(uart);
        cpu.Memory[0x0500] = 0x42;

        // Act
        byte result = cpu.ReadMemory(0x0500);

        // Assert
        Assert.Equal(0x42, result);
    }

    [Fact]
    public void Processor_MultiplePeripherals()
    {
        // Arrange
        var cpu = new Cdp1802Cpu();
        var uart = new Uart();
        var timer = new Timer();
        var gpio = new Gpio();
        cpu.RegisterPeripheral(uart);
        cpu.RegisterPeripheral(timer);
        cpu.RegisterPeripheral(gpio);

        // Act & Assert - UART
        cpu.WriteMemory(0x0100, 0x41);
        Assert.Equal(0x41, uart.LastTransmittedByte);

        // Timer
        timer.CompareValue = 5;
        for (int i = 0; i < 5; i++) timer.Tick();
        byte timerStatus = cpu.ReadMemory(0x0202);
        Assert.Equal(0x01, timerStatus & 0x01); // Interrupt pending

        // GPIO
        gpio.SetInput(0xFF);
        byte gpioInput = cpu.ReadMemory(0x0300);
        Assert.Equal(0xFF, gpioInput);
    }

    [Fact]
    public void Processor_TimerInterruptConnectsToCPU()
    {
        // Arrange
        var cpu = new Cdp1802Cpu();
        var timer = new Timer();
        cpu.RegisterPeripheral(timer);
        timer.CompareValue = 3;

        // Act
        for (int i = 0; i < 3; i++) timer.Tick();

        // Assert - Timer interrupt pending
        Assert.True(timer.InterruptPending);
        byte status = cpu.ReadMemory(0x0202);
        Assert.Equal(0x01, status & 0x01);
    }

    #endregion
}
