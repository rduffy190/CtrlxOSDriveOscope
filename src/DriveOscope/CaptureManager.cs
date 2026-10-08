using System.Collections.Concurrent;
using scope_config;

namespace DriveOscope
{
    /// <summary>
    /// Owns all captures: creates them (one worker each) and deletes them. A deleted capture disappears from
    /// the Data Layer at once, but keeps its drive reserved until its worker has released the connection.
    /// </summary>
    internal sealed class CaptureManager(Func<IDriveClient> driveClientFactory, TimeSpan workerStopTimeout)
    {
        private readonly ConcurrentDictionary<string, CaptureWorker> _captures = new();
        // Deleted captures whose worker is still cleaning up, with the task doing it.
        private readonly ConcurrentDictionary<string, (CaptureWorker worker, Task stop)> _stopping = new();
        private readonly object _gate = new();

        public IReadOnlyCollection<string> CaptureIds => [.. _captures.Keys];

        /// <summary>Validates the config and spawns a worker. Returns immediately; the worker configures the drive.</summary>
        public bool TryCreate(ScopeConfigT config, out string captureIdOrError)
        {
            string? t_error = ConfigValidator.Validate(config);
            if (t_error != null)
            {
                captureIdOrError = t_error;
                return false;
            }

            CaptureWorker t_worker;
            lock (_gate)
            {
                // Every capture that has not finished deleting holds its drive, including failed ones.
                bool t_driveInUse = _captures.Values.Any(t_w => t_w.DriveIpAddress == config.DriveIpAddress)
                    || _stopping.Values.Any(t_s => t_s.worker.DriveIpAddress == config.DriveIpAddress);
                if (t_driveInUse)
                {
                    captureIdOrError = $"A capture already uses drive {config.DriveIpAddress}.";
                    return false;
                }

                string t_captureId = Guid.NewGuid().ToString();
                t_worker = new CaptureWorker(t_captureId, config, driveClientFactory());
                _captures[t_captureId] = t_worker;
            }

            t_worker.Start();
            captureIdOrError = t_worker.CaptureId;
            return true;
        }

        public bool TryGet(string captureId, out CaptureWorker worker)
        {
            return _captures.TryGetValue(captureId, out worker!);
        }

        /// <summary>
        /// Removes the capture from the Data Layer and signals its worker to stop and close the drive connection.
        /// Does not wait for the cleanup. Returns false if the id does not exist (or is already being deleted).
        /// </summary>
        public bool RequestDelete(string captureId)
        {
            lock (_gate)
            {
                if (!_captures.TryRemove(captureId, out CaptureWorker? t_worker))
                {
                    return false;
                }
                // Added under the lock that StopAndForgetAsync needs to remove it, so the entry can't be left behind.
                _stopping[captureId] = (t_worker, Task.Run(() => StopAndForgetAsync(t_worker)));
                return true;
            }
        }

        /// <summary>Deletes every capture and waits until all workers have stopped (service shutdown).</summary>
        public async Task DeleteAllAsync()
        {
            foreach (string t_captureId in CaptureIds)
            {
                RequestDelete(t_captureId);
            }
            await Task.WhenAll(_stopping.Values.Select(t_s => t_s.stop));
        }

        private async Task StopAndForgetAsync(CaptureWorker worker)
        {
            try
            {
                await worker.StopAsync(workerStopTimeout);
                Console.WriteLine($"Capture {worker.CaptureId} cleaned up.");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Capture {worker.CaptureId}: cleanup failed: {ex}");
            }
            finally
            {
                lock (_gate)
                {
                    _stopping.TryRemove(worker.CaptureId, out _);
                }
            }
        }
    }
}
