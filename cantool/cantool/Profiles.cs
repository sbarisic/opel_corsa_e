namespace cantool;

internal static class Profiles
{
    private static readonly byte[] ConservativeFuzzValues = [0x00, 0x01, 0x02, 0x03, 0x04, 0x08, 0x10, 0x20, 0x40, 0x7F, 0x80, 0xC0, 0xFF];

    public const string ReadOnly = "read-only";
    public const string IpcReadOnlySniff = "ipc-read-only-sniff";
    public const string IpcAckSniff = "ipc-ack-sniff";
    public const string IpcPowerToggleWatch = "ipc-power-toggle-watch";
    public const string IpcWakePulseOnlyProbe = "ipc-wake-pulse-only-probe";
    public const string IpcWakeFirstProbe = "ipc-wake-first-probe";
    public const string IpcWakeRecoveryProbe = "ipc-wake-recovery-probe";
    public const string DefaultBench = "default-bench";
    public const string IpcSimulator = "ipc-simulator";
    public const string IpcSimulatorP4Speed = "ipc-simulator-p4-speed";
    public const string IpcSimulatorAltSources = "ipc-simulator-alt-sources";
    public const string IpcSimulatorSweep = "ipc-simulator-sweep";
    public const string IpcBcmLiveObjectProbe = "ipc-bcm-live-object-probe";
    public const string IpcRestartWakeOnlyProbe = "ipc-restart-wake-only-probe";
    public const string IpcRestartNmInitOnlyProbe = "ipc-restart-nm-init-only-probe";
    public const string IpcRestartPresenceOnlyProbe = "ipc-restart-presence-only-probe";
    public const string IpcRestartKeepaliveOnlyProbe = "ipc-restart-keepalive-only-probe";
    public const string IpcRestartWakeThenNmInitProbe = "ipc-restart-wake-then-nm-init-probe";
    public const string IpcDiagnosticProbe = "ipc-diagnostic-probe";
    public const string IpcDiagnosticSessionProbe = "ipc-diagnostic-session-probe";
    public const string IpcDiagnosticDidScan = "ipc-diagnostic-did-scan";
    public const string IpcDiagnosticF1RangeScan = "ipc-diagnostic-f1-range-scan";
    public const string IpcDiagnosticLocalRangeScan = "ipc-diagnostic-local-range-scan";
    public const string IpcDiagnosticLocalRangeSlowScan = "ipc-diagnostic-local-range-slow-scan";
    public const string IpcDiagnosticRead21LocalScan = "ipc-diagnostic-read21-local-scan";
    public const string IpcDiagnosticGmlanClassicProbe = "ipc-diagnostic-gmlan-classic-probe";
    public const string IpcDiagnosticIsolatedClassicProbe = "ipc-diagnostic-isolated-classic-probe";
    public const string IpcDiagnosticConfirmedIdentityProbe = "ipc-diagnostic-confirmed-identity-probe";
    public const string IpcDiagnosticClassic1AScan = "ipc-diagnostic-classic-1a-scan";
    public const string IpcDiagnosticClassic1AC0EfScan = "ipc-diagnostic-classic-1a-c0-ef-scan";
    public const string IpcDiagnosticAa00RepeatProbe = "ipc-diagnostic-aa00-repeat-probe";
    public const string IpcStandardEngineWarningProbe = "ipc-standard-engine-warning-probe";
    public const string IpcAstraHLsReferenceProbe = "ipc-astra-h-ls-reference-probe";
    public const string IpcStandardByteFuzz = "ipc-standard-byte-fuzz";
    public const string IpcGmlan29ByteFuzz = "ipc-gmlan29-byte-fuzz";
    public const string IpcGmOpelHmiProbe = "ipc-gm-opel-hmi-probe";
    public const string IpcNativeBodySeedProbe = "ipc-native-body-seed-probe";
    public const string IpcNativeContextLite = "ipc-native-context-lite";
    public const string IpcNativeKeyOnContext = "ipc-native-keyon-context";
    public const string IpcNativeKeyTransition = "ipc-native-key-transition";
    public const string IpcNativeExpandedKeyOn = "ipc-native-expanded-keyon";
    public const string IpcPriorityTier1Probe = "ipc-priority-tier1-probe";
    public const string IpcPriorityTier2Probe = "ipc-priority-tier2-probe";
    public const string IpcPriorityTier3Probe = "ipc-priority-tier3-probe";
    public const string IpcPriorityAllProbe = "ipc-priority-all-probe";
    public const string IpcPriorityTier1ByteFuzz = "ipc-priority-tier1-byte-fuzz";
    public const string IpcPriorityTier2ByteFuzz = "ipc-priority-tier2-byte-fuzz";
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
        new(ReadOnly, "Read-only sniff alias: no TX, listen until Ctrl+C by default", 0.0, ReadOnlyCapture: true),
        new(IpcReadOnlySniff, "Read-only sniff: no TX, listen until Ctrl+C by default", 0.0, ReadOnlyCapture: true),
        new(IpcAckSniff, "Read-only normal-mode sniff: no TX, but ACKs IPC frames", 0.0, ReadOnlyCapture: true, ReadOnlyListenOnly: false),
        new(IpcPowerToggleWatch, "No-flush normal-mode watch for pin 8 power/ignition toggles", 0.0, ReadOnlyCapture: true, ReadOnlyListenOnly: false, ReadOnlyFlushStale: false),
        new(IpcWakePulseOnlyProbe, "Wake isolation: only repeated SWCAN 100# wake pulses on silent bus", 30.0),
        new(IpcWakeFirstProbe, "Wake first: no wait-for-RX, wake/body context only, no diagnostics", 20.0),
        new(IpcWakeRecoveryProbe, "Wake recovery: no wait-for-RX, staged SWCAN wake/context/diagnostic nudges", 30.0),
        new(IpcSimulator, "Recommended staged IPC simulator: waits for first RX, then fake BCM/body gateway", 30.0, WaitForFirstRx: true),
        new(IpcSimulatorP4Speed, "Staged IPC simulator using priority-4 10050040 speed/RPM", 30.0, WaitForFirstRx: true),
        new(IpcSimulatorAltSources, "Staged IPC simulator cycling alternate speed/RPM source nodes", 45.0, WaitForFirstRx: true),
        new(IpcSimulatorSweep, "Staged IPC simulator sweeping priority-3 speed/RPM payload values", 30.0, WaitForFirstRx: true),
        new(IpcBcmLiveObjectProbe, "Quiet BCM-source variants of observed IPC startup/status frames", 45.0, WaitForFirstRx: true),
        new(IpcRestartWakeOnlyProbe, "Restart isolation: send only SWCAN wake 100# after first RX", 8.0, WaitForFirstRx: true),
        new(IpcRestartNmInitOnlyProbe, "Restart isolation: send only 621#0140 network-init burst after first RX", 8.0, WaitForFirstRx: true),
        new(IpcRestartPresenceOnlyProbe, "Restart isolation: send only 13FFE040 presence after first RX", 8.0, WaitForFirstRx: true),
        new(IpcRestartKeepaliveOnlyProbe, "Restart isolation: send only 621#0040 keepalive after first RX", 8.0, WaitForFirstRx: true),
        new(IpcRestartWakeThenNmInitProbe, "Restart isolation: 100# wake, pause, then 621#0140 burst", 12.0),
        new(IpcDiagnosticProbe, "Quiet post-RX IPC diagnostics 24C -> 64C", 12.0, WaitForFirstRx: true),
        new(IpcDiagnosticSessionProbe, "Quiet IPC diagnostic sessions, tester-present variants, and VIN DID", 16.0, WaitForFirstRx: true),
        new(IpcDiagnosticDidScan, "Quiet extended-session read-only IPC DID/DTC scan with ISO-TP flow control", 35.0, WaitForFirstRx: true, AutoIsoTpFlowControl: true),
        new(IpcDiagnosticF1RangeScan, "Quiet read-only scan of UDS F100-F1FF DIDs with ISO-TP flow control", 75.0, WaitForFirstRx: true, AutoIsoTpFlowControl: true),
        new(IpcDiagnosticLocalRangeScan, "Quiet read-only scan of local 0000-00FF DIDs with ISO-TP flow control", 75.0, WaitForFirstRx: true, AutoIsoTpFlowControl: true),
        new(IpcDiagnosticLocalRangeSlowScan, "Quiet settled slow read-only scan of local 0000-00FF DIDs", 145.0, WaitForFirstRx: true, AutoIsoTpFlowControl: true),
        new(IpcDiagnosticRead21LocalScan, "Quiet read-only scan of older 0x21 local identifiers 00-FF", 80.0, WaitForFirstRx: true, AutoIsoTpFlowControl: true),
        new(IpcDiagnosticGmlanClassicProbe, "Quiet classic GMLAN read-only diagnostic probe: 0x10, 0x3E, 0x1A, 0xAA", 36.0, WaitForFirstRx: true, AutoIsoTpFlowControl: true),
        new(IpcDiagnosticIsolatedClassicProbe, "Quiet isolated classic probe: chase 64C/54C responses without tester-present overlap", 45.0, WaitForFirstRx: true, AutoIsoTpFlowControl: true),
        new(IpcDiagnosticConfirmedIdentityProbe, "Fast confirmed IPC identity probe: 10 03, 013E, VIN 1A90, 1A9A, AA00", 18.0, WaitForFirstRx: true, AutoIsoTpFlowControl: true),
        new(IpcDiagnosticClassic1AScan, "Quiet read-only classic 0x1A identifier scan: 80-BF and F0-FF", 60.0, WaitForFirstRx: true, AutoIsoTpFlowControl: true),
        new(IpcDiagnosticClassic1AC0EfScan, "Quiet read-only classic 0x1A identifier scan: C0-EF gap", 36.0, WaitForFirstRx: true, AutoIsoTpFlowControl: true),
        new(IpcDiagnosticAa00RepeatProbe, "Quiet read-only AA 00 repeat probe: check if 54C response is stable", 30.0, WaitForFirstRx: true, AutoIsoTpFlowControl: true),
        new(IpcStandardEngineWarningProbe, "Standard 11-bit 0C9 RPM plus 1E5 warning/service probe", 30.0, WaitForFirstRx: true),
        new(IpcAstraHLsReferenceProbe, "Astra H LS-CAN/SWCAN reference probe: 108 speed/RPM, 145 engine, body lamps", 60.0, WaitForFirstRx: true),
        new(IpcStandardByteFuzz, "Conservative deterministic standard 11-bit byte fuzz", 300.0, WaitForFirstRx: true),
        new(IpcGmlan29ByteFuzz, "Conservative deterministic 29-bit GMLAN byte fuzz", 180.0, WaitForFirstRx: true),
        new(IpcGmOpelHmiProbe, "GM/Opel HMI/DIC/body probe: dimming, wheel buttons, key/body context, chime, DIC text", 90.0, WaitForFirstRx: true, AutoIsoTpFlowControl: true),
        new(IpcNativeBodySeedProbe, "Native Corsa E body seed replay from saved BCM captures; no wait-for-RX", 60.0),
        new(IpcNativeContextLite, "Gentle native Corsa E body context while toggling IPC ignition; no wait-for-RX", 45.0),
        new(IpcNativeKeyOnContext, "Focused native Corsa E key-on context from keyoff/keyon deltas; no wait-for-RX", 45.0),
        new(IpcNativeKeyTransition, "Native Corsa E key-off to key-on body context transition; no wait-for-RX", 45.0),
        new(IpcNativeExpandedKeyOn, "Expanded native Corsa E key-on context from all-ID state deltas; no wait-for-RX", 45.0),
        new(IpcPriorityTier1Probe, "Priority table tier 1 seed replay: power/body prerequisites", 45.0, WaitForFirstRx: true),
        new(IpcPriorityTier2Probe, "Priority table tier 2 seed replay: engine/speed/warnings", 45.0, WaitForFirstRx: true),
        new(IpcPriorityTier3Probe, "Priority table tier 3 seed replay: late body/powertrain/GMLAN probes", 60.0, WaitForFirstRx: true),
        new(IpcPriorityAllProbe, "Priority table all-tier seed replay in separated windows", 150.0, WaitForFirstRx: true),
        new(IpcPriorityTier1ByteFuzz, "Focused tier 1 byte fuzz after stable baseline", 180.0, WaitForFirstRx: true),
        new(IpcPriorityTier2ByteFuzz, "Focused tier 2 byte fuzz after stable baseline", 240.0, WaitForFirstRx: true),
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

    public static string ProfileNamesForUsage => string.Join("|", Available.Select(item => item.Name));

    public static ProfileDefinition? Find(string profile)
    {
        return Available.FirstOrDefault(item => item.Name.Equals(profile, StringComparison.OrdinalIgnoreCase));
    }

    public static List<ScheduledTxFrame> GetSchedule(string profile)
    {
        return profile.ToLowerInvariant() switch
        {
            IpcSimulator => IpcSimulatorSchedule(),
            IpcWakePulseOnlyProbe => IpcWakePulseOnlyProbeSchedule(),
            IpcWakeFirstProbe => IpcWakeFirstProbeSchedule(),
            IpcWakeRecoveryProbe => IpcWakeRecoveryProbeSchedule(),
            IpcSimulatorP4Speed => IpcSimulatorP4SpeedSchedule(),
            IpcSimulatorAltSources => IpcSimulatorAltSourcesSchedule(),
            IpcSimulatorSweep => IpcSimulatorSweepSchedule(),
            IpcBcmLiveObjectProbe => IpcBcmLiveObjectProbeSchedule(),
            IpcRestartWakeOnlyProbe => IpcRestartIsolationSchedule(IpcRestartWakeOnlyProbe),
            IpcRestartNmInitOnlyProbe => IpcRestartIsolationSchedule(IpcRestartNmInitOnlyProbe),
            IpcRestartPresenceOnlyProbe => IpcRestartIsolationSchedule(IpcRestartPresenceOnlyProbe),
            IpcRestartKeepaliveOnlyProbe => IpcRestartIsolationSchedule(IpcRestartKeepaliveOnlyProbe),
            IpcRestartWakeThenNmInitProbe => IpcRestartIsolationSchedule(IpcRestartWakeThenNmInitProbe),
            IpcDiagnosticProbe => IpcDiagnosticProbeSchedule(),
            IpcDiagnosticSessionProbe => IpcDiagnosticSessionProbeSchedule(),
            IpcDiagnosticDidScan => IpcDiagnosticDidScanSchedule(),
            IpcDiagnosticF1RangeScan => IpcDiagnosticRangeScanSchedule(0xF100, 0xF1FF, "F1xx standardized/manufacturer DID range"),
            IpcDiagnosticLocalRangeScan => IpcDiagnosticRangeScanSchedule(0x0000, 0x00FF, "local 00xx DID range"),
            IpcDiagnosticLocalRangeSlowScan => IpcDiagnosticRangeScanSchedule(0x0000, 0x00FF, "settled slow local 00xx DID range", TimeSpan.FromSeconds(8), TimeSpan.FromMilliseconds(500)),
            IpcDiagnosticRead21LocalScan => IpcDiagnosticRead21LocalScanSchedule(),
            IpcDiagnosticGmlanClassicProbe => IpcDiagnosticGmlanClassicProbeSchedule(),
            IpcDiagnosticIsolatedClassicProbe => IpcDiagnosticIsolatedClassicProbeSchedule(),
            IpcDiagnosticConfirmedIdentityProbe => IpcDiagnosticConfirmedIdentityProbeSchedule(),
            IpcDiagnosticClassic1AScan => IpcDiagnosticClassic1AScanSchedule(),
            IpcDiagnosticClassic1AC0EfScan => IpcDiagnosticClassic1AScanSchedule(Classic1AC0EfScanIdentifiers(), "classic 0x1A C0-EF gap scan"),
            IpcDiagnosticAa00RepeatProbe => IpcDiagnosticAa00RepeatProbeSchedule(),
            IpcStandardEngineWarningProbe => IpcStandardEngineWarningProbeSchedule(),
            IpcAstraHLsReferenceProbe => IpcAstraHLsReferenceProbeSchedule(),
            IpcStandardByteFuzz => IpcStandardByteFuzzSchedule(),
            IpcGmlan29ByteFuzz => IpcGmlan29ByteFuzzSchedule(),
            ReadOnly => [],
            IpcGmOpelHmiProbe => IpcGmOpelHmiProbeSchedule(),
            IpcNativeBodySeedProbe => IpcNativeBodySeedProbeSchedule(),
            IpcNativeContextLite => IpcNativeContextLiteSchedule(),
            IpcNativeKeyOnContext => IpcNativeKeyOnContextSchedule(),
            IpcNativeKeyTransition => IpcNativeKeyTransitionSchedule(),
            IpcNativeExpandedKeyOn => IpcNativeExpandedKeyOnSchedule(),
            IpcPriorityTier1Probe => IpcPriorityTier1ProbeSchedule(),
            IpcPriorityTier2Probe => IpcPriorityTier2ProbeSchedule(),
            IpcPriorityTier3Probe => IpcPriorityTier3ProbeSchedule(),
            IpcPriorityAllProbe => IpcPriorityAllProbeSchedule(),
            IpcPriorityTier1ByteFuzz => IpcPriorityTier1ByteFuzzSchedule(),
            IpcPriorityTier2ByteFuzz => IpcPriorityTier2ByteFuzzSchedule(),
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

    public static bool IsReadOnlyCapture(string profile)
    {
        return Available.Any(item => item.Name.Equals(profile, StringComparison.OrdinalIgnoreCase) && item.ReadOnlyCapture);
    }

    public static bool IsReadOnlyListenOnly(string profile)
    {
        return Available.FirstOrDefault(item => item.Name.Equals(profile, StringComparison.OrdinalIgnoreCase))?.ReadOnlyListenOnly ?? true;
    }

    public static bool ShouldReadOnlyFlushStale(string profile)
    {
        return Available.FirstOrDefault(item => item.Name.Equals(profile, StringComparison.OrdinalIgnoreCase))?.ReadOnlyFlushStale ?? true;
    }

    private static List<ScheduledTxFrame> DefaultBenchSchedule()
    {
        var schedule = NormalWakeSchedule();
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

    private static List<ScheduledTxFrame> IpcBcmLiveObjectProbeSchedule()
    {
        var schedule = IpcQuietBodyBaselineSchedule();

        AddPayloadCycleWindow(
            schedule,
            0x10244040,
            ["02", "06"],
            TimeSpan.FromMilliseconds(1000),
            true,
            "BCM-source variant of live IPC 10244060 status/state frame",
            TimeSpan.FromSeconds(2),
            TimeSpan.FromSeconds(40));

        AddPayloadCycleWindow(
            schedule,
            0x10424040,
            ["059964", "079964"],
            TimeSpan.FromMilliseconds(1000),
            true,
            "BCM-source variant of live IPC 10424060 status/config frame",
            TimeSpan.FromSeconds(2.1),
            TimeSpan.FromSeconds(40));

        AddPayloadCycleWindow(
            schedule,
            0x1045C040,
            ["44", "40"],
            TimeSpan.FromMilliseconds(2000),
            true,
            "BCM-source variant of live IPC 1045C060 byte-state frame",
            TimeSpan.FromSeconds(2.2),
            TimeSpan.FromSeconds(40));

        AddPayloadCycleWindow(
            schedule,
            0x10ACE040,
            ["1E00000000000000", "1E00200000000000", "2B00000000000000"],
            TimeSpan.FromMilliseconds(750),
            true,
            "BCM-source variant of live IPC 10ACE060 boot/status payload family",
            TimeSpan.FromSeconds(3),
            TimeSpan.FromSeconds(38));

        AddPayloadCycleWindow(
            schedule,
            0x10AE8040,
            ["030E010104050000", "0016010800000000"],
            TimeSpan.FromMilliseconds(750),
            true,
            "BCM-source variant of live IPC 10AE8060 boot/status payload family",
            TimeSpan.FromSeconds(3.15),
            TimeSpan.FromSeconds(38));

        AddPayloadCycleWindow(
            schedule,
            0x10B0A040,
            ["001E04", "002B04"],
            TimeSpan.FromMilliseconds(1500),
            true,
            "BCM-source variant of live IPC 10B0A060 short status family",
            TimeSpan.FromSeconds(3.3),
            TimeSpan.FromSeconds(38));

        AddPeriodicWindow(schedule, 0x10774040, "00", TimeSpan.FromMilliseconds(1000), true, "BCM-source variant of live IPC 10774060 one-byte status", TimeSpan.FromSeconds(4), TimeSpan.FromSeconds(42));
        AddPeriodicWindow(schedule, 0x1084A040, "00", TimeSpan.FromMilliseconds(1000), true, "BCM-source variant of live IPC 1084A060 one-byte status", TimeSpan.FromSeconds(4.1), TimeSpan.FromSeconds(42));
        AddPeriodicWindow(schedule, 0x10812040, "00", TimeSpan.FromMilliseconds(1000), true, "BCM-source variant of live IPC 10812060 one-byte status", TimeSpan.FromSeconds(4.2), TimeSpan.FromSeconds(42));
        AddPeriodicWindow(schedule, 0x10600040, "01609370015B00", TimeSpan.FromMilliseconds(5000), true, "BCM-source variant of live IPC 10600060 config/range payload", TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(40));
        AddPeriodicWindow(schedule, 0x10AFC040, "38FFFFFFFFFFFF2B", TimeSpan.FromMilliseconds(5000), true, "BCM-source variant of live IPC 10AFC060 full-state payload", TimeSpan.FromSeconds(6), TimeSpan.FromSeconds(38));

        AddOneShot(schedule, 0x103BC040, "00", true, "BCM-source boot/status one-shot variant of 103BC060", TimeSpan.FromSeconds(8));
        AddOneShot(schedule, 0x103D6040, "00", true, "BCM-source boot/status one-shot variant of 103D6060", TimeSpan.FromSeconds(8.2));
        AddOneShot(schedule, 0x103DA040, "00", true, "BCM-source boot/status one-shot variant of 103DA060", TimeSpan.FromSeconds(8.4));
        AddOneShot(schedule, 0x10448040, "00", true, "BCM-source boot/status one-shot variant of 10448060", TimeSpan.FromSeconds(8.6));
        AddOneShot(schedule, 0x10A04040, "00", true, "BCM-source boot/status one-shot variant of 10A04060", TimeSpan.FromSeconds(8.8));

        return schedule;
    }

    private static List<ScheduledTxFrame> IpcRestartIsolationSchedule(string profile)
    {
        return profile switch
        {
            IpcRestartWakeOnlyProbe =>
            [
                new ScheduledTxFrame(0x100, [], TimeSpan.Zero, false, "restart isolation: SWCAN wake 100# one-shot after first IPC RX", MaxSends: 1),
            ],
            IpcRestartNmInitOnlyProbe =>
            [
                new ScheduledTxFrame(0x621, CliOptions.ParseHexData("0140000000000000"), TimeSpan.FromMilliseconds(500), false, "restart isolation: 621#0140 network-init burst only, three sends after first IPC RX", MaxSends: 3),
            ],
            IpcRestartPresenceOnlyProbe =>
            [
                new ScheduledTxFrame(0x13FFE040, [], TimeSpan.FromMilliseconds(1000), true, "restart isolation: fake BCM/source 0x40 presence only"),
            ],
            IpcRestartKeepaliveOnlyProbe =>
            [
                new ScheduledTxFrame(0x621, CliOptions.ParseHexData("0040000000000000"), TimeSpan.FromMilliseconds(1000), false, "restart isolation: 621#0040 steady keepalive only"),
            ],
            IpcRestartWakeThenNmInitProbe =>
            [
                new ScheduledTxFrame(0x100, [], TimeSpan.Zero, false, "restart isolation: SWCAN wake 100# one-shot on otherwise silent IPC", MaxSends: 1),
                new ScheduledTxFrame(0x621, CliOptions.ParseHexData("0140000000000000"), TimeSpan.FromMilliseconds(500), false, "restart isolation: delayed 621#0140 network-init burst after wake", TimeSpan.FromSeconds(5), MaxSends: 3),
            ],
            _ => throw new ArgumentException($"unknown restart isolation profile: {profile}")
        };
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

    private static List<ScheduledTxFrame> IpcDiagnosticGmlanClassicProbeSchedule()
    {
        var schedule = IpcDiagnosticBaseSchedule();
        schedule.AddRange([
            new ScheduledTxFrame(0x24C, CliOptions.ParseHexData("021003AAAAAAAAAA"), TimeSpan.Zero, false, "classic GMLAN initiate diagnostic operation 0x10 0x03; known positive response is 64C#0150...", TimeSpan.FromSeconds(2), MaxSends: 1),
            new ScheduledTxFrame(0x24C, CliOptions.ParseHexData("023E00AAAAAAAAAA"), TimeSpan.FromMilliseconds(2000), false, "classic GMLAN tester-present keepalive; expect 64C#027E00... if supported", TimeSpan.FromSeconds(4), MaxSends: 12),
            new ScheduledTxFrame(0x24C, CliOptions.ParseHexData("021004AAAAAAAAAA"), TimeSpan.Zero, false, "classic GMLAN initiate diagnostic operation 0x10 0x04 wake-up-links probe", TimeSpan.FromSeconds(5), MaxSends: 1),
            new ScheduledTxFrame(0x24C, CliOptions.ParseHexData("021A90AAAAAAAAAA"), TimeSpan.Zero, false, "classic GMLAN read-data-by-identifier 0x1A 0x90 candidate", TimeSpan.FromSeconds(8), MaxSends: 1),
            new ScheduledTxFrame(0x24C, CliOptions.ParseHexData("021A91AAAAAAAAAA"), TimeSpan.Zero, false, "classic GMLAN read-data-by-identifier 0x1A 0x91 candidate", TimeSpan.FromSeconds(11), MaxSends: 1),
            new ScheduledTxFrame(0x24C, CliOptions.ParseHexData("021A92AAAAAAAAAA"), TimeSpan.Zero, false, "classic GMLAN read-data-by-identifier 0x1A 0x92 candidate", TimeSpan.FromSeconds(14), MaxSends: 1),
            new ScheduledTxFrame(0x24C, CliOptions.ParseHexData("021A97AAAAAAAAAA"), TimeSpan.Zero, false, "classic GMLAN read-data-by-identifier 0x1A 0x97 candidate", TimeSpan.FromSeconds(17), MaxSends: 1),
            new ScheduledTxFrame(0x24C, CliOptions.ParseHexData("021A9AAAAAAAAAAA"), TimeSpan.Zero, false, "classic GMLAN read-data-by-identifier 0x1A 0x9A candidate", TimeSpan.FromSeconds(20), MaxSends: 1),
            new ScheduledTxFrame(0x24C, CliOptions.ParseHexData("02AA00AAAAAAAAAA"), TimeSpan.Zero, false, "classic GMLAN read-data-by-packet-identifier 0xAA 0x00 candidate", TimeSpan.FromSeconds(23), MaxSends: 1),
            new ScheduledTxFrame(0x24C, CliOptions.ParseHexData("02AA01AAAAAAAAAA"), TimeSpan.Zero, false, "classic GMLAN read-data-by-packet-identifier 0xAA 0x01 candidate", TimeSpan.FromSeconds(26), MaxSends: 1),
            new ScheduledTxFrame(0x24C, CliOptions.ParseHexData("02AA02AAAAAAAAAA"), TimeSpan.Zero, false, "classic GMLAN read-data-by-packet-identifier 0xAA 0x02 candidate", TimeSpan.FromSeconds(29), MaxSends: 1),
            new ScheduledTxFrame(0x24C, CliOptions.ParseHexData("023E00AAAAAAAAAA"), TimeSpan.Zero, false, "classic GMLAN final tester-present check", TimeSpan.FromSeconds(32), MaxSends: 1),
        ]);
        return schedule;
    }

    private static List<ScheduledTxFrame> IpcDiagnosticIsolatedClassicProbeSchedule()
    {
        var schedule = IpcDiagnosticBaseSchedule();
        var requests = new (string Payload, string Note)[]
        {
            ("021003AAAAAAAAAA", "isolated classic GMLAN session/init 0x10 0x03; known useful response is 64C#0150..."),
            ("013EAAAAAAAAAAAA", "isolated tester-present service 0x3E without subfunction; watch 64C and 54C"),
            ("023E80AAAAAAAAAA", "isolated tester-present 0x3E 0x80 suppress-positive-response variant; watch 64C and 54C"),
            ("021A90AAAAAAAAAA", "isolated classic read-data-by-identifier 0x1A 0x90; watch 64C and 54C"),
            ("021A91AAAAAAAAAA", "isolated classic read-data-by-identifier 0x1A 0x91; watch 64C and 54C"),
            ("021A92AAAAAAAAAA", "isolated classic read-data-by-identifier 0x1A 0x92; watch 64C and 54C"),
            ("021A97AAAAAAAAAA", "isolated classic read-data-by-identifier 0x1A 0x97; watch 64C and 54C"),
            ("021A9AAAAAAAAAAA", "isolated classic read-data-by-identifier 0x1A 0x9A; watch 64C and 54C"),
        };

        var delay = TimeSpan.FromSeconds(2);
        var spacing = TimeSpan.FromMilliseconds(1750);
        foreach (var (payload, note) in requests)
        {
            schedule.Add(new ScheduledTxFrame(0x24C, CliOptions.ParseHexData(payload), TimeSpan.Zero, false, note, delay, MaxSends: 1));
            delay += spacing;
        }

        for (var packetId = 0x00; packetId <= 0x0F; packetId++)
        {
            var payload = $"02AA{packetId:X2}AAAAAAAAAA";
            var note = packetId == 0
                ? "isolated classic read-data-by-packet 0xAA 0x00; highest-value lead, watch for 64C and 54C#0000000000000000"
                : $"isolated classic read-data-by-packet 0xAA 0x{packetId:X2}; watch 64C and 54C within 250 ms";
            schedule.Add(new ScheduledTxFrame(0x24C, CliOptions.ParseHexData(payload), TimeSpan.Zero, false, note, delay, MaxSends: 1));
            delay += spacing;
        }

        return schedule;
    }

    private static List<ScheduledTxFrame> IpcDiagnosticConfirmedIdentityProbeSchedule()
    {
        var schedule = IpcDiagnosticBaseSchedule();
        var requests = new (double DelaySeconds, string Payload, string Note)[]
        {
            (2.0, "021003AAAAAAAAAA", "confirmed identity probe session/init 0x10 0x03; expected 64C#0150..."),
            (4.0, "013EAAAAAAAAAAAA", "confirmed identity probe tester-present without subfunction; expected 64C#017E..."),
            (6.0, "021A90AAAAAAAAAA", "confirmed identity probe VIN read DID 0x90; expected positive 64C#10135A90..."),
            (9.0, "021A9AAAAAAAAAAA", "confirmed identity probe DID 0x9A; expected 64C#045A9A0A5F..."),
            (12.0, "02AA00AAAAAAAAAA", "confirmed identity probe packet 0xAA 0x00; expected 54C#0000000000000000"),
            (15.0, "013EAAAAAAAAAAAA", "confirmed identity probe final tester-present without subfunction")
        };

        foreach (var (delaySeconds, payload, note) in requests)
        {
            schedule.Add(new ScheduledTxFrame(
                0x24C,
                CliOptions.ParseHexData(payload),
                TimeSpan.Zero,
                false,
                note,
                TimeSpan.FromSeconds(delaySeconds),
                MaxSends: 1));
        }

        return schedule;
    }

    private static List<ScheduledTxFrame> IpcDiagnosticClassic1AScanSchedule()
    {
        return IpcDiagnosticClassic1AScanSchedule(Classic1AScanIdentifiers(), "classic 0x1A scan");
    }

    private static List<ScheduledTxFrame> IpcDiagnosticClassic1AScanSchedule(IEnumerable<int> identifiers, string notePrefix)
    {
        var schedule = IpcDiagnosticBaseSchedule();
        schedule.Add(new ScheduledTxFrame(
            0x24C,
            CliOptions.ParseHexData("021003AAAAAAAAAA"),
            TimeSpan.Zero,
            false,
            $"{notePrefix} session/init 0x10 0x03; known useful response is 64C#0150...",
            TimeSpan.FromSeconds(2),
            MaxSends: 1));

        var delay = TimeSpan.FromSeconds(4);
        var sentSinceTesterPresent = 0;
        foreach (var identifier in identifiers)
        {
            if (sentSinceTesterPresent >= 16)
            {
                schedule.Add(new ScheduledTxFrame(
                    0x24C,
                    CliOptions.ParseHexData("013EAAAAAAAAAAAA"),
                    TimeSpan.Zero,
                    false,
                    $"{notePrefix} tester-present without subfunction; known useful response is 64C#017E...",
                    delay,
                    MaxSends: 1));
                delay += TimeSpan.FromMilliseconds(500);
                sentSinceTesterPresent = 0;
            }

            var payload = $"021A{identifier:X2}AAAAAAAAAA";
            schedule.Add(new ScheduledTxFrame(
                0x24C,
                CliOptions.ParseHexData(payload),
                TimeSpan.Zero,
                false,
                $"classic read-data-by-identifier 0x1A 0x{identifier:X2}; watch for positive 64C#..5A{identifier:X2}",
                delay,
                MaxSends: 1));
            delay += TimeSpan.FromMilliseconds(500);
            sentSinceTesterPresent++;
        }

        return schedule;
    }

    private static List<ScheduledTxFrame> IpcDiagnosticAa00RepeatProbeSchedule()
    {
        var schedule = IpcDiagnosticBaseSchedule();
        schedule.AddRange([
            new ScheduledTxFrame(0x24C, CliOptions.ParseHexData("021003AAAAAAAAAA"), TimeSpan.Zero, false, "AA 00 repeat probe session/init 0x10 0x03; known useful response is 64C#0150...", TimeSpan.FromSeconds(2), MaxSends: 1),
            new ScheduledTxFrame(0x24C, CliOptions.ParseHexData("013EAAAAAAAAAAAA"), TimeSpan.Zero, false, "AA 00 repeat probe tester-present without subfunction; known useful response is 64C#017E...", TimeSpan.FromSeconds(3), MaxSends: 1),
        ]);

        var delay = TimeSpan.FromSeconds(5);
        for (var i = 1; i <= 10; i++)
        {
            schedule.Add(new ScheduledTxFrame(
                0x24C,
                CliOptions.ParseHexData("02AA00AAAAAAAAAA"),
                TimeSpan.Zero,
                false,
                $"AA 00 repeat probe {i}/10; watch for repeated 54C#0000000000000000 or any changing 54C/64C response",
                delay,
                MaxSends: 1));
            delay += TimeSpan.FromSeconds(2);
        }

        schedule.Add(new ScheduledTxFrame(
            0x24C,
            CliOptions.ParseHexData("013EAAAAAAAAAAAA"),
            TimeSpan.Zero,
            false,
            "AA 00 repeat probe final tester-present without subfunction",
            delay,
            MaxSends: 1));

        return schedule;
    }

    private static IEnumerable<int> Classic1AScanIdentifiers()
    {
        for (var identifier = 0x80; identifier <= 0xBF; identifier++)
        {
            yield return identifier;
        }

        for (var identifier = 0xF0; identifier <= 0xFF; identifier++)
        {
            yield return identifier;
        }
    }

    private static IEnumerable<int> Classic1AC0EfScanIdentifiers()
    {
        for (var identifier = 0xC0; identifier <= 0xEF; identifier++)
        {
            yield return identifier;
        }
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

    private static List<ScheduledTxFrame> IpcAstraHLsReferenceProbeSchedule()
    {
        var schedule = IpcPriorityBaselineSchedule();
        var start = TimeSpan.FromSeconds(3);
        var end = TimeSpan.FromSeconds(58);
        var duration = end - start;

        AddPeriodicWindow(schedule, 0x108, "2320980004E50000", TimeSpan.FromMilliseconds(50), false, "Astra H LS-CAN speed/RPM sample: about 65 km/h and 1253 rpm", start, end);
        AddPayloadCycleWindow(schedule, 0x145, ["2000015000040000", "00011050A0040000"], TimeSpan.FromMilliseconds(100), false, "Astra H LS-CAN engine state/coolant candidate", start + TimeSpan.FromMilliseconds(10), duration);
        AddPeriodicWindow(schedule, 0x160, "0210C803", TimeSpan.FromMilliseconds(250), false, "Astra H LS-CAN 0x160 ignition/power-style reference", start + TimeSpan.FromMilliseconds(20), end);
        AddPeriodicWindow(schedule, 0x170, "20000300", TimeSpan.FromMilliseconds(500), false, "Astra H LS-CAN key-in/ignition-style reference", start + TimeSpan.FromMilliseconds(30), end);
        AddPeriodicWindow(schedule, 0x235, "00FF", TimeSpan.FromMilliseconds(1000), false, "Astra H LS-CAN LED brightness full", start + TimeSpan.FromMilliseconds(40), end);
        AddPayloadCycleWindow(schedule, 0x260, ["000000", "25437F", "000000", "3A437F", "000000", "5F327F"], TimeSpan.FromMilliseconds(500), false, "Astra H LS-CAN turn/hazard reference cycle", start + TimeSpan.FromSeconds(4), TimeSpan.FromSeconds(24));
        AddPeriodicWindow(schedule, 0x375, "00A0", TimeSpan.FromMilliseconds(100), false, "Astra H LS-CAN fuel level reference", start + TimeSpan.FromMilliseconds(60), end);
        AddPeriodicWindow(schedule, 0x445, "0073", TimeSpan.FromMilliseconds(1000), false, "Astra H LS-CAN outside temperature reference, about 17.5 C", start + TimeSpan.FromMilliseconds(70), end);
        AddPeriodicWindow(schedule, 0x500, "005C", TimeSpan.FromMilliseconds(1500), false, "Astra H LS-CAN voltage reference", start + TimeSpan.FromMilliseconds(80), end);

        schedule.Add(new ScheduledTxFrame(0x108, CliOptions.ParseHexData("1300000000000000"), TimeSpan.Zero, false, "Astra H LS-CAN speed/RPM zero-out", TimeSpan.FromSeconds(58.5), MaxSends: 1));
        schedule.Add(new ScheduledTxFrame(0x260, CliOptions.ParseHexData("000000"), TimeSpan.Zero, false, "Astra H LS-CAN turn/hazard off", TimeSpan.FromSeconds(58.7), MaxSends: 1));
        return schedule;
    }

    private static List<ScheduledTxFrame> IpcGmOpelHmiProbeSchedule()
    {
        var schedule = IpcPriorityBaselineSchedule();

        AddPayloadCycleWindow(
            schedule,
            0x10022040,
            ["0000000000000000", "007F000000000000", "00FF000000000000"],
            TimeSpan.FromMilliseconds(1000),
            true,
            "GM low-speed GMLAN dimming information arbid 0x011",
            TimeSpan.FromSeconds(3),
            TimeSpan.FromSeconds(6));

        AddPayloadCycleWindow(
            schedule,
            0x0EB,
            ["0000", "007F", "00FF"],
            TimeSpan.FromMilliseconds(1000),
            false,
            "Opel 11-bit panel brightness byte-1 ramp reference",
            TimeSpan.FromSeconds(9),
            TimeSpan.FromSeconds(6));

        var buttonDelay = TimeSpan.FromSeconds(16);
        var buttonPresses = new (string Payload, string Note)[]
        {
            ("0100000000000000", "Astra J / GM HMI steering-wheel volume up"),
            ("0200000000000000", "Astra J / GM HMI steering-wheel volume down"),
            ("0300000000000000", "Astra J / GM HMI steering-wheel next track"),
            ("0400000000000000", "Astra J / GM HMI steering-wheel previous track"),
            ("0500000000000000", "Astra J / GM HMI steering-wheel source"),
            ("0600000000000000", "Astra J / GM HMI steering-wheel voice"),
            ("0700000000000000", "Astra J / GM HMI steering-wheel mute"),
        };

        foreach (var (payload, note) in buttonPresses)
        {
            AddOneShot(schedule, 0x10438040, payload, true, note, buttonDelay);
            AddOneShot(schedule, 0x10438040, "0000000000000000", true, "Astra J / GM HMI steering-wheel button release", buttonDelay + TimeSpan.FromMilliseconds(150));
            buttonDelay += TimeSpan.FromMilliseconds(1400);
        }

        AddOneShot(schedule, 0x10754040, "0400000000000000", true, "Astra J key-present context candidate", TimeSpan.FromSeconds(28));
        AddOneShot(schedule, 0x10754040, "0000000000000000", true, "Astra J key-present context clear/no-key candidate", TimeSpan.FromSeconds(31));
        AddOneShot(schedule, 0x10630040, "0100000000000000", true, "Astra J tentative driver-door/body-state candidate", TimeSpan.FromSeconds(32));
        AddOneShot(schedule, 0x10630040, "0000000000000000", true, "Astra J tentative driver-door/body-state clear", TimeSpan.FromSeconds(35));
        AddOneShot(schedule, 0x10414040, "0100000000000000", true, "Astra J tentative central-lock/body-state candidate", TimeSpan.FromSeconds(36));
        AddOneShot(schedule, 0x10414040, "0000000000000000", true, "Astra J tentative central-lock/body-state clear", TimeSpan.FromSeconds(39));

        AddPayloadCycleWindow(
            schedule,
            0x0AA,
            ["600000", "740000", "760000"],
            TimeSpan.FromMilliseconds(1000),
            false,
            "Corsa D Opel key/ignition byte-0 reference",
            TimeSpan.FromSeconds(41),
            TimeSpan.FromSeconds(6));

        AddPayloadCycleWindow(
            schedule,
            0x131,
            ["000000", "000040", "000080", "000090", "0000A0"],
            TimeSpan.FromMilliseconds(1000),
            false,
            "Corsa D Opel exterior-light switch byte-2 reference",
            TimeSpan.FromSeconds(48),
            TimeSpan.FromSeconds(8));

        AddOneShot(schedule, 0x15E, "06019220", false, "Opel low-speed side/reverse/lights reference", TimeSpan.FromSeconds(57));

        AddOneShot(schedule, 0x1001E058, "867805FF05", true, "GMLAN chime command candidate, source 0x58", TimeSpan.FromSeconds(61));
        AddOneShot(schedule, 0x1001E040, "867805FF05", true, "GMLAN chime command candidate, fake BCM/source 0x40", TimeSpan.FromSeconds(65));
        AddOneShot(schedule, 0x1001E060, "867805FF05", true, "GMLAN chime source 0x60 variant; may look IPC-origin", TimeSpan.FromSeconds(69));

        AddOneShot(schedule, 0x10300040, "0000000000000000", true, "DIC ARB text general attributes probe arbid 0x180", TimeSpan.FromSeconds(73));
        AddOneShot(schedule, 0x10302040, "0000000000000000", true, "DIC ARB text line attributes probe arbid 0x181", TimeSpan.FromSeconds(74));
        AddOneShot(schedule, 0x10304040, "0000000000000000", true, "DIC ARB text set icon probe arbid 0x182", TimeSpan.FromSeconds(75));
        AddOneShot(schedule, 0x10308040, "0000000000000000", true, "DIC ARB text menu action/status probe arbid 0x184", TimeSpan.FromSeconds(76));
        AddOneShot(schedule, 0x1030A040, "0000000000000000", true, "DIC ARB text display parameters probe arbid 0x185", TimeSpan.FromSeconds(77));
        AddOneShot(schedule, 0x1030C040, "5445535420202020", true, "DIC ARB text display text probe: TEST", TimeSpan.FromSeconds(78));

        AddOneShot(schedule, 0x10438040, "0000000000000000", true, "final HMI button release/neutral", TimeSpan.FromSeconds(86));
        AddOneShot(schedule, 0x10754040, "0000000000000000", true, "final key-present neutral", TimeSpan.FromSeconds(86.2));
        AddOneShot(schedule, 0x10630040, "0000000000000000", true, "final door/body-state neutral", TimeSpan.FromSeconds(86.4));
        AddOneShot(schedule, 0x10414040, "0000000000000000", true, "final lock/body-state neutral", TimeSpan.FromSeconds(86.6));

        return schedule;
    }

    private static List<ScheduledTxFrame> IpcGmlan29ByteFuzzSchedule()
    {
        var schedule = new List<ScheduledTxFrame>
        {
            new(0x100, [], TimeSpan.Zero, false, "SWCAN wakeup after first IPC RX", MaxSends: 1),
            new(0x13FFE040, [], TimeSpan.Zero, true, "fake BCM/body gateway broadcast presence after first IPC RX", MaxSends: 1),
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

    private static List<ScheduledTxFrame> IpcPriorityTier1ProbeSchedule()
    {
        var schedule = IpcPriorityBaselineSchedule();
        AddTier1SeedWindow(schedule, TimeSpan.FromSeconds(3), TimeSpan.FromSeconds(40));
        return schedule;
    }

    private static List<ScheduledTxFrame> IpcNativeBodySeedProbeSchedule()
    {
        var schedule = NormalWakeSchedule();
        var start = TimeSpan.FromSeconds(2);
        var end = TimeSpan.FromSeconds(58);
        AddPayloadCycleWindow(
            schedule,
            0x0F1,
            ["000000400000", "1C0000400000", "280000400000", "340000400000"],
            TimeSpan.FromMilliseconds(15),
            false,
            "native Corsa E fast BCM/body state cycle 0x0F1 from saved captures",
            start,
            end - start);
        AddPeriodicWindow(schedule, 0x451, "000000000000", TimeSpan.FromMilliseconds(20), false, "native Corsa E display/body status 0x451", start + TimeSpan.FromMilliseconds(5), end);
        AddPayloadCycleWindow(
            schedule,
            0x12A,
            ["0006606B00000080", "0000606B00000080", "0000605900000080", "0000605D00008080", "0006605E00008080", "0006605D00008080"],
            TimeSpan.FromMilliseconds(100),
            false,
            "native Corsa E door/belt/body/DIC 0x12A observed variants",
            start + TimeSpan.FromMilliseconds(20),
            end - start);
        AddPayloadCycleWindow(
            schedule,
            0x135,
            ["04080A0002180000", "04080D0002180000", "00080A0002180000", "00080D0002180000", "00080D0202180000"],
            TimeSpan.FromMilliseconds(100),
            false,
            "native Corsa E packed body/display status 0x135 observed variants",
            start + TimeSpan.FromMilliseconds(40),
            end - start);
        AddPayloadCycleWindow(
            schedule,
            0x137,
            ["0000000000000000", "0000000030000000"],
            TimeSpan.FromMilliseconds(100),
            false,
            "native Corsa E packed body/display status 0x137 observed variants",
            start + TimeSpan.FromMilliseconds(60),
            end - start);
        AddPeriodicWindow(schedule, 0x139, "0000000000000000", TimeSpan.FromMilliseconds(100), false, "native Corsa E body/display status 0x139", start + TimeSpan.FromMilliseconds(80), end);
        AddPayloadCycleWindow(
            schedule,
            0x160,
            ["803C96B503", "0000000000"],
            TimeSpan.FromMilliseconds(100),
            false,
            "native Corsa E compact power/body state 0x160 observed variants",
            start + TimeSpan.FromMilliseconds(100),
            end - start);
        AddPayloadCycleWindow(
            schedule,
            0x1F1,
            ["AE0F173E18000072", "800E000018000072", "850F173E18000072", "AB0F173E18000072"],
            TimeSpan.FromMilliseconds(100),
            false,
            "native Corsa E power/ignition/environment 0x1F1 observed variants",
            start + TimeSpan.FromMilliseconds(120),
            end - start);
        AddPeriodicWindow(schedule, 0x4E1, "4B34303238383139", TimeSpan.FromMilliseconds(1000), false, "native Corsa E VIN/config fragment 0x4E1", start + TimeSpan.FromMilliseconds(200), end);
        AddPeriodicWindow(schedule, 0x514, "3056305845503638", TimeSpan.FromMilliseconds(1000), false, "native Corsa E VIN/config fragment 0x514", start + TimeSpan.FromMilliseconds(300), end);
        return schedule;
    }

    private static List<ScheduledTxFrame> IpcNativeContextLiteSchedule()
    {
        var schedule = NormalWakeSchedule();
        schedule.Add(new ScheduledTxFrame(0x10002040, CliOptions.ParseHexData("03"), TimeSpan.FromMilliseconds(250), true, "lite native context system power mode hold 0x03", TimeSpan.FromMilliseconds(500), MaxSends: 170));

        var start = TimeSpan.FromMilliseconds(750);
        var end = TimeSpan.FromSeconds(44);
        AddPayloadCycleWindow(
            schedule,
            0x0F1,
            ["000000400000", "1C0000400000", "280000400000", "340000400000"],
            TimeSpan.FromMilliseconds(60),
            false,
            "lite native Corsa E BCM/body state cycle 0x0F1, slower than captured bus",
            start,
            end - start);
        AddPeriodicWindow(schedule, 0x451, "000000000000", TimeSpan.FromMilliseconds(100), false, "lite native Corsa E display/body status 0x451", start + TimeSpan.FromMilliseconds(10), end);
        AddPayloadCycleWindow(
            schedule,
            0x12A,
            ["0006606B00000080", "0000606B00000080", "0000605900000080", "0000605D00008080"],
            TimeSpan.FromMilliseconds(500),
            false,
            "lite native Corsa E door/belt/body/DIC 0x12A observed variants",
            start + TimeSpan.FromMilliseconds(40),
            end - start);
        AddPayloadCycleWindow(
            schedule,
            0x135,
            ["04080A0002180000", "04080D0002180000", "00080A0002180000", "00080D0002180000"],
            TimeSpan.FromMilliseconds(500),
            false,
            "lite native Corsa E packed body/display status 0x135 observed variants",
            start + TimeSpan.FromMilliseconds(80),
            end - start);
        AddPayloadCycleWindow(
            schedule,
            0x137,
            ["0000000000000000", "0000000030000000"],
            TimeSpan.FromMilliseconds(500),
            false,
            "lite native Corsa E packed body/display status 0x137 observed variants",
            start + TimeSpan.FromMilliseconds(120),
            end - start);
        AddPeriodicWindow(schedule, 0x139, "0000000000000000", TimeSpan.FromMilliseconds(500), false, "lite native Corsa E body/display status 0x139", start + TimeSpan.FromMilliseconds(160), end);
        AddPayloadCycleWindow(
            schedule,
            0x160,
            ["803C96B503", "0000000000"],
            TimeSpan.FromMilliseconds(500),
            false,
            "lite native Corsa E compact power/body state 0x160 observed variants",
            start + TimeSpan.FromMilliseconds(200),
            end - start);
        AddPayloadCycleWindow(
            schedule,
            0x1F1,
            ["AE0F173E18000072", "800E000018000072", "850F173E18000072", "AB0F173E18000072"],
            TimeSpan.FromMilliseconds(500),
            false,
            "lite native Corsa E power/ignition/environment 0x1F1 observed variants",
            start + TimeSpan.FromMilliseconds(240),
            end - start);
        AddPeriodicWindow(schedule, 0x4E1, "4B34303238383139", TimeSpan.FromMilliseconds(2000), false, "lite native Corsa E VIN/config fragment 0x4E1", start + TimeSpan.FromMilliseconds(300), end);
        AddPeriodicWindow(schedule, 0x514, "3056305845503638", TimeSpan.FromMilliseconds(2000), false, "lite native Corsa E VIN/config fragment 0x514", start + TimeSpan.FromMilliseconds(400), end);
        return schedule;
    }

    private static List<ScheduledTxFrame> IpcNativeKeyOnContextSchedule()
    {
        var schedule = NormalWakeSchedule();
        schedule.Add(new ScheduledTxFrame(0x10002040, CliOptions.ParseHexData("03"), TimeSpan.FromMilliseconds(100), true, "key-on context GMLAN system power mode hold 0x03", TimeSpan.FromMilliseconds(500), MaxSends: 430));

        var start = TimeSpan.FromMilliseconds(700);
        var end = TimeSpan.FromSeconds(44);
        AddPayloadCycleWindow(
            schedule,
            0x0F1,
            ["000000400000", "1C0000400000", "280000400000", "340000400000"],
            TimeSpan.FromMilliseconds(30),
            false,
            "key-on native fast BCM/body state cycle 0x0F1",
            start,
            end - start);
        AddPeriodicWindow(schedule, 0x451, "000000000000", TimeSpan.FromMilliseconds(50), false, "key-on native body/display status 0x451", start + TimeSpan.FromMilliseconds(5), end);
        AddPeriodicWindow(schedule, 0x160, "803C96B503", TimeSpan.FromMilliseconds(100), false, "key-on native compact power/body state 0x160 from keyoff/keyon delta", start + TimeSpan.FromMilliseconds(10), end);
        AddPeriodicWindow(schedule, 0x1F1, "AE0F173E18000072", TimeSpan.FromMilliseconds(100), false, "key-on native ignition/environment state 0x1F1 from keyoff/keyon delta", start + TimeSpan.FromMilliseconds(20), end);
        AddPeriodicWindow(schedule, 0x12A, "0000605D00008080", TimeSpan.FromMilliseconds(100), false, "key-on native door/belt/body/DIC state 0x12A dominant", start + TimeSpan.FromMilliseconds(30), end);
        AddPeriodicWindow(schedule, 0x135, "04080D0002180000", TimeSpan.FromMilliseconds(100), false, "key-on native packed body/display status 0x135 dominant", start + TimeSpan.FromMilliseconds(40), end);
        AddPeriodicWindow(schedule, 0x137, "0000000000000000", TimeSpan.FromMilliseconds(100), false, "key-on native packed body/display status 0x137 dominant", start + TimeSpan.FromMilliseconds(50), end);
        AddPeriodicWindow(schedule, 0x139, "0000000000000000", TimeSpan.FromMilliseconds(100), false, "key-on native body/display status 0x139", start + TimeSpan.FromMilliseconds(60), end);
        AddPeriodicWindow(schedule, 0x4E1, "4B34303238383139", TimeSpan.FromMilliseconds(1000), false, "key-on native VIN/config fragment 0x4E1", start + TimeSpan.FromMilliseconds(100), end);
        AddPeriodicWindow(schedule, 0x514, "3056305845503638", TimeSpan.FromMilliseconds(1000), false, "key-on native VIN/config fragment 0x514", start + TimeSpan.FromMilliseconds(200), end);
        return schedule;
    }

    private static List<ScheduledTxFrame> IpcNativeKeyTransitionSchedule()
    {
        var schedule = NormalWakeSchedule();

        var keyOffStart = TimeSpan.FromMilliseconds(700);
        var keyOffEnd = TimeSpan.FromSeconds(4);
        AddPayloadCycleWindow(
            schedule,
            0x0F1,
            ["1C0000400000", "280000400000", "340000400000", "000000400000"],
            TimeSpan.FromMilliseconds(40),
            false,
            "transition key-off native BCM/body state cycle 0x0F1",
            keyOffStart,
            keyOffEnd - keyOffStart);
        AddPeriodicWindow(schedule, 0x451, "000000000000", TimeSpan.FromMilliseconds(50), false, "transition key-off body/display status 0x451", keyOffStart + TimeSpan.FromMilliseconds(5), keyOffEnd);
        AddPeriodicWindow(schedule, 0x160, "0000000000", TimeSpan.FromMilliseconds(100), false, "transition key-off compact power/body state 0x160", keyOffStart + TimeSpan.FromMilliseconds(10), keyOffEnd);
        AddPeriodicWindow(schedule, 0x1F1, "800E000018000072", TimeSpan.FromMilliseconds(100), false, "transition key-off ignition/environment state 0x1F1", keyOffStart + TimeSpan.FromMilliseconds(20), keyOffEnd);
        AddPeriodicWindow(schedule, 0x12A, "0000605D00000080", TimeSpan.FromMilliseconds(100), false, "transition key-off door/belt/body/DIC state 0x12A", keyOffStart + TimeSpan.FromMilliseconds(30), keyOffEnd);
        AddPeriodicWindow(schedule, 0x135, "00080D0002180000", TimeSpan.FromMilliseconds(100), false, "transition key-off packed body/display status 0x135", keyOffStart + TimeSpan.FromMilliseconds(40), keyOffEnd);
        AddPeriodicWindow(schedule, 0x137, "0000000000000000", TimeSpan.FromMilliseconds(100), false, "transition key-off packed body/display status 0x137", keyOffStart + TimeSpan.FromMilliseconds(50), keyOffEnd);
        AddPeriodicWindow(schedule, 0x139, "0000000000000000", TimeSpan.FromMilliseconds(100), false, "transition key-off body/display status 0x139", keyOffStart + TimeSpan.FromMilliseconds(60), keyOffEnd);

        schedule.Add(new ScheduledTxFrame(0x100, [], TimeSpan.Zero, false, "transition key-on edge SWCAN wake one-shot", TimeSpan.FromSeconds(4), MaxSends: 1));
        schedule.Add(new ScheduledTxFrame(0x10002040, CliOptions.ParseHexData("03"), TimeSpan.FromMilliseconds(100), true, "transition key-on GMLAN system power mode hold 0x03", TimeSpan.FromSeconds(4), MaxSends: 400));

        var keyOnStart = TimeSpan.FromSeconds(4);
        var keyOnEnd = TimeSpan.FromSeconds(44);
        AddPayloadCycleWindow(
            schedule,
            0x0F1,
            ["000000400000", "1C0000400000", "280000400000", "340000400000"],
            TimeSpan.FromMilliseconds(30),
            false,
            "transition key-on native fast BCM/body state cycle 0x0F1",
            keyOnStart + TimeSpan.FromMilliseconds(30),
            keyOnEnd - keyOnStart);
        AddPeriodicWindow(schedule, 0x451, "000000000000", TimeSpan.FromMilliseconds(50), false, "transition key-on body/display status 0x451", keyOnStart + TimeSpan.FromMilliseconds(35), keyOnEnd);
        AddPeriodicWindow(schedule, 0x160, "803C96B503", TimeSpan.FromMilliseconds(100), false, "transition key-on compact power/body state 0x160", keyOnStart + TimeSpan.FromMilliseconds(40), keyOnEnd);
        AddPeriodicWindow(schedule, 0x1F1, "AE0F173E18000072", TimeSpan.FromMilliseconds(100), false, "transition key-on ignition/environment state 0x1F1", keyOnStart + TimeSpan.FromMilliseconds(50), keyOnEnd);
        AddPeriodicWindow(schedule, 0x12A, "0000605D00008080", TimeSpan.FromMilliseconds(100), false, "transition key-on door/belt/body/DIC state 0x12A", keyOnStart + TimeSpan.FromMilliseconds(60), keyOnEnd);
        AddPeriodicWindow(schedule, 0x135, "04080D0002180000", TimeSpan.FromMilliseconds(100), false, "transition key-on packed body/display status 0x135", keyOnStart + TimeSpan.FromMilliseconds(70), keyOnEnd);
        AddPeriodicWindow(schedule, 0x137, "0000000000000000", TimeSpan.FromMilliseconds(100), false, "transition key-on packed body/display status 0x137", keyOnStart + TimeSpan.FromMilliseconds(80), keyOnEnd);
        AddPeriodicWindow(schedule, 0x139, "0000000000000000", TimeSpan.FromMilliseconds(100), false, "transition key-on body/display status 0x139", keyOnStart + TimeSpan.FromMilliseconds(90), keyOnEnd);
        AddPeriodicWindow(schedule, 0x4E1, "4B34303238383139", TimeSpan.FromMilliseconds(1000), false, "transition key-on VIN/config fragment 0x4E1", keyOnStart + TimeSpan.FromMilliseconds(150), keyOnEnd);
        AddPeriodicWindow(schedule, 0x514, "3056305845503638", TimeSpan.FromMilliseconds(1000), false, "transition key-on VIN/config fragment 0x514", keyOnStart + TimeSpan.FromMilliseconds(250), keyOnEnd);
        return schedule;
    }

    private static List<ScheduledTxFrame> IpcNativeExpandedKeyOnSchedule()
    {
        var schedule = IpcNativeKeyOnContextSchedule();
        var start = TimeSpan.FromMilliseconds(800);
        var end = TimeSpan.FromSeconds(44);

        AddPeriodicWindow(schedule, 0x0C1, "2000000020000000", TimeSpan.FromMilliseconds(100), false, "expanded key-on changed ID 0x0C1 from all-ID state delta", start + TimeSpan.FromMilliseconds(10), end);
        AddPeriodicWindow(schedule, 0x0C5, "2000000020000000", TimeSpan.FromMilliseconds(100), false, "expanded key-on changed ID 0x0C5 from all-ID state delta", start + TimeSpan.FromMilliseconds(20), end);
        AddPeriodicWindow(schedule, 0x0D1, "8000BFFA00FA00", TimeSpan.FromMilliseconds(100), false, "expanded key-on changed ID 0x0D1 from all-ID state delta", start + TimeSpan.FromMilliseconds(30), end);
        AddPeriodicWindow(schedule, 0x1C8, "40000000FFFF3FFF", TimeSpan.FromMilliseconds(100), false, "expanded key-on changed ID 0x1C8 from all-ID state delta", start + TimeSpan.FromMilliseconds(40), end);
        AddPeriodicWindow(schedule, 0x1E1, "00000400011CC0", TimeSpan.FromMilliseconds(100), false, "expanded key-on changed ID 0x1E1 from all-ID state delta", start + TimeSpan.FromMilliseconds(50), end);
        AddPayloadCycleWindow(schedule, 0x1F3, ["C0FC00", "003C00", "803C00"], TimeSpan.FromMilliseconds(100), false, "expanded key-on changed ID 0x1F3 observed variants", start + TimeSpan.FromMilliseconds(60), end - start);
        AddPeriodicWindow(schedule, 0x210, "020001FF", TimeSpan.FromMilliseconds(250), false, "expanded key-on changed ID 0x210 from all-ID state delta", start + TimeSpan.FromMilliseconds(70), end);
        AddPeriodicWindow(schedule, 0x214, "040001FE0802", TimeSpan.FromMilliseconds(250), false, "expanded key-on changed ID 0x214 from all-ID state delta", start + TimeSpan.FromMilliseconds(80), end);
        AddPeriodicWindow(schedule, 0x2F9, "0B000000000007", TimeSpan.FromMilliseconds(250), false, "expanded key-on changed ID 0x2F9 from all-ID state delta", start + TimeSpan.FromMilliseconds(90), end);
        AddPeriodicWindow(schedule, 0x3CB, "304D00000FE6", TimeSpan.FromMilliseconds(250), false, "expanded key-on changed ID 0x3CB from all-ID state delta", start + TimeSpan.FromMilliseconds(100), end);
        AddPeriodicWindow(schedule, 0x17D, "2A2443FF2000", TimeSpan.FromMilliseconds(250), false, "expanded key-on changed ID 0x17D from all-ID state delta", start + TimeSpan.FromMilliseconds(110), end);
        AddPayloadCycleWindow(schedule, 0x1E5, ["44FFA0500000026F", "44FFA0700000028F"], TimeSpan.FromMilliseconds(500), false, "expanded warning/service ID 0x1E5 key-on variants", start + TimeSpan.FromMilliseconds(120), end - start);
        return schedule;
    }

    private static List<ScheduledTxFrame> IpcPriorityTier2ProbeSchedule()
    {
        var schedule = IpcPriorityBaselineSchedule();
        AddTier2SeedWindow(schedule, TimeSpan.FromSeconds(3), TimeSpan.FromSeconds(40));
        return schedule;
    }

    private static List<ScheduledTxFrame> IpcPriorityTier3ProbeSchedule()
    {
        var schedule = IpcPriorityBaselineSchedule();
        AddTier3SeedWindow(schedule, TimeSpan.FromSeconds(3), TimeSpan.FromSeconds(55));
        return schedule;
    }

    private static List<ScheduledTxFrame> IpcPriorityAllProbeSchedule()
    {
        var schedule = IpcPriorityBaselineSchedule();
        AddTier1SeedWindow(schedule, TimeSpan.FromSeconds(3), TimeSpan.FromSeconds(42));
        AddTier2SeedWindow(schedule, TimeSpan.FromSeconds(50), TimeSpan.FromSeconds(42));
        AddTier3SeedWindow(schedule, TimeSpan.FromSeconds(97), TimeSpan.FromSeconds(48));
        return schedule;
    }

    private static List<ScheduledTxFrame> IpcPriorityTier1ByteFuzzSchedule()
    {
        var schedule = IpcPriorityBaselineSchedule();
        var delay = TimeSpan.FromSeconds(8);
        delay = AddByteSweep(schedule, 0x1F1, "AE0F173E18000072", [0, 1, 4, 7], delay, TimeSpan.FromMilliseconds(100), "tier 1 power/ignition/environment 0x1F1", isExtended: false);
        delay = AddByteSweep(schedule, 0x160, "803C96B503", [0, 1], delay, TimeSpan.FromMilliseconds(100), "tier 1 ignition/power mode 0x160", isExtended: false);
        delay = AddByteSweep(schedule, 0x12A, "0000605D00008080", [0, 4, 6, 7], delay, TimeSpan.FromMilliseconds(100), "tier 1 door/belt/body/DIC 0x12A", isExtended: false);
        delay = AddByteSweep(schedule, 0x140, "000202", Enumerable.Range(0, 3), delay, TimeSpan.FromMilliseconds(100), "tier 1 turn/lighting/body 0x140", isExtended: false);
        return schedule;
    }

    private static List<ScheduledTxFrame> IpcPriorityTier2ByteFuzzSchedule()
    {
        var schedule = IpcPriorityBaselineSchedule();
        var delay = TimeSpan.FromSeconds(8);

        var start = delay;
        delay = AddByteSweep(schedule, 0x0C9, "8412DC0000500000", [0, 1, 2], delay, TimeSpan.FromMilliseconds(100), "tier 2 RPM/engine state 0x0C9", isExtended: false);
        AddPeriodicWindow(schedule, 0x0D3, "2BBC0007001000FF", TimeSpan.FromMilliseconds(20), false, "tier 2 static 0x0D3 companion during 0x0C9 fuzz", start, delay);

        start = delay;
        delay = AddByteSweep(schedule, 0x0D3, "2BBC0007001000FF", [0, 1, 2], delay, TimeSpan.FromMilliseconds(100), "tier 2 0x0D3 companion/mirror state", isExtended: false);
        AddPayloadCycleWindow(schedule, 0x0C9, ["8412DC0000500000", "0000000000500000"], TimeSpan.FromMilliseconds(20), false, "tier 2 static 0x0C9 cycle during 0x0D3 fuzz", start, delay - start);

        delay = AddByteSweep(schedule, 0x3E9, "0010800000108000", [0, 1, 2, 3], delay, TimeSpan.FromMilliseconds(100), "tier 2 vehicle speed/distance 0x3E9", isExtended: false);
        delay = AddByteSweep(schedule, 0x4C1, "0000730000000000", [2], delay, TimeSpan.FromMilliseconds(100), "tier 2 coolant/IAT/temperature 0x4C1 byte 2", isExtended: false);
        delay = AddByteSweep(schedule, 0x4D1, "C900000000000000", [0, 1], delay, TimeSpan.FromMilliseconds(100), "tier 2 oil/fuel/service 0x4D1", isExtended: false);

        AddPayloadCycleWindow(schedule, 0x1E5, ["44FFA0100000022F", "44FFA0300000024F", "44FFA0500000026F", "44FFA0700000028F"], TimeSpan.FromMilliseconds(100), false, "tier 2 known 0x1E5 warning/service sequence before fuzz", delay, TimeSpan.FromSeconds(4));
        delay += TimeSpan.FromSeconds(4);
        delay = AddByteSweep(schedule, 0x1E5, "44FFA0100000022F", [1, 2, 3, 7], delay, TimeSpan.FromMilliseconds(100), "tier 2 gateway warning/service 0x1E5", isExtended: false);
        return schedule;
    }

    private static List<ScheduledTxFrame> IpcPriorityBaselineSchedule()
    {
        return
        [
            new ScheduledTxFrame(0x100, [], TimeSpan.Zero, false, "priority baseline SWCAN wakeup after first IPC RX", MaxSends: 1),
            new ScheduledTxFrame(0x621, CliOptions.ParseHexData("0040000000000000"), TimeSpan.FromMilliseconds(1000), false, "priority baseline network-management steady keepalive"),
            new ScheduledTxFrame(0x13FFE040, [], TimeSpan.FromMilliseconds(1000), true, "priority baseline fake BCM/source 0x40 presence"),
            new ScheduledTxFrame(0x10002040, CliOptions.ParseHexData("03"), TimeSpan.FromMilliseconds(100), true, "priority baseline system power mode candidate"),
            new ScheduledTxFrame(0x24C, CliOptions.ParseHexData("021003AAAAAAAAAA"), TimeSpan.Zero, false, "priority baseline IPC diagnostic extended-session sanity check; watch for 64C", TimeSpan.FromSeconds(2), MaxSends: 1),
        ];
    }

    private static void AddTier1SeedWindow(List<ScheduledTxFrame> schedule, TimeSpan start, TimeSpan duration)
    {
        var end = start + duration;
        AddPeriodicWindow(schedule, 0x1F1, "AE0F173E18000072", TimeSpan.FromMilliseconds(100), false, "tier 1 power/ignition/environment seed 0x1F1", start, end);
        AddPeriodicWindow(schedule, 0x160, "803C96B503", TimeSpan.FromMilliseconds(100), false, "tier 1 ignition/power mode seed 0x160", start + TimeSpan.FromMilliseconds(20), end);
        AddPayloadCycleWindow(schedule, 0x0F1, ["000000400000", "1C0000400000", "280000400000", "340000400000"], TimeSpan.FromMilliseconds(10), false, "tier 1 fast BCM/body state cycle 0x0F1", start + TimeSpan.FromMilliseconds(40), duration);
        AddPeriodicWindow(schedule, 0x12A, "0000605D00008080", TimeSpan.FromMilliseconds(100), false, "tier 1 door/belt/body/DIC seed 0x12A", start + TimeSpan.FromMilliseconds(60), end);
        AddPeriodicWindow(schedule, 0x140, "000202", TimeSpan.FromMilliseconds(1000), false, "tier 1 turn/lighting/body seed 0x140", start + TimeSpan.FromMilliseconds(80), end);
    }

    private static void AddTier2SeedWindow(List<ScheduledTxFrame> schedule, TimeSpan start, TimeSpan duration)
    {
        var end = start + duration;
        AddPayloadCycleWindow(schedule, 0x0C9, ["8412DC0000500000", "0000000000500000"], TimeSpan.FromMilliseconds(20), false, "tier 2 RPM/engine state cycle 0x0C9", start, duration);
        AddPeriodicWindow(schedule, 0x0D3, "2BBC0007001000FF", TimeSpan.FromMilliseconds(20), false, "tier 2 0x0D3 companion/mirror state", start + TimeSpan.FromMilliseconds(10), end);
        AddPayloadCycleWindow(schedule, 0x3E9, ["0000800000008000", "0010800000108000", "0040800000408000"], TimeSpan.FromMilliseconds(50), false, "tier 2 vehicle speed/distance cycle 0x3E9", start + TimeSpan.FromMilliseconds(20), duration);
        AddPeriodicWindow(schedule, 0x4C1, "0000730000000000", TimeSpan.FromMilliseconds(250), false, "tier 2 coolant/IAT/temperature seed 0x4C1", start + TimeSpan.FromMilliseconds(40), end);
        AddPayloadCycleWindow(schedule, 0x4D1, ["C900000000000000", "E900000000000000"], TimeSpan.FromMilliseconds(100), false, "tier 2 oil/fuel/service cycle 0x4D1", start + TimeSpan.FromMilliseconds(60), duration);
        AddPayloadCycleWindow(schedule, 0x1E5, ["44FFA0100000022F", "44FFA0300000024F", "44FFA0500000026F", "44FFA0700000028F"], TimeSpan.FromMilliseconds(100), false, "tier 2 gateway warning/service cycle 0x1E5", start + TimeSpan.FromMilliseconds(80), duration);
    }

    private static void AddTier3SeedWindow(List<ScheduledTxFrame> schedule, TimeSpan start, TimeSpan duration)
    {
        var end = start + duration;
        AddPeriodicWindow(schedule, 0x1E1, "00000400010000", TimeSpan.FromMilliseconds(30), false, "tier 3 body/status seed 0x1E1", start, end);
        AddPayloadCycleWindow(schedule, 0x1F3, ["407C00", "803C00", "C0FC00"], TimeSpan.FromMilliseconds(30), false, "tier 3 body/status cycle 0x1F3", start + TimeSpan.FromMilliseconds(10), duration);
        AddPeriodicWindow(schedule, 0x3CB, "313F0000144B", TimeSpan.FromMilliseconds(100), false, "tier 3 body/counter/status seed 0x3CB", start + TimeSpan.FromMilliseconds(20), end);
        AddPeriodicWindow(schedule, 0x1F5, "ED00000F1800031C", TimeSpan.FromMilliseconds(50), false, "tier 3 powertrain status/keepalive seed 0x1F5", start + TimeSpan.FromMilliseconds(30), end);
        AddPeriodicWindow(schedule, 0x3F9, "8000000000000007", TimeSpan.FromMilliseconds(50), false, "tier 3 powertrain status/keepalive seed 0x3F9", start + TimeSpan.FromMilliseconds(40), end);
        AddPeriodicWindow(schedule, 0x10050040, "00012C03A9000000", TimeSpan.FromMilliseconds(50), true, "tier 3 weak GMLAN29 vehicle speed/RPM candidate priority 4", start + TimeSpan.FromMilliseconds(50), end);
        AddPeriodicWindow(schedule, 0x0C050040, "00012C03A9000000", TimeSpan.FromMilliseconds(50), true, "tier 3 weak GMLAN29 vehicle speed/RPM candidate priority 3", start + TimeSpan.FromMilliseconds(60), end);
        AddPeriodicWindow(schedule, 0x1001E058, "867805FF05", TimeSpan.Zero, true, "tier 3 GMLAN29 chime command one-shot", end - TimeSpan.FromSeconds(5), end);
    }

    private static void AddOneShot(
        List<ScheduledTxFrame> schedule,
        uint canId,
        string payload,
        bool isExtended,
        string note,
        TimeSpan delay)
    {
        schedule.Add(new ScheduledTxFrame(canId, CliOptions.ParseHexData(payload), TimeSpan.Zero, isExtended, note, delay, MaxSends: 1));
    }

    private static void AddPayloadCycleWindow(
        List<ScheduledTxFrame> schedule,
        uint canId,
        string[] payloads,
        TimeSpan spacing,
        bool isExtended,
        string note,
        TimeSpan start,
        TimeSpan duration)
    {
        var period = TimeSpan.FromTicks(spacing.Ticks * payloads.Length);
        var end = start + duration;
        for (var i = 0; i < payloads.Length; i++)
        {
            AddPeriodicWindow(schedule, canId, payloads[i], period, isExtended, $"{note} variant {i + 1}/{payloads.Length}", start + TimeSpan.FromTicks(spacing.Ticks * i), end);
        }
    }

    private static void AddPeriodicWindow(
        List<ScheduledTxFrame> schedule,
        uint canId,
        string payload,
        TimeSpan period,
        bool isExtended,
        string note,
        TimeSpan start,
        TimeSpan end)
    {
        if (end <= start)
        {
            return;
        }

        var maxSends = 1;
        if (period > TimeSpan.Zero)
        {
            maxSends = Math.Max(1, (int)Math.Ceiling((end - start).TotalMilliseconds / period.TotalMilliseconds));
        }

        schedule.Add(new ScheduledTxFrame(canId, CliOptions.ParseHexData(payload), period, isExtended, note, start, MaxSends: maxSends));
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
            new ScheduledTxFrame(0x621, CliOptions.ParseHexData("0040000000000000"), TimeSpan.Zero, false, "network-management steady keepalive seed after first IPC RX", MaxSends: 1),
            new ScheduledTxFrame(0x13FFE040, [], TimeSpan.FromMilliseconds(1000), true, "fake BCM/body gateway broadcast presence, source 0x40", TimeSpan.FromMilliseconds(1000)),
            new ScheduledTxFrame(0x621, CliOptions.ParseHexData("0040000000000000"), TimeSpan.FromMilliseconds(1000), false, "network-management steady keepalive", TimeSpan.FromMilliseconds(1000)),
            new ScheduledTxFrame(0x10002040, CliOptions.ParseHexData("03"), TimeSpan.FromMilliseconds(100), true, "system power mode run candidate, short DLC value 0x03"),
            new ScheduledTxFrame(0x0C030040, CliOptions.ParseHexData("0075A708"), TimeSpan.FromMilliseconds(1000), true, "battery voltage example, priority 3, arbid 0x018, sender 0x40"),
        ];
    }

    private static List<ScheduledTxFrame> IpcWakeRecoveryProbeSchedule()
    {
        return
        [
            new ScheduledTxFrame(0x100, [], TimeSpan.FromMilliseconds(1000), false, "SWCAN wakeup repeated slowly while IPC is silent", MaxSends: 10),
            new ScheduledTxFrame(0x621, CliOptions.ParseHexData("0140000000000000"), TimeSpan.FromMilliseconds(5000), false, "network-management initiate retry, intentionally sparse", MaxSends: 3),
            new ScheduledTxFrame(0x13FFE040, [], TimeSpan.FromMilliseconds(1000), true, "fake BCM/source 0x40 presence during wake recovery"),
            new ScheduledTxFrame(0x621, CliOptions.ParseHexData("0040000000000000"), TimeSpan.FromMilliseconds(1000), false, "network-management steady keepalive during wake recovery"),
            new ScheduledTxFrame(0x10002040, CliOptions.ParseHexData("03"), TimeSpan.FromMilliseconds(250), true, "system power mode run candidate during wake recovery", TimeSpan.FromMilliseconds(500), MaxSends: 80),
            new ScheduledTxFrame(0x10754040, CliOptions.ParseHexData("0400000000000000"), TimeSpan.FromMilliseconds(1000), true, "Astra/GM key-present context candidate during wake recovery", TimeSpan.FromSeconds(2), MaxSends: 10),
            new ScheduledTxFrame(0x0AA, CliOptions.ParseHexData("740000"), TimeSpan.FromMilliseconds(1000), false, "Opel ignition/key-state candidate during wake recovery", TimeSpan.FromSeconds(2.25), MaxSends: 10),
            new ScheduledTxFrame(0x24C, CliOptions.ParseHexData("021003AAAAAAAAAA"), TimeSpan.FromMilliseconds(2500), false, "IPC diagnostic session probe; watch for 64C if IPC wakes", TimeSpan.FromSeconds(3), MaxSends: 6),
            new ScheduledTxFrame(0x24C, CliOptions.ParseHexData("013EAAAAAAAAAAAA"), TimeSpan.FromMilliseconds(2500), false, "IPC tester-present probe; watch for 64C if IPC wakes", TimeSpan.FromSeconds(4.25), MaxSends: 6),
            new ScheduledTxFrame(0x10754040, CliOptions.ParseHexData("0000000000000000"), TimeSpan.Zero, true, "clear key-present candidate at end of wake recovery", TimeSpan.FromSeconds(28), MaxSends: 1),
        ];
    }

    private static List<ScheduledTxFrame> IpcWakePulseOnlyProbeSchedule()
    {
        return
        [
            new ScheduledTxFrame(0x100, [], TimeSpan.FromMilliseconds(1000), false, "wake isolation: SWCAN 100# wake pulse only; no NM/init/body context", MaxSends: 30),
        ];
    }

    private static List<ScheduledTxFrame> IpcWakeFirstProbeSchedule()
    {
        return
        [
            new ScheduledTxFrame(0x100, [], TimeSpan.FromMilliseconds(1000), false, "wake-first SWCAN wake 100# repeated slowly; verify pin 8 run/crank is powered", MaxSends: 8),
            new ScheduledTxFrame(0x13FFE040, [], TimeSpan.Zero, true, "wake-first fake BCM/source 0x40 presence seed", MaxSends: 1),
            new ScheduledTxFrame(0x621, CliOptions.ParseHexData("0040000000000000"), TimeSpan.Zero, false, "wake-first network-management keepalive seed", MaxSends: 1),
            new ScheduledTxFrame(0x621, CliOptions.ParseHexData("0140000000000000"), TimeSpan.FromMilliseconds(3000), false, "wake-first sparse 621#0140 network-init retry", TimeSpan.FromMilliseconds(1500), MaxSends: 3),
            new ScheduledTxFrame(0x13FFE040, [], TimeSpan.FromMilliseconds(1000), true, "wake-first fake BCM/source 0x40 presence"),
            new ScheduledTxFrame(0x621, CliOptions.ParseHexData("0040000000000000"), TimeSpan.FromMilliseconds(1000), false, "wake-first network-management steady keepalive"),
            new ScheduledTxFrame(0x10002040, CliOptions.ParseHexData("03"), TimeSpan.FromMilliseconds(100), true, "wake-first system power mode run candidate 0x03", TimeSpan.FromMilliseconds(500), MaxSends: 160),
            new ScheduledTxFrame(0x10754040, CliOptions.ParseHexData("0400000000000000"), TimeSpan.FromMilliseconds(500), true, "wake-first key-present/body context candidate", TimeSpan.FromMilliseconds(1000), MaxSends: 30),
            new ScheduledTxFrame(0x0AA, CliOptions.ParseHexData("740000"), TimeSpan.FromMilliseconds(500), false, "wake-first Opel ignition/key-state candidate", TimeSpan.FromMilliseconds(1250), MaxSends: 30),
            new ScheduledTxFrame(0x10754040, CliOptions.ParseHexData("0000000000000000"), TimeSpan.Zero, true, "wake-first clear key-present candidate before stop", TimeSpan.FromSeconds(19), MaxSends: 1),
        ];
    }

    private static List<ScheduledTxFrame> IpcDiagnosticBaseSchedule()
    {
        return
        [
            new ScheduledTxFrame(0x13FFE040, [], TimeSpan.FromMilliseconds(1000), true, "quiet diagnostic fake BCM/source 0x40 presence; no hard wake/init burst", TimeSpan.FromMilliseconds(1000)),
            new ScheduledTxFrame(0x621, CliOptions.ParseHexData("0040000000000000"), TimeSpan.FromMilliseconds(1000), false, "quiet diagnostic steady keepalive; no 621#0140 startup burst", TimeSpan.FromMilliseconds(1000)),
        ];
    }

    private static List<ScheduledTxFrame> IpcQuietBodyBaselineSchedule()
    {
        return
        [
            new ScheduledTxFrame(0x13FFE040, [], TimeSpan.Zero, true, "quiet fake BCM/source 0x40 presence after first IPC RX", MaxSends: 1),
            new ScheduledTxFrame(0x621, CliOptions.ParseHexData("0040000000000000"), TimeSpan.Zero, false, "quiet network-management steady keepalive seed after first IPC RX", MaxSends: 1),
            new ScheduledTxFrame(0x13FFE040, [], TimeSpan.FromMilliseconds(1000), true, "quiet fake BCM/source 0x40 presence; no hard wake/init burst", TimeSpan.FromMilliseconds(1000)),
            new ScheduledTxFrame(0x621, CliOptions.ParseHexData("0040000000000000"), TimeSpan.FromMilliseconds(1000), false, "quiet network-management steady keepalive; no 621#0140 startup burst", TimeSpan.FromMilliseconds(1000)),
            new ScheduledTxFrame(0x10002040, CliOptions.ParseHexData("03"), TimeSpan.FromMilliseconds(250), true, "quiet system power mode hold, source 0x40, short DLC value 0x03", TimeSpan.FromMilliseconds(500)),
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

    private static List<ScheduledTxFrame> NormalWakeSchedule()
    {
        return
        [
            new ScheduledTxFrame(0x100, [], TimeSpan.Zero, false, "SWCAN wakeup, normal one-shot", MaxSends: 1),
            new ScheduledTxFrame(0x13FFE040, [], TimeSpan.Zero, true, "normal fake BCM/source 0x40 presence seed", MaxSends: 1),
            new ScheduledTxFrame(0x621, CliOptions.ParseHexData("0040000000000000"), TimeSpan.Zero, false, "normal network-management steady keepalive seed", MaxSends: 1),
            new ScheduledTxFrame(0x13FFE040, [], TimeSpan.FromMilliseconds(1000), true, "normal fake BCM/source 0x40 presence; no 621#0140 startup burst", TimeSpan.FromMilliseconds(1000)),
            new ScheduledTxFrame(0x621, CliOptions.ParseHexData("0040000000000000"), TimeSpan.FromMilliseconds(1000), false, "normal network-management steady keepalive; no 621#0140 startup burst", TimeSpan.FromMilliseconds(1000)),
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
        var schedule = NormalWakeSchedule();
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
        var schedule = NormalWakeSchedule();
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
        var schedule = NormalWakeSchedule();
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
    bool ReadOnlyCapture = false,
    bool ReadOnlyListenOnly = true,
    bool ReadOnlyFlushStale = true);
