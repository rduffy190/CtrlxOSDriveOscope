using System.Diagnostics;
using System.Globalization;
using DriveCapture;
using Google.FlatBuffers;
using scope_config;

namespace DriveOscope
{
    internal enum CaptureState
    {
        Arming,
        Armed,
        Recording,
        /// <summary>Transient: samples read, result being stored in memory.</summary>
        Saved,
        Done,
        Failed,
    }

    /// <summary>
    /// One worker per capture. Owns the drive connection and is the only code that changes the capture state;
    /// the Data Layer handlers send the trigger value and read the published state.
    /// </summary>
    internal sealed class CaptureWorker
    {
        private const int _statePollIntervalMs = 200;

        private readonly ScopeConfigT _config;
        private readonly IDriveClient _drive;
        private readonly CancellationTokenSource _cts = new();
        private readonly SemaphoreSlim _wakeUp = new(0);
        private readonly object _gate = new();

        private CaptureState _state = CaptureState.Arming;
        private bool _triggerValue;
        private string _errorMessage = "";
        private byte[]? _captureBytes;
        private Task _task = Task.CompletedTask;

        public CaptureWorker(string captureId, ScopeConfigT config, IDriveClient drive)
        {
            CaptureId = captureId;
            _config = config;
            _drive = drive;
        }

        public string CaptureId { get; }

        public string DriveIpAddress => _config.DriveIpAddress;

        public CaptureState State
        {
            get { lock (_gate) { return _state; } }
        }

        public bool Ready => State == CaptureState.Armed;

        public bool Done => State == CaptureState.Done;

        public bool TriggerValue
        {
            get { lock (_gate) { return _triggerValue; } }
        }

        public string ErrorMessage
        {
            get { lock (_gate) { return _errorMessage; } }
        }

        /// <summary>The finished ScopeCapture as FlatBuffers bytes, or null until the capture is done.</summary>
        public byte[]? GetCapture()
        {
            lock (_gate)
            {
                return _state == CaptureState.Done ? _captureBytes : null;
            }
        }

        public void Start()
        {
            _task = Task.Factory.StartNew(RunAsync, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default).Unwrap();
        }

        /// <summary>
        /// Hands the trigger command to the worker. Rejected while the drive is still being configured
        /// and after a failure, because the scope settings are not (or could not be) written then.
        /// </summary>
        public bool TrySetTrigger(bool value)
        {
            lock (_gate)
            {
                if (_state == CaptureState.Arming || _state == CaptureState.Failed)
                {
                    return false;
                }
                _triggerValue = value;
            }
            _wakeUp.Release();
            return true;
        }

        /// <summary>Stops the worker and closes the drive connection.</summary>
        public async Task StopAsync(TimeSpan timeout)
        {
            _cts.Cancel();
            _wakeUp.Release();

            Task t_finished = await Task.WhenAny(_task, Task.Delay(timeout));
            if (t_finished != _task)
            {
                // The worker is blocked inside a drive call; closing the connection should unblock it.
                Console.Error.WriteLine($"Capture {CaptureId}: worker did not stop within {timeout.TotalMilliseconds:F0} ms, closing drive connection.");
                _drive.Dispose();
                await Task.WhenAny(_task, Task.Delay(timeout));
            }
        }

        private async Task RunAsync()
        {
            CancellationToken t_token = _cts.Token;
            try
            {
                Log($"connecting to drive {_config.DriveIpAddress}");
                _drive.Connect(_config.DriveIpAddress);
                t_token.ThrowIfCancellationRequested();
                Log("writing scope settings");
                DriveSettingsReadback t_readback = _drive.Configure(_config);
                Log($"scope configured (P-0-0032={t_readback.P0032}, P-0-0033={t_readback.P0033})");
                t_token.ThrowIfCancellationRequested();
                _drive.Arm();
                SetState(CaptureState.Armed);
                Log("armed");

                while (true)
                {
                    await WaitForTriggerValueAsync(true, t_token);
                    SetState(CaptureState.Recording);

                    await RecordAsync(t_readback, t_token);

                    // Trigger already written false while recording: straight back to armed.
                    if (TriggerValue)
                    {
                        SetState(CaptureState.Done);
                        await WaitForTriggerValueAsync(false, t_token);
                    }
                    Log("re-arming");
                    _drive.Arm();
                    SetState(CaptureState.Armed);
                    Log("armed");
                }
            }
            catch (OperationCanceledException)
            {
                // DELETE requested; nothing to report.
            }
            catch (Exception ex)
            {
                if (!t_token.IsCancellationRequested)
                {
                    Console.Error.WriteLine($"Capture {CaptureId} failed: {ex}");
                    Fail(ex.Message);
                }
            }
            finally
            {
                _drive.Dispose();
            }
        }

        private async Task RecordAsync(DriveSettingsReadback readback, CancellationToken token)
        {
            Log("raising trigger");
            _drive.RaiseTrigger();
            Log("trigger raised, waiting for the scope to finish");
            DateTimeOffset t_capturedAt = DateTimeOffset.Now;
            Stopwatch t_timer = Stopwatch.StartNew();
            DriveScopeState? t_lastDriveState = null;

            // measurement_timeout_ms is measured from the trigger.
            while (true)
            {
                token.ThrowIfCancellationRequested();
                DriveScopeState t_driveState = _drive.GetState();
                if (t_driveState != t_lastDriveState)
                {
                    Log($"drive scope state: {t_driveState}");
                    t_lastDriveState = t_driveState;
                }
                if (t_driveState == DriveScopeState.Ready)
                {
                    // The EAL doc sample pauses briefly after Ready before reading data.
                    await Task.Delay(_statePollIntervalMs, token);
                    break;
                }
                if (t_driveState == DriveScopeState.Error)
                {
                    throw new InvalidOperationException("Drive oscilloscope reported an error.");
                }
                if (t_timer.ElapsedMilliseconds > _config.MeasurementTimeoutMs)
                {
                    throw new TimeoutException($"Capture did not complete within {_config.MeasurementTimeoutMs} ms of the trigger.");
                }
                await Task.Delay(_statePollIntervalMs, token);
            }

            IReadOnlyList<float[]> t_channels = _drive.ReadChannels(_config.Signals.Count);
            Log($"read {t_channels.Count} channel(s), {(t_channels.Count == 0 ? 0 : t_channels.Max(t_c => t_c.Length))} samples");
            ScopeCaptureT t_capture = BuildCapture(t_capturedAt, readback, t_channels);

            SetState(CaptureState.Saved);
            byte[] t_bytes = Serialize(t_capture);
            lock (_gate)
            {
                _captureBytes = t_bytes;
            }
        }

        private ScopeCaptureT BuildCapture(DateTimeOffset capturedAt, DriveSettingsReadback readback, IReadOnlyList<float[]> channels)
        {
            int t_sampleCount = channels.Count == 0 ? 0 : channels.Max(t_c => t_c.Length);
            // The drive returns only the samples it recorded: a trigger that comes before the pre-trigger
            // window has filled yields fewer than memory_size samples. P-0-0033 holds the samples after the
            // trigger, so everything before them is pre-trigger. (Not documented in the EAL docs; measured on
            // a real drive: 3315 samples when triggered right after arming, 4096 when triggered later, P-0-0033 = 3276.)
            uint t_preTriggerSamples = (uint)Math.Max(0L, (long)t_sampleCount - readback.P0033);

            ScopeCaptureT t_capture = new()
            {
                CapturedAt = capturedAt.ToString("o", CultureInfo.InvariantCulture),
                DriveIpAddress = _config.DriveIpAddress,
                TimeResolutionUs = _config.TimeResolutionUs,
                MemorySize = _config.MemorySize,
                PreTriggerPercent = _config.PreTriggerPercent,
                PreTriggerSamples = t_preTriggerSamples,
                DriveP0032 = readback.P0032,
                DriveP0033 = readback.P0033,
                TimeUs = new List<int>(t_sampleCount),
                Channels = new List<ChannelT>(channels.Count),
            };

            // Time relative to the trigger; negative = pre-trigger.
            for (int t_i = 0; t_i < t_sampleCount; t_i++)
            {
                t_capture.TimeUs.Add(checked((int)(((long)t_i - t_preTriggerSamples) * _config.TimeResolutionUs)));
            }
            for (int t_i = 0; t_i < channels.Count; t_i++)
            {
                t_capture.Channels.Add(new ChannelT
                {
                    Index = (byte)(t_i + 1),
                    Signal = _config.Signals[t_i],
                    Values = [.. channels[t_i]],
                });
            }
            return t_capture;
        }

        private static byte[] Serialize(ScopeCaptureT capture)
        {
            FlatBufferBuilder t_builder = new(1024);
            t_builder.Finish(ScopeCapture.Pack(t_builder, capture).Value);
            return t_builder.SizedByteArray();
        }

        private async Task WaitForTriggerValueAsync(bool wanted, CancellationToken token)
        {
            while (TriggerValue != wanted)
            {
                await _wakeUp.WaitAsync(token);
            }
            token.ThrowIfCancellationRequested();
        }

        private void Log(string message)
        {
            Console.WriteLine($"Capture {CaptureId}: {message}");
        }

        private void SetState(CaptureState state)
        {
            lock (_gate)
            {
                _state = state;
            }
        }

        private void Fail(string message)
        {
            lock (_gate)
            {
                _state = CaptureState.Failed;
                _errorMessage = message;
            }
        }
    }
}
