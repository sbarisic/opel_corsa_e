using System.Buffers.Binary;

namespace cantool;

internal static class CanConstants
{
    public const uint EffFlag = 0x80000000;
    public const uint EffMask = 0x1FFFFFFF;
}

internal readonly record struct CanFrame(double Timestamp, uint CanId, byte[] Data, bool IsExtended)
{
    public string DataHex => Convert.ToHexString(Data);
    public string CandumpId => IsExtended ? $"{CanId:X8}" : $"{CanId:X3}";
    public Gmlan29Id? Gmlan29 => IsExtended ? Gmlan29Id.Decode(CanId) : null;

    public static CanFrame FromGsUsb(byte[] raw)
    {
        var canIdRaw = BinaryPrimitives.ReadUInt32LittleEndian(raw.AsSpan(4, 4));
        var dlc = Math.Clamp(raw[8], (byte)0, (byte)8);
        var data = raw.Skip(12).Take(dlc).ToArray();
        var isExtended = (canIdRaw & CanConstants.EffFlag) != 0;
        var arbitrationId = canIdRaw & CanConstants.EffMask;
        var ts = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() / 1000.0;
        return new CanFrame(ts, arbitrationId, data, isExtended);
    }

    public string ToCandumpLine() => $"({Timestamp:0.000000}) can0 {CandumpId}#{DataHex}";
}

internal readonly record struct Gmlan29Id(int Priority, uint ArbitrationId, uint Sender)
{
    public static Gmlan29Id Decode(uint canId)
    {
        return new Gmlan29Id(
            (int)((canId >> 26) & 0x7),
            (canId >> 13) & 0x1FFF,
            canId & 0x1FFF);
    }

    public string ToSummaryString() => $"prio={Priority} arbid=0x{ArbitrationId:X3} sender=0x{Sender:X3}";
    public string ToAnnotatedSummaryString()
    {
        var arbidName = Gmlan29Catalog.GetArbitrationName(ArbitrationId);
        var senderName = Gmlan29Catalog.GetSenderRangeName(Sender);
        return $"prio={Priority} arbid=0x{ArbitrationId:X3} {arbidName} sender=0x{Sender:X3} {senderName}";
    }
}

internal static class Gmlan29Catalog
{
    private static readonly Dictionary<uint, string> ArbitrationNames = new()
    {
        [0x001] = "System Power Mode",
        [0x00F] = "Chime Command",
        [0x010] = "Chime Status",
        [0x011] = "Dimming Information",
        [0x012] = "VIN Digits 2-9",
        [0x013] = "VIN Digits 10-17",
        [0x018] = "Battery Voltage",
        [0x025] = "Transmission Gear",
        [0x026] = "Fuel Information",
        [0x027] = "Odo/Brake/Wash Level",
        [0x028] = "Vehicle Speed Information",
        [0x029] = "Engine Information 1",
        [0x02F] = "Brake/Cruise Status",
        [0x032] = "Engine Information 3",
        [0x037] = "Engine Information 2",
        [0x061] = "Outside Air Temp",
        [0x062] = "ABS/Traction Status",
        [0x068] = "Wheel Controls",
        [0x180] = "DIC Text Attributes",
        [0x181] = "DIC Text Line Attributes",
        [0x182] = "DIC Set Display Icon",
        [0x183] = "DIC Text Status",
        [0x184] = "DIC Menu Action",
        [0x185] = "DIC Set Display Parameters",
        [0x186] = "DIC Set Display Text",
        [0x1FFF] = "Wake/Network Keepalive",
    };

    public static string GetArbitrationName(uint arbitrationId)
    {
        return ArbitrationNames.TryGetValue(arbitrationId, out var name) ? name : "Unknown";
    }

    public static string GetSenderRangeName(uint sender)
    {
        return sender switch
        {
            <= 0x01F => "Powertrain",
            <= 0x03F => "Chassis",
            <= 0x057 => "Body/Integration",
            <= 0x05F => "Restraints",
            <= 0x06F => "Driver Info/Displays",
            <= 0x07F => "Lighting",
            <= 0x08F => "Entertainment/Audio",
            <= 0x097 => "Personal Communication",
            <= 0x09F => "HVAC",
            <= 0x0BF => "Convenience",
            <= 0x0C7 => "Security",
            <= 0x0CB => "EV Energy",
            <= 0x0FD => "Future Expansion",
            _ => "Unknown Sender",
        };
    }
}

internal sealed record ScheduledTxFrame(
    uint CanId,
    byte[] Data,
    TimeSpan Period,
    bool IsExtended,
    string Note,
    TimeSpan InitialDelay = default,
    int? MaxSends = null)
{
    public DateTimeOffset NextDue { get; set; }
    public int SentCount { get; set; }
    public string CandumpId => IsExtended ? $"{CanId:X8}" : $"{CanId:X3}";

    public byte[] ToGsUsbFrame(bool hwTimestamp)
    {
        var size = hwTimestamp ? 24 : 20;
        var raw = new byte[size];
        var canIdRaw = CanId | (IsExtended ? CanConstants.EffFlag : 0);
        BinaryPrimitives.WriteUInt32LittleEndian(raw.AsSpan(0, 4), 0);
        BinaryPrimitives.WriteUInt32LittleEndian(raw.AsSpan(4, 4), canIdRaw);
        raw[8] = (byte)Data.Length;
        raw[9] = 0;
        raw[10] = 0;
        raw[11] = 0;
        Data.CopyTo(raw.AsSpan(12, Data.Length));
        return raw;
    }
}
