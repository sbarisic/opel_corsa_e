namespace cantool;

internal static class Profiles
{
    private static readonly byte[] ConservativeFuzzValues = [0x00, 0x01, 0x02, 0x03, 0x04, 0x08, 0x10, 0x20, 0x40, 0x7F, 0x80, 0xC0, 0xFF];

    public const string ReadOnly = "read-only";
    public const string DefaultBench = "default-bench";
    public const string IpcSimulator = "ipc-simulator";
    public const string IpcSimulatorP4Speed = "ipc-simulator-p4-speed";
    public const string IpcSimulatorAltSources = "ipc-simulator-alt-sources";
    public const string IpcSimulatorSweep = "ipc-simulator-sweep";
    public const string IpcDiagnosticProbe = "ipc-diagnostic-probe";
    public const string IpcDiagnosticSessionProbe = "ipc-diagnostic-session-probe";
    public const string IpcDiagnosticDidScan = "ipc-diagnostic-did-scan";
    public const string IpcDiagnosticF1RangeScan = "ipc-diagnostic-f1-range-scan";
    public const string IpcDiagnosticLocalRangeScan = "ipc-diagnostic-local-range-scan";
    public const string IpcDiagnosticLocalRangeSlowScan = "ipc-diagnostic-local-range-slow-scan";
    public const string IpcDiagnosticRead21LocalScan = "ipc-diagnostic-read21-local-scan";
    public const string IpcStandardEngineWarningProbe = "ipc-standard-engine-warning-probe";
    public const string IpcStandardByteFuzz = "ipc-standard-byte-fuzz";
    public const string IpcGmlan29ByteFuzz = "ipc-gmlan29-byte-fuzz";
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
        new(ReadOnly, "Passive listen-only logger: waits and records RX frames only", 0.0, ListenOnly: true),
        new(IpcSimulator, "Recommended staged IPC simulator: waits for first RX, then fake BCM/body gateway", 30.0, WaitForFirstRx: true),
        new(IpcSimulatorP4Speed, "Staged IPC simulator using priority-4 10050040 speed/RPM", 30.0, WaitForFirstRx: true),
        new(IpcSimulatorAltSources, "Staged IPC simulator cycling alternate speed/RPM source nodes", 45.0, WaitForFirstRx: true),
        new(IpcSimulatorSweep, "Staged IPC simulator sweeping priority-3 speed/RPM payload values", 30.0, WaitForFirstRx: true),
        new(IpcDiagnosticProbe, "Waits for first RX, then probes IPC diagnostics 24C -> 64C", 12.0, WaitForFirstRx: true),
        new(IpcDiagnosticSessionProbe, "Probes IPC diagnostic sessions, tester-present variants, and VIN DID", 16.0, WaitForFirstRx: true),
        new(IpcDiagnosticDidScan, "Extended-session read-only IPC DID/DTC scan with ISO-TP flow control", 35.0, WaitForFirstRx: true, AutoIsoTpFlowControl: true),
        new(IpcDiagnosticF1RangeScan, "Read-only scan of UDS F100-F1FF DIDs with ISO-TP flow control", 75.0, WaitForFirstRx: true, AutoIsoTpFlowControl: true),
        new(IpcDiagnosticLocalRangeScan, "Read-only scan of local 0000-00FF DIDs with ISO-TP flow control", 75.0, WaitForFirstRx: true, AutoIsoTpFlowControl: true),
        new(IpcDiagnosticLocalRangeSlowScan, "Settled slow read-only scan of local 0000-00FF DIDs", 145.0, WaitForFirstRx: true, AutoIsoTpFlowControl: true),
        new(IpcDiagnosticRead21LocalScan, "Read-only scan of older 0x21 local identifiers 00-FF", 80.0, WaitForFirstRx: true, AutoIsoTpFlowControl: true),
        new(IpcStandardEngineWarningProbe, "Standard 11-bit 0C9 RPM plus 1E5 warning/service probe", 30.0, WaitForFirstRx: true),
        new(IpcStandardByteFuzz, "Conservative deterministic standard 11-bit byte fuzz", 300.0, WaitForFirstRx: true),
        new(IpcGmlan29ByteFuzz, "Conservative deterministic 29-bit GMLAN byte fuzz", 180.0, WaitForFirstRx: true),
        new(DefaultBench, "Recommended Corsa E IPC test: wake + GMLAN Bible payload probes"),
        new(FirmwareWake, "Short wake/init burst plus steady keepalive"),
        new(Gmlan29AllTargeted, "Run all targeted 29-bit GMLAN payload tests in one sequence", 60.0),
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
            IpcSimulator => IpcSimulatorSchedule(),
            IpcSimulatorP4Speed => IpcSimulatorP4SpeedSchedule(),
            IpcSimulatorAltSources => IpcSimulatorAltSourcesSchedule(),
            IpcSimulatorSweep => IpcSimulatorSweepSchedule(),
            IpcDiagnosticProbe => IpcDiagnosticProbeSchedule(),
            IpcDiagnosticSessionProbe => IpcDiagnosticSessionProbeSchedule(),
            IpcDiagnosticDidScan => IpcDiagnosticDidScanSchedule(),
            IpcDiagnosticF1RangeScan => IpcDiagnosticRangeScanSchedule(0xF100, 0xF1FF, "F1xx standardized/manufacturer DID range"),
            IpcDiagnosticLocalRangeScan => IpcDiagnosticRangeScanSchedule(0x0000, 0x00FF, "local 00xx DID range"),
            IpcDiagnosticLocalRangeSlowScan => IpcDiagnosticRangeScanSchedule(0x0000, 0x00FF, "settled slow local 00xx DID range", TimeSpan.FromSeconds(8), TimeSpan.FromMilliseconds(500)),
            IpcDiagnosticRead21LocalScan => IpcDiagnosticRead21LocalScanSchedule(),
            IpcStandardEngineWarningProbe => IpcStandardEngineWarningProbeSchedule(),
            IpcStandardByteFuzz => IpcStandardByteFuzzSchedule(),
            IpcGmlan29ByteFuzz => IpcGmlan29ByteFuzzSchedule(),
            ReadOnly => [],
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

    public static double GetDefaultSeconds(string profile)
    {
        return Available.FirstOrDefault(item => item.Name.Equals(profile, StringComparison.OrdinalIgnoreCase))?.DefaultSeconds ?? 15.0;
    }

    public static bool WaitsForFirstRx(string profile)
    {
        return Available.Any(item => item.Name.Equals(profile, StringComparison.OrdinalIgnoreCase) && item.WaitForFirstRx);
    }

    public static bool UsesAutoIsoTpFlowControl(string profile)
    {
        return Available.Any(item => item.Name.Equals(profile, StringComparison.OrdinalIgnoreCase) && item.AutoIsoTpFlowControl);
    }

    public static bool UsesListenOnly(string profile)
    {
        return Available.Any(item => item.Name.Equals(profile, StringComparison.OrdinalIgnoreCase) && item.ListenOnly);
    }

    private static List<ScheduledTxFrame> DefaultBenchSchedule()
    {
        var schedule = FirmwareWakeSchedule();
        schedule.AddRange(Gmlan29CorePayloadSchedule(TimeSpan.FromMilliseconds(2000)));
        return schedule;
    }

    private static List<ScheduledTxFrame> IpcSimulatorSchedule()
    {
        var schedule = IpcSimulatorBaseSchedule();
        schedule.Add(new ScheduledTxFrame(0x0C050040, CliOptions.ParseHexData("00012C03A9000000"), TimeSpan.FromMilliseconds(50), true, "priority 3 vehicle speed/RPM probe, arbid 0x028, sender 0x40"));
        schedule.Add(new ScheduledTxFrame(0x1001E058, CliOptions.ParseHexData("867805FF05"), TimeSpan.Zero, true, "GMLAN Bible chime command example after simulator settle", TimeSpan.FromSeconds(5), MaxSends: 1));
        schedule.Add(new ScheduledTxFrame(0x0C050040, CliOptions.ParseHexData("0000000000000000"), TimeSpan.Zero, true, "priority 3 speed/RPM zero-out frame before simulator stop", TimeSpan.FromSeconds(28), MaxSends: 1));
        return schedule;
    }

    private static List<ScheduledTxFrame> IpcSimulatorP4SpeedSchedule()
    {
        var schedule = IpcSimulatorBaseSchedule();
        schedule.Add(new ScheduledTxFrame(0x10050040, CliOptions.ParseHexData("00012C03A9000000"), TimeSpan.FromMilliseconds(50), true, "priority 4 vehicle speed/RPM probe, arbid 0x028, sender 0x40"));
        schedule.Add(new ScheduledTxFrame(0x1001E058, CliOptions.ParseHexData("867805FF05"), TimeSpan.Zero, true, "GMLAN Bible chime command example after simulator settle", TimeSpan.FromSeconds(5), MaxSends: 1));
        schedule.Add(new ScheduledTxFrame(0x10050040, CliOptions.ParseHexData("0000000000000000"), TimeSpan.Zero, true, "priority 4 speed/RPM zero-out frame before simulator stop", TimeSpan.FromSeconds(28), MaxSends: 1));
        return schedule;
    }

    private static List<ScheduledTxFrame> IpcSimulatorAltSourcesSchedule()
    {
        var schedule = IpcSimulatorBaseSchedule();
        schedule.AddRange([
            new ScheduledTxFrame(0x0C050010, CliOptions.ParseHexData("00012C03A9000000"), TimeSpan.FromMilliseconds(50), true, "priority 3 speed/RPM candidate, powertrain sender 0x10", TimeSpan.FromSeconds(2), MaxSends: 160),
            new ScheduledTxFrame(0x0C050010, CliOptions.ParseHexData("0000000000000000"), TimeSpan.Zero, true, "priority 3 powertrain speed/RPM zero-out", TimeSpan.FromSeconds(10), MaxSends: 1),
            new ScheduledTxFrame(0x0C050028, CliOptions.ParseHexData("00012C03A9000000"), TimeSpan.FromMilliseconds(50), true, "priority 3 speed/RPM candidate, chassis sender 0x28", TimeSpan.FromSeconds(12), MaxSends: 160),
            new ScheduledTxFrame(0x0C050028, CliOptions.ParseHexData("0000000000000000"), TimeSpan.Zero, true, "priority 3 chassis speed/RPM zero-out", TimeSpan.FromSeconds(20), MaxSends: 1),
            new ScheduledTxFrame(0x10050010, CliOptions.ParseHexData("00012C03A9000000"), TimeSpan.FromMilliseconds(50), true, "priority 4 speed/RPM candidate, powertrain sender 0x10", TimeSpan.FromSeconds(22), MaxSends: 160),
            new ScheduledTxFrame(0x10050010, CliOptions.ParseHexData("0000000000000000"), TimeSpan.Zero, true, "priority 4 powertrain speed/RPM zero-out", TimeSpan.FromSeconds(30), MaxSends: 1),
            new ScheduledTxFrame(0x10050028, CliOptions.ParseHexData("00012C03A9000000"), TimeSpan.FromMilliseconds(50), true, "priority 4 speed/RPM candidate, chassis sender 0x28", TimeSpan.FromSeconds(32), MaxSends: 160),
            new ScheduledTxFrame(0x10050028, CliOptions.ParseHexData("0000000000000000"), TimeSpan.Zero, true, "priority 4 chassis speed/RPM zero-out", TimeSpan.FromSeconds(40), MaxSends: 1),
        ]);
        return schedule;
    }

    private static List<ScheduledTxFrame> IpcSimulatorSweepSchedule()
    {
        var schedule = IpcSimulatorBaseSchedule();
        schedule.AddRange([
            new ScheduledTxFrame(0x0C050040, CliOptions.ParseHexData("0000000000000000"), TimeSpan.FromMilliseconds(50), true, "priority 3 speed/RPM sweep zero baseline", TimeSpan.FromSeconds(2), MaxSends: 100),
            new ScheduledTxFrame(0x0C050040, CliOptions.ParseHexData("0000C80177000000"), TimeSpan.FromMilliseconds(50), true, "priority 3 speed/RPM sweep approx 20 km/h 1500 rpm", TimeSpan.FromSeconds(8), MaxSends: 100),
            new ScheduledTxFrame(0x0C050040, CliOptions.ParseHexData("00012C03A9000000"), TimeSpan.FromMilliseconds(50), true, "priority 3 speed/RPM sweep workbook example", TimeSpan.FromSeconds(14), MaxSends: 100),
            new ScheduledTxFrame(0x0C050040, CliOptions.ParseHexData("00025802EE000000"), TimeSpan.FromMilliseconds(50), true, "priority 3 speed/RPM sweep approx 60 km/h 3000 rpm", TimeSpan.FromSeconds(20), MaxSends: 100),
            new ScheduledTxFrame(0x0C050040, CliOptions.ParseHexData("0000000000000000"), TimeSpan.Zero, true, "priority 3 speed/RPM sweep zero-out", TimeSpan.FromSeconds(28), MaxSends: 1),
        ]);
        return schedule;
    }

    private static List<ScheduledTxFrame> IpcDiagnosticProbeSchedule()
    {
        var schedule = IpcDiagnosticBaseSchedule();
        schedule.Add(new ScheduledTxFrame(0x24C, CliOptions.ParseHexData("023E00AAAAAAAAAA"), TimeSpan.FromMilliseconds(1000), false, "IPC diagnostic tester-present probe; watch for 64C#027E00 response", TimeSpan.FromSeconds(2), MaxSends: 6));
        return schedule;
    }

    private static List<ScheduledTxFrame> IpcDiagnosticSessionProbeSchedule()
    {
        var schedule = IpcDiagnosticBaseSchedule();
        schedule.AddRange([
            new ScheduledTxFrame(0x24C, CliOptions.ParseHexData("021001AAAAAAAAAA"), TimeSpan.Zero, false, "IPC diagnostic default-session request; expect 64C#025001... if accepted", TimeSpan.FromSeconds(2), MaxSends: 1),
            new ScheduledTxFrame(0x24C, CliOptions.ParseHexData("021003AAAAAAAAAA"), TimeSpan.Zero, false, "IPC diagnostic extended-session request; expect 64C#025003... if accepted", TimeSpan.FromSeconds(4), MaxSends: 1),
            new ScheduledTxFrame(0x24C, CliOptions.ParseHexData("023E80AAAAAAAAAA"), TimeSpan.Zero, false, "IPC tester-present suppress-positive-response variant", TimeSpan.FromSeconds(6), MaxSends: 1),
            new ScheduledTxFrame(0x24C, CliOptions.ParseHexData("023E00AAAAAAAAAA"), TimeSpan.Zero, false, "IPC tester-present normal variant", TimeSpan.FromSeconds(8), MaxSends: 1),
            new ScheduledTxFrame(0x24C, CliOptions.ParseHexData("0322F190AAAAAAAA"), TimeSpan.Zero, false, "IPC read-data-by-identifier VIN DID F190 probe", TimeSpan.FromSeconds(10), MaxSends: 1),
        ]);
        return schedule;
    }

    private static List<ScheduledTxFrame> IpcDiagnosticDidScanSchedule()
    {
        var schedule = IpcDiagnosticBaseSchedule();
        schedule.AddRange([
            new ScheduledTxFrame(0x24C, CliOptions.ParseHexData("021003AAAAAAAAAA"), TimeSpan.Zero, false, "IPC diagnostic extended-session request before DID scan", TimeSpan.FromSeconds(2), MaxSends: 1),
            new ScheduledTxFrame(0x24C, CliOptions.ParseHexData("0322F186AAAAAAAA"), TimeSpan.Zero, false, "read DID F186 active diagnostic session", TimeSpan.FromSeconds(4), MaxSends: 1),
            new ScheduledTxFrame(0x24C, CliOptions.ParseHexData("0322F187AAAAAAAA"), TimeSpan.Zero, false, "read DID F187 manufacturer spare part number", TimeSpan.FromSeconds(6), MaxSends: 1),
            new ScheduledTxFrame(0x24C, CliOptions.ParseHexData("0322F188AAAAAAAA"), TimeSpan.Zero, false, "read DID F188 manufacturer ECU software number", TimeSpan.FromSeconds(8), MaxSends: 1),
            new ScheduledTxFrame(0x24C, CliOptions.ParseHexData("0322F189AAAAAAAA"), TimeSpan.Zero, false, "read DID F189 manufacturer ECU software version", TimeSpan.FromSeconds(10), MaxSends: 1),
            new ScheduledTxFrame(0x24C, CliOptions.ParseHexData("0322F18AAAAAAAAA"), TimeSpan.Zero, false, "read DID F18A system supplier identifier", TimeSpan.FromSeconds(12), MaxSends: 1),
            new ScheduledTxFrame(0x24C, CliOptions.ParseHexData("0322F18BAAAAAAAA"), TimeSpan.Zero, false, "read DID F18B ECU manufacturing date", TimeSpan.FromSeconds(14), MaxSends: 1),
            new ScheduledTxFrame(0x24C, CliOptions.ParseHexData("0322F18CAAAAAAAA"), TimeSpan.Zero, false, "read DID F18C ECU serial number", TimeSpan.FromSeconds(16), MaxSends: 1),
            new ScheduledTxFrame(0x24C, CliOptions.ParseHexData("0322F190AAAAAAAA"), TimeSpan.Zero, false, "read DID F190 VIN", TimeSpan.FromSeconds(18), MaxSends: 1),
            new ScheduledTxFrame(0x24C, CliOptions.ParseHexData("0322F191AAAAAAAA"), TimeSpan.Zero, false, "read DID F191 ECU hardware number", TimeSpan.FromSeconds(20), MaxSends: 1),
            new ScheduledTxFrame(0x24C, CliOptions.ParseHexData("0322F1A0AAAAAAAA"), TimeSpan.Zero, false, "read DID F1A0 GM/application identifier candidate", TimeSpan.FromSeconds(22), MaxSends: 1),
            new ScheduledTxFrame(0x24C, CliOptions.ParseHexData("0322F1A1AAAAAAAA"), TimeSpan.Zero, false, "read DID F1A1 GM/application identifier candidate", TimeSpan.FromSeconds(24), MaxSends: 1),
            new ScheduledTxFrame(0x24C, CliOptions.ParseHexData("031902FFAAAAAAAA"), TimeSpan.Zero, false, "read DTCs by status mask 0xFF", TimeSpan.FromSeconds(26), MaxSends: 1),
            new ScheduledTxFrame(0x24C, CliOptions.ParseHexData("021901AAAAAAAAAA"), TimeSpan.Zero, false, "read DTC count by status mask", TimeSpan.FromSeconds(28), MaxSends: 1),
        ]);
        return schedule;
    }

    private static List<ScheduledTxFrame> IpcDiagnosticRangeScanSchedule(int firstDid, int lastDid, string rangeNote)
    {
        return IpcDiagnosticRangeScanSchedule(firstDid, lastDid, rangeNote, TimeSpan.FromSeconds(3), TimeSpan.FromMilliseconds(250));
    }

    private static List<ScheduledTxFrame> IpcDiagnosticRangeScanSchedule(int firstDid, int lastDid, string rangeNote, TimeSpan firstRequestDelay, TimeSpan requestSpacing)
    {
        var schedule = IpcDiagnosticBaseSchedule();
        schedule.Add(new ScheduledTxFrame(0x24C, CliOptions.ParseHexData("021003AAAAAAAAAA"), TimeSpan.Zero, false, $"IPC diagnostic extended-session request before {rangeNote}", TimeSpan.FromSeconds(2), MaxSends: 1));

        var delay = firstRequestDelay;
        for (var did = firstDid; did <= lastDid; did++)
        {
            var request = $"0322{did:X4}AAAAAAAA";
            schedule.Add(new ScheduledTxFrame(
                0x24C,
                CliOptions.ParseHexData(request),
                TimeSpan.Zero,
                false,
                $"read DID {did:X4} ({rangeNote})",
                delay,
                MaxSends: 1));
            delay += requestSpacing;
        }

        return schedule;
    }

    private static List<ScheduledTxFrame> IpcDiagnosticRead21LocalScanSchedule()
    {
        var schedule = IpcDiagnosticBaseSchedule();
        schedule.Add(new ScheduledTxFrame(0x24C, CliOptions.ParseHexData("021003AAAAAAAAAA"), TimeSpan.Zero, false, "IPC diagnostic extended-session request before 0x21 local-ID scan", TimeSpan.FromSeconds(2), MaxSends: 1));

        var delay = TimeSpan.FromSeconds(8);
        for (var id = 0; id <= 0xFF; id++)
        {
            var request = $"0221{id:X2}AAAAAAAAAA";
            schedule.Add(new ScheduledTxFrame(
                0x24C,
                CliOptions.ParseHexData(request),
                TimeSpan.Zero,
                false,
                $"read local identifier {id:X2} with service 0x21",
                delay,
                MaxSends: 1));
            delay += TimeSpan.FromMilliseconds(250);
        }

        return schedule;
    }

    private static List<ScheduledTxFrame> IpcStandardEngineWarningProbeSchedule()
    {
        return
        [
            new ScheduledTxFrame(0x13FFE040, [], TimeSpan.Zero, true, "fake BCM/body gateway broadcast presence after first IPC RX", MaxSends: 1),
            new ScheduledTxFrame(0x621, CliOptions.ParseHexData("0040000000000000"), TimeSpan.Zero, false, "network-management steady keepalive seed after first IPC RX", MaxSends: 1),
            new ScheduledTxFrame(0x13FFE040, [], TimeSpan.FromMilliseconds(1000), true, "fake BCM/body gateway broadcast presence, source 0x40", TimeSpan.FromMilliseconds(1000)),
            new ScheduledTxFrame(0x621, CliOptions.ParseHexData("0040000000000000"), TimeSpan.FromMilliseconds(1000), false, "network-management steady keepalive", TimeSpan.FromMilliseconds(1000)),
            new ScheduledTxFrame(0x0C9, CliOptions.ParseHexData("0004C40000500000"), TimeSpan.FromMilliseconds(20), false, "standard RPM / engine-state candidate 0x0C9"),
            new ScheduledTxFrame(0x1E5, CliOptions.ParseHexData("44003910000000C9"), TimeSpan.FromMilliseconds(300), false, "standard warning/service candidate 0x1E5 variant C9", TimeSpan.FromMilliseconds(100)),
            new ScheduledTxFrame(0x1E5, CliOptions.ParseHexData("44003930000000E9"), TimeSpan.FromMilliseconds(300), false, "standard warning/service candidate 0x1E5 variant E9", TimeSpan.FromMilliseconds(200)),
            new ScheduledTxFrame(0x1E5, CliOptions.ParseHexData("4400395000000109"), TimeSpan.FromMilliseconds(300), false, "standard warning/service candidate 0x1E5 variant 109", TimeSpan.FromMilliseconds(300)),
        ];
    }

    private static List<ScheduledTxFrame> IpcGmlan29ByteFuzzSchedule()
    {
        var schedule = new List<ScheduledTxFrame>
        {
            new(0x100, [], TimeSpan.Zero, false, "SWCAN wakeup after first IPC RX", MaxSends: 1),
            new(0x13FFE040, [], TimeSpan.Zero, true, "fake BCM/body gateway broadcast presence after first IPC RX", MaxSends: 1),
            new(0x621, CliOptions.ParseHexData("0140000000000000"), TimeSpan.Zero, false, "network-management initiate after first IPC RX", MaxSends: 1),
            new(0x621, CliOptions.ParseHexData("0040000000000000"), TimeSpan.Zero, false, "network-management steady keepalive seed after first IPC RX", MaxSends: 1),
            new(0x13FFE040, [], TimeSpan.FromMilliseconds(1000), true, "fake BCM/body gateway broadcast presence during byte fuzz", TimeSpan.FromMilliseconds(1000)),
            new(0x621, CliOptions.ParseHexData("0040000000000000"), TimeSpan.FromMilliseconds(1000), false, "network-management steady keepalive during byte fuzz", TimeSpan.FromMilliseconds(1000)),
            new(0x10002040, CliOptions.ParseHexData("03"), TimeSpan.FromMilliseconds(250), true, "conservative system power mode hold during byte fuzz"),
        };

        var delay = TimeSpan.FromSeconds(8);
        delay = AddByteSweep(schedule, 0x10002040, "03", [0], delay, TimeSpan.FromMilliseconds(100), "system power mode");
        delay = AddByteSweep(schedule, 0x10050040, "00012C03A9000000", [0, 1, 2, 3, 4, 5], delay, TimeSpan.FromMilliseconds(100), "vehicle speed/RPM priority 4");
        delay = AddByteSweep(schedule, 0x0C050040, "00012C03A9000000", [0, 1, 2, 3, 4, 5], delay, TimeSpan.FromMilliseconds(100), "vehicle speed/RPM priority 3");

        schedule.Add(new ScheduledTxFrame(0x10050040, CliOptions.ParseHexData("0000000000000000"), TimeSpan.Zero, true, "default-duration speed/RPM zero-out priority 4", TimeSpan.FromSeconds(178), MaxSends: 1));
        schedule.Add(new ScheduledTxFrame(0x0C050040, CliOptions.ParseHexData("0000000000000000"), TimeSpan.Zero, true, "default-duration speed/RPM zero-out priority 3", TimeSpan.FromSeconds(178.2), MaxSends: 1));

        delay = AddByteSweep(schedule, 0x10030040, "0075A708", [0, 1, 2, 3], delay, TimeSpan.FromMilliseconds(100), "battery voltage priority 4");
        delay = AddByteSweep(schedule, 0x0C030040, "0075A708", [0, 1, 2, 3], delay, TimeSpan.FromMilliseconds(100), "battery voltage priority 3");
        delay = AddByteSweep(schedule, 0x10052040, "0000000000000000", Enumerable.Range(0, 8), delay, TimeSpan.FromMilliseconds(100), "engine information 1");
        delay = AddByteSweep(schedule, 0x1006E040, "0000000000000000", Enumerable.Range(0, 8), delay, TimeSpan.FromMilliseconds(100), "engine information 2");
        delay = AddByteSweep(schedule, 0x1004C040, "0000000000000000", Enumerable.Range(0, 8), delay, TimeSpan.FromMilliseconds(100), "fuel information");
        delay = AddByteSweep(schedule, 0x1004E040, "0000000000000000", Enumerable.Range(0, 8), delay, TimeSpan.FromMilliseconds(100), "odometer/brake/wash level");
        delay = AddByteSweep(schedule, 0x1005E040, "0000000000000000", Enumerable.Range(0, 8), delay, TimeSpan.FromMilliseconds(100), "brake/cruise status");
        delay = AddByteSweep(schedule, 0x1001E058, "867805FF05", [0, 1, 2, 3, 4], delay, TimeSpan.FromMilliseconds(500), "chime command");

        schedule.Add(new ScheduledTxFrame(0x10050040, CliOptions.ParseHexData("0000000000000000"), TimeSpan.Zero, true, "full-sequence speed/RPM zero-out priority 4", delay, MaxSends: 1));
        schedule.Add(new ScheduledTxFrame(0x0C050040, CliOptions.ParseHexData("0000000000000000"), TimeSpan.Zero, true, "full-sequence speed/RPM zero-out priority 3", delay + TimeSpan.FromMilliseconds(200), MaxSends: 1));
        return schedule;
    }

    private static List<ScheduledTxFrame> IpcStandardByteFuzzSchedule()
    {
        var schedule = new List<ScheduledTxFrame>
        {
            new(0x100, [], TimeSpan.Zero, false, "SWCAN wakeup after first IPC RX", MaxSends: 1),
            new(0x13FFE040, [], TimeSpan.Zero, true, "fake BCM/body gateway broadcast presence after first IPC RX", MaxSends: 1),
            new(0x621, CliOptions.ParseHexData("0140000000000000"), TimeSpan.Zero, false, "network-management initiate after first IPC RX", MaxSends: 1),
            new(0x621, CliOptions.ParseHexData("0040000000000000"), TimeSpan.Zero, false, "network-management steady keepalive seed after first IPC RX", MaxSends: 1),
            new(0x13FFE040, [], TimeSpan.FromMilliseconds(1000), true, "fake BCM/body gateway broadcast presence during standard-ID byte fuzz", TimeSpan.FromMilliseconds(1000)),
            new(0x621, CliOptions.ParseHexData("0040000000000000"), TimeSpan.FromMilliseconds(1000), false, "network-management steady keepalive during standard-ID byte fuzz", TimeSpan.FromMilliseconds(1000)),
            new(0x10002040, CliOptions.ParseHexData("03"), TimeSpan.FromMilliseconds(250), true, "conservative system power mode hold during standard-ID byte fuzz"),
        };

        var delay = TimeSpan.FromSeconds(8);
        delay = AddByteSweep(schedule, 0x0C9, "0004C40000500000", Enumerable.Range(0, 8), delay, TimeSpan.FromMilliseconds(100), "standard engine/RPM candidate 0x0C9", isExtended: false);
        delay = AddByteSweep(schedule, 0x1E5, "44003910000000C9", Enumerable.Range(0, 8), delay, TimeSpan.FromMilliseconds(100), "standard warning/service candidate 0x1E5", isExtended: false);
        delay = AddByteSweep(schedule, 0x3E9, "0000000000000000", Enumerable.Range(0, 8), delay, TimeSpan.FromMilliseconds(100), "standard vehicle-speed candidate 0x3E9", isExtended: false);
        delay = AddByteSweep(schedule, 0x4C1, "0000000000000000", Enumerable.Range(0, 8), delay, TimeSpan.FromMilliseconds(100), "standard coolant/temperature candidate 0x4C1", isExtended: false);
        delay = AddByteSweep(schedule, 0x4D1, "0000000000000000", Enumerable.Range(0, 8), delay, TimeSpan.FromMilliseconds(100), "standard oil/fuel/service candidate 0x4D1", isExtended: false);
        delay = AddByteSweep(schedule, 0x3D1, "0000000000000000", Enumerable.Range(0, 8), delay, TimeSpan.FromMilliseconds(100), "standard limiter/dashboard-settings candidate 0x3D1", isExtended: false);
        delay = AddByteSweep(schedule, 0x160, "0000000000000000", Enumerable.Range(0, 8), delay, TimeSpan.FromMilliseconds(100), "standard ignition/power candidate 0x160", isExtended: false);
        delay = AddByteSweep(schedule, 0x1F1, "0000000000000000", Enumerable.Range(0, 8), delay, TimeSpan.FromMilliseconds(100), "standard ignition/environment candidate 0x1F1", isExtended: false);

        delay = AddByteSweep(schedule, 0x0AA, "74A13B", Enumerable.Range(0, 3), delay, TimeSpan.FromMilliseconds(100), "older Opel IPC-on candidate 0x0AA", isExtended: false);
        delay = AddByteSweep(schedule, 0x06C, "000000000000001E", Enumerable.Range(0, 8), delay, TimeSpan.FromMilliseconds(100), "older Opel direct needle candidate 0x06C", isExtended: false);
        delay = AddByteSweep(schedule, 0x092, "150000000000000C", Enumerable.Range(0, 8), delay, TimeSpan.FromMilliseconds(100), "older Opel cruise telltale candidate 0x092", isExtended: false);
        delay = AddByteSweep(schedule, 0x104, "7F3280", Enumerable.Range(0, 3), delay, TimeSpan.FromMilliseconds(100), "older Opel indicator/hazard candidate 0x104", isExtended: false);
        delay = AddByteSweep(schedule, 0x119, "60051E0133", Enumerable.Range(0, 5), delay, TimeSpan.FromMilliseconds(250), "older Opel chime candidate 0x119", isExtended: false);
        delay = AddByteSweep(schedule, 0x15E, "06019220", Enumerable.Range(0, 4), delay, TimeSpan.FromMilliseconds(100), "older Opel lights telltale candidate 0x15E", isExtended: false);

        schedule.Add(new ScheduledTxFrame(0x0C9, CliOptions.ParseHexData("0000000000000000"), TimeSpan.Zero, false, "standard engine/RPM zero-out", delay, MaxSends: 1));
        schedule.Add(new ScheduledTxFrame(0x1E5, CliOptions.ParseHexData("0000000000000000"), TimeSpan.Zero, false, "standard warning/service zero-out", delay + TimeSpan.FromMilliseconds(200), MaxSends: 1));
        return schedule;
    }

    private static TimeSpan AddByteSweep(
        List<ScheduledTxFrame> schedule,
        uint canId,
        string basePayloadHex,
        IEnumerable<int> byteIndexes,
        TimeSpan startDelay,
        TimeSpan sendPeriod,
        string note,
        bool isExtended = true)
    {
        var basePayload = CliOptions.ParseHexData(basePayloadHex);
        var valueWindow = sendPeriod == TimeSpan.FromMilliseconds(500) ? TimeSpan.FromMilliseconds(500) : TimeSpan.FromSeconds(1);
        var sendsPerValue = Math.Max(1, (int)Math.Round(valueWindow.TotalMilliseconds / sendPeriod.TotalMilliseconds));
        var delay = startDelay;

        foreach (var byteIndex in byteIndexes)
        {
            if (byteIndex < 0 || byteIndex >= basePayload.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(byteIndexes), $"byte index {byteIndex} is outside payload length {basePayload.Length}");
            }

            foreach (var value in ConservativeFuzzValues)
            {
                var payload = basePayload.ToArray();
                payload[byteIndex] = value;
                schedule.Add(new ScheduledTxFrame(
                    canId,
                    payload,
                    sendPeriod,
                    isExtended,
                    $"byte fuzz {note} byte={byteIndex} value=0x{value:X2}",
                    delay,
                    MaxSends: sendsPerValue));
                delay += valueWindow;
            }
        }

        return delay;
    }

    private static List<ScheduledTxFrame> IpcSimulatorBaseSchedule()
    {
        return
        [
            new ScheduledTxFrame(0x100, [], TimeSpan.Zero, false, "SWCAN wakeup after first IPC RX", MaxSends: 1),
            new ScheduledTxFrame(0x13FFE040, [], TimeSpan.Zero, true, "fake BCM/body gateway broadcast presence after first IPC RX", MaxSends: 1),
            new ScheduledTxFrame(0x621, CliOptions.ParseHexData("0140000000000000"), TimeSpan.Zero, false, "network-management initiate after first IPC RX", MaxSends: 1),
            new ScheduledTxFrame(0x621, CliOptions.ParseHexData("0040000000000000"), TimeSpan.Zero, false, "network-management steady keepalive seed after first IPC RX", MaxSends: 1),
            new ScheduledTxFrame(0x13FFE040, [], TimeSpan.FromMilliseconds(1000), true, "fake BCM/body gateway broadcast presence, source 0x40", TimeSpan.FromMilliseconds(1000)),
            new ScheduledTxFrame(0x621, CliOptions.ParseHexData("0040000000000000"), TimeSpan.FromMilliseconds(1000), false, "network-management steady keepalive", TimeSpan.FromMilliseconds(1000)),
            new ScheduledTxFrame(0x10002040, CliOptions.ParseHexData("03"), TimeSpan.FromMilliseconds(100), true, "system power mode run candidate, short DLC value 0x03"),
            new ScheduledTxFrame(0x0C030040, CliOptions.ParseHexData("0075A708"), TimeSpan.FromMilliseconds(1000), true, "battery voltage example, priority 3, arbid 0x018, sender 0x40"),
        ];
    }

    private static List<ScheduledTxFrame> IpcDiagnosticBaseSchedule()
    {
        return
        [
            new ScheduledTxFrame(0x100, [], TimeSpan.Zero, false, "SWCAN wakeup after first IPC RX", MaxSends: 1),
            new ScheduledTxFrame(0x13FFE040, [], TimeSpan.Zero, true, "fake BCM/body gateway broadcast presence after first IPC RX", MaxSends: 1),
            new ScheduledTxFrame(0x621, CliOptions.ParseHexData("0140000000000000"), TimeSpan.Zero, false, "network-management initiate after first IPC RX", MaxSends: 1),
            new ScheduledTxFrame(0x621, CliOptions.ParseHexData("0040000000000000"), TimeSpan.Zero, false, "network-management steady keepalive seed after first IPC RX", MaxSends: 1),
            new ScheduledTxFrame(0x13FFE040, [], TimeSpan.FromMilliseconds(1000), true, "fake BCM/body gateway broadcast presence, source 0x40", TimeSpan.FromMilliseconds(1000)),
            new ScheduledTxFrame(0x621, CliOptions.ParseHexData("0040000000000000"), TimeSpan.FromMilliseconds(1000), false, "network-management steady keepalive", TimeSpan.FromMilliseconds(1000)),
        ];
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
        return new List<ScheduledTxFrame>
        {
            new ScheduledTxFrame(0x10050040, CliOptions.ParseHexData("00012C03A9000000"), TimeSpan.FromMilliseconds(75), true, "GMLAN Bible speed/RPM movement payload, arbid 0x028, sender 0x40", MaxSends: 90),
            new ScheduledTxFrame(0x10050040, CliOptions.ParseHexData("0000000000000000"), TimeSpan.Zero, true, "GMLAN speed/RPM zero-out frame", TimeSpan.FromMilliseconds(9500), MaxSends: 1),
        }.Select(frame => frame with { InitialDelay = frame.InitialDelay + offset }).ToList();
    }

    private static List<ScheduledTxFrame> Gmlan29SpeedRpmAltSourcePayloads(TimeSpan offset)
    {
        return new List<ScheduledTxFrame>
        {
            new ScheduledTxFrame(0x0C050040, CliOptions.ParseHexData("00012C03A9000000"), TimeSpan.FromMilliseconds(100), true, "priority 3 arbid 0x028 speed/RPM candidate, sender 0x40", MaxSends: 35),
            new ScheduledTxFrame(0x10050010, CliOptions.ParseHexData("00012C03A9000000"), TimeSpan.FromMilliseconds(100), true, "priority 4 arbid 0x028 speed/RPM candidate, engine-controller sender 0x10", TimeSpan.FromMilliseconds(4000), MaxSends: 35),
            new ScheduledTxFrame(0x10050028, CliOptions.ParseHexData("00012C03A9000000"), TimeSpan.FromMilliseconds(100), true, "priority 4 arbid 0x028 speed/RPM candidate, brake-controller sender 0x28", TimeSpan.FromMilliseconds(8000), MaxSends: 35),
            new ScheduledTxFrame(0x10050040, CliOptions.ParseHexData("0000000000000000"), TimeSpan.Zero, true, "GMLAN speed/RPM zero-out frame", TimeSpan.FromMilliseconds(12000), MaxSends: 1),
        }.Select(frame => frame with { InitialDelay = frame.InitialDelay + offset }).ToList();
    }

    private static List<ScheduledTxFrame> Gmlan29PowerModePayloads(TimeSpan offset)
    {
        return new List<ScheduledTxFrame>
        {
            new ScheduledTxFrame(0x10002040, CliOptions.ParseHexData("02"), TimeSpan.FromMilliseconds(3000), true, "system power mode short DLC value 0x02"),
            new ScheduledTxFrame(0x10002040, CliOptions.ParseHexData("03"), TimeSpan.FromMilliseconds(3000), true, "system power mode short DLC value 0x03", TimeSpan.FromMilliseconds(500)),
            new ScheduledTxFrame(0x10002040, CliOptions.ParseHexData("04"), TimeSpan.FromMilliseconds(3000), true, "system power mode short DLC value 0x04", TimeSpan.FromMilliseconds(1000)),
            new ScheduledTxFrame(0x10002040, CliOptions.ParseHexData("0200000000000000"), TimeSpan.FromMilliseconds(3000), true, "system power mode full DLC value 0x02", TimeSpan.FromMilliseconds(1500)),
            new ScheduledTxFrame(0x10002040, CliOptions.ParseHexData("0300000000000000"), TimeSpan.FromMilliseconds(3000), true, "system power mode full DLC value 0x03", TimeSpan.FromMilliseconds(2000)),
            new ScheduledTxFrame(0x10002040, CliOptions.ParseHexData("0400000000000000"), TimeSpan.FromMilliseconds(3000), true, "system power mode full DLC value 0x04", TimeSpan.FromMilliseconds(2500)),
        }.Select(frame => frame with { InitialDelay = frame.InitialDelay + offset }).ToList();
    }

    private static List<ScheduledTxFrame> Gmlan29EnvironmentPayloads(TimeSpan offset)
    {
        return new List<ScheduledTxFrame>
        {
            new ScheduledTxFrame(0x10030040, CliOptions.ParseHexData("0075A708"), TimeSpan.FromMilliseconds(1000), true, "battery voltage example, priority 4, arbid 0x018, sender 0x40"),
            new ScheduledTxFrame(0x0C030040, CliOptions.ParseHexData("0075A708"), TimeSpan.FromMilliseconds(1000), true, "battery voltage example, priority 3, arbid 0x018, sender 0x40", TimeSpan.FromMilliseconds(250)),
            new ScheduledTxFrame(0x100C2099, CliOptions.ParseHexData("0073"), TimeSpan.FromMilliseconds(2000), true, "outside air temp 17.5 C example, arbid 0x061, sender 0x99", TimeSpan.FromMilliseconds(500)),
            new ScheduledTxFrame(0x100C2099, CliOptions.ParseHexData("006C"), TimeSpan.FromMilliseconds(2000), true, "outside air temp 14.0 C example, arbid 0x061, sender 0x99", TimeSpan.FromMilliseconds(1500)),
        }.Select(frame => frame with { InitialDelay = frame.InitialDelay + offset }).ToList();
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

internal sealed record ProfileDefinition(
    string Name,
    string Description,
    double DefaultSeconds = 15.0,
    bool WaitForFirstRx = false,
    bool AutoIsoTpFlowControl = false,
    bool ListenOnly = false);
