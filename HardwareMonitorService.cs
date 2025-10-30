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
            IsStorageEnabled = true,
            IsNetworkEnabled = true,
            IsBatteryEnabled = true,
            IsControllerEnabled = true,
            IsPsuEnabled = true
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
        if (processAll)
            activeComponents.UnionWith(new[]
                { "cpu", "gpu", "memory", "motherboard", "storage", "network", "battery", "controller", "psu" });

        foreach (IHardware hardware in _computer.Hardware)
        {
            var (targetList, componentName, itemType) = hardware.HardwareType switch
            {
                HardwareType.Cpu => (report.CPU, "cpu", "CPU"),
                HardwareType.GpuNvidia or HardwareType.GpuAmd or HardwareType.GpuIntel => (report.GPU, "gpu", "GPU"),
                HardwareType.Memory => (report.Memory, "memory", "Memory"),
                HardwareType.Motherboard => (report.Motherboard, "motherboard", "Motherboard"),
                HardwareType.Storage => (report.Storage, "storage", "Storage"),
                HardwareType.Network => (report.Network, "network", "Network"),
                HardwareType.Battery => (report.Battery, "battery", "Battery"),
                HardwareType.EmbeddedController => (report.Controller, "controller", "Controller"),
                HardwareType.Psu => (report.Psu, "psu", "PSU"),
                _ => (null, null, null)
            };

            if (targetList != null && activeComponents.Contains(componentName!))
            {
                HardwareItemInfo itemInfo = ProcessHardwareItem(hardware, itemType!);
                targetList.Add(itemInfo);
            }
        }

        return report;
    }

    private HardwareItemInfo ProcessHardwareItem(IHardware hardwareItem, string itemTypeOverride)
    {
        var info = new HardwareItemInfo
        {
            Name = hardwareItem.Name,
            HardwareType = itemTypeOverride
        };

        foreach (ISensor sensor in hardwareItem.Sensors)
        {
            var sensorValue = sensor.Value;
            
            float? sanitizedValue = (sensorValue.HasValue && !float.IsInfinity(sensorValue.Value) && !float.IsNaN(sensorValue.Value))
                ? sensorValue
                : null;

            info.Sensors.Add(new SensorInfo
            {
                Name = sensor.Name,
                Value = sanitizedValue,
                Type = sensor.SensorType.ToString(),
                Unit = GetSensorUnit(sensor),
                Identifier = sensor.Identifier.ToString()
            });
        }

        foreach (IHardware subHardware in hardwareItem.SubHardware)
        {
            var subItemType = subHardware.HardwareType.ToString();
            if (hardwareItem.HardwareType == HardwareType.Cpu) subItemType = "CPU Core";

            info.SubHardware.Add(ProcessHardwareItem(subHardware, subItemType));
        }

        return info;
    }

    private string GetSensorUnit(ISensor sensor)
    {
        return sensor.SensorType switch
        {
            SensorType.Voltage => "V",
            SensorType.Current => "A",
            SensorType.Power => "W",
            SensorType.Clock => "MHz",
            SensorType.Temperature => "°C",
            SensorType.Load => "%",
            SensorType.Frequency => "Hz",
            SensorType.Fan => "RPM",
            SensorType.Flow => "L/h",
            SensorType.Control => "%",
            SensorType.Level => "%",
            SensorType.Energy => "Wh",
            SensorType.Noise => "dBA",
            SensorType.Data or SensorType.SmallData => sensor.Name.Contains("GB", StringComparison.OrdinalIgnoreCase) ? "GB" : "MB",
            SensorType.Throughput => sensor.Name switch
            {
                var n when n.Contains("GB/s", StringComparison.OrdinalIgnoreCase) => "GB/s",
                var n when n.Contains("MB/s", StringComparison.OrdinalIgnoreCase) => "MB/s",
                var n when n.Contains("KB/s", StringComparison.OrdinalIgnoreCase) => "KB/s",
                _ => "B/s"
            },
            _ => string.Empty
        };
    }
}