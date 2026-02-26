using Amuse.UI.Enums;
using Amuse.UI.Models;
using Microsoft.Extensions.Logging;
using Microsoft.ML.OnnxRuntime;
using OnnxStack.Device;
using OnnxStack.Device.Common;
using OnnxStack.Device.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Threading.Tasks;

namespace Amuse.UI.Services
{
    public class DeviceService : IDeviceService
    {
        private readonly static byte[] _testModel = [8, 10, 18, 0, 58, 73, 10, 18, 10, 1, 120, 10, 1, 107, 18, 1, 118, 18, 1, 105, 34, 4, 84, 111, 112, 75, 18, 1, 116, 90, 9, 10, 1, 120, 18, 4, 10, 2, 8, 1, 90, 15, 10, 1, 107, 18, 10, 10, 8, 8, 7, 18, 4, 10, 2, 8, 1, 98, 9, 10, 1, 118, 18, 4, 10, 2, 8, 1, 98, 9, 10, 1, 105, 18, 4, 10, 2, 8, 7, 66, 2, 16, 21];
        private readonly AmuseSettings _settings;
        private readonly IHardwareService _hardwareService;
        private readonly ILogger<DeviceService> _logger;
        private readonly OrtEnvironment _environment;
        private readonly List<Device> _devices = [];
        private readonly int _amdVendorId = 4098;
        private readonly int[] _invalidVendorIds = [4136];

        private Device _baseDevice;
        private bool _isRyzenAI;
        private bool _isNPUSuperResolutionSupported;
        private bool _isNPUStableDiffusionSupported;

        /// <summary>
        /// Initializes a new instance of the <see cref="DeviceService"/> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        public DeviceService(AmuseSettings settings, IHardwareService hardwareService, ILogger<DeviceService> logger)
        {
            _logger = logger;
            _settings = settings;
            _hardwareService = hardwareService;
            _environment = CreateEnvironment();
            DetectDevices();
            Task.Run(UpdateDevices);
        }

        /// <summary>
        /// Gets the base device (CPU).
        /// </summary>
        public Device BaseDevice => _baseDevice;

        /// <summary>
        /// Gets the devices.
        /// </summary>
        public IReadOnlyList<Device> Devices => _devices;

        /// <summary>
        /// Gets a value indicating whether the CPU is RyzenAI.
        /// </summary>
        /// <value><c>true</c> if the CPU is RyzenAI; otherwise, <c>false</c>.</value>
        public bool IsRyzenAI => _isRyzenAI;

        public bool IsNPUSuperResolutionSupported => _isNPUSuperResolutionSupported;

        public bool IsNPUStableDiffusionSupported => _isNPUStableDiffusionSupported;


        /// <summary>
        /// Gets the hardware profile.
        /// </summary>
        /// <param name="settings">The settings.</param>
        /// <returns></returns>
        public HardwareProfile GetHardwareProfile()
        {
            var deviceCount = Devices.Count;
            var baseDevice = BaseDevice.Name;
            var defaultDevice = Devices.FirstOrDefault(x => x.IsDefault);
            var baseMemoryGB = defaultDevice?.MemoryGB ?? _baseDevice.MemoryGB;
            var baseDeviceSpecificProfile = _settings.HardwareProfiles
                .Where(x => x.Devices is not null && x.Devices.Any(d => baseDevice.StartsWith(d, StringComparison.OrdinalIgnoreCase)))
                .Where(x => x.MinMemory <= baseMemoryGB)
                .OrderByDescending(x => x.MinMemory)
                .FirstOrDefault();

            // RyzenAI
            if (baseDeviceSpecificProfile != null && _isRyzenAI)
                return baseDeviceSpecificProfile;

            // if iGPU and dGPU exist, choose dGPU
            if (deviceCount > 1)
                baseDeviceSpecificProfile = null;

            var amdDeviceSpecificProfile = _settings.HardwareProfiles
               .Where(x => defaultDevice is not null && defaultDevice.IsAMDDevice && x.Name.StartsWith("AMDGPU") && x.MinMemory <= _settings.DefaultExecutionDevice.MemoryGB)
               .OrderByDescending(x => x.MinMemory)
               .FirstOrDefault();

            var defaultDeviceSpecificProfile = _settings.HardwareProfiles
                .Where(x => defaultDevice is not null && x.Devices is not null && x.Devices.Contains(defaultDevice.Name, StringComparer.OrdinalIgnoreCase))
                .OrderByDescending(x => x.MinMemory)
                .FirstOrDefault();

            var deviceSpecificProfile = _settings.HardwareProfiles
               .Where(x => x.Devices is null && x.MinMemory <= _settings.DefaultExecutionDevice.MemoryGB)
               .OrderByDescending(x => x.MinMemory)
               .FirstOrDefault();

            var hardwareProfile = baseDeviceSpecificProfile
                ?? amdDeviceSpecificProfile
                ?? defaultDeviceSpecificProfile
                ?? deviceSpecificProfile
                ?? _settings.HardwareProfiles[0];
            return hardwareProfile;
        }


        private void DetectDevices()
        {
            //LogOrtDevices();
            _logger.LogInformation($"[DetectDevices] - Detecting devices...");
            IsInvalidVendor();
            DetectCPU();
            DetectNPU();
            DetectGPU();
            SetDefaultDevice();
            _logger.LogInformation($"[DetectDevices] - Detecting complete.");
        }


        private void DetectCPU()
        {
            // Get CPU
            _logger.LogInformation($"[DetectDevices] - Detecting CPU...");
            var cpuDevice = GetCPU();
            _baseDevice = cpuDevice;
            _isRyzenAI = IsRyzenAIDevice(_baseDevice.Name);
            _devices.Add(_baseDevice);
            _logger.LogInformation($"[DetectDevices] - CPU: {_baseDevice.Name}, Memory: {_baseDevice.MemoryGB}GB");
            _logger.LogInformation($"[DetectDevices] - Detecting CPU complete.");
        }


        private void DetectNPU()
        {
            _logger.LogInformation($"[DetectDevices] - Detecting NPU...");
            var npuDevice = GetNPU();
            if (npuDevice != null)
            {
                _isNPUSuperResolutionSupported = true;
                if (_isRyzenAI)
                {
                    _isNPUStableDiffusionSupported = IsDriverSupported(npuDevice.DriverVersion, "32.0.203.240");
                }
                _devices.Add(npuDevice);
                _logger.LogInformation($"[DetectDevices] - NPU: {npuDevice.Name}, Memory: {npuDevice.MemoryGB}GB, Driver: {npuDevice.DriverVersion}, SuperResolution: {_isNPUSuperResolutionSupported}, StableDiffusion: {_isNPUStableDiffusionSupported}");
                _logger.LogInformation($"[DetectDevices] - Detecting NPU complete.");
            }
            else
            {
                _logger.LogInformation($"[DetectDevices] - No NPU device found.");
            }

        }


        private void DetectGPU()
        {
            _logger.LogInformation($"[DetectDevices] - Detecting GPU...");
            var gpuDevices = GetGPU();
            if (gpuDevices.Length > 0)
            {
                foreach (var gpuDevice in gpuDevices)
                {
                    if (gpuDevice == null)
                        continue;

                    _devices.Add(gpuDevice);
                    _logger.LogInformation($"[DetectDevices] - GPU: {gpuDevice.Name}, Memory: {gpuDevice.MemoryGB}GB, Driver: {gpuDevice.DriverVersion}, Provider: {gpuDevice.Provider}");
                }
                _logger.LogInformation($"[DetectDevices] - Detecting GPU complete.");
            }
            else
            {
                _logger.LogInformation($"[DetectDevices] - No GPU devices found.");
            }
        }


        private void SetDefaultDevice()
        {
            var defaultDevice = _devices
                 .Where(x => x.DeviceType == DeviceType.GPU)
                 .MaxBy(x => x.Memory);
            if (defaultDevice == null)
                return;

            defaultDevice.IsDefault = true;
            if (!_settings.DefaultDeviceId.HasValue || !_settings.DefaultProvider.HasValue)
            {
                _settings.DefaultDeviceId = defaultDevice.DeviceId;
                _settings.DefaultProvider = defaultDevice.Provider;
                _logger.LogInformation($"[DetectDevices] - Set default device, Name: {defaultDevice.Name}, Id: {defaultDevice.DeviceId}, Provider: {defaultDevice.Provider}");
            }
        }


        private Device GetCPU()
        {
            var cpuDevice = _hardwareService.CPUDevice;
            var cpuName = cpuDevice.Name.Split("w/", StringSplitOptions.TrimEntries).FirstOrDefault();
            return new Device(DeviceType.CPU, ExecutionProvider.CPU, cpuName, 0, cpuDevice.MemoryTotal, 0, cpuDevice.Name, "0.0.0", false);
        }


        private Device[] GetGPU()
        {
            var devices = new List<Device>();
            foreach (var gpuDevice in _hardwareService.GPUDevices)
            {
                LogAdapter(gpuDevice);

                if ((gpuDevice.AdapterInfo.SubSysId == 0 && gpuDevice.AdapterInfo.Revision == 0) || !gpuDevice.AdapterInfo.IsHardware)
                {
                    _logger.LogInformation($"Skipping incompatible, Id: {gpuDevice.Id}, Name: {gpuDevice.Name}");
                    continue;
                }

                var memory = gpuDevice.MemoryTotal;
                var memoryShared = gpuDevice.SharedMemoryTotal;
                var isAMDGPUDevice = gpuDevice.AdapterInfo.VendorId == _amdVendorId;
                if (gpuDevice.AdapterInfo.IsIntegrated)
                {
                    memory += memoryShared;
                    memoryShared = 0;
                }

                foreach (var provider in _settings.SupportedProviders)
                {
                    if (provider == ExecutionProvider.CPU)
                        continue;

                    if (provider == ExecutionProvider.AMDGPU && !isAMDGPUDevice)
                        continue;

                    if (!IsProviderSupported(provider, gpuDevice))
                        continue;

                    devices.Add(new Device(DeviceType.GPU, provider, gpuDevice.Name, gpuDevice.Id, memory, memoryShared, gpuDevice.AdapterId, gpuDevice.DriverVersion, isAMDGPUDevice));
                }
            }
            return devices.ToArray();
        }


        private Device GetNPU()
        {
            var npuDevice = _hardwareService.NPUDevice;
            if (npuDevice != null)
            {
                LogAdapter(npuDevice);
                return new Device(DeviceType.NPU, ExecutionProvider.CPU, npuDevice.Name, 0, npuDevice.MemoryTotal, 0, npuDevice.Name, npuDevice.DriverVersion, true);
            }
            return GetNPULegacy();
        }


        private Device GetNPULegacy()
        {
            try
            {
                var knownNPUDevices = new[] { "AMD NPU Device", "NPU Compute Accelerator Device" };
                using (var searcherDevice = new ManagementObjectSearcher(new ObjectQuery($"SELECT DeviceName, DriverVersion FROM Win32_PnPSignedDriver WHERE DeviceName LIKE '%{knownNPUDevices[0]}%' OR DeviceName LIKE '%{knownNPUDevices[1]}%'")))
                {
                    foreach (var result in searcherDevice.Get())
                    {
                        var deviceName = result["DeviceName"]?.ToString() ?? string.Empty;
                        var driverVersion = result["DriverVersion"]?.ToString() ?? string.Empty;
                        return new Device(DeviceType.NPU, ExecutionProvider.CPU, deviceName, 0, 0, 0, deviceName, driverVersion, true);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[DetectNPULegacy] - Failed to detect NPU");
            }
            return default;
        }


        private bool IsRyzenAIDevice(string deviceName)
        {
            return deviceName.StartsWith("AMD Ryzen AI", StringComparison.OrdinalIgnoreCase)
                || deviceName.StartsWith("AMD Eng Sample: 100-000000994-37_Y", StringComparison.OrdinalIgnoreCase);
        }


        private bool IsDriverSupported(string currentVersion, string minimumVersion)
        {
            if (!long.TryParse(currentVersion.Replace(".", ""), out long currentVersionNum))
                return false;
            if (!long.TryParse(minimumVersion.Replace(".", ""), out long minimumVersionNum))
                return false;
            return currentVersionNum >= minimumVersionNum;
        }


        private bool IsProviderSupported(ExecutionProvider provider, GPUDevice device)
        {
            try
            {
                using (var session = new SessionOptions())
                {
                    session.LogSeverityLevel = OrtLoggingLevel.ORT_LOGGING_LEVEL_ERROR;
                    if (provider == ExecutionProvider.DirectML)
                        session.AppendExecutionProvider_DML(device.Id);
                    else if (provider == ExecutionProvider.AMDGPU)
                        session.AppendExecutionProvider_MIGraphX(new OrtMIGraphXProviderOptions { DeviceId = device.Id });

                    using (var inferenceSession = new InferenceSession(_testModel, session))
                    {
                        _logger.LogInformation("Sucessfully initialized device, Name: {Name}, Id: {Id}, Provider: {provider}", device.Name, device.Id, provider);
                        return true;
                    }
                }
            }
            catch (Exception)
            {
                _logger.LogWarning("Failed to initialize device, Name: {Name}, Id: {Id}, Provider: {provider}", device.Name, device.Id, provider);
                return false;
            }
        }


        private void LogAdapter(GPUDevice device)
        {
            _logger.LogDebug("---------------------------GPU---------------------------");
            _logger.LogDebug("Id: {Id}", device.Id);
            _logger.LogDebug("Name: {Name}", device.Name);
            _logger.LogDebug("DriverVersion: {DriverVersion}", device.DriverVersion);
            _logger.LogDebug("AdapterId: {AdapterId}", device.AdapterId);
            LogAdapter(device.AdapterInfo);
            _logger.LogDebug($"IsAMDGPU: {device.AdapterInfo.VendorId == _amdVendorId}");
            _logger.LogDebug("---------------------------------------------------------");
        }


        private void LogAdapter(NPUDevice device)
        {
            _logger.LogDebug("---------------------------NPU---------------------------");
            _logger.LogDebug("Id: {Id}", device.Id);
            _logger.LogDebug("Name: {Name}", device.Name);
            _logger.LogDebug("DriverVersion: {DriverVersion}", device.DriverVersion);
            _logger.LogDebug("AdapterId: {AdapterId}", device.AdapterId);
            LogAdapter(device.AdapterInfo);
            _logger.LogDebug("---------------------------------------------------------");
        }

        private void LogAdapter(AdapterInfo adapter)
        {
            _logger.LogDebug("DedicatedVideoMemory: {Size:F0}MB", (adapter.DedicatedVideoMemory / 1024f / 1024f));
            _logger.LogDebug("SharedSystemMemory: {Size:F0}MB", (adapter.SharedSystemMemory / 1024f / 1024f));
            _logger.LogDebug("DedicatedSystemMemory: {Size:F0}MB", (adapter.DedicatedSystemMemory / 1024f / 1024f));
            _logger.LogDebug("DeviceId: {DeviceId}", adapter.DeviceId);
            _logger.LogDebug("VendorId: {VendorId}", adapter.VendorId);
            _logger.LogDebug("SubSysId: {SubSysId}", adapter.SubSysId);
            _logger.LogDebug("Revision: {Revision}", adapter.Revision);
            _logger.LogDebug("IsHardware: {IsHardware}", adapter.IsHardware);
            _logger.LogDebug("IsIntegrated: {IsIntegrated}", adapter.IsIntegrated);
            _logger.LogDebug("IsDetachable: {IsDetachable}", adapter.IsDetachable);
            _logger.LogDebug("AdapterLuid: {HighPart}-{LowPart}", adapter.AdapterLuid.HighPart, adapter.AdapterLuid.LowPart);
            _logger.LogDebug("IsLegacy: {IsLegacy}", adapter.IsLegacy);
        }


        private async Task UpdateDevices()
        {
            var process = Process.GetCurrentProcess();
            while (!process.HasExited)
            {
                try
                {
                    process.Refresh(); // Update stats
                    var cpuStatus = _hardwareService.CPUStatus;
                    var cpuUpdate = Devices.FirstOrDefault(x => x.DeviceType == DeviceType.CPU);
                    if (cpuUpdate != null)
                    {
                        var memoryUsage = (cpuStatus.MemoryTotal - cpuStatus.MemoryAvailable);
                        var processMemoryUsage = process.WorkingSet64 / 1024f / 1024f;
                        cpuUpdate.Usage = cpuStatus.Usage;
                        cpuUpdate.MemoryUsage = memoryUsage / 1024f;
                        cpuUpdate.ProcessMemoryUsage = processMemoryUsage / 1024f;
                    }

                    var npuStatus = _hardwareService.NPUStatus;
                    var npuUpdate = Devices.FirstOrDefault(x => x.DeviceType == DeviceType.NPU);
                    if (npuUpdate != null)
                    {
                        npuUpdate.Usage = npuStatus.Usage;
                        npuUpdate.MemoryUsage = npuStatus.MemoryUsage / 1024f;
                    }

                    foreach (var gpuStatus in _hardwareService.GPUStatus)
                    {
                        var gpuUpdate = Devices.FirstOrDefault(x => x.DeviceId == gpuStatus.Id && x.DeviceType == DeviceType.GPU);
                        if (gpuUpdate == null)
                            continue;

                        gpuUpdate.Usage = GetGPUUsage(gpuStatus);
                        gpuUpdate.MemoryUsage = gpuStatus.MemoryUsage / 1024f;
                        gpuUpdate.ProcessMemoryUsage = gpuStatus.ProcessMemoryTotal / 1024f;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occured updating device statistics");
                }
                await Task.Delay(250); // Non-blocking delay
            }
        }


        /// <summary>
        /// Gets the GPU usage.
        /// </summary>
        /// <param name="status">The status.</param>
        /// <returns>System.Double.</returns>
        private int GetGPUUsage(GPUStatus status)
        {
            var compute = Math.Max(status.UsageCompute, status.UsageCompute1);
            var graphics = Math.Max(status.UsageGraphics, status.UsageGraphics1);
            var usage = Math.Max(compute, graphics);
            return Math.Min(usage, 100);
        }


        private OrtEnvironment CreateEnvironment()
        {
            var options = new EnvironmentCreationOptions
            {
                logId = "Amuse",
                logLevel = GetLogSeverityLevel(),
                loggingFunction = (param, severity, category, logId, codeLocation, message) =>
                {
                    logId = string.IsNullOrEmpty(logId) ? "OnnxRuntime" : logId;
                    var level = GetLogLevel(severity);
                    _logger.Log(level, "[{logId}] [{codeLocation}] [{message}]", logId, codeLocation, message);
                }
            };
            return new OrtEnvironment(options, OrtEnv.CreateInstanceWithOptions(ref options));
        }


        private LogLevel GetLogLevel(OrtLoggingLevel ortLogging)
        {
            return ortLogging switch
            {
                OrtLoggingLevel.ORT_LOGGING_LEVEL_VERBOSE => LogLevel.Trace,
                OrtLoggingLevel.ORT_LOGGING_LEVEL_INFO => LogLevel.Information,
                OrtLoggingLevel.ORT_LOGGING_LEVEL_WARNING => LogLevel.Warning,
                OrtLoggingLevel.ORT_LOGGING_LEVEL_ERROR => LogLevel.Error,
                OrtLoggingLevel.ORT_LOGGING_LEVEL_FATAL => LogLevel.Critical,
                _ => LogLevel.Information
            };
        }


        private static OrtLoggingLevel GetLogSeverityLevel()
        {
#if DEBUG || DEBUG_DIRECT
            return OrtLoggingLevel.ORT_LOGGING_LEVEL_VERBOSE;
#else
            return OrtLoggingLevel.ORT_LOGGING_LEVEL_ERROR;
#endif
        }


        private void LogOrtDevices()
        {

            try
            {
                _logger.LogDebug("----------ORT Providers----------");
                var providers = _environment.Environment.GetAvailableProviders();
                foreach (var provider in providers)
                {
                    _logger.LogDebug($"Provider: {provider}");
                }
                _logger.LogDebug("---------------------------------");

                _logger.LogDebug("-----------ORT Devices-----------");
                var devices = _environment.Environment.GetEpDevices();
                foreach (var device in devices)
                {
                    _logger.LogDebug($"{device.EpName} - {device.EpVendor}");
                    _logger.LogDebug($"Type: {device.HardwareDevice.Type}");
                    _logger.LogDebug($"Vendor: {device.HardwareDevice.Vendor}");
                    _logger.LogDebug($"VendorId: {device.HardwareDevice.VendorId}");
                    _logger.LogDebug($"DeviceId: {device.HardwareDevice.DeviceId}");
                    foreach (var item in device.HardwareDevice.Metadata.Entries)
                    {
                        _logger.LogDebug($"{item.Key}: {item.Value}");
                    }
                    _logger.LogDebug("---------------------------------");
                }
            }
            catch (Exception)
            {

                throw;
            }
        }


        public bool IsInvalidVendor()
        {
            try
            {
                foreach (var invalidVendorId in _invalidVendorIds)
                {
                    var hexCode = invalidVendorId.ToString("X");
                    var query = new SelectQuery($"SELECT DeviceID FROM Win32_PnPEntity WHERE DeviceID LIKE 'PCI\\\\VEN_{hexCode}%'");
                    using (var searcher = new ManagementObjectSearcher(query))
                    {
                        using (var results = searcher.Get())
                        {
                            if (results.Count > 0)
                                throw new Exception("Amuse is not compatible with this system.");
                        }
                    }
                }
            }
            catch { }
            return false;
        }
    }

    public record OrtEnvironment(EnvironmentCreationOptions Options, OrtEnv Environment);
}
