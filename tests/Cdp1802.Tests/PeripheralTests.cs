using Cdp1802.Core;
using Xunit;
using Timer = Cdp1802.Core.Timer;

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
}
