using System;
using System.IO;
using System.Linq;
using System.Management;
using System.Text.RegularExpressions;
using Microsoft.Win32;
using Winside.Models;

namespace Winside.Services
{
    public class SystemAuditService
    {
        public static HardwareAuditResult PerformAudit()
        {
            var result = new HardwareAuditResult();

            // 1. CPU Name & Feature Heuristic
            try
            {
                using var searcher = new ManagementObjectSearcher("SELECT Name FROM Win32_Processor");
                foreach (ManagementObject obj in searcher.Get())
                {
                    string? name = obj["Name"]?.ToString()?.Trim();
                    if (!string.IsNullOrWhiteSpace(name))
                    {
                        result.CpuName = name;
                        InferCpuFeatures(name, result);
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                LoggerService.Instance.LogWarning($"WMI CPU detection fallback: {ex.Message}");
                string regCpu = Registry.GetValue(@"HKEY_LOCAL_MACHINE\HARDWARE\DESCRIPTION\System\CentralProcessor\0", "ProcessorNameString", string.Empty)?.ToString() ?? "Unknown CPU";
                result.CpuName = regCpu.Trim();
                InferCpuFeatures(result.CpuName, result);
            }

            // 2. TPM 2.0 Check
            try
            {
                using var tpmSearcher = new ManagementObjectSearcher(@"root\CIMV2\Security\MicrosoftTpm", "SELECT IsActivated_InitialValue, SpecVersion FROM Win32_Tpm");
                var tpmItems = tpmSearcher.Get();
                if (tpmItems.Count > 0)
                {
                    result.TpmPresent = true;
                    foreach (ManagementObject tpm in tpmItems)
                    {
                        string? spec = tpm["SpecVersion"]?.ToString();
                        result.TpmVersion = !string.IsNullOrWhiteSpace(spec) ? $"TPM {spec}" : "TPM Present";
                        break;
                    }
                }
                else
                {
                    result.TpmPresent = false;
                    result.TpmVersion = "Not Detected";
                }
            }
            catch
            {
                result.TpmPresent = false;
                result.TpmVersion = "Not Detected / Disabled";
            }

            // 3. Secure Boot Check
            try
            {
                object? secBootVal = Registry.GetValue(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\SecureBoot\State", "UEFISecureBootEnabled", null);
                if (secBootVal is int intVal)
                {
                    result.SecureBootEnabled = (intVal == 1);
                }
                else
                {
                    result.SecureBootEnabled = null;
                }
            }
            catch
            {
                result.SecureBootEnabled = null;
            }

            // 4. Firmware Type (UEFI vs BIOS)
            try
            {
                object? fwVal = Registry.GetValue(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control", "PEFirmwareType", null);
                if (fwVal is int fwInt)
                {
                    result.FirmwareType = fwInt switch
                    {
                        2 => "UEFI",
                        1 => "Legacy BIOS",
                        _ => "Unknown"
                    };
                }
                else
                {
                    result.FirmwareType = "UEFI (Default)";
                }
            }
            catch
            {
                result.FirmwareType = "Unknown";
            }

            // 5. Total RAM
            try
            {
                using var ramSearcher = new ManagementObjectSearcher("SELECT TotalPhysicalMemory FROM Win32_ComputerSystem");
                foreach (ManagementObject item in ramSearcher.Get())
                {
                    if (item["TotalPhysicalMemory"] != null && ulong.TryParse(item["TotalPhysicalMemory"].ToString(), out ulong bytes))
                    {
                        result.TotalRamGb = Math.Round(bytes / (1024.0 * 1024.0 * 1024.0), 1);
                        break;
                    }
                }
            }
            catch
            {
                result.TotalRamGb = 0;
            }

            // 6. System Drive Free Space
            try
            {
                string sysDrive = Path.GetPathRoot(Environment.SystemDirectory) ?? "C:\\";
                var drive = new DriveInfo(sysDrive);
                result.SystemDriveFreeGb = Math.Round(drive.AvailableFreeSpace / (1024.0 * 1024.0 * 1024.0), 1);
            }
            catch
            {
                result.SystemDriveFreeGb = 0;
            }

            // 7. OS Version
            try
            {
                string? prodName = Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion", "ProductName", string.Empty)?.ToString();
                string? buildNumber = Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion", "CurrentBuild", string.Empty)?.ToString();
                result.OsVersion = $"{prodName} (Build {buildNumber})";
            }
            catch
            {
                result.OsVersion = Environment.OSVersion.ToString();
            }

            return result;
        }

        private static void InferCpuFeatures(string cpuName, HardwareAuditResult result)
        {
            if (string.IsNullOrWhiteSpace(cpuName))
            {
                result.Sse42Supported = null;
                result.PopcntSupported = null;
                return;
            }

            if (cpuName.Contains("Intel", StringComparison.OrdinalIgnoreCase))
            {
                if (Regex.IsMatch(cpuName, @"Core 2|Pentium D|Pentium\(R\) Dual", RegexOptions.IgnoreCase))
                {
                    result.Sse42Supported = false;
                    result.PopcntSupported = false;
                }
                else if (Regex.IsMatch(cpuName, @"Core\(TM\) i[3579]|Xeon|W-|E3-|E5-|E7-|Silver|Gold|Platinum|N100|N200|Core Ultra", RegexOptions.IgnoreCase))
                {
                    result.Sse42Supported = true;
                    result.PopcntSupported = true;
                }
                else
                {
                    result.Sse42Supported = true; // Modern default for post-2010 Intel
                    result.PopcntSupported = true;
                }
            }
            else if (cpuName.Contains("AMD", StringComparison.OrdinalIgnoreCase))
            {
                if (Regex.IsMatch(cpuName, @"Ryzen|EPYC|Threadripper|FX|Bulldozer|Piledriver|Steamroller|Excavator|Athlon 3000", RegexOptions.IgnoreCase))
                {
                    result.Sse42Supported = true;
                    result.PopcntSupported = true;
                }
                else if (Regex.IsMatch(cpuName, @"Phenom II|Athlon II|Opteron 23|Opteron 24", RegexOptions.IgnoreCase))
                {
                    result.Sse42Supported = false;
                    result.PopcntSupported = true;
                }
                else
                {
                    result.Sse42Supported = true;
                    result.PopcntSupported = true;
                }
            }
            else
            {
                result.Sse42Supported = null;
                result.PopcntSupported = null;
            }
        }
    }
}
