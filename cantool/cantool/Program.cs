namespace cantool;

internal static class Program
{
    private static int Main(string[] args)
    {
        AppClock.Start();

        if (args.Length == 0)
        {
            return CommandHandlers.InteractiveMenu();
        }

        if (args[0] is "-h" or "--help" or "help")
        {
            PrintUsage();
            return 0;
        }

        try
        {
            return args[0].ToLowerInvariant() switch
            {
                "list" => CommandHandlers.ListDevices(),
                "bench-health" => CommandHandlers.BenchHealth(args[1..]),
                "wake-watch" => CommandHandlers.WakeWatch(args[1..]),
                "wake-matrix" => CommandHandlers.WakeMatrix(args[1..]),
                "wake-then-profile" => CommandHandlers.WakeThenProfile(args[1..]),
                "capture" => CommandHandlers.Capture(args[1..]),
                "send" => CommandHandlers.Send(args[1..]),
                "send-profile" => CommandHandlers.SendProfile(args[1..]),
                "summarize" => CommandHandlers.Summarize(args[1..]),
                "analyze-diag" => CommandHandlers.AnalyzeDiagnostics(args[1..]),
                "analyze-restart" => CommandHandlers.AnalyzeRestart(args[1..]),
                "analyze-wake" => CommandHandlers.AnalyzeWake(args[1..]),
                "profile-info" => CommandHandlers.ProfileInfo(args[1..]),
                _ => Fail($"unknown command: {args[0]}")
            };
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"error: {ex.Message}");
            return 1;
        }
    }

    private static void PrintUsage()
    {
        Console.WriteLine($$"""
            cantool - candleLight/gs_usb low-speed CAN helper

            Commands:
              <no args>   open interactive profile runner menu
              list
              bench-health [--seconds N]
              wake-watch [--seconds N] [--out PATH] [--preclear-ms N] [--no-preclear] [--stop-on-wake]
              wake-matrix [--phase-seconds N] [--out PATH] [--passive-only] [--include-nm-init] [--manual-advance] [--no-stop-on-wake]
              wake-then-profile --profile NAME [--seconds N] [--wait-rx-timeout-ms N] [--no-preclear]
              capture [--seconds N] [--out PATH] [--listen-only] [--no-flush] [--log-flush]
              send --id ID [--data HEX] [--period-ms N] [--count N] [--seconds N]
              send-profile --profile {{Profiles.ProfileNamesForUsage}} [--seconds N] [--file send_profile.can] [--wait-rx-timeout-ms N] [--no-flush] [--log-flush]
              summarize (--log PATH | --latest [--pattern GLOB]) [--ignore-tx-echo]
              analyze-diag (--log PATH | --latest [--pattern GLOB]) [--positive-only]
              analyze-restart (--log PATH | --latest [--pattern GLOB]) [--window-ms N] [--min-unique N]
              analyze-wake (--log PATH | --latest [--pattern GLOB])
              profile-info [--profile NAME]

            CAN timing is the known-good IPC low-speed profile:
              bitrate 33.333 kbit/s, brp=255, prop=1, phase1=13, phase2=5, sjw=4
            Interactive menu default: ipc-read-only-sniff, listen-only until Ctrl+C with no TX
            Interactive wake shortcuts:
              w = wake-watch --stop-on-wake, no TX; wait for ARMED, then toggle IPC pin 8 run/crank
              m = wake-matrix --manual-advance, guided pin 3 / pin 4 wake matrix with log phase markers
              d = wake then classic IPC diagnostics after first RX
              i = wake then isolated classic IPC diagnostics after first RX
              c = wake then confirmed IPC identity diagnostics after first RX
              s = wake then staged IPC simulator after first RX
            Use ipc-ack-sniff when the standalone IPC may need the adapter to ACK frames.
            """);
    }

    private static int Fail(string message)
    {
        Console.Error.WriteLine(message);
        PrintUsage();
        return 1;
    }
}
