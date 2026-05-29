using System.Buffers.Binary;
using LibUsbDotNet;
using LibUsbDotNet.Main;

namespace cantool;

internal sealed class GsUsbDevice : IDisposable
{
    private const byte RequestTypeOut = 0x41;
    private const byte RequestTypeIn = 0xC1;
    private const byte BreqBitTiming = 1;
    private const byte BreqMode = 2;
    private const byte BreqBtConst = 4;

    private const uint ModeReset = 0;
    private const uint ModeStart = 1;
    private const uint ModeListenOnly = 1;
    private const uint ModeHwTimestamp = 16;

    public static readonly (int Vid, int Pid, string Name)[] KnownDevices =
    [
        (0x1D50, 0x606F, "gs_usb/candleLight"),
        (0x1209, 0x2323, "candleLight"),
        (0x1CD2, 0x606F, "CANext FD"),
        (0x16D0, 0x10B8, "CANDebugger FD"),
        (0x1209, 0xCA01, "CANnectivity")
    ];

    private readonly UsbDevice _device;
    private readonly UsbEndpointReader _reader;
    private readonly UsbEndpointWriter _writer;
    private readonly bool _hwTimestamp;

    private GsUsbDevice(UsbDevice device, UsbEndpointReader reader, UsbEndpointWriter writer, bool hwTimestamp)
    {
        _device = device;
        _reader = reader;
        _writer = writer;
        _hwTimestamp = hwTimestamp;
    }

    public static GsUsbDevice Open(bool listenOnly)
    {
        UsbDevice? device = null;
        foreach (var (vid, pid, _) in KnownDevices)
        {
            device = UsbDevice.OpenUsbDevice(new UsbDeviceFinder(vid, pid));
            if (device is not null)
            {
                break;
            }
        }

        if (device is null)
        {
            throw new InvalidOperationException("no gs_usb/candleLight devices found");
        }

        if (device is IUsbDevice wholeDevice)
        {
            wholeDevice.SetConfiguration(1);
            wholeDevice.ClaimInterface(0);
        }

        var gs = new GsUsbDevice(
            device,
            device.OpenEndpointReader(ReadEndpointID.Ep01, 24),
            device.OpenEndpointWriter(WriteEndpointID.Ep02),
            hwTimestamp: true);

        gs.Stop(ignoreErrors: true);
        var capability = gs.ReadCapability();
        var flags = ModeHwTimestamp | (listenOnly ? ModeListenOnly : 0u);
        flags &= capability.Feature;
        flags &= ModeHwTimestamp | ModeListenOnly;

        gs.SetTiming(propSeg: 1, phaseSeg1: 13, phaseSeg2: 5, sjw: 4, brp: 255);
        gs.Start(flags);
        gs._reader.Flush();
        return gs;
    }

    public int FlushStale(TimeSpan duration)
    {
        var end = DateTime.UtcNow + duration;
        var count = 0;
        while (DateTime.UtcNow < end)
        {
            if (ReadFrame(timeoutMs: 10) is not null)
            {
                count++;
            }
        }

        return count;
    }

    public CanFrame? ReadFrame(int timeoutMs)
    {
        var size = _hwTimestamp ? 24 : 20;
        var buffer = new byte[size];
        var result = _reader.Read(buffer, timeoutMs, out var transferred);
        if (result is ErrorCode.IoTimedOut || (result == ErrorCode.None && transferred == 0))
        {
            return null;
        }

        if (result != ErrorCode.None || transferred < size)
        {
            return null;
        }

        return CanFrame.FromGsUsb(buffer);
    }

    public void SendFrame(ScheduledTxFrame frame)
    {
        var raw = frame.ToGsUsbFrame(_hwTimestamp);
        var result = _writer.Write(raw, timeout: 1000, out var transferred);
        if (result != ErrorCode.None || transferred != raw.Length)
        {
            throw new InvalidOperationException($"USB write failed: {result}, transferred={transferred}");
        }
    }

    public void Dispose()
    {
        Stop(ignoreErrors: true);
        if (_device is IUsbDevice wholeDevice)
        {
            wholeDevice.ReleaseInterface(0);
        }

        _reader.Dispose();
        _writer.Dispose();
        _device.Close();
    }

    private DeviceCapability ReadCapability()
    {
        var data = ControlIn(BreqBtConst, 40);
        return DeviceCapability.FromBytes(data);
    }

    private void SetTiming(uint propSeg, uint phaseSeg1, uint phaseSeg2, uint sjw, uint brp)
    {
        Span<byte> data = stackalloc byte[20];
        BinaryPrimitives.WriteUInt32LittleEndian(data[0..4], propSeg);
        BinaryPrimitives.WriteUInt32LittleEndian(data[4..8], phaseSeg1);
        BinaryPrimitives.WriteUInt32LittleEndian(data[8..12], phaseSeg2);
        BinaryPrimitives.WriteUInt32LittleEndian(data[12..16], sjw);
        BinaryPrimitives.WriteUInt32LittleEndian(data[16..20], brp);
        ControlOut(BreqBitTiming, data.ToArray());
    }

    private void Start(uint flags)
    {
        Span<byte> data = stackalloc byte[8];
        BinaryPrimitives.WriteUInt32LittleEndian(data[0..4], ModeStart);
        BinaryPrimitives.WriteUInt32LittleEndian(data[4..8], flags);
        ControlOut(BreqMode, data.ToArray());
    }

    private void Stop(bool ignoreErrors)
    {
        Span<byte> data = stackalloc byte[8];
        BinaryPrimitives.WriteUInt32LittleEndian(data[0..4], ModeReset);
        BinaryPrimitives.WriteUInt32LittleEndian(data[4..8], 0);
        try
        {
            ControlOut(BreqMode, data.ToArray());
        }
        catch when (ignoreErrors)
        {
        }
    }

    private byte[] ControlIn(byte request, short length)
    {
        var buffer = new byte[length];
        var packet = new UsbSetupPacket(RequestTypeIn, request, 0, 0, length);
        if (!_device.ControlTransfer(ref packet, buffer, length, out var transferred) || transferred != length)
        {
            throw new InvalidOperationException($"control IN request {request} failed: transferred={transferred}");
        }

        return buffer;
    }

    private void ControlOut(byte request, byte[] data)
    {
        var packet = new UsbSetupPacket(RequestTypeOut, request, 0, 0, (short)data.Length);
        if (!_device.ControlTransfer(ref packet, data, data.Length, out var transferred) || transferred != data.Length)
        {
            throw new InvalidOperationException($"control OUT request {request} failed: transferred={transferred}");
        }
    }

    private readonly record struct DeviceCapability(uint Feature, uint Clock)
    {
        public static DeviceCapability FromBytes(byte[] data)
        {
            return new DeviceCapability(
                BinaryPrimitives.ReadUInt32LittleEndian(data.AsSpan(0, 4)),
                BinaryPrimitives.ReadUInt32LittleEndian(data.AsSpan(4, 4)));
        }
    }
}
