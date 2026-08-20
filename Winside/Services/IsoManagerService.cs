using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Winside.Services
{
    public class IsoManagerService
    {
        public static async Task<string?> MountIsoAsync(string isoPath)
        {
            if (!File.Exists(isoPath))
            {
                LoggerService.Instance.LogError($"ISO file not found at: {isoPath}");
                return null;
            }

            LoggerService.Instance.LogInfo($"Mounting ISO image: {isoPath}...");

            return await Task.Run(() =>
            {
                try
                {
                    // PowerShell command to mount disk image and retrieve assigned drive letter
                    string script = $"$disk = Mount-DiskImage -ImagePath '{isoPath.Replace("'", "''")}' -PassThru -ErrorAction Stop; " +
                                   "$vol = Get-Volume -DiskImage $disk -ErrorAction SilentlyContinue | Where-Object DriveLetter; " +
                                   "if ($vol) { $vol.DriveLetter }";

                    var psi = new ProcessStartInfo
                    {
                        FileName = "powershell.exe",
                        Arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"{script}\"",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };

                    using var proc = Process.Start(psi);
                    if (proc == null) return null;

                    string output = proc.StandardOutput.ReadToEnd().Trim();
                    proc.WaitForExit(15000);

                    if (!string.IsNullOrWhiteSpace(output))
                    {
                        string driveLetter = output.Length == 1 ? $"{output}:" : output;
                        if (!driveLetter.EndsWith("\\")) driveLetter += "\\";
                        LoggerService.Instance.LogSuccess($"ISO mounted successfully at drive: {driveLetter}");
                        return driveLetter;
                    }

                    // Fallback search across drives
                    foreach (var drive in DriveInfo.GetDrives())
                    {
                        if (drive.DriveType == DriveType.CDRom && drive.IsReady)
                        {
                            if (File.Exists(Path.Combine(drive.RootDirectory.FullName, "setup.exe")) ||
                                File.Exists(Path.Combine(drive.RootDirectory.FullName, "sources", "install.wim")) ||
                                File.Exists(Path.Combine(drive.RootDirectory.FullName, "sources", "install.esd")))
                            {
                                LoggerService.Instance.LogSuccess($"Detected mounted ISO at: {drive.RootDirectory.FullName}");
                                return drive.RootDirectory.FullName;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    LoggerService.Instance.LogError($"Error mounting ISO: {ex.Message}");
                }

                return null;
            });
        }

        public static async Task DismountIsoAsync(string isoPath)
        {
            if (string.IsNullOrWhiteSpace(isoPath) || !File.Exists(isoPath)) return;

            await Task.Run(() =>
            {
                try
                {
                    LoggerService.Instance.LogInfo("Dismounting ISO image...");
                    var psi = new ProcessStartInfo
                    {
                        FileName = "powershell.exe",
                        Arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"Dismount-DiskImage -ImagePath '{isoPath.Replace("'", "''")}' -ErrorAction SilentlyContinue\"",
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };
                    using var proc = Process.Start(psi);
                    proc?.WaitForExit(10000);
                    LoggerService.Instance.LogInfo("ISO dismounted.");
                }
                catch (Exception ex)
                {
                    LoggerService.Instance.LogWarning($"Dismount notification: {ex.Message}");
                }
            });
        }

        public static async Task<bool> ExtractIsoFilesAsync(
            string sourceDrive,
            string destinationFolder,
            IProgress<(double percent, string status, long copied, long total)> progress,
            CancellationToken ct)
        {
            return await Task.Run(() =>
            {
                try
                {
                    if (!Directory.Exists(destinationFolder))
                    {
                        Directory.CreateDirectory(destinationFolder);
                    }

                    var sourceDirInfo = new DirectoryInfo(sourceDrive);
                    var allFiles = sourceDirInfo.GetFiles("*", SearchOption.AllDirectories);
                    long totalFiles = allFiles.Length;

                    if (totalFiles == 0)
                    {
                        LoggerService.Instance.LogError("No files found on the mounted drive.");
                        return false;
                    }

                    LoggerService.Instance.LogInfo($"Starting extraction of {totalFiles} files to: {destinationFolder}");
                    long copiedFiles = 0;

                    foreach (var file in allFiles)
                    {
                        if (ct.IsCancellationRequested)
                        {
                            LoggerService.Instance.LogWarning("Extraction cancelled by user.");
                            return false;
                        }

                        string relativePath = Path.GetRelativePath(sourceDrive, file.FullName);
                        string targetFilePath = Path.Combine(destinationFolder, relativePath);
                        string? targetDir = Path.GetDirectoryName(targetFilePath);

                        if (!string.IsNullOrEmpty(targetDir) && !Directory.Exists(targetDir))
                        {
                            Directory.CreateDirectory(targetDir);
                        }

                        file.CopyTo(targetFilePath, true);

                        copiedFiles++;
                        double percent = Math.Round((double)copiedFiles / totalFiles * 100.0, 1);
                        progress.Report((percent, $"Extracting: {file.Name}", copiedFiles, totalFiles));
                    }

                    LoggerService.Instance.LogSuccess($"Successfully extracted all {totalFiles} files.");
                    return true;
                }
                catch (Exception ex)
                {
                    LoggerService.Instance.LogError($"File extraction failed: {ex.Message}");
                    return false;
                }
            }, ct);
        }

        public static string? FindSetupExecutable(string extractedFolder)
        {
            if (string.IsNullOrWhiteSpace(extractedFolder) || !Directory.Exists(extractedFolder))
                return null;

            string prepPath = Path.Combine(extractedFolder, "sources", "setupprep.exe");
            if (File.Exists(prepPath)) return prepPath;

            string setupPath = Path.Combine(extractedFolder, "setup.exe");
            if (File.Exists(setupPath)) return setupPath;

            return null;
        }
    }
}
