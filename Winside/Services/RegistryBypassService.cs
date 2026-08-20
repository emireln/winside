using System;
using Microsoft.Win32;
using Winside.Models;

namespace Winside.Services
{
    public class RegistryBypassService
    {
        public static bool ApplyBypasses(InstallTweakOptions options)
        {
            try
            {
                LoggerService.Instance.LogInfo("Applying Windows 11 installation bypass registry keys...");

                // 1. LabConfig
                using (var setupKey = Registry.LocalMachine.OpenSubKey(@"SYSTEM\Setup", true))
                {
                    if (setupKey != null)
                    {
                        using var labConfig = setupKey.CreateSubKey("LabConfig", true);
                        if (options.BypassTpm) labConfig.SetValue("BypassTPMCheck", 1, RegistryValueKind.DWord);
                        if (options.BypassSecureBoot) labConfig.SetValue("BypassSecureBootCheck", 1, RegistryValueKind.DWord);
                        if (options.BypassRam) labConfig.SetValue("BypassRAMCheck", 1, RegistryValueKind.DWord);
                        if (options.BypassCpu) labConfig.SetValue("BypassCPUCheck", 1, RegistryValueKind.DWord);
                        if (options.BypassStorage)
                        {
                            labConfig.SetValue("BypassStorageCheck", 1, RegistryValueKind.DWord);
                            labConfig.SetValue("BypassDiskCheck", 1, RegistryValueKind.DWord);
                        }
                    }
                }

                // 2. MoSetup
                if (options.BypassMoSetup)
                {
                    using var setupKey = Registry.LocalMachine.OpenSubKey(@"SYSTEM\Setup", true);
                    if (setupKey != null)
                    {
                        using var moSetup = setupKey.CreateSubKey("MoSetup", true);
                        moSetup.SetValue("AllowUpgradesWithUnsupportedTPMOrCPU", 1, RegistryValueKind.DWord);
                    }
                }

                // 3. OOBE BypassNRO (Allow local offline account setup without MSA)
                if (options.BypassNroMsa)
                {
                    using var oobeKey = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\OOBE", true);
                    oobeKey?.SetValue("BypassNRO", 1, RegistryValueKind.DWord);
                }

                LoggerService.Instance.LogSuccess("Bypass registry keys successfully configured.");
                return true;
            }
            catch (Exception ex)
            {
                LoggerService.Instance.LogError($"Failed to apply registry keys: {ex.Message}");
                return false;
            }
        }

        public static bool ResetBypasses()
        {
            try
            {
                LoggerService.Instance.LogInfo("Resetting Windows 11 bypass registry keys to Windows defaults...");

                using (var setupKey = Registry.LocalMachine.OpenSubKey(@"SYSTEM\Setup", true))
                {
                    if (setupKey != null)
                    {
                        try { setupKey.DeleteSubKeyTree("LabConfig", false); } catch { }
                        
                        try
                        {
                            using var moSetup = setupKey.OpenSubKey("MoSetup", true);
                            moSetup?.DeleteValue("AllowUpgradesWithUnsupportedTPMOrCPU", false);
                        }
                        catch { }
                    }
                }

                try
                {
                    using var oobeKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\OOBE", true);
                    oobeKey?.DeleteValue("BypassNRO", false);
                }
                catch { }

                LoggerService.Instance.LogSuccess("Registry keys reset successfully.");
                return true;
            }
            catch (Exception ex)
            {
                LoggerService.Instance.LogError($"Failed to reset registry keys: {ex.Message}");
                return false;
            }
        }

        public static bool CheckIfBypassActive()
        {
            try
            {
                using var labConfig = Registry.LocalMachine.OpenSubKey(@"SYSTEM\Setup\LabConfig");
                if (labConfig != null) return true;

                using var moSetup = Registry.LocalMachine.OpenSubKey(@"SYSTEM\Setup\MoSetup");
                if (moSetup?.GetValue("AllowUpgradesWithUnsupportedTPMOrCPU") != null) return true;

                return false;
            }
            catch
            {
                return false;
            }
        }
    }
}
