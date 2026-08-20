using System;
using System.Diagnostics;
using Winside.Models;

namespace Winside.Services
{
    public class SetupLauncherService
    {
        public static bool LaunchSetup(string setupExecutable, SetupMode mode, string customArgs)
        {
            try
            {
                string args = mode switch
                {
                    SetupMode.ServerUpgradeBypass => "/product server",
                    SetupMode.StandardSetup       => string.Empty,
                    SetupMode.CustomArgs          => customArgs.Trim(),
                    _                             => "/product server"
                };

                LoggerService.Instance.LogInfo($"Launching Windows 11 Setup: '{setupExecutable}' with arguments: '{args}'");

                var psi = new ProcessStartInfo
                {
                    FileName = setupExecutable,
                    Arguments = args,
                    UseShellExecute = true,
                    Verb = "runas"
                };

                var proc = Process.Start(psi);
                if (proc != null)
                {
                    LoggerService.Instance.LogSuccess("Windows 11 Setup launched successfully! Follow the on-screen installation wizard.");
                    return true;
                }
                else
                {
                    LoggerService.Instance.LogError("Process failed to start.");
                    return false;
                }
            }
            catch (Exception ex)
            {
                LoggerService.Instance.LogError($"Failed to launch setup executable: {ex.Message}");
                return false;
            }
        }
    }
}
