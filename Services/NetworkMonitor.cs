using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;

namespace SpeedoMeter.Services;

public sealed class NetworkMonitor
{
    private readonly Dictionary<string, AdapterCounterState> _previousAdapterCounters = new(StringComparer.OrdinalIgnoreCase);
    private bool _initialized;

    public long DownloadSpeed { get; private set; }
    public long UploadSpeed { get; private set; }
    public TelemetrySnapshot CurrentSnapshot { get; private set; } = TelemetrySnapshot.Empty;

    public TelemetrySnapshot Update()
    {
        try
        {
            var interfaces = NetworkInterface.GetAllNetworkInterfaces()
                .Where(networkInterface => networkInterface.OperationalStatus == OperationalStatus.Up
                    && networkInterface.NetworkInterfaceType != NetworkInterfaceType.Loopback);

            var currentCounters = new Dictionary<string, AdapterCounterState>(StringComparer.OrdinalIgnoreCase);
            var adapters = new List<AdapterTelemetry>();

            foreach (var ni in interfaces)
            {
                try
                {
                    var stats = ni.GetIPv4Statistics();

                    long downloadSpeed = 0;
                    long uploadSpeed = 0;
                    string adapterId = string.IsNullOrWhiteSpace(ni.Id) ? ni.Name : ni.Id;

                    if (_initialized && _previousAdapterCounters.TryGetValue(adapterId, out var previous))
                    {
                        downloadSpeed = Math.Max(0, stats.BytesReceived - previous.BytesReceived);
                        uploadSpeed = Math.Max(0, stats.BytesSent - previous.BytesSent);
                    }

                    currentCounters[adapterId] = new AdapterCounterState(stats.BytesReceived, stats.BytesSent);
                    adapters.Add(new AdapterTelemetry(
                        adapterId,
                        ni.Name,
                        GetAdapterType(ni.NetworkInterfaceType, ni.Description),
                        downloadSpeed,
                        uploadSpeed,
                        ni.Description));
                }
                catch
                {
                    // Some adapters may not support IPv4 stats
                }
            }

            _previousAdapterCounters.Clear();
            foreach (var counter in currentCounters)
            {
                _previousAdapterCounters[counter.Key] = counter.Value;
            }

            _initialized = true;

            var orderedAdapters = adapters
                .OrderByDescending(adapter => adapter.TotalSpeed)
                .ThenBy(adapter => adapter.Name)
                .ToArray();

            DownloadSpeed = orderedAdapters.Sum(adapter => adapter.DownloadSpeed);
            UploadSpeed = orderedAdapters.Sum(adapter => adapter.UploadSpeed);

            CurrentSnapshot = new TelemetrySnapshot(DownloadSpeed, UploadSpeed, orderedAdapters);
        }
        catch
        {
            // Network subsystem unavailable
            CurrentSnapshot = TelemetrySnapshot.Empty;
            DownloadSpeed = CurrentSnapshot.DownloadSpeed;
            UploadSpeed = CurrentSnapshot.UploadSpeed;
            _previousAdapterCounters.Clear();
            _initialized = false;
        }

        return CurrentSnapshot;
    }

    private static string GetAdapterType(NetworkInterfaceType interfaceType, string description)
    {
        string loweredDescription = description.ToLowerInvariant();

        if (loweredDescription.Contains("vpn") || interfaceType == NetworkInterfaceType.Ppp)
        {
            return "VPN";
        }

        if (interfaceType == NetworkInterfaceType.Wireless80211)
        {
            return "Wi-Fi";
        }

        if (interfaceType == NetworkInterfaceType.Ethernet
            || interfaceType == NetworkInterfaceType.GigabitEthernet
            || interfaceType == NetworkInterfaceType.FastEthernetFx
            || interfaceType == NetworkInterfaceType.FastEthernetT)
        {
            return "Ethernet";
        }

        if (loweredDescription.Contains("virtual") || loweredDescription.Contains("hyper-v") || loweredDescription.Contains("vmware"))
        {
            return "Virtual";
        }

        return interfaceType.ToString();
    }

    private readonly record struct AdapterCounterState(long BytesReceived, long BytesSent);
}
