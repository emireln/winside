using System;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using Microsoft.Win32;
using Winside.Models;
using Winside.Services;

namespace Winside
{
    public partial class MainWindow : Window
    {
        private CancellationTokenSource? _extractCts;
        private string? _detectedSetupExe;

        public MainWindow()
        {
            InitializeComponent();

            LogListBox.ItemsSource = LoggerService.Instance.Entries;
            ((INotifyCollectionChanged)LoggerService.Instance.Entries).CollectionChanged += (_, _) =>
            {
                if (LogListBox.Items.Count > 0)
                {
                    LogListBox.ScrollIntoView(LogListBox.Items[^1]);
                }
            };

            LoggerService.Instance.LogInfo("Winside v2.0 initialized.");

            string desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            TxtExtractDest.Text = Path.Combine(desktop, "Windows11_Setup_Files");

            Loaded += async (_, _) => await RunHardwareAuditAsync();
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            try
            {
                DwmHelper.ApplyWindows11Style(this, DwmHelper.BackdropType.Mica, DwmHelper.CornerPreference.Round);
            }
            catch (Exception ex)
            {
                LoggerService.Instance.LogWarning($"DWM Mica styling notice: {ex.Message}");
            }
        }

        #region Hardware Audit

        private async Task RunHardwareAuditAsync()
        {
            TxtGlobalStatus.Text = "Auditing system hardware compatibility...";
            LoggerService.Instance.LogInfo("Starting hardware compatibility audit...");

            var audit = await Task.Run(SystemAuditService.PerformAudit);

            TxtCpuName.Text = audit.CpuName;
            TxtFirmware.Text = audit.FirmwareType;

            // SSE4.2
            UpdateBadge(BadgeSse42, TxtSse42, audit.Sse42Supported);
            // POPCNT
            UpdateBadge(BadgePopcnt, TxtPopcnt, audit.PopcntSupported);

            // TPM
            TxtTpm.Text = audit.TpmVersion;
            TxtTpm.Foreground = (audit.TpmPresent == true) ? new SolidColorBrush(Color.FromRgb(46, 204, 113)) : new SolidColorBrush(Color.FromRgb(231, 76, 60));

            // Secure Boot
            TxtSecureBoot.Text = audit.SecureBootEnabled switch
            {
                true => "Enabled (Active)",
                false => "Disabled / Not Supported",
                null => "Unknown / Legacy State"
            };
            TxtSecureBoot.Foreground = (audit.SecureBootEnabled == true) ? new SolidColorBrush(Color.FromRgb(46, 204, 113)) : new SolidColorBrush(Color.FromRgb(243, 156, 18));

            // RAM & Disk
            TxtRam.Text = $"{audit.TotalRamGb} GB (Min 4.0 GB required)";
            TxtDiskSpace.Text = $"{audit.SystemDriveFreeGb} GB free (Min 64 GB recommended)";
            TxtOsVersion.Text = audit.OsVersion;

            if (audit.MeetsOfficialRequirements)
            {
                TxtEvaluationSummary.Text = "System meets all official Microsoft Windows 11 hardware requirements.";
                TxtEvaluationSummary.Foreground = new SolidColorBrush(Color.FromRgb(46, 204, 113));
                LoggerService.Instance.LogSuccess("Hardware audit completed: System meets official Windows 11 requirements.");
            }
            else
            {
                TxtEvaluationSummary.Text = "Some hardware components (TPM, CPU, or SecureBoot) do not meet official requirements. Applying bypasses in Tab 2 or using 'Server Mode' is recommended.";
                TxtEvaluationSummary.Foreground = new SolidColorBrush(Color.FromRgb(243, 156, 18));
                LoggerService.Instance.LogWarning("Hardware audit completed: Unsupported components detected. Bypass modes are recommended.");
            }

            TxtGlobalStatus.Text = "Hardware audit completed.";
        }

        private static void UpdateBadge(System.Windows.Controls.Border border, System.Windows.Controls.TextBlock textBlock, bool? state)
        {
            if (state == true)
            {
                border.Background = new SolidColorBrush(Color.FromRgb(16, 124, 65));
                textBlock.Text = "SUPPORTED";
            }
            else if (state == false)
            {
                border.Background = new SolidColorBrush(Color.FromRgb(192, 57, 43));
                textBlock.Text = "NOT SUPPORTED";
            }
            else
            {
                border.Background = new SolidColorBrush(Color.FromRgb(100, 100, 100));
                textBlock.Text = "UNKNOWN";
            }
        }

        private async void BtnRefreshAudit_Click(object sender, RoutedEventArgs e)
        {
            await RunHardwareAuditAsync();
        }

        private void BtnCoreinfo_Click(object sender, RoutedEventArgs e)
        {
            OpenBrowserUrl("https://learn.microsoft.com/sysinternals/downloads/coreinfo");
        }

        #endregion

        #region Bypasses & Registry

        private void BtnApplyBypasses_Click(object sender, RoutedEventArgs e)
        {
            var options = new InstallTweakOptions
            {
                BypassTpm = ChkTPM.IsChecked == true,
                BypassSecureBoot = ChkSecureBoot.IsChecked == true,
                BypassRam = ChkRAM.IsChecked == true,
                BypassCpu = ChkCPU.IsChecked == true,
                BypassStorage = ChkStorage.IsChecked == true,
                BypassMoSetup = ChkMoSetup.IsChecked == true,
                BypassNroMsa = ChkBypassNro.IsChecked == true
            };

            bool success = RegistryBypassService.ApplyBypasses(options);
            if (success)
            {
                MessageBox.Show("Windows 11 bypass registry keys successfully configured!", "Emir Tech Tools", MessageBoxButton.OK, MessageBoxImage.Information);
                TxtGlobalStatus.Text = "Bypass keys applied.";
            }
            else
            {
                MessageBox.Show("Failed to apply some registry keys. Make sure the app is running as Administrator.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnVerifyRegistry_Click(object sender, RoutedEventArgs e)
        {
            bool active = RegistryBypassService.CheckIfBypassActive();
            if (active)
            {
                MessageBox.Show("Active bypass keys are currently detected in your system registry.", "Registry Status", MessageBoxButton.OK, MessageBoxImage.Information);
                LoggerService.Instance.LogInfo("Registry check: Bypass keys are ACTIVE.");
            }
            else
            {
                MessageBox.Show("No Windows 11 bypass registry keys detected (System is at standard defaults).", "Registry Status", MessageBoxButton.OK, MessageBoxImage.Information);
                LoggerService.Instance.LogInfo("Registry check: No bypass keys found.");
            }
        }

        private void BtnResetBypasses_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "Are you sure you want to remove all Windows 11 bypass registry keys and restore defaults?",
                "Confirm Reset",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                RegistryBypassService.ResetBypasses();
                MessageBox.Show("Registry bypass keys have been removed.", "Emir Tech Tools", MessageBoxButton.OK, MessageBoxImage.Information);
                TxtGlobalStatus.Text = "Bypass keys reset to defaults.";
            }
        }

        #endregion

        #region ISO & Installer

        private void BtnDownloadIso_Click(object sender, RoutedEventArgs e)
        {
            OpenBrowserUrl("https://www.microsoft.com/software-download/windows11");
        }

        private void BtnBrowseIso_Click(object sender, RoutedEventArgs e)
        {
            var ofd = new OpenFileDialog
            {
                Filter = "Windows ISO Image (*.iso)|*.iso|All Files (*.*)|*.*",
                Title = "Select Windows 11 ISO Image"
            };

            if (ofd.ShowDialog() == true)
            {
                TxtIsoPath.Text = ofd.FileName;
                string isoName = Path.GetFileNameWithoutExtension(ofd.FileName);
                string desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
                TxtExtractDest.Text = Path.Combine(desktop, isoName);
                LoggerService.Instance.LogInfo($"Selected ISO: {ofd.FileName}");
            }
        }

        private void BtnBrowseDest_Click(object sender, RoutedEventArgs e)
        {
            var ofd = new OpenFolderDialog
            {
                Title = "Select Destination Extraction Folder",
                Multiselect = false
            };

            if (ofd.ShowDialog() == true)
            {
                TxtExtractDest.Text = ofd.FolderName;
                LoggerService.Instance.LogInfo($"Selected extraction destination: {ofd.FolderName}");
            }
        }

        private async void BtnExtractIso_Click(object sender, RoutedEventArgs e)
        {
            string isoPath = TxtIsoPath.Text.Trim();
            string destPath = TxtExtractDest.Text.Trim();

            if (string.IsNullOrWhiteSpace(isoPath) || !File.Exists(isoPath))
            {
                MessageBox.Show("Please select a valid Windows 11 ISO file first.", "Missing ISO", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(destPath))
            {
                MessageBox.Show("Please specify a target destination folder.", "Missing Destination", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            BtnExtractIso.IsEnabled = false;
            BtnCancelExtract.IsEnabled = true;
            ExtractProgressPanel.Visibility = Visibility.Visible;
            ExtractProgressBar.Value = 0;
            TxtExtractStatus.Text = "Mounting ISO...";
            TxtExtractPercent.Text = "0%";
            TxtGlobalStatus.Text = "Mounting and extracting ISO...";

            _extractCts = new CancellationTokenSource();

            try
            {
                string? drive = await IsoManagerService.MountIsoAsync(isoPath);
                if (string.IsNullOrWhiteSpace(drive))
                {
                    MessageBox.Show("Failed to mount the ISO image or determine drive letter.", "Mount Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var progress = new Progress<(double percent, string status, long copied, long total)>(report =>
                {
                    ExtractProgressBar.Value = report.percent;
                    TxtExtractPercent.Text = $"{report.percent}%";
                    TxtExtractStatus.Text = $"{report.status} ({report.copied}/{report.total})";
                });

                bool success = await IsoManagerService.ExtractIsoFilesAsync(drive, destPath, progress, _extractCts.Token);
                await IsoManagerService.DismountIsoAsync(isoPath);

                if (success)
                {
                    _detectedSetupExe = IsoManagerService.FindSetupExecutable(destPath);
                    if (!string.IsNullOrEmpty(_detectedSetupExe))
                    {
                        MessageBox.Show($"ISO extraction completed successfully!\r\nSetup executable ready:\r\n{_detectedSetupExe}", "Extraction Complete", MessageBoxButton.OK, MessageBoxImage.Information);
                        TxtGlobalStatus.Text = "ISO extraction finished successfully.";
                    }
                    else
                    {
                        MessageBox.Show("Extraction finished, but neither setupprep.exe nor setup.exe was found in the destination.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
            }
            catch (Exception ex)
            {
                LoggerService.Instance.LogError($"Extraction error: {ex.Message}");
                MessageBox.Show($"An error occurred during extraction:\r\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                BtnExtractIso.IsEnabled = true;
                BtnCancelExtract.IsEnabled = false;
                _extractCts?.Dispose();
                _extractCts = null;
            }
        }

        private void BtnCancelExtract_Click(object sender, RoutedEventArgs e)
        {
            _extractCts?.Cancel();
            TxtExtractStatus.Text = "Cancelling extraction...";
            LoggerService.Instance.LogWarning("Extraction cancellation requested.");
        }

        private void BtnLaunchSetup_Click(object sender, RoutedEventArgs e)
        {
            string destPath = TxtExtractDest.Text.Trim();
            string? setupExe = _detectedSetupExe ?? IsoManagerService.FindSetupExecutable(destPath);

            if (string.IsNullOrWhiteSpace(setupExe) || !File.Exists(setupExe))
            {
                var prompt = MessageBox.Show(
                    "Setup executable not found in destination folder.\r\nWould you like to browse manually for setup.exe or setupprep.exe?",
                    "Setup Not Found",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (prompt == MessageBoxResult.Yes)
                {
                    var ofd = new OpenFileDialog
                    {
                        Filter = "Setup Executable (setup.exe, setupprep.exe)|*.exe",
                        Title = "Select Windows Setup Executable"
                    };
                    if (ofd.ShowDialog() == true)
                    {
                        setupExe = ofd.FileName;
                        _detectedSetupExe = setupExe;
                    }
                    else return;
                }
                else return;
            }

            var mode = SetupMode.ServerUpgradeBypass;
            if (RbStandardMode.IsChecked == true) mode = SetupMode.StandardSetup;
            else if (RbCustomMode.IsChecked == true) mode = SetupMode.CustomArgs;

            string confirmMsg = mode switch
            {
                SetupMode.ServerUpgradeBypass => "Launch Windows 11 Setup in Server Mode (/product server)?\r\n\r\nThis bypasses compatibility checks for in-place upgrades.",
                SetupMode.StandardSetup       => "Launch Windows 11 Setup in Standard Mode?",
                SetupMode.CustomArgs          => $"Launch Windows 11 Setup with custom arguments:\r\n'{TxtCustomArgs.Text}'?",
                _                             => "Launch Windows 11 Setup?"
            };

            var confirm = MessageBox.Show(confirmMsg, "Confirm Launch", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (confirm == MessageBoxResult.Yes)
            {
                bool launched = SetupLauncherService.LaunchSetup(setupExe, mode, TxtCustomArgs.Text);
                if (launched)
                {
                    TxtGlobalStatus.Text = "Windows 11 Setup launched!";
                    MessageBox.Show(
                        "Windows 11 Setup has started.\r\n\r\nWhen the installation is completely finished, you can safely delete the extracted folder to free up disk space.",
                        "Setup Running",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
            }
        }

        #endregion

        #region Logs

        private void BtnCopyLogs_Click(object sender, RoutedEventArgs e)
        {
            var sb = new StringBuilder();
            foreach (var entry in LoggerService.Instance.Entries)
            {
                sb.AppendLine($"[{entry.FormattedTime}] [{entry.Level}] {entry.Message}");
            }

            try
            {
                Clipboard.SetText(sb.ToString());
                MessageBox.Show("Logs copied to clipboard.", "Copied", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to copy to clipboard: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnOpenLogFile_Click(object sender, RoutedEventArgs e)
        {
            string path = LoggerService.Instance.LogFilePath;
            if (File.Exists(path))
            {
                try
                {
                    Process.Start(new ProcessStartInfo("notepad.exe", $"\"{path}\"") { UseShellExecute = true });
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Could not open log file: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                MessageBox.Show($"Log file does not exist yet at:\r\n{path}", "Notice", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void BtnClearLogs_Click(object sender, RoutedEventArgs e)
        {
            LoggerService.Instance.Entries.Clear();
            LoggerService.Instance.LogInfo("Log console cleared.");
        }

        #endregion

        private static void OpenBrowserUrl(string url)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                LoggerService.Instance.LogError($"Failed to open URL {url}: {ex.Message}");
            }
        }
    }
}
