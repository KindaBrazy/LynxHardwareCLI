using LibreHardwareMonitor.Hardware;

namespace LynxHardwareCLI;

public class HardwareMonitorService : IDisposable
{
    private readonly Computer _computer;
    private readonly UpdateVisitor _updateVisitor;
    private bool _isOpen;

    public HardwareMonitorService()
    {
        _computer = new Computer
        {
            IsCpuEnabled = true,
            IsGpuEnabled = true,
            IsMemoryEnabled = true,
            IsMotherboardEnabled = true,
            IsStorageEnabled = true
        };
        _updateVisitor = new UpdateVisitor();
    }

    public void Dispose()
    {
        Close();
        GC.SuppressFinalize(this);
    }

    public void Open()
    {
        if (!_isOpen)
            try
            {
                _computer.Open();
                _isOpen = true;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error opening LibreHardwareMonitor: {ex.Message}");
            }
    }

    public void Close()
    {
        if (_isOpen)
        {
            _computer.Close();
            _isOpen = false;
        }
    }

    public HardwareReport GetHardwareReport(IEnumerable<string> requestedComponents)
    {
        if (!_isOpen)
        {
            Open();
            if (!_isOpen) return new HardwareReport { Timestamp = DateTime.UtcNow };
        }


        _computer.Accept(_updateVisitor);

        var report = new HardwareReport { Timestamp = DateTime.UtcNow };
        var activeComponents = requestedComponents
            .Select(c => c.Trim().ToLowerInvariant())
            .ToHashSet();

        var processAll = activeComponents.Contains("all") || !activeComponents.Any();
        if (processAll) activeComponents.UnionWith(new[] { "cpu", "gpu", "memory", "motherboard", "storage" });

        foreach (IHardware? hardware in _computer.Hardware)
        {
            HardwareItemInfo? itemInfo = null;

            switch (hardware.HardwareType)
            {
                case HardwareType.Cpu:
                    if (processAll || activeComponents.Contains("cpu"))
                        itemInfo = ProcessHardwareItem(hardware, "CPU");
                    if (itemInfo != null) report.CPU.Add(itemInfo);
                    break;

                case HardwareType.GpuNvidia:
                case HardwareType.GpuAmd:
                case HardwareType.GpuIntel:
                    if (processAll || activeComponents.Contains("gpu"))
                        itemInfo = ProcessHardwareItem(hardware, "GPU");
                    if (itemInfo != null) report.GPU.Add(itemInfo);
                    break;

                case HardwareType.Memory:
                    if (processAll || activeComponents.Contains("memory"))
                        itemInfo = ProcessHardwareItem(hardware, "Memory");
                    if (itemInfo != null) report.Memory.Add(itemInfo);
                    break;

                case HardwareType.Motherboard:
                    if (processAll || activeComponents.Contains("motherboard"))
                        itemInfo = ProcessHardwareItem(hardware, "Motherboard");
                    if (itemInfo != null) report.Motherboard.Add(itemInfo);
                    break;

                case HardwareType.Storage:
                    if (processAll || activeComponents.Contains("storage"))
                        itemInfo = ProcessHardwareItem(hardware, "Storage");
                    if (itemInfo != null) report.Storage.Add(itemInfo);
                    break;
            }
        }

        return report;
    }

    private HardwareItemInfo ProcessHardwareItem(IHardware hardwareItem, string itemTypeOverride)
    {
        var info = new HardwareItemInfo
        {
            Name = hardwareItem.Name,
            HardwareType = itemTypeOverride ?? hardwareItem.HardwareType.ToString()
        };

        foreach (ISensor? sensor in hardwareItem.Sensors)
            info.Sensors.Add(new SensorInfo
            {
                Name = sensor.Name,
                Value = sensor.Value,
                Type = sensor.SensorType.ToString(),
                Unit = GetSensorUnit(sensor),
                Identifier = sensor.Identifier.ToString()
            });

        if (hardwareItem.SubHardware != null)
            foreach (IHardware? subHardware in hardwareItem.SubHardware)
            {
                var subItemType = subHardware.HardwareType.ToString();
                if (hardwareItem.HardwareType == HardwareType.Cpu) subItemType = "CPU Core";

                info.SubHardware.Add(ProcessHardwareItem(subHardware, subItemType));
            }

        return info;
    }

    private string GetSensorUnit(ISensor sensor)
    {
        switch (sensor.SensorType)
        {
            case SensorType.Voltage: return "V";
            case SensorType.Current: return "A";
            case SensorType.Power: return "W";
            case SensorType.Clock: return "MHz";
            case SensorType.Temperature: return "°C";
            case SensorType.Load: return "%";
            case SensorType.Frequency: return "Hz";
            case SensorType.Fan: return "RPM";
            case SensorType.Flow: return "L/h";
            case SensorType.Control: return "%";
            case SensorType.Level: return "%";
            case SensorType.Factor: return "";
            case SensorType.Data:
            case SensorType.SmallData:
                if (sensor.Name.Contains("GB", StringComparison.OrdinalIgnoreCase)) return "GB";
                return "MB";
            case SensorType.Throughput:
                if (sensor.Name.Contains("GB/s", StringComparison.OrdinalIgnoreCase)) return "GB/s";
                if (sensor.Name.Contains("MB/s", StringComparison.OrdinalIgnoreCase)) return "MB/s";
                if (sensor.Name.Contains("KB/s", StringComparison.OrdinalIgnoreCase)) return "KB/s";
                return "B/s";
            case SensorType.Energy: return "Wh";
            case SensorType.Noise: return "dBA";
            default: return "";
        }
    }
}