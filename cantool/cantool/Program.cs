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
                "capture" => CommandHandlers.Capture(args[1..]),
                "send" => CommandHandlers.Send(args[1..]),
                "send-profile" => CommandHandlers.SendProfile(args[1..]),
                "summarize" => CommandHandlers.Summarize(args[1..]),
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
        Console.WriteLine("""
            cantool - candleLight/gs_usb low-speed CAN helper

            Commands:
              <no args>   open interactive profile sender menu
              list
              capture [--seconds N] [--out PATH] [--listen-only]
              send --id ID [--data HEX] [--period-ms N] [--count N] [--seconds N]
              send-profile --profile default-bench|firmware-wake|opel-reference-probe|gmlan29-probe|gmlan29-known-payloads|gmlan29-chime|gmlan29-speed-sweep [--seconds N]
              summarize --log PATH

            CAN timing is the known-good IPC low-speed profile:
              bitrate 33.333 kbit/s, brp=255, prop=1, phase1=13, phase2=5, sjw=4
            Interactive menu default duration: 15 seconds
            """);
    }

    private static int Fail(string message)
    {
        Console.Error.WriteLine(message);
        PrintUsage();
        return 1;
    }
}
