using System.Text.Json;
using comm.datalayer;
using Datalayer;
using Google.FlatBuffers;
using scope_config;

namespace DriveOscope
{
    /// <summary>
    /// Serves the Data Layer endpoints under {root}/captures. Registered on the collection address and on
    /// the wildcard {root}/captures/** so per-capture nodes exist as soon as the capture is created,
    /// without registering nodes at runtime (same approach as libExamples/ctrlx/datalayer.provider.virtual).
    ///
    ///   {root}/captures                   create (ScopeConfig) / browse
    ///   {root}/captures/{id}              remove / browse
    ///   {root}/captures/{id}/ready        read bool
    ///   {root}/captures/{id}/trigger      read / write bool
    ///   {root}/captures/{id}/done         read bool
    ///   {root}/captures/{id}/capture      read ScopeCapture
    ///   {root}/captures/{id}/state        read string (lifecycle state)
    ///   {root}/captures/{id}/error        read string (failure reason)
    ///
    /// Handlers only hand commands to the worker and read its state; they never change it.
    /// </summary>
    internal sealed class CaptureNodeHandler(CaptureManager manager, string capturesAddress, string configTypeAddress, string captureTypeAddress) : IProviderNodeHandler
    {
        private const string _boolType = "types/datalayer/bool8";
        private const string _stringType = "types/datalayer/string";

        private static readonly string[] _leafNames = ["ready", "trigger", "done", "capture", "state", "error"];

        public void OnCreate(string address, IVariant args, IProviderNodeResult result)
        {
            if (address != capturesAddress)
            {
                result.SetResult(DLR_RESULT.DL_UNSUPPORTED);
                return;
            }

            ScopeConfigT? t_config = ParseConfig(args);
            if (t_config == null)
            {
                Console.Error.WriteLine("Create rejected: body is not a ScopeConfig flatbuffer.");
                result.SetResult(DLR_RESULT.DL_FAILED);
                return;
            }

            if (!manager.TryCreate(t_config, out string t_captureIdOrError))
            {
                Console.Error.WriteLine($"Create rejected: {t_captureIdOrError}");
                result.SetResult(DLR_RESULT.DL_FAILED);
                return;
            }

            // The SDK has no Location header; the reply value is the new capture's address.
            string t_location = $"{capturesAddress}/{t_captureIdOrError}";
            Console.WriteLine($"Created capture {t_location} for drive {t_config.DriveIpAddress}");
            result.SetResult(DLR_RESULT.DL_OK, new Variant(t_location));
        }

        public void OnRemove(string address, IProviderNodeResult result)
        {
            (string? t_captureId, string? t_leaf) = Split(address);
            // Fire and forget: confirm the request now; the worker stops and closes the drive in the background.
            if (t_captureId == null || t_leaf != null || !manager.RequestDelete(t_captureId))
            {
                Console.Error.WriteLine($"Delete rejected: no capture at '{address}'.");
                result.SetResult(DLR_RESULT.DL_INVALID_ADDRESS);
                return;
            }
            Console.WriteLine($"Delete requested for capture {t_captureId}");
            result.SetResult(DLR_RESULT.DL_OK);
        }

        public void OnBrowse(string address, IProviderNodeResult result)
        {
            (string? t_captureId, string? t_leaf) = Split(address);
            if (t_captureId == null)
            {
                result.SetResult(DLR_RESULT.DL_OK, new Variant(manager.CaptureIds.ToArray()));
                return;
            }
            if (!manager.TryGet(t_captureId, out _))
            {
                result.SetResult(DLR_RESULT.DL_INVALID_ADDRESS);
                return;
            }
            result.SetResult(DLR_RESULT.DL_OK, new Variant(t_leaf == null ? _leafNames : []));
        }

        public void OnRead(string address, IVariant args, IProviderNodeResult result)
        {
            (string? t_captureId, string? t_leaf) = Split(address);
            if (t_captureId == null || t_leaf == null || !manager.TryGet(t_captureId, out CaptureWorker t_worker))
            {
                result.SetResult(DLR_RESULT.DL_INVALID_ADDRESS);
                return;
            }

            switch (t_leaf)
            {
                case "ready":
                    result.SetResult(DLR_RESULT.DL_OK, new Variant(t_worker.Ready));
                    break;
                case "done":
                    result.SetResult(DLR_RESULT.DL_OK, new Variant(t_worker.Done));
                    break;
                case "trigger":
                    result.SetResult(DLR_RESULT.DL_OK, new Variant(t_worker.TriggerValue));
                    break;
                case "state":
                    result.SetResult(DLR_RESULT.DL_OK, new Variant(t_worker.State.ToString().ToLowerInvariant()));
                    break;
                case "error":
                    result.SetResult(DLR_RESULT.DL_OK, new Variant(t_worker.ErrorMessage));
                    break;
                case "capture":
                    byte[]? t_capture = t_worker.GetCapture();
                    if (t_capture == null)
                    {
                        result.SetResult(DLR_RESULT.DL_FAILED);
                        return;
                    }
                    result.SetResult(DLR_RESULT.DL_OK, new Variant(new ByteBuffer(t_capture)));
                    break;
                default:
                    result.SetResult(DLR_RESULT.DL_INVALID_ADDRESS);
                    break;
            }
        }

        public void OnWrite(string address, IVariant writeValue, IProviderNodeResult result)
        {
            (string? t_captureId, string? t_leaf) = Split(address);
            if (t_captureId == null || t_leaf != "trigger" || !manager.TryGet(t_captureId, out CaptureWorker t_worker))
            {
                result.SetResult(DLR_RESULT.DL_UNSUPPORTED);
                return;
            }

            bool? t_trigger = ParseTrigger(writeValue);
            if (t_trigger == null)
            {
                result.SetResult(DLR_RESULT.DL_INVALID_VALUE);
                return;
            }

            if (!t_worker.TrySetTrigger(t_trigger.Value))
            {
                // Scope settings not written yet (arming) or failed to write.
                result.SetResult(DLR_RESULT.DL_FAILED);
                return;
            }
            result.SetResult(DLR_RESULT.DL_OK, new Variant(t_trigger.Value));
        }

        public void OnMetadata(string address, IProviderNodeResult result)
        {
            (string? t_captureId, string? t_leaf) = Split(address);
            Variant? t_metadata = (t_captureId, t_leaf) switch
            {
                (null, _) => new MetadataBuilder(AllowedOperationFlags.Create | AllowedOperationFlags.Browse, "Scope captures. Create with a ScopeConfig.")
                    .SetNodeClass(NodeClass.Folder)
                    .AddReference(ReferenceType.CreateType, configTypeAddress)
                    .Build(),
                (_, null) => new MetadataBuilder(AllowedOperationFlags.Delete | AllowedOperationFlags.Browse, "One scope capture. Delete to stop it and free the drive.")
                    .SetNodeClass(NodeClass.Folder)
                    .Build(),
                (_, "ready") => ReadOnly("True when the drive scope is armed and waiting for the trigger.", _boolType),
                (_, "done") => ReadOnly("True when the last capture has finished.", _boolType),
                (_, "state") => ReadOnly("Lifecycle state: arming, armed, recording, saved, done, failed.", _stringType),
                (_, "error") => ReadOnly("Failure reason, empty unless state is failed.", _stringType),
                (_, "capture") => ReadOnly("The finished ScopeCapture. Fails until done.", captureTypeAddress),
                (_, "trigger") => new MetadataBuilder(AllowedOperationFlags.Read | AllowedOperationFlags.Write, "Write true to fire the trigger, false to re-arm.")
                    .SetNodeClass(NodeClass.Variable)
                    .AddReference(ReferenceType.ReadType, _boolType)
                    .AddReference(ReferenceType.WriteType, _boolType)
                    .Build(),
                _ => null,
            };

            if (t_metadata == null || (t_captureId != null && !manager.TryGet(t_captureId, out _)))
            {
                result.SetResult(DLR_RESULT.DL_INVALID_ADDRESS);
                return;
            }
            result.SetResult(DLR_RESULT.DL_OK, t_metadata);
        }

        private static Variant ReadOnly(string description, string typeAddress)
        {
            return new MetadataBuilder(AllowedOperationFlags.Read, description)
                .SetNodeClass(NodeClass.Variable)
                .AddReference(ReferenceType.ReadType, typeAddress)
                .Build();
        }

        /// <summary>Splits an address below {root}/captures into (captureId, leaf); both null for the collection itself.</summary>
        private (string? captureId, string? leaf) Split(string address)
        {
            if (address == capturesAddress || !address.StartsWith(capturesAddress + "/", StringComparison.Ordinal))
            {
                return (null, null);
            }

            string[] t_parts = address[(capturesAddress.Length + 1)..].Split('/');
            return t_parts.Length switch
            {
                1 => (t_parts[0], null),
                2 => (t_parts[0], t_parts[1]),
                _ => (t_parts[0], "\0invalid"),
            };
        }

        private static ScopeConfigT? ParseConfig(IVariant args)
        {
            try
            {
                if (args == null || args.DataType != DLR_VARIANT_TYPE.DLR_VARIANT_TYPE_FLATBUFFERS)
                {
                    return null;
                }

                ByteBuffer t_buffer = args.ToFlatbuffers();
                // Not the generated ScopeConfig.VerifyScopeConfig: it passes "" as file identifier, which
                // Google.FlatBuffers rejects (the schema declares no file_identifier).
                if (!new Verifier(t_buffer).VerifyBuffer(null, false, ScopeConfigVerify.Verify))
                {
                    return null;
                }
                return ScopeConfig.GetRootAsScopeConfig(t_buffer).UnPack();
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Could not read ScopeConfig: {ex.Message}");
                return null;
            }
        }

        /// <summary>Accepts a bool, or the design doc's JSON body { "trigger": true } sent as a string.</summary>
        private static bool? ParseTrigger(IVariant value)
        {
            if (value == null)
            {
                return null;
            }
            if (value.DataType == DLR_VARIANT_TYPE.DLR_VARIANT_TYPE_BOOL8)
            {
                return value.ToBool();
            }
            if (value.DataType != DLR_VARIANT_TYPE.DLR_VARIANT_TYPE_STRING)
            {
                return null;
            }

            try
            {
                using JsonDocument t_json = JsonDocument.Parse(value.ToString());
                if (t_json.RootElement.ValueKind == JsonValueKind.Object
                    && t_json.RootElement.TryGetProperty("trigger", out JsonElement t_trigger)
                    && (t_trigger.ValueKind == JsonValueKind.True || t_trigger.ValueKind == JsonValueKind.False))
                {
                    return t_trigger.GetBoolean();
                }
            }
            catch (JsonException)
            {
            }
            return null;
        }
    }
}
