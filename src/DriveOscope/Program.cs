using System.Runtime.InteropServices;
using Datalayer;

namespace DriveOscope
{
    /// <summary>
    /// Scope Capture Service: a ctrlX Data Layer provider that configures, triggers and reads a drive
    /// oscilloscope through the Easy Automation Library. See designDocs/description/scope_capture_service.md.
    /// Provider setup follows libExamples/ctrlx/datalayer.provider.simple and datalayer.provider.virtual.
    /// </summary>
    internal static class Program
    {
        /// <summary>Data Layer root the service publishes under; the endpoints live at {root}/captures.</summary>
        public const string DataLayerRoot = "ctrlx-drive-oscope";

        private const string _capturesAddress = $"{DataLayerRoot}/captures";
        private const string _configTypeAddress = $"types/{DataLayerRoot}/scope-config";
        private const string _captureTypeAddress = $"types/{DataLayerRoot}/scope-capture";

        // How long a deleted capture's worker gets to stop before its drive connection is force-closed (and again after).
        private const int _workerStopTimeoutMs = 4000;

        // No arguments and no settings: a snap starts the service without any.
        private static async Task<int> Main()
        {

            TaskCompletionSource t_shutdown = new();
            // Not AppDomain.ProcessExit as in the SDK samples: it fires while the runtime is already exiting,
            // so Main never reached the cleanup below. Cancelling SIGTERM lets Main finish (snapd stops daemons with SIGTERM).
            using PosixSignalRegistration t_sigterm = PosixSignalRegistration.Create(PosixSignal.SIGTERM, t_context =>
            {
                Console.WriteLine("Received 'SIGTERM' event.");
                t_context.Cancel = true;
                t_shutdown.TrySetResult();
            });
            Console.CancelKeyPress += (_, e) =>
            {
                e.Cancel = true;
                t_shutdown.TrySetResult();
            };

            using DatalayerSystem t_system = new();
            // A broker is already running on ctrlX CORE.
            t_system.Start(startBroker: false);
            Console.WriteLine("ctrlX Data Layer system started.");

            // ProtocolScheme.AUTO: "ipc://" when running in a snap on the ctrlX CORE, otherwise TCP to the
            // factory default CORE (192.168.1.1:443, boschrexroth/boschrexroth) for running from a dev PC.
            string t_remote = new Remote().ToString();
            using IProvider t_provider = t_system.Factory.CreateProvider(t_remote);

            DLR_RESULT t_result = t_provider.RegisterType(_configTypeAddress, Path.Combine(AppContext.BaseDirectory, "bfbs", "scope_config.bfbs"));
            Console.WriteLine($"Registering type '{_configTypeAddress}': {t_result}");
            t_result = t_provider.RegisterType(_captureTypeAddress, Path.Combine(AppContext.BaseDirectory, "bfbs", "scope_capture.bfbs"));
            Console.WriteLine($"Registering type '{_captureTypeAddress}': {t_result}");

            CaptureManager t_manager = new(
                () => new EalDriveClient(),
                TimeSpan.FromMilliseconds(_workerStopTimeoutMs));
            CaptureNodeHandler t_handler = new(t_manager, _capturesAddress, _configTypeAddress, _captureTypeAddress);

            foreach (string t_address in new[] { _capturesAddress, $"{_capturesAddress}/**" })
            {
                (t_result, _) = t_provider.RegisterNode(t_address, t_handler);
                Console.WriteLine($"Registering node '{t_address}': {t_result}");
                if (t_result.IsBad())
                {
                    return 1;
                }
            }

            t_result = t_provider.Start();
            Console.WriteLine($"Provider started: {t_result}");
            if (t_result.IsBad() || !t_provider.IsConnected)
            {
                // Exit and let the snap restart-delay retry.
                Console.WriteLine("Provider is not connected -> exit");
                return 1;
            }

            Console.WriteLine($"Scope Capture Service ready on '{_capturesAddress}'.");
            await t_shutdown.Task;
            Console.WriteLine("Graceful shutdown.");

            // Captures do not survive a restart: stop every worker and close its drive connection.
            await t_manager.DeleteAllAsync();

            t_provider.Stop();
            t_system.Stop();
            Console.WriteLine("ctrlX Data Layer system stopped.");
            return 0;
        }
    }
}
