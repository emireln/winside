namespace Winside.Models
{
    public enum SetupMode
    {
        ServerUpgradeBypass, // /product server (Recommended bypass for in-place upgrade)
        StandardSetup,       // standard setup.exe
        CustomArgs           // custom user switches
    }

    public class InstallTweakOptions
    {
        public bool BypassTpm { get; set; } = true;
        public bool BypassSecureBoot { get; set; } = true;
        public bool BypassRam { get; set; } = true;
        public bool BypassCpu { get; set; } = true;
        public bool BypassStorage { get; set; } = true;
        public bool BypassMoSetup { get; set; } = true;
        public bool BypassNroMsa { get; set; } = true; // Bypass Microsoft Account requirement (OOBE)

        public SetupMode Mode { get; set; } = SetupMode.ServerUpgradeBypass;
        public string CustomArguments { get; set; } = string.Empty;

        public string SelectedIsoPath { get; set; } = string.Empty;
        public string ExtractionDestination { get; set; } = string.Empty;
    }
}
