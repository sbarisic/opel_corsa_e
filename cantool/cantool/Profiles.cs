namespace cantool;

internal static class Profiles
{
    public const string DefaultBench = "default-bench";
    public const string FirmwareWake = "firmware-wake";
    public const string Gmlan29Probe = "gmlan29-probe";
    public const string OpelReferenceProbe = "opel-reference-probe";

    public static readonly ProfileDefinition[] Available =
    [
        new(DefaultBench, "Wake + older Opel probes + 29-bit GMLAN probes"),
        new(FirmwareWake, "BCM-derived wake/network-management frames"),
        new(OpelReferenceProbe, "Older Opel 11-bit wake, IPC-on, and needle sweep probes"),
        new(Gmlan29Probe, "29-bit GMLAN zero-payload BCM-to-IPC candidate probes"),
    ];

    public static List<ScheduledTxFrame> GetSchedule(string profile)
    {
        return profile.ToLowerInvariant() switch
        {
            DefaultBench => DefaultBenchSchedule(),
            FirmwareWake => FirmwareWakeSchedule(),
            Gmlan29Probe => Gmlan29ProbeSchedule(),
            OpelReferenceProbe => OpelReferenceProbeSchedule(),
            _ => throw new ArgumentException($"unknown profile: {profile}")
        };
    }

    private static List<ScheduledTxFrame> DefaultBenchSchedule()
    {
        var schedule = FirmwareWakeSchedule();
        schedule.AddRange(OpelReferenceProbeSchedule());
        schedule.AddRange(Gmlan29ProbeSchedule().Select(frame => frame with
        {
            InitialDelay = frame.InitialDelay + TimeSpan.FromMilliseconds(2200)
        }));
        return schedule;
    }

    private static List<ScheduledTxFrame> FirmwareWakeSchedule()
    {
        return
        [
            new ScheduledTxFrame(0x100, [], TimeSpan.FromMilliseconds(500), false, "firmware-confirmed standard DLC0 network wake object"),
            new ScheduledTxFrame(0x13FFE040, [], TimeSpan.FromMilliseconds(500), true, "firmware-confirmed extended DLC0 IPC heartbeat candidate"),
            new ScheduledTxFrame(0x621, CliOptions.ParseHexData("0140000000000000"), TimeSpan.FromMilliseconds(1000), false, "firmware-shaped network-management initiate"),
            new ScheduledTxFrame(0x621, CliOptions.ParseHexData("0040000000000000"), TimeSpan.FromMilliseconds(500), false, "firmware-shaped network-management continue"),
        ];
    }

    private static List<ScheduledTxFrame> OpelReferenceProbeSchedule()
    {
        return
        [
            new ScheduledTxFrame(0x278, CliOptions.ParseHexData("0048500000000000"), TimeSpan.FromMilliseconds(1000), false, "older Opel low-speed GMLAN wake reference", TimeSpan.FromMilliseconds(100)),
            new ScheduledTxFrame(0x0AA, CliOptions.ParseHexData("74A13B"), TimeSpan.FromMilliseconds(200), false, "older Opel IPC-on reference", TimeSpan.FromMilliseconds(150)),
            new ScheduledTxFrame(0x06C, CliOptions.ParseHexData("000000000000001E"), TimeSpan.FromMilliseconds(2000), false, "older Opel direct needle sweep low", TimeSpan.FromMilliseconds(300)),
            new ScheduledTxFrame(0x06C, CliOptions.ParseHexData("002000002000001E"), TimeSpan.FromMilliseconds(2000), false, "older Opel direct needle sweep quarter", TimeSpan.FromMilliseconds(800)),
            new ScheduledTxFrame(0x06C, CliOptions.ParseHexData("004000004000001E"), TimeSpan.FromMilliseconds(2000), false, "older Opel direct needle sweep half", TimeSpan.FromMilliseconds(1300)),
            new ScheduledTxFrame(0x06C, CliOptions.ParseHexData("008000008000001E"), TimeSpan.FromMilliseconds(2000), false, "older Opel direct needle sweep high", TimeSpan.FromMilliseconds(1800)),
        ];
    }

    private static List<ScheduledTxFrame> Gmlan29ProbeSchedule()
    {
        return
        [
            new ScheduledTxFrame(0x10002040, CliOptions.ParseHexData("0000000000000000"), TimeSpan.FromMilliseconds(500), true, "GMLAN 29-bit arbid 0x001 system power mode, guessed source 0x40"),
            new ScheduledTxFrame(0x10030040, CliOptions.ParseHexData("0000000000000000"), TimeSpan.FromMilliseconds(1000), true, "GMLAN 29-bit arbid 0x018 battery voltage, guessed source 0x40", TimeSpan.FromMilliseconds(100)),
            new ScheduledTxFrame(0x10050040, CliOptions.ParseHexData("0000000000000000"), TimeSpan.FromMilliseconds(1000), true, "GMLAN 29-bit arbid 0x028 vehicle speed information, guessed source 0x40", TimeSpan.FromMilliseconds(200)),
            new ScheduledTxFrame(0x10052040, CliOptions.ParseHexData("0000000000000000"), TimeSpan.FromMilliseconds(1000), true, "GMLAN 29-bit arbid 0x029 engine information 1, guessed source 0x40", TimeSpan.FromMilliseconds(300)),
            new ScheduledTxFrame(0x1004C040, CliOptions.ParseHexData("0000000000000000"), TimeSpan.FromMilliseconds(1500), true, "GMLAN 29-bit arbid 0x026 fuel information, guessed source 0x40", TimeSpan.FromMilliseconds(400)),
            new ScheduledTxFrame(0x1004E040, CliOptions.ParseHexData("0000000000000000"), TimeSpan.FromMilliseconds(1500), true, "GMLAN 29-bit arbid 0x027 odometer/brake/wash level, guessed source 0x40", TimeSpan.FromMilliseconds(500)),
            new ScheduledTxFrame(0x1005E040, CliOptions.ParseHexData("0000000000000000"), TimeSpan.FromMilliseconds(1500), true, "GMLAN 29-bit arbid 0x02F brake/cruise status, guessed source 0x40", TimeSpan.FromMilliseconds(600)),
        ];
    }
}

internal sealed record ProfileDefinition(string Name, string Description);
