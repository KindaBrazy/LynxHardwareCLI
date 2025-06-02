namespace HardwareMonitor;

public class SensorInfo
{
    public string Name { get; set; } = string.Empty;
    public float? Value { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public string Identifier { get; set; } = string.Empty;
}

public class HardwareItemInfo
{
    public string Name { get; set; } = string.Empty;
    public string HardwareType { get; set; } = string.Empty;
    public List<SensorInfo> Sensors { get; set; } = new();

    public List<HardwareItemInfo> SubHardware { get; set; } = new();
}

public class HardwareReport
{
    public DateTime Timestamp { get; set; }
    public List<HardwareItemInfo> CPU { get; set; } = new();
    public List<HardwareItemInfo> GPU { get; set; } = new();
    public List<HardwareItemInfo> Memory { get; set; } = new();
    public List<HardwareItemInfo> Motherboard { get; set; } = new();
    public List<HardwareItemInfo> Storage { get; set; } = new();
    public List<HardwareItemInfo> Network { get; set; } = new();
}