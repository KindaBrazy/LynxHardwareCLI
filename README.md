# LynxHardwareCLI .NET Monitor

A .NET 8 console application designed to retrieve and display hardware information from your system using
`LibreHardwareMonitorLib`. The output is provided in JSON format, making it easy to parse and integrate with other tools
or scripts.

## Features

- **Cross-Platform:** Built with .NET 8, aiming for compatibility with Windows, Linux, and macOS.
    * **Windows:** Fullest support for hardware detection.
    * **Linux:** Good support, may require elevated privileges for full data access.
    * **macOS:** Limited hardware data due to OS restrictions on direct hardware access.
- **Comprehensive Data:** Gathers information on:
    * CPU (Overall and Per-Core: Name, Load, Temperature, Clocks, Power)
    * GPU (Name, Load, Temperature, Clocks, Memory Usage, Power)
    * Memory (RAM Usage, Available Memory)
    * Motherboard (Name, various sensors if available)
    * Storage (Name, Temperature, Used Space - *detection can be system-dependent*)
    * Network Adapters (Name, Data Sent/Received - *detection can be system-dependent*)
- **Flexible Output Control:**
    * **One-Time Report:** Get a snapshot of current hardware status.
    * **Timed Reporting:** Continuously output hardware status at user-defined intervals (in milliseconds).
    * **Component Filtering:** Specify which hardware components to include in the report (e.g., only CPU and GPU).
- **JSON Output:** Clean, structured JSON output for easy programmatic use.

## Prerequisites

- **.NET 8.0 SDK or Runtime:**
    * To build the project, you need the **.NET 8.0 SDK**.
    * To run the compiled executable, users need the **.NET 8.0 Runtime** installed on their system.
    * Download from [dotnet.microsoft.com](https://dotnet.microsoft.com/download/dotnet/8.0)

## How to Build

1. **Clone the repository (or download the source files):**
   ```bash
   git clone https://github.com/KindaBrazy/LynxHardwareCLI
   cd LynxHardwareCLI
   ```
2. **Restore dependencies:**
   ```bash
   dotnet restore LynxHardwareCLI.csproj
   ```
3. **Build the project:**
   ```bash
   dotnet build LynxHardwareCLI.csproj -c Release
   ```
   The executable will be located in `bin/Release/net8.0/LynxHardwareCLI.exe` (Windows) or
   `bin/Release/net8.0/LynxHardwareCLI` (Linux/macOS).

## How to Run

Navigate to the directory containing the executable.

**Usage:**

```
LynxHardwareCLI.exe [--mode \<once|timed\>] [--interval \<milliseconds\>] [--components \<list\>]
```

**Arguments:**

- `--mode <once|timed>`:
    * `once`: (Default) Outputs the hardware information once and exits.
    * `timed`: Outputs hardware information repeatedly at the specified interval.
- `--interval <milliseconds>`:
    * Specifies the update interval in milliseconds for `timed` mode.
    * Defaults to `1000` milliseconds (1 second).
    * Minimum recommended interval is `50` milliseconds.
- `--components <list>`:
    * A comma or semicolon-separated list of components to include.
    * Valid components: `cpu`, `gpu`, `memory`, `motherboard`, `storage`, `network`, `all`.
    * Defaults to `all`.

**Examples:**

1. **Get all hardware information once:**
   ```bash
   ./LynxHardwareCLI
   # or on Windows:
   LynxHardwareCLI.exe
   ```

2. **Get CPU and GPU information every 500 milliseconds:**
   ```bash
   ./LynxHardwareCLI --mode timed --interval 500 --components cpu,gpu
   ```

3. **Get Memory and Storage information once:**
   ```bash
   ./LynxHardwareCLI --components memory,storage
   ```

4. **Get all hardware information every 2 seconds (2000 milliseconds):**
   ```bash
   ./LynxHardwareCLI --mode timed --interval 2000
   ```

**Note on Permissions:**
On Linux, you might need to run the application with `sudo` for full access to hardware sensors, especially for storage
and network details:

```bash
sudo ./LynxHardwareCLI --components all
````

On Windows, running as Administrator might provide more detailed information in some cases.

## Technologies Used

- **.NET 8**
- **LibreHardwareMonitorLib (0.9.4):** For accessing hardware information.
- **System.Text.Json:** For JSON serialization.

## Compatibility Notes & Troubleshooting

- **macOS Limitations:** Due to strict macOS restrictions on direct hardware access, the amount of data retrieved on
  macOS will be significantly less than on Windows or Linux. Many sensors may not be available.
- **Virtual Machines:** Hardware information reported within a VM might be limited or reflect the virtualized hardware
  rather than the host's physical hardware.

## Contributing

Contributions are welcome\! If you'd like to contribute, please feel free to fork the repository, make your changes, and
submit a pull request.

