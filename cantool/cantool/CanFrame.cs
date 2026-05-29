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
}

internal sealed record ScheduledTxFrame(
    uint CanId,
    byte[] Data,
    TimeSpan Period,
    bool IsExtended,
    string Note,
    TimeSpan InitialDelay = default)
{
    public DateTimeOffset NextDue { get; set; }
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
