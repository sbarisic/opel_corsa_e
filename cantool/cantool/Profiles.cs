namespace cantool;

internal static class Profiles
{
    public const string ReadOnly = "read-only";
    public const string IpcReadOnlySniff = "ipc-read-only-sniff";
    public const string IpcAckSniff = "ipc-ack-sniff";
    public const string IpcPowerToggleWatch = "ipc-power-toggle-watch";
    public const string IpcWakePulseOnlyProbe = "ipc-wake-pulse-only-probe";
    public const string IpcWakeFirstProbe = "ipc-wake-first-probe";
    public const string IpcWakeRecoveryProbe = "ipc-wake-recovery-probe";
    public const string IpcSimulator = "ipc-simulator";
    public const string IpcSimulatorKeyOnHold = "ipc-simulator-keyon-hold";
    public const string IpcNativeKeyFirstTurnReplay = "ipc-native-key-first-turn-replay";
    public const string IpcWrappedGaugeProbe = "ipc-wrapped-gauge-probe";
    public const string IpcSimulatorDataFill = "ipc-simulator-data-fill";
    public const string IpcDicTextRadioProbe = "ipc-dic-text-radio-probe";
    public const string IpcActiveGaugeContextProbe = "ipc-active-gauge-context-probe";
    public const string Ipc624StateProbe = "ipc-624-state-probe";
    public const string IpcTpmsOkDismissProbe = "ipc-tpms-ok-dismiss-probe";
    public const string IpcNativeLightsProbe = "ipc-native-lights-probe";
    public const string IpcNativeHandbrakeProbe = "ipc-native-handbrake-probe";
    public const string IpcNativeTcProbe = "ipc-native-tc-probe";
    public const string IpcNativeDimOdoProbe = "ipc-native-dim-odo-probe";
    public const string IpcLiveReload = "ipc-live-reload";
    public const string IpcSimulatorKeyOnEdge = "ipc-simulator-keyon-edge";
    public const string IpcSimulatorNativeTransition = "ipc-simulator-native-transition";
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

    public static readonly ProfileDefinition[] Available =
    [
        new(ReadOnly, "Read-only sniff alias: no TX, listen until Ctrl+C by default", 0.0, ReadOnlyCapture: true),
        new(IpcReadOnlySniff, "Read-only sniff: no TX, listen until Ctrl+C by default", 0.0, ReadOnlyCapture: true),
        new(IpcAckSniff, "Read-only normal-mode sniff: no TX, but ACKs IPC frames", 0.0, ReadOnlyCapture: true, ReadOnlyListenOnly: false),
        new(IpcPowerToggleWatch, "No-flush normal-mode watch for pin 8 power/ignition toggles", 0.0, ReadOnlyCapture: true, ReadOnlyListenOnly: false, ReadOnlyFlushStale: false),
        new(IpcWakePulseOnlyProbe, "Wake isolation: only repeated SWCAN 100# wake pulses on silent bus", 30.0),
        new(IpcWakeFirstProbe, "Wake first: no wait-for-RX, wake/body context only, no diagnostics", 20.0),
        new(IpcWakeRecoveryProbe, "Wake recovery: no wait-for-RX, staged SWCAN wake/context/diagnostic nudges", 30.0),
        new(IpcSimulator, "Native Corsa E IPC simulator: stable key-on hold from real SWCAN captures", 45.0),
        new(IpcSimulatorKeyOnHold, "Alias for native stable key-on hold replay", 45.0),
        new(IpcSimulatorDataFill, "Native key-on hold plus transient odometer/fuel/body data candidates", 45.0),
        new(IpcDicTextRadioProbe, "Native hold plus radio/OnStar DIC text probes; source 0x60 remains RX-only", 60.0),
        new(Ipc624StateProbe, "Native hold with isolated 621/624 state-heartbeat windows", 45.0),
        new(IpcActiveGaugeContextProbe, "Native hold plus 0x58/0x80 presence, battery, speed/RPM, and engine context; no 624", 75.0),
        new(IpcTpmsOkDismissProbe, "Native hold plus safe TPMS OK/dismiss HMI and DIC menu-action button probes", 70.0),
        new(IpcNativeLightsProbe, "Native source-0x40 lights/TC isolation: hold one 10240040/102CC040/102CA040 value at a time", 85.0),
        new(IpcNativeHandbrakeProbe, "Native source-0x40 parking-brake/handbrake telltale toggle using 103B4040#04", 45.0),
        new(IpcNativeTcProbe, "Native source-0x40 TC telltale toggle using confirmed 102CC040#0400C00000000000", 45.0),
        new(IpcLiveReload, "Live-reload send_profile.can and transmit only enabled rows while you edit", 0.0),
        new(IpcNativeKeyFirstTurnReplay, "Native source-0x40 key-first-turn replay from IPC-disconnected SWCAN logs", 60.0),
        new(IpcNativeDimOdoProbe, "Native source-0x40 dim/odometer isolation: window 10240040, 102CA040, and 10248040", 75.0),
        new(IpcWrappedGaugeProbe, "Native key-first-turn replay plus wrapped 11-bit RPM/speed/temp/service candidates", 90.0),
        new(IpcSimulatorKeyOnEdge, "Native Corsa E short pre-key to key-on edge replay", 45.0),
        new(IpcSimulatorNativeTransition, "Native Corsa E transition replay: off/key-in/key-on/cleanup, can drive odometer-only state", 45.0),
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
            IpcSimulator => IpcSimulatorKeyOnHoldSchedule(),
            IpcSimulatorKeyOnHold => IpcSimulatorKeyOnHoldSchedule(),
            IpcNativeKeyFirstTurnReplay => IpcNativeKeyFirstTurnReplaySchedule(),
            IpcWrappedGaugeProbe => IpcWrappedGaugeProbeSchedule(),
            IpcSimulatorDataFill => IpcSimulatorDataFillSchedule(),
            IpcDicTextRadioProbe => IpcDicTextRadioProbeSchedule(),
            Ipc624StateProbe => Ipc624StateProbeSchedule(),
            IpcActiveGaugeContextProbe => IpcActiveGaugeContextProbeSchedule(),
            IpcTpmsOkDismissProbe => IpcTpmsOkDismissProbeSchedule(),
            IpcNativeLightsProbe => IpcNativeLightsProbeSchedule(),
            IpcNativeHandbrakeProbe => IpcNativeHandbrakeProbeSchedule(),
            IpcNativeTcProbe => IpcNativeTcProbeSchedule(),
            IpcNativeDimOdoProbe => IpcNativeDimOdoProbeSchedule(),
            IpcLiveReload => [],
            IpcSimulatorKeyOnEdge => IpcSimulatorSchedule(),
            IpcSimulatorNativeTransition => IpcSimulatorNativeTransitionSchedule(),
            IpcWakePulseOnlyProbe => IpcWakePulseOnlyProbeSchedule(),
            IpcWakeFirstProbe => IpcWakeFirstProbeSchedule(),
            IpcWakeRecoveryProbe => IpcWakeRecoveryProbeSchedule(),
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
            ReadOnly => [],
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

    private static List<ScheduledTxFrame> IpcSimulatorSchedule()
    {
        var schedule = new List<ScheduledTxFrame>();

        var edge = TimeSpan.FromSeconds(3);
        var hmiStart = TimeSpan.FromSeconds(12);
        var hmiEnd = TimeSpan.FromSeconds(38);
        var runEnd = TimeSpan.FromSeconds(45);

        AddOneShot(schedule, 0x100, "", false, "native Corsa E SWCAN wake pulse", TimeSpan.Zero);
        AddOneShot(schedule, 0x13FFE040, "", true, "native fake BCM/source 0x40 presence seed", TimeSpan.Zero);
        AddOneShot(schedule, 0x621, "0140000000000000", false, "native network-management startup/init seed", TimeSpan.Zero);
        AddOneShot(schedule, 0x621, "0002000000000000", false, "native short neutral/pre-key network-management seed", TimeSpan.FromMilliseconds(100));

        AddPeriodicWindow(schedule, 0x13FFE040, "", TimeSpan.FromMilliseconds(1200), true, "native fake BCM/source 0x40 presence, captured-like cadence", TimeSpan.FromMilliseconds(1200), runEnd);

        AddPeriodicWindow(schedule, 0x621, "0002000000000000", TimeSpan.FromMilliseconds(1000), false, "native short neutral/pre-key network-management context before key-on edge", TimeSpan.FromMilliseconds(1000), edge);
        AddPeriodicWindow(schedule, 0x10754040, "040000", TimeSpan.FromMilliseconds(1000), true, "native short key-present context before key-on edge", TimeSpan.FromMilliseconds(500), edge);
        AddPeriodicWindow(schedule, 0x10242040, "01", TimeSpan.FromMilliseconds(1000), true, "native short 10242040 pre-key-on state before edge", TimeSpan.FromMilliseconds(500), edge);
        AddPeriodicWindow(schedule, 0x102C0040, "0000000000", TimeSpan.FromMilliseconds(1000), true, "native short 102C0040 neutral state before key-on edge", TimeSpan.Zero, edge);

        AddOneShot(schedule, 0x621, "0052000000000000", false, "native key-on network-management edge", edge);
        AddPeriodicWindow(schedule, 0x621, "0052000000000000", TimeSpan.FromMilliseconds(1000), false, "native key-on network-management context", edge + TimeSpan.FromMilliseconds(500), runEnd);

        AddPeriodicWindow(schedule, 0x10210040, "0000800000008000", TimeSpan.FromMilliseconds(100), true, "native BCM/body base status from IPC_disconnected capture", TimeSpan.Zero, runEnd);
        AddPeriodicWindow(schedule, 0x10264040, "0000000000000000", TimeSpan.FromMilliseconds(1000), true, "native BCM/body base zero-status frame", TimeSpan.Zero, runEnd);
        AddPeriodicWindow(schedule, 0x102CA040, "0000000000000F00", TimeSpan.FromMilliseconds(1000), true, "native BCM/body base 102CA040 context", TimeSpan.Zero, runEnd);

        AddPeriodicWindow(schedule, 0x10220040, "1000000040120000", TimeSpan.FromMilliseconds(1000), true, "native BCM/body 10220040 short pre-key-on variant", TimeSpan.Zero, edge);
        AddPeriodicWindow(schedule, 0x10220040, "1000000040080000", TimeSpan.FromMilliseconds(1000), true, "native BCM/body 10220040 key-on variant", edge, runEnd);

        AddPayloadCycleWindow(schedule, 0x10230040, ["0000000000000000", "1000000010000000", "2000000020000000", "3000000030000000"], TimeSpan.FromMilliseconds(250), true, "native BCM/body 10230040 captured counter/state cycle", TimeSpan.Zero, runEnd);
        AddPayloadCycleWindow(schedule, 0x1022E040, ["1000000010000000", "2000000020000000", "3000000030000000"], TimeSpan.FromMilliseconds(250), true, "native BCM/body 1022E040 key-on counter/state cycle", edge, runEnd - edge);

        AddPeriodicWindow(schedule, 0x10240040, "8004880100000007", TimeSpan.FromMilliseconds(1000), true, "native BCM/body 10240040 short pre-key-on variant", TimeSpan.Zero, edge);
        AddPeriodicWindow(schedule, 0x10240040, "83FE880100D70FFD", TimeSpan.FromMilliseconds(1000), true, "native BCM/body 10240040 key-on/second-turn variant", edge, runEnd);

        AddPeriodicWindow(schedule, 0x102C0040, "803C96B503", TimeSpan.FromMilliseconds(1000), true, "native BCM/body 102C0040 key-on captured context", edge, runEnd);
        AddPeriodicWindow(schedule, 0x10754040, "040400", TimeSpan.FromMilliseconds(1000), true, "native key/body context key-on hold variant", edge, runEnd);
        AddPeriodicWindow(schedule, 0x10242040, "02", TimeSpan.FromMilliseconds(1000), true, "native 10242040 second-turn/key-on hold state", edge, runEnd);

        AddOneShot(schedule, 0x10414040, "00017B05", true, "native lock/body key-on context one-shot captured variant", edge + TimeSpan.FromSeconds(1));
        AddPeriodicWindow(schedule, 0x10304058, "96B501", TimeSpan.FromMilliseconds(110), true, "native source 0x58 DIC/icon traffic captured with IPC connected", hmiStart, hmiEnd);

        AddOneShot(schedule, 0x10438040, "01", true, "native HMI button probe DLC-1 press: 0x01", TimeSpan.FromSeconds(20));
        AddOneShot(schedule, 0x10438040, "00", true, "native HMI button release after 0x01", TimeSpan.FromSeconds(20.15));
        AddOneShot(schedule, 0x10438040, "02", true, "native HMI button probe DLC-1 press: 0x02", TimeSpan.FromSeconds(24));
        AddOneShot(schedule, 0x10438040, "00", true, "native HMI button release after 0x02", TimeSpan.FromSeconds(24.15));

        return schedule;
    }

    private static List<ScheduledTxFrame> IpcSimulatorKeyOnHoldSchedule()
    {
        return IpcSimulatorKeyOnHoldSchedule(TimeSpan.FromSeconds(45), includeKeyOnHeartbeat: true);
    }

    private static List<ScheduledTxFrame> IpcSimulatorKeyOnHoldSchedule(
        TimeSpan runEnd,
        bool includeKeyOnHeartbeat,
        bool includeNativeHmiButtons = true,
        string keyOn10240040Payload = "83FE880100D70FFD",
        bool includeNativeIconTraffic = true,
        bool includeNative10240040 = true,
        bool includeNative102CA040 = true)
    {
        var schedule = new List<ScheduledTxFrame>();

        var hmiStart = TimeSpan.FromSeconds(12);
        var hmiEnd = TimeSpan.FromTicks(Math.Min(TimeSpan.FromSeconds(38).Ticks, runEnd.Ticks));

        AddOneShot(schedule, 0x100, "", false, "native Corsa E SWCAN wake pulse", TimeSpan.Zero);
        AddOneShot(schedule, 0x13FFE040, "", true, "native fake BCM/source 0x40 presence seed", TimeSpan.Zero);
        AddOneShot(schedule, 0x621, "0140000000000000", false, "native network-management startup/init seed", TimeSpan.Zero);
        if (includeKeyOnHeartbeat)
        {
            AddOneShot(schedule, 0x621, "0052000000000000", false, "native key-on network-management seed", TimeSpan.FromMilliseconds(100));
        }

        AddPeriodicWindow(schedule, 0x13FFE040, "", TimeSpan.FromMilliseconds(1200), true, "native fake BCM/source 0x40 presence, captured-like cadence", TimeSpan.FromMilliseconds(1200), runEnd);
        if (includeKeyOnHeartbeat)
        {
            AddPeriodicWindow(schedule, 0x621, "0052000000000000", TimeSpan.FromMilliseconds(1000), false, "native key-on network-management context", TimeSpan.FromMilliseconds(1000), runEnd);
        }

        AddPeriodicWindow(schedule, 0x10210040, "0000800000008000", TimeSpan.FromMilliseconds(100), true, "native BCM/body base status from IPC_disconnected capture", TimeSpan.Zero, runEnd);
        AddPeriodicWindow(schedule, 0x10264040, "0000000000000000", TimeSpan.FromMilliseconds(1000), true, "native BCM/body base zero-status frame", TimeSpan.Zero, runEnd);
        if (includeNative102CA040)
        {
            AddPeriodicWindow(schedule, 0x102CA040, "0000000000000F00", TimeSpan.FromMilliseconds(1000), true, "native BCM/body base 102CA040 context", TimeSpan.Zero, runEnd);
        }
        AddPeriodicWindow(schedule, 0x10220040, "1000000040080000", TimeSpan.FromMilliseconds(1000), true, "native BCM/body 10220040 key-on variant", TimeSpan.Zero, runEnd);

        AddPayloadCycleWindow(schedule, 0x10230040, ["0000000000000000", "1000000010000000", "2000000020000000", "3000000030000000"], TimeSpan.FromMilliseconds(250), true, "native BCM/body 10230040 captured counter/state cycle", TimeSpan.Zero, runEnd);
        AddPayloadCycleWindow(schedule, 0x1022E040, ["1000000010000000", "2000000020000000", "3000000030000000"], TimeSpan.FromMilliseconds(250), true, "native BCM/body 1022E040 key-on counter/state cycle", TimeSpan.Zero, runEnd);

        if (includeNative10240040)
        {
            AddPeriodicWindow(schedule, 0x10240040, keyOn10240040Payload, TimeSpan.FromMilliseconds(1000), true, $"native BCM/body 10240040 key-on/second-turn variant {keyOn10240040Payload}", TimeSpan.Zero, runEnd);
        }
        AddPeriodicWindow(schedule, 0x102C0040, "803C96B503", TimeSpan.FromMilliseconds(1000), true, "native BCM/body 102C0040 key-on captured context", TimeSpan.Zero, runEnd);

        AddPeriodicWindow(schedule, 0x10754040, "040400", TimeSpan.FromMilliseconds(1000), true, "native key/body context immediate key-on hold variant", TimeSpan.Zero, runEnd);
        AddPeriodicWindow(schedule, 0x10242040, "02", TimeSpan.FromMilliseconds(1000), true, "native 10242040 immediate second-turn/key-on hold state", TimeSpan.Zero, runEnd);

        if (includeNativeIconTraffic)
        {
            AddOneShot(schedule, 0x10414040, "00017B05", true, "native lock/body key-on context one-shot captured variant", TimeSpan.FromSeconds(3));
            AddPeriodicWindow(schedule, 0x10304058, "96B501", TimeSpan.FromMilliseconds(110), true, "native source 0x58 DIC/icon traffic captured with IPC connected", hmiStart, hmiEnd);
        }

        if (includeNativeHmiButtons)
        {
            AddOneShot(schedule, 0x10438040, "01", true, "native HMI button probe DLC-1 press: 0x01", TimeSpan.FromSeconds(20));
            AddOneShot(schedule, 0x10438040, "00", true, "native HMI button release after 0x01", TimeSpan.FromSeconds(20.15));
            AddOneShot(schedule, 0x10438040, "02", true, "native HMI button probe DLC-1 press: 0x02", TimeSpan.FromSeconds(24));
            AddOneShot(schedule, 0x10438040, "00", true, "native HMI button release after 0x02", TimeSpan.FromSeconds(24.15));
        }

        return schedule;
    }

    private static List<ScheduledTxFrame> IpcNativeKeyFirstTurnReplaySchedule()
    {
        return IpcNativeKeyFirstTurnReplaySchedule(TimeSpan.FromSeconds(60));
    }

    private static List<ScheduledTxFrame> IpcNativeKeyFirstTurnReplaySchedule(TimeSpan runEnd)
    {
        var schedule = new List<ScheduledTxFrame>();

        AddOneShot(schedule, 0x100, "", false, "native key-first-turn replay SWCAN wake pulse from disconnected seed", TimeSpan.Zero);
        AddOneShot(schedule, 0x13FFE040, "", true, "native key-first-turn replay BCM/source 0x40 presence seed", TimeSpan.Zero);
        AddOneShot(schedule, 0x621, "0052000000000000", false, "native key-first-turn replay 621 key-on seed", TimeSpan.FromMilliseconds(100));

        AddPeriodicWindow(schedule, 0x13FFE040, "", TimeSpan.FromMilliseconds(1200), true, "native key-first-turn replay BCM/source 0x40 presence", TimeSpan.FromMilliseconds(1200), runEnd);
        AddPeriodicWindow(schedule, 0x621, "0052000000000000", TimeSpan.FromMilliseconds(1000), false, "native key-first-turn replay 621 key-on keepalive", TimeSpan.FromMilliseconds(1000), runEnd);

        AddPeriodicWindow(schedule, 0x10210040, "0000800000008000", TimeSpan.FromMilliseconds(100), true, "native key-first-turn replay source 0x40 body status 0x108", TimeSpan.Zero, runEnd);
        AddPeriodicWindow(schedule, 0x10220040, "1000000040080000", TimeSpan.FromMilliseconds(100), true, "native key-first-turn replay source 0x40 body status 0x110", TimeSpan.Zero, runEnd);
        AddPayloadCycleWindow(schedule, 0x1022E040, ["1000000010000000", "2000000020000000", "3000000030000000", "0000000000000000"], TimeSpan.FromMilliseconds(100), true, "native key-first-turn replay source 0x40 1022E040 captured state cycle", TimeSpan.Zero, runEnd);
        AddPayloadCycleWindow(schedule, 0x10230040, ["0000000000000000", "1000000010000000", "2000000020000000", "3000000030000000"], TimeSpan.FromMilliseconds(100), true, "native key-first-turn replay source 0x40 10230040 captured state cycle", TimeSpan.Zero, runEnd);
        AddPeriodicWindow(schedule, 0x10240040, "83FF880100D80FFD", TimeSpan.FromMilliseconds(100), true, "native key-first-turn replay source 0x40 10240040 top captured payload", TimeSpan.Zero, runEnd);
        AddPeriodicWindow(schedule, 0x10264040, "0000000000000000", TimeSpan.FromMilliseconds(100), true, "native key-first-turn replay source 0x40 body status 0x132", TimeSpan.Zero, runEnd);
        AddPeriodicWindow(schedule, 0x102C0040, "803C96B503", TimeSpan.FromMilliseconds(100), true, "native key-first-turn replay wrapped 0x160 ignition/power mode from disconnected seed", TimeSpan.Zero, runEnd);
        AddPeriodicWindow(schedule, 0x102CA040, "0000000000000F00", TimeSpan.FromMilliseconds(100), true, "native key-first-turn replay source 0x40 body status 0x165", TimeSpan.Zero, runEnd);

        AddPeriodicWindow(schedule, 0x106B8040, "0000000000000000", TimeSpan.FromMilliseconds(200), true, "native key-first-turn replay source 0x40 body/HMI status 0x35C", TimeSpan.FromMilliseconds(25), runEnd);
        AddPeriodicWindow(schedule, 0x1022A040, "00B00000000000", TimeSpan.FromMilliseconds(250), true, "native key-first-turn replay source 0x40 body status 0x115", TimeSpan.FromMilliseconds(50), runEnd);
        AddPeriodicWindow(schedule, 0x1022C040, "00000000", TimeSpan.FromMilliseconds(250), true, "native key-first-turn replay source 0x40 body status 0x116", TimeSpan.FromMilliseconds(75), runEnd);
        AddPeriodicWindow(schedule, 0x102CC040, "0800000000000000", TimeSpan.FromMilliseconds(250), true, "native key-first-turn replay source 0x40 body status 0x166", TimeSpan.FromMilliseconds(100), runEnd);
        AddPeriodicWindow(schedule, 0x102CE040, "0700070000000000", TimeSpan.FromMilliseconds(250), true, "native key-first-turn replay source 0x40 body status 0x167", TimeSpan.FromMilliseconds(125), runEnd);
        AddPeriodicWindow(schedule, 0x10780040, "0000000000000000", TimeSpan.FromMilliseconds(250), true, "native key-first-turn replay source 0x40 body/HMI status 0x3C0", TimeSpan.FromMilliseconds(150), runEnd);
        AddPeriodicWindow(schedule, 0x10800040, "000000000000", TimeSpan.FromMilliseconds(250), true, "native key-first-turn replay source 0x40 body/HMI status 0x400", TimeSpan.FromMilliseconds(175), runEnd);

        AddPeriodicWindow(schedule, 0x10248040, "00005DA6A300", TimeSpan.FromMilliseconds(500), true, "native key-first-turn replay source 0x40 dynamic body/DIC status 0x124", TimeSpan.FromMilliseconds(200), runEnd);
        AddPeriodicWindow(schedule, 0x102D0040, "0000000000000000", TimeSpan.FromMilliseconds(500), true, "native key-first-turn replay source 0x40 body status 0x168", TimeSpan.FromMilliseconds(250), runEnd);
        AddPeriodicWindow(schedule, 0x10788040, "100A00000000", TimeSpan.FromMilliseconds(500), true, "native key-first-turn replay source 0x40 body/HMI status 0x3C4", TimeSpan.FromMilliseconds(300), runEnd);
        AddPeriodicWindow(schedule, 0x10806040, "0000000000", TimeSpan.FromMilliseconds(550), true, "native key-first-turn replay source 0x40 body/HMI status 0x403", TimeSpan.FromMilliseconds(350), runEnd);

        AddPeriodicWindow(schedule, 0x10242040, "01", TimeSpan.FromMilliseconds(5000), true, "native key-first-turn replay source 0x40 key/ignition sub-state 0x121", TimeSpan.FromMilliseconds(500), runEnd);
        AddPeriodicWindow(schedule, 0x10754040, "040400", TimeSpan.FromMilliseconds(1000), true, "native key-first-turn replay key-present/key-on context", TimeSpan.FromMilliseconds(600), runEnd);

        return schedule;
    }

    private static List<ScheduledTxFrame> IpcWrappedGaugeProbeSchedule()
    {
        var runEnd = TimeSpan.FromSeconds(90);
        var schedule = IpcNativeKeyFirstTurnReplaySchedule(runEnd);

        AddPeriodicWindow(schedule, WrapBodyId(0x1F5), "ED00000F1800031C", TimeSpan.FromMilliseconds(100), true, "wrapped 11-bit status companion original 0x1F5 -> source 0x40", TimeSpan.FromSeconds(4), TimeSpan.FromSeconds(7.5));
        AddPeriodicWindow(schedule, WrapBodyId(0x3F9), "0003F725FA00EC24", TimeSpan.FromMilliseconds(100), true, "wrapped 11-bit powertrain/status companion original 0x3F9 -> source 0x40", TimeSpan.FromSeconds(4.05), TimeSpan.FromSeconds(7.5));
        AddPeriodicWindow(schedule, WrapBodyId(0x3D1), "0000000000000000", TimeSpan.FromMilliseconds(100), true, "wrapped 11-bit limiter/DIC settings neutral original 0x3D1 -> source 0x40", TimeSpan.FromSeconds(4.1), TimeSpan.FromSeconds(7.5));

        AddPeriodicWindow(schedule, WrapBodyId(0x0C9), "0004C40000500000", TimeSpan.FromMilliseconds(20), true, "wrapped 11-bit RPM/engine candidate original 0x0C9 -> source 0x40", TimeSpan.FromSeconds(8), TimeSpan.FromSeconds(28));
        AddPeriodicWindow(schedule, WrapBodyId(0x0D3), "2BBC0007001000FF", TimeSpan.FromMilliseconds(20), true, "wrapped 11-bit paired engine/status candidate original 0x0D3 -> source 0x40", TimeSpan.FromSeconds(8), TimeSpan.FromSeconds(28));

        AddPayloadHoldSequence(
            schedule,
            WrapBodyId(0x3E9),
            [
                ("0000800000008000", "speed/odometer seed payload"),
                ("0010800000108000", "speed/odometer low-mid payload"),
                ("0040800000408000", "speed/odometer higher payload"),
            ],
            TimeSpan.FromMilliseconds(50),
            TimeSpan.FromSeconds(8),
            true,
            "wrapped 11-bit speed/odometer candidate original 0x3E9 -> source 0x40",
            TimeSpan.FromSeconds(30));

        AddPeriodicWindow(schedule, WrapBodyId(0x4C1), "0000730000000000", TimeSpan.FromMilliseconds(250), true, "wrapped 11-bit coolant/IAT/temp warning candidate original 0x4C1 -> source 0x40", TimeSpan.FromSeconds(56), TimeSpan.FromSeconds(66));

        AddPayloadHoldSequence(
            schedule,
            WrapBodyId(0x4D1),
            [
                ("C900000000000000", "oil/fuel/service C9 payload"),
                ("E900000000000000", "oil/fuel/service E9 payload"),
            ],
            TimeSpan.FromMilliseconds(100),
            TimeSpan.FromSeconds(6),
            true,
            "wrapped 11-bit oil/fuel/service candidate original 0x4D1 -> source 0x40",
            TimeSpan.FromSeconds(66));

        AddPayloadHoldSequence(
            schedule,
            WrapBodyId(0x1E5),
            [
                ("44003910000000C9", "warning/service C9 payload"),
                ("44003930000000E9", "warning/service E9 payload"),
                ("4400395000000109", "warning/service 0109 payload"),
            ],
            TimeSpan.FromMilliseconds(100),
            TimeSpan.FromSeconds(3),
            true,
            "wrapped 11-bit warning/service candidate original 0x1E5 -> source 0x40",
            TimeSpan.FromSeconds(78));

        AddOneShot(schedule, WrapBodyId(0x3E9), "0000000000000000", true, "wrapped speed/odometer zero-out original 0x3E9 before stop", TimeSpan.FromSeconds(88));
        AddOneShot(schedule, WrapBodyId(0x0C9), "0000000000000000", true, "wrapped RPM/engine zero-out original 0x0C9 before stop", TimeSpan.FromSeconds(88.2));
        AddOneShot(schedule, WrapBodyId(0x0D3), "0000000000000000", true, "wrapped paired engine/status zero-out original 0x0D3 before stop", TimeSpan.FromSeconds(88.4));

        return schedule;
    }

    private static List<ScheduledTxFrame> IpcNativeDimOdoProbeSchedule()
    {
        var runEnd = TimeSpan.FromSeconds(75);
        var schedule = new List<ScheduledTxFrame>();

        AddOneShot(schedule, 0x100, "", false, "dim/odo isolation SWCAN wake pulse", TimeSpan.Zero);
        AddOneShot(schedule, 0x13FFE040, "", true, "dim/odo isolation BCM/source 0x40 presence seed", TimeSpan.Zero);
        AddOneShot(schedule, 0x621, "0052000000000000", false, "dim/odo isolation key-on 621 seed", TimeSpan.FromMilliseconds(100));

        AddPeriodicWindow(schedule, 0x13FFE040, "", TimeSpan.FromMilliseconds(1200), true, "dim/odo isolation BCM/source 0x40 presence", TimeSpan.FromMilliseconds(1200), runEnd);
        AddPeriodicWindow(schedule, 0x621, "0052000000000000", TimeSpan.FromMilliseconds(1000), false, "dim/odo isolation key-on 621 keepalive", TimeSpan.FromMilliseconds(1000), runEnd);

        AddPeriodicWindow(schedule, 0x10210040, "0000800000008000", TimeSpan.FromMilliseconds(100), true, "dim/odo isolation core source 0x40 body status 0x108", TimeSpan.Zero, runEnd);
        AddPeriodicWindow(schedule, 0x10220040, "1000000040080000", TimeSpan.FromMilliseconds(100), true, "dim/odo isolation core source 0x40 body status 0x110", TimeSpan.Zero, runEnd);
        AddPeriodicWindow(schedule, 0x10264040, "0000000000000000", TimeSpan.FromMilliseconds(500), true, "dim/odo isolation core source 0x40 body status 0x132", TimeSpan.Zero, runEnd);
        AddPeriodicWindow(schedule, 0x102C0040, "803C96B503", TimeSpan.FromMilliseconds(100), true, "dim/odo isolation core wrapped 0x160 ignition/power mode", TimeSpan.Zero, runEnd);
        AddPeriodicWindow(schedule, 0x10242040, "01", TimeSpan.FromMilliseconds(1000), true, "dim/odo isolation core key-first-turn sub-state", TimeSpan.FromMilliseconds(500), runEnd);
        AddPeriodicWindow(schedule, 0x10754040, "040400", TimeSpan.FromMilliseconds(1000), true, "dim/odo isolation core key-present/key-on context", TimeSpan.FromMilliseconds(600), runEnd);

        AddPeriodicWindow(schedule, 0x10240040, "83FF880100D80FFD", TimeSpan.FromMilliseconds(100), true, "dim/odo window 10-20s: 10240040 captured top payload only", TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(20));
        AddPeriodicWindow(schedule, 0x102CA040, "0000000000000F00", TimeSpan.FromMilliseconds(100), true, "dim/odo window 20-30s: 102CA040 neutral dim payload only", TimeSpan.FromSeconds(20), TimeSpan.FromSeconds(30));
        AddPeriodicWindow(schedule, 0x10248040, "00005DA6A300", TimeSpan.FromMilliseconds(500), true, "dim/odo window 30-40s: 10248040 dynamic body/DIC status only", TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(40));

        AddPeriodicWindow(schedule, 0x10240040, "83FF880100D80FFD", TimeSpan.FromMilliseconds(100), true, "dim/odo window 40-50s: 10240040 plus 102CA040", TimeSpan.FromSeconds(40), TimeSpan.FromSeconds(50));
        AddPeriodicWindow(schedule, 0x102CA040, "0000000000000F00", TimeSpan.FromMilliseconds(100), true, "dim/odo window 40-50s: 10240040 plus 102CA040", TimeSpan.FromSeconds(40), TimeSpan.FromSeconds(50));

        AddPeriodicWindow(schedule, 0x10240040, "83FF880100D80FFD", TimeSpan.FromMilliseconds(100), true, "dim/odo window 50-60s: 10240040 plus 10248040", TimeSpan.FromSeconds(50), TimeSpan.FromSeconds(60));
        AddPeriodicWindow(schedule, 0x10248040, "00005DA6A300", TimeSpan.FromMilliseconds(500), true, "dim/odo window 50-60s: 10240040 plus 10248040", TimeSpan.FromSeconds(50), TimeSpan.FromSeconds(60));

        AddPeriodicWindow(schedule, 0x10240040, "83FF880100D80FFD", TimeSpan.FromMilliseconds(100), true, "dim/odo window 60-72s: 10240040 plus 102CA040 plus 10248040; restart IPC here", TimeSpan.FromSeconds(60), TimeSpan.FromSeconds(72));
        AddPeriodicWindow(schedule, 0x102CA040, "0000000000000F00", TimeSpan.FromMilliseconds(100), true, "dim/odo window 60-72s: 10240040 plus 102CA040 plus 10248040; restart IPC here", TimeSpan.FromSeconds(60), TimeSpan.FromSeconds(72));
        AddPeriodicWindow(schedule, 0x10248040, "00005DA6A300", TimeSpan.FromMilliseconds(500), true, "dim/odo window 60-72s: 10240040 plus 102CA040 plus 10248040; restart IPC here", TimeSpan.FromSeconds(60), TimeSpan.FromSeconds(72));

        AddOneShot(schedule, 0x10754040, "000000", true, "dim/odo cleanup clear key/body context", TimeSpan.FromSeconds(73));
        AddOneShot(schedule, 0x10242040, "00", true, "dim/odo cleanup clear key sub-state", TimeSpan.FromSeconds(73.1));
        AddOneShot(schedule, 0x102C0040, "0000000000", true, "dim/odo cleanup clear ignition/power mode", TimeSpan.FromSeconds(73.2));
        AddOneShot(schedule, 0x621, "0040000000000000", false, "dim/odo cleanup steady keepalive", TimeSpan.FromSeconds(73.3));

        return schedule;
    }

    private static List<ScheduledTxFrame> IpcSimulatorDataFillSchedule()
    {
        var schedule = IpcSimulatorKeyOnHoldSchedule();

        var start = TimeSpan.FromSeconds(2);
        var end = TimeSpan.FromSeconds(45);

        AddPeriodicWindow(schedule, 0x10030040, "0075A708", TimeSpan.FromMilliseconds(1000), true, "data fill battery voltage candidate around 14.7 V", start, end);
        AddPeriodicWindow(schedule, 0x100C4040, "0000000000000000", TimeSpan.FromMilliseconds(500), true, "data fill ABS/traction OK candidate, arbid 0x062 source 0x40", start, end);
        AddPeriodicWindow(schedule, 0x1005E040, "0000000000000000", TimeSpan.FromMilliseconds(500), true, "data fill brake/cruise OK candidate, arbid 0x02F source 0x40", start, end);
        AddPeriodicWindow(schedule, 0x10052040, "0000000000000000", TimeSpan.FromMilliseconds(1000), true, "data fill engine information 1 neutral candidate, arbid 0x029 source 0x40", start, end);
        AddPeriodicWindow(schedule, 0x1006E040, "0000000000000000", TimeSpan.FromMilliseconds(1000), true, "data fill engine information 2 neutral candidate, arbid 0x037 source 0x40", start, end);
        AddPeriodicWindow(schedule, 0x10064040, "0000730000000000", TimeSpan.FromMilliseconds(1000), true, "data fill engine information 3 / temperature candidate, arbid 0x032 source 0x40", start, end);
        AddPeriodicWindow(schedule, 0x100C2040, "0073", TimeSpan.FromMilliseconds(2000), true, "data fill outside air temperature candidate about 17.5 C, arbid 0x061 source 0x40", start + TimeSpan.FromMilliseconds(250), end);

        AddPayloadHoldSequence(
            schedule,
            0x1004C040,
            [
                ("0000200000000000", "fuel low-ish candidate"),
                ("0000800000000000", "fuel mid-scale candidate"),
                ("0000C00000000000", "fuel high-ish candidate"),
                ("0000000000000000", "fuel neutral/clear candidate"),
            ],
            TimeSpan.FromMilliseconds(500),
            TimeSpan.FromSeconds(5),
            true,
            "data fill fuel information arbid 0x026 source 0x40",
            TimeSpan.FromSeconds(5));

        AddPayloadHoldSequence(
            schedule,
            0x1004E040,
            [
                ("0000000000000000", "odometer/brake/wash neutral candidate"),
                ("0001E24000000000", "odometer 123456-style candidate bytes 0-2"),
                ("000001E240000000", "odometer 123456-style candidate bytes 1-3"),
                ("01E2400000000000", "odometer 123456-style candidate no leading zero"),
                ("00000001E2400000", "odometer 123456-style candidate bytes 3-5"),
            ],
            TimeSpan.FromMilliseconds(500),
            TimeSpan.FromSeconds(4),
            true,
            "data fill odometer/brake/wash arbid 0x027 source 0x40; transient CAN status only, no programming",
            TimeSpan.FromSeconds(24));

        return schedule;
    }

    private static List<ScheduledTxFrame> IpcDicTextRadioProbeSchedule()
    {
        var runEnd = TimeSpan.FromSeconds(60);
        var schedule = IpcSimulatorKeyOnHoldSchedule(runEnd, includeKeyOnHeartbeat: true, includeNativeHmiButtons: false);

        AddPeriodicWindow(schedule, 0x13FFE080, "", TimeSpan.FromMilliseconds(1500), true, "radio/source 0x80 companion presence from connected SWCAN logs", TimeSpan.FromMilliseconds(250), runEnd);
        AddPeriodicWindow(schedule, 0x13FFE058, "", TimeSpan.FromMilliseconds(1700), true, "SDM/source 0x58 companion presence from connected SWCAN logs", TimeSpan.FromMilliseconds(350), runEnd);

        AddOneShot(schedule, 0x1030A080, "000448800003", true, "radio-source DIC display-parameter probe, arbid 0x185, source 0x80; source 0x60 framing intentionally omitted", TimeSpan.FromSeconds(5));
        AddOneShot(schedule, 0x1030C080, "4501005445535404", true, "radio-source DIC display-text probe, arbid 0x186, ASCII TEST", TimeSpan.FromSeconds(5.08));
        AddOneShot(schedule, 0x1030A080, "000448800003", true, "radio-source DIC display-parameter retry", TimeSpan.FromSeconds(20));
        AddOneShot(schedule, 0x1030C080, "4501005445535404", true, "radio-source DIC display-text retry, ASCII TEST", TimeSpan.FromSeconds(20.08));

        AddOneShot(schedule, 0x1030A097, "000448800003", true, "OnStar-style source 0x97 DIC display-parameter fallback", TimeSpan.FromSeconds(35));
        AddOneShot(schedule, 0x1030C097, "4501005445535404", true, "OnStar-style source 0x97 DIC display-text fallback, ASCII TEST", TimeSpan.FromSeconds(35.08));
        AddOneShot(schedule, 0x1030A097, "000448800003", true, "OnStar-style source 0x97 DIC display-parameter retry", TimeSpan.FromSeconds(48));
        AddOneShot(schedule, 0x1030C097, "4501005445535404", true, "OnStar-style source 0x97 DIC display-text retry, ASCII TEST", TimeSpan.FromSeconds(48.08));

        return schedule;
    }

    private static List<ScheduledTxFrame> IpcActiveGaugeContextProbeSchedule()
    {
        var runEnd = TimeSpan.FromSeconds(75);
        var schedule = IpcSimulatorKeyOnHoldSchedule(runEnd, includeKeyOnHeartbeat: true);

        AddPeriodicWindow(schedule, 0x13FFE080, "", TimeSpan.FromMilliseconds(1500), true, "radio/source 0x80 companion presence during active gauge context", TimeSpan.FromMilliseconds(250), runEnd);
        AddPeriodicWindow(schedule, 0x13FFE058, "", TimeSpan.FromMilliseconds(1700), true, "SDM/source 0x58 companion presence during active gauge context", TimeSpan.FromMilliseconds(350), runEnd);

        AddPeriodicWindow(schedule, 0x10030040, "0060A70800000000", TimeSpan.FromMilliseconds(100), true, "active gauge context battery around 12.6 V, arbid 0x018 source 0x40", TimeSpan.FromSeconds(2.5), runEnd);
        AddPeriodicWindow(schedule, 0x10052040, "0000000000000000", TimeSpan.FromMilliseconds(1000), true, "active gauge context engine information 1 neutral companion, arbid 0x029 source 0x40", TimeSpan.FromSeconds(3), runEnd);
        AddPeriodicWindow(schedule, 0x10064040, "0000730000000000", TimeSpan.FromMilliseconds(1000), true, "active gauge context engine information 3 / temperature companion, arbid 0x032 source 0x40", TimeSpan.FromSeconds(3.1), runEnd);
        AddPeriodicWindow(schedule, 0x1006E040, "0000000000000000", TimeSpan.FromMilliseconds(1000), true, "active gauge context engine information 2 neutral companion, arbid 0x037 source 0x40", TimeSpan.FromSeconds(3.2), runEnd);
        AddPeriodicWindow(schedule, 0x1005E040, "0000000000000000", TimeSpan.FromMilliseconds(500), true, "active gauge context brake/cruise OK companion, arbid 0x02F source 0x40", TimeSpan.FromSeconds(3.3), runEnd);
        AddPeriodicWindow(schedule, 0x100C4040, "0000000000000000", TimeSpan.FromMilliseconds(500), true, "active gauge context ABS/traction OK companion, arbid 0x062 source 0x40", TimeSpan.FromSeconds(3.4), runEnd);

        AddPeriodicWindow(schedule, 0x10050040, "00012C03A9000000", TimeSpan.FromMilliseconds(100), true, "active gauge context speed/RPM workbook example: 30.0 units, approx 3748 rpm", TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(35));
        AddPeriodicWindow(schedule, 0x10050040, "0003E802EE000000", TimeSpan.FromMilliseconds(100), true, "active gauge context speed/RPM higher point: 100.0 units, approx 3000 rpm", TimeSpan.FromSeconds(38), TimeSpan.FromSeconds(70));
        AddOneShot(schedule, 0x10050040, "0000000000000000", true, "active gauge context speed/RPM zero-out before stop", TimeSpan.FromSeconds(72));

        return schedule;
    }

    private static List<ScheduledTxFrame> Ipc624StateProbeSchedule()
    {
        var runEnd = TimeSpan.FromSeconds(45);
        var schedule = IpcSimulatorKeyOnHoldSchedule(runEnd, includeKeyOnHeartbeat: false);

        AddOneShot(schedule, 0x621, "0052000000000000", false, "624 isolation: key-on 621 state seed, no 624 yet", TimeSpan.FromMilliseconds(100));
        AddPeriodicWindow(schedule, 0x621, "0052000000000000", TimeSpan.FromMilliseconds(1000), false, "624 isolation: key-on 621 state without 624", TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(12));
        AddOneShot(schedule, 0x624, "0120000000000000", false, "624 isolation: auxiliary state one-shot; watch for 62C", TimeSpan.FromSeconds(13));
        AddPeriodicWindow(schedule, 0x621, "0052000000000000", TimeSpan.FromMilliseconds(1000), false, "624 isolation: key-on 621 state with periodic 624", TimeSpan.FromSeconds(13), TimeSpan.FromSeconds(30));
        AddPeriodicWindow(schedule, 0x624, "0020000000000000", TimeSpan.FromMilliseconds(3000), false, "624 isolation: periodic auxiliary 624 state; watch for 62C", TimeSpan.FromSeconds(15), TimeSpan.FromSeconds(30));
        return schedule;
    }

    private static List<ScheduledTxFrame> IpcTpmsOkDismissProbeSchedule()
    {
        var runEnd = TimeSpan.FromSeconds(70);
        var schedule = IpcSimulatorKeyOnHoldSchedule(runEnd, includeKeyOnHeartbeat: true, includeNativeHmiButtons: false);

        AddPeriodicWindow(schedule, 0x13FFE080, "", TimeSpan.FromMilliseconds(1500), true, "radio/source 0x80 companion presence during TPMS OK/dismiss probe", TimeSpan.FromMilliseconds(250), runEnd);
        AddPeriodicWindow(schedule, 0x13FFE058, "", TimeSpan.FromMilliseconds(1700), true, "SDM/source 0x58 companion presence during TPMS OK/dismiss probe", TimeSpan.FromMilliseconds(350), runEnd);

        var delay = TimeSpan.FromSeconds(5);
        var hmiButtonCodes = new byte[] { 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F };
        foreach (var code in hmiButtonCodes)
        {
            var shortPayload = code.ToString("X2");
            var fullPayload = shortPayload + "00000000000000";
            AddOneShot(schedule, 0x10438040, shortPayload, true, $"TPMS dismiss HMI DLC-1 button candidate 0x{code:X2}", delay);
            AddOneShot(schedule, 0x10438040, "00", true, "TPMS dismiss HMI DLC-1 release", delay + TimeSpan.FromMilliseconds(140));
            delay += TimeSpan.FromMilliseconds(900);
            AddOneShot(schedule, 0x10438040, fullPayload, true, $"TPMS dismiss HMI full-DLC button candidate 0x{code:X2}", delay);
            AddOneShot(schedule, 0x10438040, "0000000000000000", true, "TPMS dismiss HMI full-DLC release", delay + TimeSpan.FromMilliseconds(140));
            delay += TimeSpan.FromMilliseconds(900);
        }

        delay = TimeSpan.FromSeconds(34);
        var menuPayloads = new[]
        {
            "000001",
            "000002",
            "000004",
            "000008",
            "000010",
            "010000",
            "020000",
            "040000",
            "080000",
            "100000",
        };
        foreach (var payload in menuPayloads)
        {
            AddOneShot(schedule, 0x10308040, payload, true, $"TPMS dismiss DIC menu-action candidate payload {payload}, arbid 0x184 source 0x40", delay);
            AddOneShot(schedule, 0x10308040, "000000", true, "TPMS dismiss DIC menu-action neutral/release", delay + TimeSpan.FromMilliseconds(150));
            delay += TimeSpan.FromMilliseconds(1100);
        }

        delay = TimeSpan.FromSeconds(48);
        var opelButtonPayloads = new[] { "000000", "010000", "020000", "040000", "080000", "100000", "200000", "400000", "800000" };
        foreach (var payload in opelButtonPayloads)
        {
            AddOneShot(schedule, 0x0AF, payload, false, $"TPMS dismiss Opel 11-bit steering/control candidate payload {payload}", delay);
            AddOneShot(schedule, 0x0AF, "000000", false, "TPMS dismiss Opel 11-bit steering/control neutral/release", delay + TimeSpan.FromMilliseconds(150));
            delay += TimeSpan.FromMilliseconds(900);
        }

        AddOneShot(schedule, 0x10438040, "00", true, "TPMS dismiss final HMI DLC-1 release", runEnd - TimeSpan.FromSeconds(2));
        AddOneShot(schedule, 0x10438040, "0000000000000000", true, "TPMS dismiss final HMI full-DLC release", runEnd - TimeSpan.FromSeconds(1.8));
        AddOneShot(schedule, 0x10308040, "000000", true, "TPMS dismiss final DIC menu-action neutral", runEnd - TimeSpan.FromSeconds(1.6));
        AddOneShot(schedule, 0x0AF, "000000", false, "TPMS dismiss final Opel steering/control neutral", runEnd - TimeSpan.FromSeconds(1.4));

        return schedule;
    }

    private static List<ScheduledTxFrame> IpcNativeLightsProbeSchedule()
    {
        var runEnd = TimeSpan.FromSeconds(85);
        var schedule = IpcSimulatorKeyOnHoldSchedule(
            runEnd,
            includeKeyOnHeartbeat: true,
            includeNativeHmiButtons: false,
            includeNativeIconTraffic: false,
            includeNative10240040: false,
            includeNative102CA040: false);

        AddOneShot(schedule, 0x10240040, "83FE880100D70FFD", true, "lights/TC isolation baseline seed before held windows", TimeSpan.Zero);
        AddOneShot(schedule, 0x102CA040, "0000000000000F00", true, "lights/TC isolation neutral dimmer seed before held windows", TimeSpan.Zero);

        AddPayloadHoldSequence(
            schedule,
            0x10240040,
            [
                ("83FE880100D70000", "10240040 day/off v1"),
                ("83FF880100D70000", "10240040 day/off v2"),
                ("83FE880100D70FFD", "10240040 day/off v3"),
                ("83FF880100D70FFD", "10240040 day/off v4"),
                ("83FE8A1100D70000", "10240040 night/lights-on v1"),
                ("83FF8A1100D70000", "10240040 night/lights-on v2"),
                ("83FE8A1100D70FFD", "10240040 night/lights-on v3"),
                ("83FF8A1100D70FFD", "10240040 night/lights-on v4"),
            ],
            TimeSpan.FromMilliseconds(100),
            TimeSpan.FromSeconds(5),
            true,
            "lights/TC isolation held 10240040 value; watch TC and beam symbol",
            TimeSpan.FromSeconds(5));

        AddPayloadHoldSequence(
            schedule,
            0x102CC040,
            [
                ("0000C00000000000", "102CC040 TC clear/off candidate"),
                ("0400C00000000000", "102CC040 TC ON confirmed at lights-probe t=50s"),
            ],
            TimeSpan.FromMilliseconds(250),
            TimeSpan.FromSeconds(5),
            true,
            "lights/TC isolation held 102CC040 value; confirmed TC telltale ON payload is 0400C0...",
            TimeSpan.FromSeconds(45));

        AddPayloadHoldSequence(
            schedule,
            0x102CA040,
            [
                ("0000000000000F00", "102CA040 neutral dimmer"),
                ("0000000000010F00", "102CA040 dimmer low"),
                ("00000000006D0F00", "102CA040 dimmer mid"),
                ("0000000000C50F00", "102CA040 dimmer high"),
                ("0000000000FE0F00", "102CA040 dimmer max"),
                ("0000000000000F00", "102CA040 neutral return"),
            ],
            TimeSpan.FromMilliseconds(100),
            TimeSpan.FromSeconds(5),
            true,
            "lights/TC isolation held 102CA040 value; watch TC and illumination",
            TimeSpan.FromSeconds(55));

        return schedule;
    }

    private static List<ScheduledTxFrame> IpcNativeHandbrakeProbeSchedule()
    {
        var runEnd = TimeSpan.FromSeconds(45);
        var schedule = IpcSimulatorKeyOnHoldSchedule(
            runEnd,
            includeKeyOnHeartbeat: true,
            includeNativeHmiButtons: false,
            keyOn10240040Payload: "83FE880100D10FFD",
            includeNativeIconTraffic: false);

        AddPeriodicWindow(schedule, 0x1020C040, "0040040401", TimeSpan.FromMilliseconds(250), true, "parking-brake support state held released: 1020C040 neutral/released", TimeSpan.Zero, runEnd);

        AddPeriodicWindow(schedule, 0x103B4040, "00", TimeSpan.FromMilliseconds(500), true, "parking-brake clear window 0-8s: 103B4040#00", TimeSpan.Zero, TimeSpan.FromSeconds(8));
        AddPeriodicWindow(schedule, 0x103B4040, "04", TimeSpan.FromMilliseconds(500), true, "parking-brake ON window 8-18s: confirmed 103B4040#04", TimeSpan.FromSeconds(8), TimeSpan.FromSeconds(18));
        AddPeriodicWindow(schedule, 0x103B4040, "00", TimeSpan.FromMilliseconds(500), true, "parking-brake OFF window 18-28s: 103B4040#00", TimeSpan.FromSeconds(18), TimeSpan.FromSeconds(28));
        AddPeriodicWindow(schedule, 0x103B4040, "04", TimeSpan.FromMilliseconds(500), true, "parking-brake ON window 28-38s: confirmed 103B4040#04", TimeSpan.FromSeconds(28), TimeSpan.FromSeconds(38));
        AddPeriodicWindow(schedule, 0x103B4040, "00", TimeSpan.FromMilliseconds(500), true, "parking-brake final OFF window 38-45s: 103B4040#00", TimeSpan.FromSeconds(38), runEnd);

        return schedule;
    }

    private static List<ScheduledTxFrame> IpcNativeTcProbeSchedule()
    {
        var runEnd = TimeSpan.FromSeconds(45);
        var schedule = IpcSimulatorKeyOnHoldSchedule(
            runEnd,
            includeKeyOnHeartbeat: true,
            includeNativeHmiButtons: false,
            includeNativeIconTraffic: false,
            includeNative10240040: false,
            includeNative102CA040: false);

        AddOneShot(schedule, 0x10240040, "83FE880100D70FFD", true, "TC isolation neutral 10240040 seed before held windows", TimeSpan.Zero);
        AddOneShot(schedule, 0x102CA040, "0000000000000F00", true, "TC isolation neutral 102CA040 seed before held windows", TimeSpan.Zero);

        AddPeriodicWindow(schedule, 0x102CC040, "0000C00000000000", TimeSpan.FromMilliseconds(250), true, "TC clear/off candidate window 0-10s: baseline before confirmed ON payload", TimeSpan.Zero, TimeSpan.FromSeconds(10));
        AddPeriodicWindow(schedule, 0x102CC040, "0400C00000000000", TimeSpan.FromMilliseconds(250), true, "TC ON window 10-20s: confirmed by lights probe t=50s", TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(20));
        AddPeriodicWindow(schedule, 0x102CC040, "0000C00000000000", TimeSpan.FromMilliseconds(250), true, "TC clear/off candidate window 20-30s: verify whether 0000 clears the telltale", TimeSpan.FromSeconds(20), TimeSpan.FromSeconds(30));
        AddPeriodicWindow(schedule, 0x102CC040, "0400C00000000000", TimeSpan.FromMilliseconds(250), true, "TC ON repeat window 30-40s: confirmed 102CC040#0400C00000000000", TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(40));
        AddPeriodicWindow(schedule, 0x102CC040, "0000C00000000000", TimeSpan.FromMilliseconds(250), true, "TC final clear/off candidate window 40-45s", TimeSpan.FromSeconds(40), runEnd);

        return schedule;
    }

    private static List<ScheduledTxFrame> IpcSimulatorNativeTransitionSchedule()
    {
        var schedule = new List<ScheduledTxFrame>();

        var offStart = TimeSpan.Zero;
        var keyInStart = TimeSpan.FromSeconds(6);
        var keyOnStart = TimeSpan.FromSeconds(12);
        var secondTurnStart = TimeSpan.FromSeconds(28);
        var cleanupStart = TimeSpan.FromSeconds(42);
        var runEnd = TimeSpan.FromSeconds(45);

        AddOneShot(schedule, 0x100, "", false, "native Corsa E SWCAN wake pulse", offStart);
        AddOneShot(schedule, 0x13FFE040, "", true, "native fake BCM/source 0x40 presence seed", offStart);
        AddOneShot(schedule, 0x621, "0140000000000000", false, "native network-management startup/init seed", offStart);
        AddOneShot(schedule, 0x621, "0002000000000000", false, "native unlock/key-in network-management seed", TimeSpan.FromMilliseconds(100));

        AddPeriodicWindow(schedule, 0x13FFE040, "", TimeSpan.FromMilliseconds(1200), true, "native fake BCM/source 0x40 presence, captured-like cadence", TimeSpan.FromMilliseconds(1200), cleanupStart);
        AddPeriodicWindow(schedule, 0x621, "0002000000000000", TimeSpan.FromMilliseconds(1000), false, "native unlock/key-in network-management context", TimeSpan.FromMilliseconds(1000), keyOnStart);
        AddPeriodicWindow(schedule, 0x621, "0052000000000000", TimeSpan.FromMilliseconds(1000), false, "native key-on network-management context", keyOnStart, cleanupStart);

        AddPeriodicWindow(schedule, 0x10210040, "0000800000008000", TimeSpan.FromMilliseconds(100), true, "native BCM/body base status from IPC_disconnected capture", offStart, cleanupStart);
        AddPeriodicWindow(schedule, 0x10264040, "0000000000000000", TimeSpan.FromMilliseconds(1000), true, "native BCM/body base zero-status frame", offStart, cleanupStart);
        AddPeriodicWindow(schedule, 0x102CA040, "0000000000000F00", TimeSpan.FromMilliseconds(1000), true, "native BCM/body base 102CA040 context", offStart, cleanupStart);

        AddPeriodicWindow(schedule, 0x10220040, "1000000040120000", TimeSpan.FromMilliseconds(1000), true, "native BCM/body 10220040 unlock/key-in variant", offStart, keyOnStart);
        AddPeriodicWindow(schedule, 0x10220040, "1000000040080000", TimeSpan.FromMilliseconds(1000), true, "native BCM/body 10220040 key-on variant", keyOnStart, cleanupStart);

        AddPayloadCycleWindow(schedule, 0x10230040, ["0000000000000000", "1000000010000000", "2000000020000000", "3000000030000000"], TimeSpan.FromMilliseconds(250), true, "native BCM/body 10230040 captured counter/state cycle", offStart, cleanupStart);
        AddPayloadCycleWindow(schedule, 0x1022E040, ["0000000000000000", "1000000010000000", "2000000020000000", "3000000030000000"], TimeSpan.FromMilliseconds(250), true, "native BCM/body 1022E040 captured counter/state cycle", keyInStart, cleanupStart - keyInStart);

        AddPeriodicWindow(schedule, 0x10240040, "8004880100000007", TimeSpan.FromMilliseconds(1000), true, "native BCM/body 10240040 base/off variant", offStart, secondTurnStart);
        AddPeriodicWindow(schedule, 0x10240040, "83FE880100D70FFD", TimeSpan.FromMilliseconds(1000), true, "native BCM/body 10240040 key-on/second-turn variant", secondTurnStart, cleanupStart);

        AddPeriodicWindow(schedule, 0x102C0040, "0000000000", TimeSpan.FromMilliseconds(1000), true, "native BCM/body 102C0040 neutral captured context", offStart, keyOnStart);
        AddPeriodicWindow(schedule, 0x102C0040, "803C96B503", TimeSpan.FromMilliseconds(1000), true, "native BCM/body 102C0040 key-on captured context", keyOnStart, cleanupStart);

        AddPeriodicWindow(schedule, 0x10754040, "000000", TimeSpan.FromMilliseconds(1000), true, "native key/body context no-key/off variant", offStart, keyInStart);
        AddPeriodicWindow(schedule, 0x10754040, "040000", TimeSpan.FromMilliseconds(1000), true, "native key/body context key-present variant", keyInStart, keyOnStart);
        AddPeriodicWindow(schedule, 0x10754040, "040400", TimeSpan.FromMilliseconds(1000), true, "native key/body context key-on variant", keyOnStart, cleanupStart);

        AddPeriodicWindow(schedule, 0x10242040, "00", TimeSpan.FromMilliseconds(1000), true, "native 10242040 off/unlock state", offStart, keyInStart);
        AddPeriodicWindow(schedule, 0x10242040, "01", TimeSpan.FromMilliseconds(1000), true, "native 10242040 key-in/key-on state", keyInStart, secondTurnStart);
        AddPeriodicWindow(schedule, 0x10242040, "02", TimeSpan.FromMilliseconds(1000), true, "native 10242040 second-turn candidate", secondTurnStart, cleanupStart);

        AddPeriodicWindow(schedule, 0x10304058, "96B501", TimeSpan.FromMilliseconds(110), true, "native source 0x58 DIC/icon traffic captured with IPC connected", TimeSpan.FromSeconds(20), TimeSpan.FromSeconds(36));

        AddOneShot(schedule, 0x10414040, "0000FB05", true, "native lock/body context one-shot captured variant", TimeSpan.FromSeconds(14));
        AddOneShot(schedule, 0x10414040, "00017B05", true, "native lock/body context one-shot captured variant", TimeSpan.FromSeconds(18));

        AddOneShot(schedule, 0x10438040, "01", true, "native HMI button probe DLC-1 press: 0x01", TimeSpan.FromSeconds(22));
        AddOneShot(schedule, 0x10438040, "00", true, "native HMI button release after 0x01", TimeSpan.FromSeconds(22.15));
        AddOneShot(schedule, 0x10438040, "02", true, "native HMI button probe DLC-1 press: 0x02", TimeSpan.FromSeconds(24));
        AddOneShot(schedule, 0x10438040, "00", true, "native HMI button release after 0x02", TimeSpan.FromSeconds(24.15));

        AddOneShot(schedule, 0x10754040, "000000", true, "native cleanup clear key/body context", cleanupStart);
        AddOneShot(schedule, 0x102C0040, "0000000000", true, "native cleanup clear 102C0040 context", cleanupStart + TimeSpan.FromMilliseconds(50));
        AddOneShot(schedule, 0x10242040, "00", true, "native cleanup clear 10242040 state", cleanupStart + TimeSpan.FromMilliseconds(100));
        AddOneShot(schedule, 0x621, "0040000000000000", false, "native cleanup steady keepalive", cleanupStart + TimeSpan.FromMilliseconds(150));
        AddOneShot(schedule, 0x13FFE040, "", true, "native cleanup final fake BCM/source 0x40 presence", runEnd - TimeSpan.FromMilliseconds(250));

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

    private static uint WrapBodyId(uint standardId)
    {
        return 0x10000000u | (standardId << 13) | 0x40u;
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

    private static void AddPayloadHoldSequence(
        List<ScheduledTxFrame> schedule,
        uint canId,
        (string Payload, string Label)[] payloads,
        TimeSpan period,
        TimeSpan hold,
        bool isExtended,
        string note,
        TimeSpan start)
    {
        for (var i = 0; i < payloads.Length; i++)
        {
            var windowStart = start + TimeSpan.FromTicks(hold.Ticks * i);
            AddPeriodicWindow(
                schedule,
                canId,
                payloads[i].Payload,
                period,
                isExtended,
                $"{note}: {payloads[i].Label}",
                windowStart,
                windowStart + hold);
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
