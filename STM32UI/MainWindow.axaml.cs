using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.Interactivity;
using System;
using System.IO.Ports;
using System.Collections.Generic;

namespace STM32UI;

public partial class MainWindow : Window
{
    private SerialPort? _port;

    public MainWindow()
    {
        InitializeComponent();

        InitializeUiDefaults();
        OpenSerialPort();
    }

    private void InitializeUiDefaults()
    {
        // BMP180
        BmpTempValueText.Text = "-";
        BmpTempStateText.Text = "";
        BmpPressureValueText.Text = "-";
        BmpPressureStateText.Text = "";
        BmpAltitudeValueText.Text = "-";

        // TSL2591
        TslCh0ValueText.Text = "-";
        TslCh1ValueText.Text = "-";
        LightLevelBar.Value = 0;
        LightLevelText.Text = "0 %";

        // HC-SR04
        HcsrDistanceValueText.Text = "-";
        HcsrZoneText.Text = "-";

        SetHcsrIndicatorsIdle();
    }

    private void OpenSerialPort()
    {
        try
        {
            _port = new SerialPort("/dev/tty.usbserial-A50285BI", 115200);
            _port.NewLine = "\r\n";
            _port.DataReceived += Port_DataReceived;
            _port.Open();
        }
        catch (Exception)
        {
            // Port açılamazsa sadece değerler boş kalsın
            BmpTempValueText.Text = "PORT ERROR";
            BmpPressureValueText.Text = "PORT ERROR";
            BmpAltitudeValueText.Text = "PORT ERROR";

            TslCh0ValueText.Text = "PORT ERROR";
            TslCh1ValueText.Text = "PORT ERROR";
            LightLevelText.Text = "PORT ERROR";

            HcsrDistanceValueText.Text = "PORT ERROR";
            HcsrZoneText.Text = "PORT ERROR";
        }
    }

    private void Port_DataReceived(object? sender, SerialDataReceivedEventArgs e)
    {
        if (_port == null || !_port.IsOpen)
            return;

        try
        {
            string line = _port.ReadLine().Trim();

            if (string.IsNullOrWhiteSpace(line))
                return;

            Dispatcher.UIThread.Post(() =>
            {
                ParseIncomingLine(line);
            });
        }
        catch
        {
            // Şimdilik sessiz geçiyoruz
        }
    }

    private void ParseIncomingLine(string line)
    {

        string[] parts = line.Split('|', StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length == 0)
            return;

        string sensorType = parts[0].Trim().ToUpperInvariant();
        Dictionary<string, string> values = BuildValueMap(parts);

        if (sensorType == "BMP")
        {
            UpdateBmpUi(values);
        }
        else if (sensorType == "TSL")
        {
            UpdateTslUi(values);
        }
        else if (sensorType == "HCSR04")
        {
            UpdateHcsrUi(values);
        }
    }

    private Dictionary<string, string> BuildValueMap(string[] parts)
    {
        Dictionary<string, string> map = new();

        for (int i = 1; i < parts.Length; i++)
        {
            string[] kv = parts[i].Split(':', 2, StringSplitOptions.None);

            if (kv.Length == 2)
            {
                string key = kv[0].Trim().ToUpperInvariant();
                string value = kv[1].Trim();
                map[key] = value;
            }
        }

        return map;
    }

    private void UpdateBmpUi(Dictionary<string, string> values)
    {
        if (values.TryGetValue("TEMP", out string? temp))
            BmpTempValueText.Text = temp + " C";

        if (values.TryGetValue("TEMP_STATE", out string? tempState))
        {
            BmpTempStateText.Text = tempState;

            if (tempState == "WARMER")
                BmpTempStateText.Foreground = Brushes.OrangeRed;
            else if (tempState == "COLDER")
                BmpTempStateText.Foreground = Brushes.LightBlue;
            else
                BmpTempStateText.Foreground = Brushes.White;
        }

        if (values.TryGetValue("PRES", out string? pres))
            BmpPressureValueText.Text = pres + " Pa";

        if (values.TryGetValue("PRES_STATE", out string? presState))
        {
            BmpPressureStateText.Text = presState;

            if (presState == "UP")
                BmpPressureStateText.Foreground = Brushes.LimeGreen;
            else if (presState == "DOWN")
                BmpPressureStateText.Foreground = Brushes.Red;
            else
                BmpPressureStateText.Foreground = Brushes.White;
        }

        if (values.TryGetValue("ALT", out string? alt))
            BmpAltitudeValueText.Text = alt + " m";
    }

    private void UpdateTslUi(Dictionary<string, string> values)
    {
        if (values.TryGetValue("CH0", out string? ch0))
            TslCh0ValueText.Text = ch0;

        if (values.TryGetValue("CH1", out string? ch1))
            TslCh1ValueText.Text = ch1;

        if (values.TryGetValue("LEVEL", out string? levelText))
        {
            if (double.TryParse(levelText, out double level))
            {
                if (level < 0) level = 0;
                if (level > 100) level = 100;

                LightLevelBar.Value = level;
                LightLevelText.Text = $"{level:0} %";
            }
        }
    }

    private void UpdateHcsrUi(Dictionary<string, string> values)
    {
        if (values.TryGetValue("DIST", out string? dist))
            HcsrDistanceValueText.Text = dist + " cm";

        if (values.TryGetValue("ZONE", out string? zone))
        {
            string upperZone = zone.ToUpperInvariant();
            HcsrZoneText.Text = upperZone;

            if (upperZone == "RED")
            {
                HcsrZoneText.Foreground = Brushes.Red;
                SetHcsrIndicatorsRed();
            }
            else if (upperZone == "YELLOW")
            {
                HcsrZoneText.Foreground = Brushes.Gold;
                SetHcsrIndicatorsYellow();
            }
            else if (upperZone == "GREEN")
            {
                HcsrZoneText.Foreground = Brushes.LimeGreen;
                SetHcsrIndicatorsGreen();
            }
            else
            {
                HcsrZoneText.Foreground = Brushes.White;
                SetHcsrIndicatorsIdle();
            }
        }
    }

    private void SetHcsrIndicatorsIdle()
    {
        RedIndicator.Background = new SolidColorBrush(Color.Parse("#3A1A1A"));
        YellowIndicator.Background = new SolidColorBrush(Color.Parse("#3A3612"));
        GreenIndicator.Background = new SolidColorBrush(Color.Parse("#16351C"));
    }

    private void SetHcsrIndicatorsRed()
    {
        RedIndicator.Background = Brushes.Red;
        YellowIndicator.Background = new SolidColorBrush(Color.Parse("#3A3612"));
        GreenIndicator.Background = new SolidColorBrush(Color.Parse("#16351C"));
    }

    private void SetHcsrIndicatorsYellow()
    {
        RedIndicator.Background = new SolidColorBrush(Color.Parse("#3A1A1A"));
        YellowIndicator.Background = Brushes.Gold;
        GreenIndicator.Background = new SolidColorBrush(Color.Parse("#16351C"));
    }

    private void SetHcsrIndicatorsGreen()
    {
        RedIndicator.Background = new SolidColorBrush(Color.Parse("#3A1A1A"));
        YellowIndicator.Background = new SolidColorBrush(Color.Parse("#3A3612"));
        GreenIndicator.Background = Brushes.LimeGreen;
    }

    private void SendCommand(string command)
    {
        try
        {
            if (_port != null && _port.IsOpen)
            {
                Console.WriteLine("GONDERILEN: " + command);
                _port.WriteLine(command);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("SERIAL ERROR: " + ex.Message);
        }
    }

    private void BmpUseSensorButton_Click(object? sender, RoutedEventArgs e)
    {
        SendCommand("BMP");
    }
    
    
}