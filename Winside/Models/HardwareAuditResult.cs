namespace Winside.Models
{
    public class HardwareAuditResult
    {
        public string CpuName { get; set; } = "Detecting...";
        public bool? Sse42Supported { get; set; }
        public bool? PopcntSupported { get; set; }
        public bool? TpmPresent { get; set; }
        public string TpmVersion { get; set; } = "Unknown";
        public bool? SecureBootEnabled { get; set; }
        public double TotalRamGb { get; set; }
        public double SystemDriveFreeGb { get; set; }
        public string FirmwareType { get; set; } = "Unknown"; // UEFI or Legacy BIOS
        public string OsVersion { get; set; } = string.Empty;

        public bool MeetsOfficialRequirements =>
            (Sse42Supported == true) &&
            (PopcntSupported == true) &&
            (TpmPresent == true && (TpmVersion.Contains("2.0") || TpmVersion.Contains("2"))) &&
            (SecureBootEnabled == true) &&
            (TotalRamGb >= 4.0) &&
            (SystemDriveFreeGb >= 64.0);

        public static string StatusText(bool? value) => value switch
        {
            true => "SUPPORTED",
            false => "NOT SUPPORTED",
            null => "UNKNOWN"
        };
    }
}
