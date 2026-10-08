using EAL.EALConnection;
using EAL.Interfaces;
using EAL.Interfaces.Oscilloscope;
using scope_config;

namespace DriveOscope
{
    /// <summary>
    /// Easy Automation Library implementation of <see cref="IDriveClient"/>.
    /// Scope flow follows libDocs/eal/interface_e_a_l_1_1_interfaces_1_1_oscilloscope_1_1_i_oscilloscope.html
    /// and libExamples/eal/OscopeTest (synchronous connection).
    /// </summary>
    internal sealed class EalDriveClient : IDriveClient
    {
        private readonly IEALConnection _connection = new EALConnection();
        private int _disposed;
        private bool _connected;

        private IOscilloscope Scope => _connection.Oscilloscope;

        public void Connect(string ipAddress)
        {
            _connection.Connect(ipAddress);
            _connected = true;
        }

        public DriveSettingsReadback Configure(ScopeConfigT config)
        {
            // Scope settings are write protected while a recording is active (e.g. left running by an earlier session).
            if (Scope.IsStarted)
            {
                Scope.Stop();
            }

            Scope.TimeResolution = checked((int)config.TimeResolutionUs);
            Scope.MemorySize = checked((int)config.MemorySize);
            for (int t_i = 0; t_i < config.Signals.Count; t_i++)
            {
                Scope.Channels[t_i].Signal = config.Signals[t_i];
            }
            Scope.Trigger.Type = TriggerType.ManualTrigger;
            // Set after MemorySize: per the EAL docs PreTrigger writes P-0-0032 and P-0-0033.
            Scope.Trigger.PreTrigger = config.PreTriggerPercent;

            uint t_p0032 = ReadUInt("P-0-0032.0.0");
            uint t_p0033 = ReadUInt("P-0-0033.0.0");
            return new DriveSettingsReadback(t_p0032, t_p0033);
        }

        public void Arm()
        {
            if (Scope.IsStarted)
            {
                Scope.Stop();
            }
            Scope.Start();
        }

        public void RaiseTrigger()
        {
            Scope.RaiseTrigger();
        }

        public DriveScopeState GetState()
        {
            return Scope.State switch
            {
                State.Ready => DriveScopeState.Ready,
                State.Error => DriveScopeState.Error,
                State.Triggered => DriveScopeState.Recording,
                _ => DriveScopeState.Waiting,
            };
        }

        public IReadOnlyList<float[]> ReadChannels(int channelCount)
        {
            List<float[]> t_channels = new(channelCount);
            for (int t_i = 0; t_i < channelCount; t_i++)
            {
                double[] t_values = Scope.Channels[t_i].GetDataArrayDouble();
                t_channels.Add(Array.ConvertAll(t_values, t_v => (float)t_v));
            }
            return t_channels;
        }

        public void Dispose()
        {
            // May be called by the worker and by a DELETE at the same time; only the first call acts.
            if (Interlocked.Exchange(ref _disposed, 1) != 0)
            {
                return;
            }

            try
            {
                if (_connected)
                {
                    _connection.Disconnect();
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Drive disconnect failed: {ex.Message}");
            }
            finally
            {
                _connection.Dispose();
            }
        }

        private uint ReadUInt(string parameter)
        {
            object? t_raw = _connection.Parameter.ReadData(parameter);
            return Convert.ToUInt32(t_raw);
        }
    }
}
