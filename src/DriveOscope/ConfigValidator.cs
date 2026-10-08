using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using scope_config;

namespace DriveOscope
{
    /// <summary>Validation rules from the design doc; run before a worker is spawned.</summary>
    internal static class ConfigValidator
    {
        // SERCOS IDN: S-x-xxxx / P-x-xxxx (set 0-7, four digit block number).
        private static readonly Regex _idnPattern = new(@"^[SP]-\d-\d{4}$", RegexOptions.Compiled);

        /// <summary>Returns null when the config is valid, otherwise a description of the first problem.</summary>
        public static string? Validate(ScopeConfigT config)
        {
            if (!IsIpv4(config.DriveIpAddress))
            {
                return "drive_ip_address is not a valid IPv4 address.";
            }
            if (config.Signals == null || config.Signals.Count == 0)
            {
                return "signals must contain at least one SERCOS IDN.";
            }
            foreach (string t_signal in config.Signals)
            {
                if (t_signal == null || !_idnPattern.IsMatch(t_signal))
                {
                    return $"signal '{t_signal}' is not a SERCOS IDN of the form S-x-xxxx / P-x-xxxx.";
                }
            }
            if (config.TimeResolutionUs == 0)
            {
                return "time_resolution_us must be > 0.";
            }
            if (config.MemorySize == 0)
            {
                return "memory_size must be > 0.";
            }
            if (config.MeasurementTimeoutMs == 0)
            {
                return "measurement_timeout_ms must be > 0.";
            }
            if (config.PreTriggerPercent > 100)
            {
                return "pre_trigger_percent must be between 0 and 100.";
            }
            return null;
        }

        private static bool IsIpv4(string? value)
        {
            // IPAddress.TryParse accepts short forms such as "1" or "1.2"; require four dotted octets.
            return !string.IsNullOrWhiteSpace(value)
                && value.Count(t_c => t_c == '.') == 3
                && IPAddress.TryParse(value, out IPAddress? t_address)
                && t_address.AddressFamily == AddressFamily.InterNetwork;
        }
    }
}
