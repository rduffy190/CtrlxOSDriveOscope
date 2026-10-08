using scope_config;

namespace DriveOscope
{
    internal enum DriveScopeState
    {
        /// <summary>Armed or not yet triggered.</summary>
        Waiting,
        Recording,
        /// <summary>Measurement finished; scope memory can be read.</summary>
        Ready,
        Error,
    }

    /// <summary>Drive parameters read back after the scope settings were written.</summary>
    internal sealed record DriveSettingsReadback(uint P0032, uint P0033);

    /// <summary>
    /// Everything the worker needs from a drive. Kept behind an interface so the worker can be
    /// tested with a fake drive. Calls block; Dispose must be safe to call from another thread
    /// to unblock a worker that is waiting on the drive.
    /// </summary>
    internal interface IDriveClient : IDisposable
    {
        void Connect(string ipAddress);

        /// <summary>Writes the scope settings (channels, resolution, memory, pre-trigger, manual trigger).</summary>
        DriveSettingsReadback Configure(ScopeConfigT config);

        /// <summary>Starts recording so the scope waits for the trigger. Safe to call again to re-arm.</summary>
        void Arm();

        void RaiseTrigger();

        DriveScopeState GetState();

        /// <summary>Reads the recorded samples, one array per configured channel.</summary>
        IReadOnlyList<float[]> ReadChannels(int channelCount);
    }
}
