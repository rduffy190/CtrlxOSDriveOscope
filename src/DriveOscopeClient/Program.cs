using System.Diagnostics;
using System.Text.Json;
using Datalayer;
using DriveCapture;
using Google.FlatBuffers;
using scope_config;

namespace DriveOscopeClient
{
    /// <summary>
    /// Command line client for the Scope Capture Service: creates a capture from a ScopeConfig JSON file,
    /// fires the trigger, waits for done, saves the ScopeCapture as JSON, and deletes the capture on exit.
    /// Client setup follows libExamples/ctrlx/datalayer.client.simple.
    ///
    /// Usage: DriveOscopeClient [scopeConfig.json] [--core-ip 192.168.1.1] [--ssl-port 443]
    ///        [--user boschrexroth] [--password boschrexroth] [--output output]
    /// </summary>
    internal static class Program
    {
        // Must match the service's fixed Data Layer root (DriveOscope Program.DataLayerRoot).
        private const string _capturesAddress = "ctrlx-drive-oscope/captures";
        private const int _pollIntervalMs = 200;
        private const int _armTimeoutMs = 30000;

        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true,
        };

        private static async Task<int> Main(string[] args)
        {
            Dictionary<string, string> t_options = ParseOptions(args, out string? t_configArg);
            string t_configPath = t_configArg ?? Path.Combine(AppContext.BaseDirectory, "scopeConfig.json");
            string t_outputDirectory = Path.GetFullPath(t_options.GetValueOrDefault("output", "output"));

            ScopeConfigT t_config;
            try
            {
                t_config = JsonSerializer.Deserialize<ScopeConfigT>(await File.ReadAllTextAsync(t_configPath), _jsonOptions)
                    ?? throw new InvalidDataException("Config file is empty.");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Could not load config '{t_configPath}': {ex.Message}");
                return 1;
            }

            using DatalayerSystem t_system = new();
            t_system.Start(startBroker: false);

            string t_remote = new Remote(
                ip: t_options.GetValueOrDefault("core-ip", "192.168.1.1"),
                user: t_options.GetValueOrDefault("user", "boschrexroth"),
                password: t_options.GetValueOrDefault("password", "boschrexroth"),
                sslPort: int.Parse(t_options.GetValueOrDefault("ssl-port", "443"))).ToString();
            using IClient t_client = t_system.Factory.CreateClient(t_remote);
            if (!t_client.IsConnected)
            {
                Console.Error.WriteLine("ctrlX Data Layer client is not connected.");
                return 1;
            }

            string? t_captureAddress = CreateCapture(t_client, _capturesAddress, t_config);
            if (t_captureAddress == null)
            {
                return 1;
            }

            try
            {
                Console.WriteLine($"Capture created: {t_captureAddress}. Waiting for the drive scope to arm...");
                if (!await WaitForAsync(t_client, t_captureAddress, "ready", _armTimeoutMs))
                {
                    return 1;
                }

                while (true)
                {
                    Console.Write("Armed. Press Enter to trigger, or type q + Enter to quit: ");
                    if (string.Equals(Console.ReadLine()?.Trim(), "q", StringComparison.OrdinalIgnoreCase))
                    {
                        return 0;
                    }

                    if (!WriteTrigger(t_client, t_captureAddress, true))
                    {
                        return 1;
                    }
                    Console.WriteLine("Triggered. Waiting for the capture to finish...");

                    // The service enforces measurement_timeout_ms; allow a margin for reading the samples.
                    int t_doneTimeoutMs = checked((int)t_config.MeasurementTimeoutMs) + _armTimeoutMs;
                    if (!await WaitForAsync(t_client, t_captureAddress, "done", t_doneTimeoutMs))
                    {
                        return 1;
                    }

                    string? t_path = await SaveCaptureAsync(t_client, t_captureAddress, t_outputDirectory);
                    if (t_path == null)
                    {
                        return 1;
                    }
                    Console.WriteLine($"Saved {t_path}");

                    // Writing false re-arms the scope for the next capture.
                    if (!WriteTrigger(t_client, t_captureAddress, false)
                        || !await WaitForAsync(t_client, t_captureAddress, "ready", _armTimeoutMs))
                    {
                        return 1;
                    }
                }
            }
            finally
            {
                DLR_RESULT t_removed = t_client.Remove(t_captureAddress);
                Console.WriteLine($"Deleted capture {t_captureAddress}: {t_removed}");
                t_system.Stop();
            }
        }

        private static string? CreateCapture(IClient client, string capturesAddress, ScopeConfigT config)
        {
            FlatBufferBuilder t_builder = new(Variant.DefaultFlatbuffersInitialSize);
            t_builder.Finish(ScopeConfig.Pack(t_builder, config).Value);

            var (t_result, t_location) = client.Create(capturesAddress, new Variant(t_builder));
            if (t_result.IsBad())
            {
                Console.Error.WriteLine($"Create on '{capturesAddress}' failed: {t_result}. Check the service log for the reason.");
                return null;
            }
            return t_location.ToString();
        }

        private static bool WriteTrigger(IClient client, string captureAddress, bool value)
        {
            DLR_RESULT t_result = client.Write($"{captureAddress}/trigger", new Variant(value));
            if (t_result.IsBad())
            {
                Console.Error.WriteLine($"Writing trigger={value} failed: {t_result}");
                ReportError(client, captureAddress);
                return false;
            }
            return true;
        }

        /// <summary>Polls a bool node until it is true. Stops early if the capture failed.</summary>
        private static async Task<bool> WaitForAsync(IClient client, string captureAddress, string leaf, int timeoutMs)
        {
            Stopwatch t_timer = Stopwatch.StartNew();
            while (t_timer.ElapsedMilliseconds < timeoutMs)
            {
                var (t_result, t_value) = client.Read($"{captureAddress}/{leaf}");
                if (t_result.IsGood() && t_value.ToBool())
                {
                    return true;
                }

                var (t_stateResult, t_state) = client.Read($"{captureAddress}/state");
                if (t_stateResult.IsGood() && t_state.ToString() == "failed")
                {
                    ReportError(client, captureAddress);
                    return false;
                }
                await Task.Delay(_pollIntervalMs);
            }

            Console.Error.WriteLine($"Timed out after {timeoutMs} ms waiting for '{leaf}'.");
            return false;
        }

        private static async Task<string?> SaveCaptureAsync(IClient client, string captureAddress, string outputDirectory)
        {
            var (t_result, t_value) = client.Read($"{captureAddress}/capture");
            if (t_result.IsBad())
            {
                Console.Error.WriteLine($"Reading the capture failed: {t_result}");
                return null;
            }

            ScopeCaptureT t_capture = ScopeCapture.GetRootAsScopeCapture(t_value.ToFlatbuffers()).UnPack();

            Directory.CreateDirectory(outputDirectory);
            string t_path = Path.Combine(outputDirectory, $"scope_{DateTime.Now:yyyyMMdd_HHmmss}.json");
            await using FileStream t_stream = File.Create(t_path);
            await JsonSerializer.SerializeAsync(t_stream, t_capture, _jsonOptions);
            return t_path;
        }

        private static void ReportError(IClient client, string captureAddress)
        {
            var (t_result, t_error) = client.Read($"{captureAddress}/error");
            if (t_result.IsGood() && !string.IsNullOrEmpty(t_error.ToString()))
            {
                Console.Error.WriteLine($"Capture failed: {t_error}");
            }
        }

        private static Dictionary<string, string> ParseOptions(string[] args, out string? configPath)
        {
            Dictionary<string, string> t_options = new(StringComparer.OrdinalIgnoreCase);
            configPath = null;
            for (int t_i = 0; t_i < args.Length; t_i++)
            {
                if (args[t_i].StartsWith("--", StringComparison.Ordinal) && t_i + 1 < args.Length)
                {
                    t_options[args[t_i][2..]] = args[++t_i];
                }
                else
                {
                    configPath = args[t_i];
                }
            }
            return t_options;
        }
    }
}
