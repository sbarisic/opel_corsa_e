namespace cantool;

internal static class Profiles
{
    public const string DefaultBench = "default-bench";
    public const string FirmwareWake = "firmware-wake";
    public const string Gmlan29Probe = "gmlan29-probe";
    public const string Gmlan29KnownPayloads = "gmlan29-known-payloads";
    public const string Gmlan29Chime = "gmlan29-chime";
    public const string Gmlan29SpeedSweep = "gmlan29-speed-sweep";
    public const string Gmlan29SpeedRpm = "gmlan29-speed-rpm";
    public const string Gmlan29SpeedRpmAltSources = "gmlan29-speed-rpm-alt-sources";
    public const string Gmlan29PowerMode = "gmlan29-power-mode";
    public const string Gmlan29Environment = "gmlan29-environment";
    public const string Gmlan29AllTargeted = "gmlan29-all-targeted";
    public const string OpelReferenceProbe = "opel-reference-probe";

    public static readonly ProfileDefinition[] Available =
    [
        new(DefaultBench, "Recommended Corsa E IPC test: wake + GMLAN Bible payload probes"),
        new(FirmwareWake, "Short wake/init burst plus steady keepalive"),
        new(Gmlan29AllTargeted, "Run all targeted 29-bit GMLAN payload tests in one sequence"),
        new(Gmlan29SpeedRpm, "Vehicle speed/RPM payload test with zero-out near end"),
        new(Gmlan29SpeedRpmAltSources, "Vehicle speed/RPM payload with alternate source/priority IDs"),
        new(Gmlan29PowerMode, "System power mode value probes"),
        new(Gmlan29Environment, "Battery voltage and outside-temperature examples"),
        new(Gmlan29Chime, "29-bit GMLAN chime command probes with wake prelude"),
        new(Gmlan29KnownPayloads, "Compatibility bundle matching default GMLAN payload probes"),
        new(Gmlan29SpeedSweep, "Legacy speed sweep using arbid 0x028"),
        new(OpelReferenceProbe, "Legacy older Opel 11-bit wake, IPC-on, and needle sweep probes"),
        new(Gmlan29Probe, "Discovery zero-payload 29-bit GMLAN probes"),
    ];

    public static List<ScheduledTxFrame> GetSchedule(string profile)
    {
        return profile.ToLowerInvariant() switch
        {
            DefaultBench => DefaultBenchSchedule(),
            FirmwareWake => FirmwareWakeSchedule(),
            Gmlan29Probe => Gmlan29ProbeSchedule(),
            Gmlan29KnownPayloads => Gmlan29KnownPayloadsSchedule(),
            Gmlan29Chime => Gmlan29ChimeSchedule(),
            Gmlan29SpeedSweep => Gmlan29SpeedSweepSchedule(),
            Gmlan29SpeedRpm => Gmlan29SpeedRpmSchedule(),
            Gmlan29SpeedRpmAltSources => Gmlan29SpeedRpmAltSourcesSchedule(),
            Gmlan29PowerMode => Gmlan29PowerModeSchedule(),
            Gmlan29Environment => Gmlan29EnvironmentSchedule(),
            Gmlan29AllTargeted => Gmlan29AllTargetedSchedule(),
            OpelReferenceProbe => OpelReferenceProbeSchedule(),
            _ => throw new ArgumentException($"unknown profile: {profile}")
        };
    }

    private static List<ScheduledTxFrame> DefaultBenchSchedule()
    {
        var schedule = FirmwareWakeSchedule();
        schedule.AddRange(Gmlan29CorePayloadSchedule(TimeSpan.FromMilliseconds(2000)));
        return schedule;
    }

    private static List<ScheduledTxFrame> FirmwareWakeSchedule()
    {
        return
        [
            new ScheduledTxFrame(0x100, [], TimeSpan.Zero, false, "SWCAN wakeup, startup one-shot", MaxSends: 1),
            new ScheduledTxFrame(0x621, CliOptions.ParseHexData("0140000000000000"), TimeSpan.FromMilliseconds(500), false, "network-management initiate, three-frame startup burst", MaxSends: 3),
            new ScheduledTxFrame(0x13FFE040, [], TimeSpan.FromMilliseconds(1000), true, "extended broadcast presence / keepalive from source 0x40"),
            new ScheduledTxFrame(0x621, CliOptions.ParseHexData("0040000000000000"), TimeSpan.FromMilliseconds(1000), false, "network-management steady keepalive"),
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

    private static List<ScheduledTxFrame> Gmlan29KnownPayloadsSchedule()
    {
        var schedule = FirmwareWakeSchedule();
        schedule.AddRange(Gmlan29CorePayloadSchedule(TimeSpan.FromMilliseconds(2000)));
        return schedule;
    }

    private static List<ScheduledTxFrame> Gmlan29ChimeSchedule()
    {
        return WithWakePrelude(Gmlan29ChimePayloads(TimeSpan.Zero));
    }

    private static List<ScheduledTxFrame> Gmlan29SpeedRpmSchedule()
    {
        return WithWakePrelude(Gmlan29SpeedRpmPayloads(TimeSpan.Zero));
    }

    private static List<ScheduledTxFrame> Gmlan29SpeedRpmAltSourcesSchedule()
    {
        return WithWakePrelude(Gmlan29SpeedRpmAltSourcePayloads(TimeSpan.Zero));
    }

    private static List<ScheduledTxFrame> Gmlan29PowerModeSchedule()
    {
        return WithWakePrelude(Gmlan29PowerModePayloads(TimeSpan.Zero));
    }

    private static List<ScheduledTxFrame> Gmlan29EnvironmentSchedule()
    {
        return WithWakePrelude(Gmlan29EnvironmentPayloads(TimeSpan.Zero));
    }

    private static List<ScheduledTxFrame> Gmlan29AllTargetedSchedule()
    {
        var schedule = FirmwareWakeSchedule();
        schedule.AddRange(Gmlan29SpeedRpmPayloads(TimeSpan.FromSeconds(2)));
        schedule.AddRange(Gmlan29SpeedRpmAltSourcePayloads(TimeSpan.FromSeconds(13)));
        schedule.AddRange(Gmlan29PowerModePayloads(TimeSpan.FromSeconds(27)));
        schedule.AddRange(Gmlan29EnvironmentPayloads(TimeSpan.FromSeconds(38)));
        schedule.AddRange(Gmlan29ChimePayloads(TimeSpan.FromSeconds(47)));
        return schedule;
    }

    private static List<ScheduledTxFrame> Gmlan29SpeedRpmPayloads(TimeSpan offset)
    {
        return
        [
            new ScheduledTxFrame(0x10050040, CliOptions.ParseHexData("00012C03A9000000"), TimeSpan.FromMilliseconds(75), true, "GMLAN Bible speed/RPM movement payload, arbid 0x028, sender 0x40", MaxSends: 90),
            new ScheduledTxFrame(0x10050040, CliOptions.ParseHexData("0000000000000000"), TimeSpan.Zero, true, "GMLAN speed/RPM zero-out frame", TimeSpan.FromMilliseconds(9500), MaxSends: 1),
        ].Select(frame => frame with { InitialDelay = frame.InitialDelay + offset }).ToList();
    }

    private static List<ScheduledTxFrame> Gmlan29SpeedRpmAltSourcePayloads(TimeSpan offset)
    {
        return
        [
            new ScheduledTxFrame(0x0C050040, CliOptions.ParseHexData("00012C03A9000000"), TimeSpan.FromMilliseconds(100), true, "priority 3 arbid 0x028 speed/RPM candidate, sender 0x40", MaxSends: 35),
            new ScheduledTxFrame(0x10050010, CliOptions.ParseHexData("00012C03A9000000"), TimeSpan.FromMilliseconds(100), true, "priority 4 arbid 0x028 speed/RPM candidate, engine-controller sender 0x10", TimeSpan.FromMilliseconds(4000), MaxSends: 35),
            new ScheduledTxFrame(0x10050028, CliOptions.ParseHexData("00012C03A9000000"), TimeSpan.FromMilliseconds(100), true, "priority 4 arbid 0x028 speed/RPM candidate, brake-controller sender 0x28", TimeSpan.FromMilliseconds(8000), MaxSends: 35),
            new ScheduledTxFrame(0x10050040, CliOptions.ParseHexData("0000000000000000"), TimeSpan.Zero, true, "GMLAN speed/RPM zero-out frame", TimeSpan.FromMilliseconds(12000), MaxSends: 1),
        ].Select(frame => frame with { InitialDelay = frame.InitialDelay + offset }).ToList();
    }

    private static List<ScheduledTxFrame> Gmlan29PowerModePayloads(TimeSpan offset)
    {
        return
        [
            new ScheduledTxFrame(0x10002040, CliOptions.ParseHexData("02"), TimeSpan.FromMilliseconds(3000), true, "system power mode short DLC value 0x02"),
            new ScheduledTxFrame(0x10002040, CliOptions.ParseHexData("03"), TimeSpan.FromMilliseconds(3000), true, "system power mode short DLC value 0x03", TimeSpan.FromMilliseconds(500)),
            new ScheduledTxFrame(0x10002040, CliOptions.ParseHexData("04"), TimeSpan.FromMilliseconds(3000), true, "system power mode short DLC value 0x04", TimeSpan.FromMilliseconds(1000)),
            new ScheduledTxFrame(0x10002040, CliOptions.ParseHexData("0200000000000000"), TimeSpan.FromMilliseconds(3000), true, "system power mode full DLC value 0x02", TimeSpan.FromMilliseconds(1500)),
            new ScheduledTxFrame(0x10002040, CliOptions.ParseHexData("0300000000000000"), TimeSpan.FromMilliseconds(3000), true, "system power mode full DLC value 0x03", TimeSpan.FromMilliseconds(2000)),
            new ScheduledTxFrame(0x10002040, CliOptions.ParseHexData("0400000000000000"), TimeSpan.FromMilliseconds(3000), true, "system power mode full DLC value 0x04", TimeSpan.FromMilliseconds(2500)),
        ].Select(frame => frame with { InitialDelay = frame.InitialDelay + offset }).ToList();
    }

    private static List<ScheduledTxFrame> Gmlan29EnvironmentPayloads(TimeSpan offset)
    {
        return
        [
            new ScheduledTxFrame(0x10030040, CliOptions.ParseHexData("0075A708"), TimeSpan.FromMilliseconds(1000), true, "battery voltage example, priority 4, arbid 0x018, sender 0x40"),
            new ScheduledTxFrame(0x0C030040, CliOptions.ParseHexData("0075A708"), TimeSpan.FromMilliseconds(1000), true, "battery voltage example, priority 3, arbid 0x018, sender 0x40", TimeSpan.FromMilliseconds(250)),
            new ScheduledTxFrame(0x100C2099, CliOptions.ParseHexData("0073"), TimeSpan.FromMilliseconds(2000), true, "outside air temp 17.5 C example, arbid 0x061, sender 0x99", TimeSpan.FromMilliseconds(500)),
            new ScheduledTxFrame(0x100C2099, CliOptions.ParseHexData("006C"), TimeSpan.FromMilliseconds(2000), true, "outside air temp 14.0 C example, arbid 0x061, sender 0x99", TimeSpan.FromMilliseconds(1500)),
        ].Select(frame => frame with { InitialDelay = frame.InitialDelay + offset }).ToList();
    }

    private static List<ScheduledTxFrame> Gmlan29ChimePayloads(TimeSpan offset)
    {
        return
        [
            new ScheduledTxFrame(0x1001E058, CliOptions.ParseHexData("867805FF05"), TimeSpan.Zero, true, "GMLAN Bible low chime example", offset, MaxSends: 1),
            new ScheduledTxFrame(0x1001E058, CliOptions.ParseHexData("847803FF05"), TimeSpan.Zero, true, "GMLAN Bible beep-style chime probe", offset + TimeSpan.FromMilliseconds(3000), MaxSends: 1),
            new ScheduledTxFrame(0x1001E058, CliOptions.ParseHexData("877803FF05"), TimeSpan.Zero, true, "GMLAN Bible high chime probe", offset + TimeSpan.FromMilliseconds(6000), MaxSends: 1),
        ];
    }

    private static List<ScheduledTxFrame> Gmlan29SpeedSweepSchedule()
    {
        return WithWakePrelude([
            new ScheduledTxFrame(0x10050040, CliOptions.ParseHexData("0000000000000000"), TimeSpan.FromMilliseconds(3000), true, "GMLAN arbid 0x028 vehicle speed / engine zero baseline"),
            new ScheduledTxFrame(0x10050040, CliOptions.ParseHexData("0000640000000000"), TimeSpan.FromMilliseconds(3000), true, "GMLAN arbid 0x028 low speed value probe", TimeSpan.FromMilliseconds(1000)),
            new ScheduledTxFrame(0x10050040, CliOptions.ParseHexData("00012C03A9000000"), TimeSpan.FromMilliseconds(3000), true, "GMLAN Bible speed/RPM example payload", TimeSpan.FromMilliseconds(2000)),
        ]);
    }

    private static List<ScheduledTxFrame> Gmlan29CorePayloadSchedule(TimeSpan offset)
    {
        return
        [
            new ScheduledTxFrame(0x10050040, CliOptions.ParseHexData("00012C03A9000000"), TimeSpan.FromMilliseconds(75), true, "GMLAN Bible speed/RPM movement payload, arbid 0x028, sender 0x40", offset, MaxSends: 120),
            new ScheduledTxFrame(0x10030040, CliOptions.ParseHexData("0075A708"), TimeSpan.FromMilliseconds(1000), true, "battery voltage example, priority 4, arbid 0x018, sender 0x40", offset + TimeSpan.FromMilliseconds(125)),
            new ScheduledTxFrame(0x0C030040, CliOptions.ParseHexData("0075A708"), TimeSpan.FromMilliseconds(1000), true, "battery voltage example, priority 3, arbid 0x018, sender 0x40", offset + TimeSpan.FromMilliseconds(250)),
            new ScheduledTxFrame(0x10002040, CliOptions.ParseHexData("02"), TimeSpan.FromMilliseconds(3000), true, "system power mode short DLC value 0x02", offset + TimeSpan.FromMilliseconds(500)),
            new ScheduledTxFrame(0x10002040, CliOptions.ParseHexData("03"), TimeSpan.FromMilliseconds(3000), true, "system power mode short DLC value 0x03", offset + TimeSpan.FromMilliseconds(1000)),
            new ScheduledTxFrame(0x10002040, CliOptions.ParseHexData("04"), TimeSpan.FromMilliseconds(3000), true, "system power mode short DLC value 0x04", offset + TimeSpan.FromMilliseconds(1500)),
            new ScheduledTxFrame(0x10002040, CliOptions.ParseHexData("0200000000000000"), TimeSpan.FromMilliseconds(3000), true, "system power mode full DLC value 0x02", offset + TimeSpan.FromMilliseconds(2000)),
            new ScheduledTxFrame(0x10002040, CliOptions.ParseHexData("0300000000000000"), TimeSpan.FromMilliseconds(3000), true, "system power mode full DLC value 0x03", offset + TimeSpan.FromMilliseconds(2500)),
            new ScheduledTxFrame(0x10002040, CliOptions.ParseHexData("0400000000000000"), TimeSpan.FromMilliseconds(3000), true, "system power mode full DLC value 0x04", offset + TimeSpan.FromMilliseconds(3000)),
            new ScheduledTxFrame(0x1001E058, CliOptions.ParseHexData("867805FF05"), TimeSpan.Zero, true, "GMLAN Bible chime command example, arbid 0x00F, sender 0x58", offset + TimeSpan.FromMilliseconds(9000), MaxSends: 1),
            new ScheduledTxFrame(0x10050040, CliOptions.ParseHexData("0000000000000000"), TimeSpan.Zero, true, "GMLAN speed/RPM zero-out frame", offset + TimeSpan.FromMilliseconds(12000), MaxSends: 1),
        ];
    }

    private static List<ScheduledTxFrame> WithWakePrelude(List<ScheduledTxFrame> probes)
    {
        var schedule = FirmwareWakeSchedule();
        schedule.AddRange(probes.Select(frame => frame with
        {
            InitialDelay = frame.InitialDelay + TimeSpan.FromMilliseconds(2000)
        }));
        return schedule;
    }
}

internal sealed record ProfileDefinition(string Name, string Description);
