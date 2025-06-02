using System.Text.Json;

namespace LynxHardwareCLI;

internal class Program
{
    private static void Main(string[] args)
    {
        var mode = "once";
        var intervalMilliseconds = 1000;
        var componentsToInclude = new List<string> { "all" };


        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i].ToLowerInvariant();
            switch (arg)
            {
                case "--mode":
                    if (i + 1 < args.Length)
                    {
                        mode = args[++i].ToLowerInvariant();
                        if (mode != "once" && mode != "timed")
                        {
                            Console.Error.WriteLine($"Invalid mode: {mode}. Use 'once' or 'timed'.");
                            PrintUsage();
                            return;
                        }
                    }
                    else
                    {
                        Console.Error.WriteLine("Missing mode value for --mode.");
                        PrintUsage();
                        return;
                    }

                    break;
                case "--interval":
                    if (i + 1 < args.Length && int.TryParse(args[++i], out var val))
                    {
                        intervalMilliseconds = Math.Max(50, val);
                    }
                    else
                    {
                        Console.Error.WriteLine(
                            "Invalid or missing interval value for --interval (must be an integer for milliseconds).");
                        PrintUsage();
                        return;
                    }

                    break;
                case "--components":
                    if (i + 1 < args.Length)
                    {
                        componentsToInclude = args[++i]
                            .Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                            .Select(s => s.Trim().ToLowerInvariant())
                            .Where(s => !string.IsNullOrEmpty(s))
                            .ToList();
                        if (!componentsToInclude.Any()) componentsToInclude.Add("all");
                    }
                    else
                    {
                        Console.Error.WriteLine("Missing component list for --components.");
                        PrintUsage();
                        return;
                    }

                    break;
                default:
                    if (arg.StartsWith("-"))
                    {
                        Console.Error.WriteLine($"Unknown argument: {args[i]}");
                        PrintUsage();
                        return;
                    }

                    break;
            }
        }

        var validComponents = new HashSet<string>
            { "cpu", "gpu", "memory", "motherboard", "storage", "network", "all" };
        foreach (var comp in componentsToInclude)
            if (!validComponents.Contains(comp))
            {
                Console.Error.WriteLine($"Invalid component specified: {comp}");
                PrintUsage();
                return;
            }

        if (!componentsToInclude.Any()) componentsToInclude.Add("all");

        var jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true
        };

        using (var monitorService = new HardwareMonitorService())
        {
            monitorService.Open();

            if (mode == "once")
            {
                HardwareReport report = monitorService.GetHardwareReport(componentsToInclude);
                var json = JsonSerializer.Serialize(report, jsonOptions);
                Console.WriteLine(json);
            }
            else if (mode == "timed")
            {
                Console.WriteLine(
                    $"Starting timed monitoring. Interval: {intervalMilliseconds}ms. Components: {string.Join(", ", componentsToInclude)}. Press Ctrl+C to exit.");

                var cts = new CancellationTokenSource();
                Console.CancelKeyPress += (_, eventArgs) =>
                {
                    Console.WriteLine("\nExiting timed mode...");
                    eventArgs.Cancel = true;
                    cts.Cancel();
                };

                try
                {
                    while (!cts.Token.IsCancellationRequested)
                    {
                        HardwareReport report = monitorService.GetHardwareReport(componentsToInclude);
                        var json = JsonSerializer.Serialize(report, jsonOptions);
                        Console.WriteLine(json);
                        if (cts.Token.IsCancellationRequested) break;

                        Task.Delay(intervalMilliseconds, cts.Token).Wait(cts.Token);
                    }
                }
                catch (OperationCanceledException)
                {
                    Console.WriteLine("Timed mode operation cancelled.");
                }
                catch (AggregateException ae) when (ae.InnerExceptions.OfType<TaskCanceledException>().Any())
                {
                    Console.WriteLine("Timed mode task cancelled.");
                }
            }
        }
    }

    private static void PrintUsage()
    {
        Console.WriteLine(
            "\nUsage: HardwareInfo.exe [--mode <once|timed>] [--interval <milliseconds>] [--components <list>]");
        Console.WriteLine(
            "  <list> is a comma or semicolon separated list of: cpu,gpu,memory,motherboard,storage,network,all");
        Console.WriteLine("Defaults: --mode once --components all");
        Console.WriteLine(
            "If --mode is timed, --interval defaults to 1000 milliseconds. Minimum interval is 50ms.");
        Console.WriteLine("\nExamples:");
        Console.WriteLine("  HardwareInfo.exe");
        Console.WriteLine("  HardwareInfo.exe --mode timed --interval 500");
        Console.WriteLine("  HardwareInfo.exe --components cpu,gpu,network");
        Console.WriteLine(
            "  HardwareInfo.exe --mode timed --interval 2000 --components memory;storage");
    }
}