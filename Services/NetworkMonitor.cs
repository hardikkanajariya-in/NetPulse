using System;
using System.Linq;
using System.Net.NetworkInformation;

namespace SpeedoMeter.Services;

public sealed class NetworkMonitor
{
    private long _previousBytesReceived;
    private long _previousBytesSent;
    private bool _initialized;

    public long DownloadSpeed { get; private set; }
    public long UploadSpeed { get; private set; }

    public void Update()
    {
        try
        {
            var interfaces = NetworkInterface.GetAllNetworkInterfaces()
                .Where(n => n.OperationalStatus == OperationalStatus.Up
                         && n.NetworkInterfaceType != NetworkInterfaceType.Loopback
                         && n.NetworkInterfaceType != NetworkInterfaceType.Tunnel);

            long totalReceived = 0;
            long totalSent = 0;

            foreach (var ni in interfaces)
            {
                try
                {
                    var stats = ni.GetIPv4Statistics();
                    totalReceived += stats.BytesReceived;
                    totalSent += stats.BytesSent;
                }
                catch
                {
                    // Some adapters may not support IPv4 stats
                }
            }

            if (_initialized)
            {
                long diffReceived = totalReceived - _previousBytesReceived;
                long diffSent = totalSent - _previousBytesSent;

                // Clamp negatives to 0 (handles sleep/resume, adapter changes)
                DownloadSpeed = diffReceived > 0 ? diffReceived : 0;
                UploadSpeed = diffSent > 0 ? diffSent : 0;
            }
            else
            {
                DownloadSpeed = 0;
                UploadSpeed = 0;
                _initialized = true;
            }

            _previousBytesReceived = totalReceived;
            _previousBytesSent = totalSent;
        }
        catch
        {
            // Network subsystem unavailable
            DownloadSpeed = 0;
            UploadSpeed = 0;
            _initialized = false;
        }
    }
}
