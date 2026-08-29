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
        private HardwareAuditResult? _lastAudit;

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

            ApplyLocalization();

            Loaded += async (_, _) => await RunHardwareAuditAsync();
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            try
            {
                DwmHelper.ApplyWindows11Style(this, DwmHelper.BackdropType.None, DwmHelper.CornerPreference.Round);
                DwmHelper.RemoveTitlebarIcon(this);
            }
            catch (Exception ex)
            {
                LoggerService.Instance.LogWarning($"DWM styling notice: {ex.Message}");
            }
        }

        private void NavButton_Click(object sender, RoutedEventArgs e)
        {
            ViewCompatibility.Visibility = NavCompatibility.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
            ViewBypasses.Visibility = NavBypasses.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
            ViewInstaller.Visibility = NavInstaller.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
            ViewLogs.Visibility = NavLogs.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
        }

        private void BtnPortfolio_Click(object sender, RoutedEventArgs e)
        {
            OpenBrowserUrl("https://emirln.com");
        }

        private void BtnLangToggle_Click(object sender, RoutedEventArgs e)
        {
            LocalizationService.Instance.ToggleLanguage();
            ApplyLocalization();
            if (_lastAudit != null)
            {
                DisplayAuditResult(_lastAudit);
            }
        }

        #region Dynamic Bilingual Localization

        private void ApplyLocalization()
        {
            bool isPt = LocalizationService.Instance.IsPtBr;

            // Lang toggle button
            TxtLangCode.Text = isPt ? "PT" : "EN";
            BtnLangToggle.ToolTip = isPt ? "Alternar Idioma / Switch Language (PT / EN)" : "Switch Language / Alternar Idioma (EN / PT)";

            // Window Title
            Title = isPt
                ? "Winside - Gerenciador de Compatibilidade e Instalação do Windows 11"
                : "Winside - Windows 11 Deployment, Compatibility & Bypass Manager";

            // Sidebar tooltips
            NavCompatibility.ToolTip = isPt ? "Auditoria de Hardware" : "Hardware Compatibility Audit";
            NavBypasses.ToolTip = isPt ? "Bypasses e Registro" : "Bypasses & Registry";
            NavInstaller.ToolTip = isPt ? "ISO e Instalação" : "ISO & Installation";
            NavLogs.ToolTip = isPt ? "Diagnósticos e Logs" : "Diagnostics & Logs";
            BtnPortfolio.ToolTip = isPt ? "Portfólio (https://emirln.com)" : "Portfolio (https://emirln.com)";
            BadgeAdminIcon.ToolTip = isPt ? "Executando com Privilégios de Administrador" : "Running with Administrator Privileges";

            // View 1: Hardware Compatibility
            TxtCompatTitle.Text = isPt ? "Avaliação de Compatibilidade do Sistema" : "System Compatibility Evaluation";
            BtnRefreshAudit.Content = isPt ? "Refazer Auditoria" : "Re-run Audit";
            TxtProcSection.Text = isPt ? "Processador e Suporte a Instruções" : "Processor & Instruction Support";
            LblCpuName.Text = isPt ? "Processador:" : "Processor:";
            LblSse42.Text = isPt ? "Instrução SSE4.2:" : "SSE4.2 Instruction:";
            LblPopcnt.Text = isPt ? "Instrução POPCNT:" : "POPCNT Instruction:";
            LblFirmware.Text = isPt ? "Tipo de Firmware:" : "Firmware Type:";
            TxtSecuritySection.Text = isPt ? "Segurança e Especificações de Hardware" : "Security & Hardware Specifications";
            LblTpm.Text = isPt ? "Módulo TPM:" : "TPM Module:";
            LblSecureBoot.Text = isPt ? "Inicialização Segura:" : "Secure Boot:";
            LblRam.Text = isPt ? "Memória RAM:" : "System RAM:";
            LblDisk.Text = isPt ? "Espaço em Disco:" : "System Drive Space:";
            LblOs.Text = isPt ? "Sistema Operacional:" : "Operating System:";

            // View 2: Bypasses & Registry
            TxtBypassTitle.Text = isPt ? "Bypasses de Compatibilidade do Windows 11" : "Windows 11 Compatibility Bypasses";
            TxtBypassDesc.Text = isPt
                ? "Aplique as chaves de registro abaixo para contornar verificações de hardware durante a instalação ou atualização."
                : "Apply registry keys below to bypass hardware gating checks during Windows 11 installation or in-place upgrade.";

            ChkTPM.Content = isPt
                ? "Ignorar verificação de TPM 2.0 (LabConfig\\BypassTPMCheck)"
                : "Bypass TPM 2.0 check (LabConfig\\BypassTPMCheck)";
            ChkSecureBoot.Content = isPt
                ? "Ignorar verificação de Inicialização Segura / Secure Boot (LabConfig\\BypassSecureBootCheck)"
                : "Bypass Secure Boot check (LabConfig\\BypassSecureBootCheck)";
            ChkRAM.Content = isPt
                ? "Ignorar verificação de Memória RAM mínima (LabConfig\\BypassRAMCheck)"
                : "Bypass minimum RAM check (LabConfig\\BypassRAMCheck)";
            ChkCPU.Content = isPt
                ? "Ignorar verificação de Modelo e Geração de CPU (LabConfig\\BypassCPUCheck)"
                : "Bypass CPU model & generation check (LabConfig\\BypassCPUCheck)";
            ChkStorage.Content = isPt
                ? "Ignorar verificação de Tamanho Mínimo de Disco (LabConfig\\BypassStorageCheck)"
                : "Bypass minimum drive storage check (LabConfig\\BypassStorageCheck)";
            ChkMoSetup.Content = isPt
                ? "Permitir Atualização Direta com TPM ou CPU incompatíveis (MoSetup\\AllowUpgradesWithUnsupportedTPMOrCPU)"
                : "Allow in-place upgrade with unsupported TPM or CPU (MoSetup\\AllowUpgradesWithUnsupportedTPMOrCPU)";
            ChkBypassNro.Content = isPt
                ? "Ignorar exigência de Conta Microsoft (MSA) no OOBE (OOBE\\BypassNRO)"
                : "Bypass Microsoft Account (MSA) requirement during OOBE (OOBE\\BypassNRO)";

            BtnApplyBypasses.Content = isPt ? "Aplicar Bypasses" : "Apply Bypasses";
            BtnVerifyRegistry.Content = isPt ? "Verificar Registro Ativo" : "Verify Active Registry";
            BtnResetBypasses.Content = isPt ? "Restaurar Padrões / Limpar" : "Reset to Defaults";
            TxtGuidanceTitle.Text = isPt ? "Orientações e Requisitos do Windows 11" : "Windows 11 Guidance & Requirements";
            TxtGuidance1.Text = isPt
                ? "• Para atualizar máquinas não suportadas mantendo programas e arquivos intactos, o 'Modo Servidor' (/product server) executa o instalador ignorando os bloqueios de compatibilidade com alta estabilidade."
                : "• To upgrade unsupported PCs while keeping apps and files intact, 'Server Mode' (/product server) runs the setup bypassing compatibility checks.";
            TxtGuidance2.Text = isPt
                ? "• A partir do Windows 11 24H2, o processador exige compulsoriamente suporte às instruções SSE4.2 e POPCNT em nível de microcódigo."
                : "• Starting with Windows 11 24H2, the CPU strictly requires hardware SSE4.2 and POPCNT instruction set support.";

            // View 3: ISO Manager & Installer
            TxtStep1Title.Text = isPt ? "Etapa 1: Selecionar Arquivo ISO do Windows 11" : "Step 1: Select Windows 11 ISO Image";
            BtnDownloadIso.Content = isPt ? "Baixar ISO Oficial da Microsoft" : "Download Official ISO from Microsoft";
            LblIsoPath.Text = isPt ? "Caminho do arquivo ISO:" : "ISO File Path:";
            BtnBrowseIso.Content = isPt ? "Procurar ISO..." : "Browse ISO...";
            LblExtractDest.Text = isPt ? "Pasta de Destino da Extração:" : "Extraction Destination Folder:";
            BtnBrowseDest.Content = isPt ? "Procurar Pasta..." : "Browse Folder...";
            BtnExtractIso.Content = isPt ? "Montar e Extrair ISO" : "Mount & Extract ISO";
            BtnCancelExtract.Content = isPt ? "Cancelar Extração" : "Cancel Extraction";

            TxtStep2Title.Text = isPt ? "Etapa 2: Iniciar Instalação do Windows 11" : "Step 2: Launch Windows 11 Setup";
            RbServerMode.Content = isPt
                ? "Modo Servidor (Recomendado: comando /product server para contornar verificações)"
                : "Server Mode (Recommended: /product server command bypass)";
            RbStandardMode.Content = isPt
                ? "Modo Padrão (Assistente padrão do Windows 11 sem parâmetros adicionais)"
                : "Standard Mode (Standard Windows 11 setup wizard without extra parameters)";
            RbCustomMode.Content = isPt ? "Parâmetros Personalizados:" : "Custom Parameters:";
            BtnLaunchSetup.Content = isPt ? "Iniciar Instalação do Windows 11" : "Launch Windows 11 Setup";

            // View 4: Logs
            BtnCopyLogs.Content = isPt ? "Copiar Todos os Logs" : "Copy All Logs";
            BtnOpenLogFile.Content = isPt ? "Abrir Log no Bloco de Notas" : "Open Log in Notepad";
            BtnClearLogs.Content = isPt ? "Limpar Console" : "Clear Console";

            // Status bar
            TxtGlobalStatus.Text = isPt ? "Pronto." : "Ready.";
        }

        #endregion

        #region Hardware Audit

        private async Task RunHardwareAuditAsync()
        {
            bool isPt = LocalizationService.Instance.IsPtBr;
            TxtGlobalStatus.Text = isPt ? "Analisando compatibilidade do hardware..." : "Auditing hardware compatibility...";
            LoggerService.Instance.LogInfo(isPt ? "Iniciando auditoria de compatibilidade de hardware..." : "Starting hardware compatibility audit...");

            var audit = await Task.Run(SystemAuditService.PerformAudit);
            _lastAudit = audit;

            DisplayAuditResult(audit);

            TxtGlobalStatus.Text = isPt ? "Auditoria de hardware concluída." : "Hardware audit completed.";
        }

        private void DisplayAuditResult(HardwareAuditResult audit)
        {
            bool isPt = LocalizationService.Instance.IsPtBr;

            TxtCpuName.Text = audit.CpuName;
            TxtFirmware.Text = audit.FirmwareType;

            // SSE4.2
            UpdateBadge(BadgeSse42, TxtSse42, audit.Sse42Supported, isPt);
            // POPCNT
            UpdateBadge(BadgePopcnt, TxtPopcnt, audit.PopcntSupported, isPt);

            // TPM
            TxtTpm.Text = audit.TpmVersion;
            TxtTpm.Foreground = (audit.TpmPresent == true) ? new SolidColorBrush(Color.FromRgb(34, 197, 94)) : new SolidColorBrush(Color.FromRgb(239, 68, 68));

            // Secure Boot
            TxtSecureBoot.Text = audit.SecureBootEnabled switch
            {
                true => isPt ? "Ativado (Seguro)" : "Enabled (Secure)",
                false => isPt ? "Desativado / Não Suportado" : "Disabled / Unsupported",
                null => isPt ? "Desconhecido" : "Unknown"
            };
            TxtSecureBoot.Foreground = (audit.SecureBootEnabled == true) ? new SolidColorBrush(Color.FromRgb(34, 197, 94)) : new SolidColorBrush(Color.FromRgb(245, 158, 11));

            // RAM & Disk
            TxtRam.Text = isPt
                ? $"{audit.TotalRamGb} GB (Mínimo de 4.0 GB)"
                : $"{audit.TotalRamGb} GB (Minimum 4.0 GB)";
            TxtDiskSpace.Text = isPt
                ? $"{audit.SystemDriveFreeGb} GB livres de {audit.TotalDriveSizeGb} GB"
                : $"{audit.SystemDriveFreeGb} GB free of {audit.TotalDriveSizeGb} GB";
            TxtOsVersion.Text = audit.OsVersion;

            if (audit.MeetsOfficialRequirements)
            {
                TxtEvaluationSummary.Text = isPt
                    ? "O sistema atende integralmente a todos os requisitos oficiais da Microsoft para o Windows 11."
                    : "System fully meets all official Microsoft requirements for Windows 11.";
                TxtEvaluationSummary.Foreground = new SolidColorBrush(Color.FromRgb(34, 197, 94));
            }
            else
            {
                TxtEvaluationSummary.Text = isPt
                    ? "Alguns componentes não atendem aos requisitos oficiais. O uso de bypasses ou do 'Modo Servidor' (/product server) é recomendado."
                    : "Some components do not meet official requirements. Registry bypasses or Server Mode (/product server) is recommended.";
                TxtEvaluationSummary.Foreground = new SolidColorBrush(Color.FromRgb(245, 158, 11));
            }
        }

        private static void UpdateBadge(System.Windows.Controls.Border border, System.Windows.Controls.TextBlock textBlock, bool? state, bool isPt)
        {
            if (state == true)
            {
                border.Background = new SolidColorBrush(Color.FromRgb(16, 59, 30));
                border.BorderBrush = new SolidColorBrush(Color.FromRgb(21, 128, 61));
                textBlock.Foreground = new SolidColorBrush(Color.FromRgb(34, 197, 94));
                textBlock.Text = isPt ? "SUPORTADO" : "SUPPORTED";
            }
            else if (state == false)
            {
                border.Background = new SolidColorBrush(Color.FromRgb(61, 16, 16));
                border.BorderBrush = new SolidColorBrush(Color.FromRgb(185, 28, 28));
                textBlock.Foreground = new SolidColorBrush(Color.FromRgb(239, 68, 68));
                textBlock.Text = isPt ? "NÃO SUPORTADO" : "NOT SUPPORTED";
            }
            else
            {
                border.Background = new SolidColorBrush(Color.FromRgb(44, 44, 50));
                border.BorderBrush = new SolidColorBrush(Color.FromRgb(62, 62, 72));
                textBlock.Foreground = new SolidColorBrush(Color.FromRgb(161, 161, 170));
                textBlock.Text = isPt ? "DESCONHECIDO" : "UNKNOWN";
            }
        }

        private async void BtnRefreshAudit_Click(object sender, RoutedEventArgs e)
        {
            await RunHardwareAuditAsync();
        }

        #endregion

        #region Bypasses & Registry

        private void BtnApplyBypasses_Click(object sender, RoutedEventArgs e)
        {
            bool isPt = LocalizationService.Instance.IsPtBr;

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
                MessageBox.Show(
                    isPt ? "Chaves de bypass do Windows 11 aplicadas com sucesso no Registro do sistema!" : "Windows 11 bypass keys applied successfully to the registry!",
                    "Winside",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                TxtGlobalStatus.Text = isPt ? "Chaves de bypass aplicadas." : "Bypass registry keys applied.";
            }
            else
            {
                MessageBox.Show(
                    isPt ? "Falha ao aplicar algumas chaves de registro. Verifique se o aplicativo foi executado como Administrador." : "Failed to apply some registry keys. Make sure the app was launched as Administrator.",
                    isPt ? "Erro" : "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void BtnVerifyRegistry_Click(object sender, RoutedEventArgs e)
        {
            bool isPt = LocalizationService.Instance.IsPtBr;
            bool active = RegistryBypassService.CheckIfBypassActive();

            if (active)
            {
                MessageBox.Show(
                    isPt ? "Chaves de bypass ativas detectadas no Registro do sistema." : "Active bypass keys detected in the system registry.",
                    isPt ? "Status do Registro" : "Registry Status",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                LoggerService.Instance.LogInfo(isPt ? "Verificação de registro: Chaves de bypass estão ATIVAS." : "Registry check: Bypass keys are ACTIVE.");
            }
            else
            {
                MessageBox.Show(
                    isPt ? "Nenhuma chave de bypass detectada (o sistema está nos padrões originais da Microsoft)." : "No bypass keys detected (system is using Microsoft default configuration).",
                    isPt ? "Status do Registro" : "Registry Status",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                LoggerService.Instance.LogInfo(isPt ? "Verificação de registro: Nenhuma chave de bypass ativa." : "Registry check: No bypass keys active.");
            }
        }

        private void BtnResetBypasses_Click(object sender, RoutedEventArgs e)
        {
            bool isPt = LocalizationService.Instance.IsPtBr;

            var result = MessageBox.Show(
                isPt ? "Tem certeza de que deseja remover todas as chaves de bypass e restaurar os padrões originais do Windows?" : "Are you sure you want to remove all bypass keys and restore Windows defaults?",
                isPt ? "Confirmar Restauração" : "Confirm Reset",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                RegistryBypassService.ResetBypasses();
                MessageBox.Show(
                    isPt ? "As chaves de bypass foram removidas com sucesso." : "Bypass keys removed successfully.",
                    "Winside",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                TxtGlobalStatus.Text = isPt ? "Chaves de bypass restauradas aos padrões." : "Bypass keys reset to defaults.";
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
            bool isPt = LocalizationService.Instance.IsPtBr;

            var ofd = new OpenFileDialog
            {
                Filter = isPt ? "Imagem ISO do Windows (*.iso)|*.iso|Todos os Arquivos (*.*)|*.*" : "Windows ISO Image (*.iso)|*.iso|All Files (*.*)|*.*",
                Title = isPt ? "Selecionar Arquivo ISO do Windows 11" : "Select Windows 11 ISO Image File"
            };

            if (ofd.ShowDialog() == true)
            {
                TxtIsoPath.Text = ofd.FileName;
                string isoName = Path.GetFileNameWithoutExtension(ofd.FileName);
                string desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
                TxtExtractDest.Text = Path.Combine(desktop, isoName);
                LoggerService.Instance.LogInfo($"ISO: {ofd.FileName}");
            }
        }

        private void BtnBrowseDest_Click(object sender, RoutedEventArgs e)
        {
            bool isPt = LocalizationService.Instance.IsPtBr;

            var ofd = new OpenFolderDialog
            {
                Title = isPt ? "Selecionar Pasta de Destino da Extração" : "Select Extraction Destination Folder",
                Multiselect = false
            };

            if (ofd.ShowDialog() == true)
            {
                TxtExtractDest.Text = ofd.FolderName;
                LoggerService.Instance.LogInfo($"Destination: {ofd.FolderName}");
            }
        }

        private async void BtnExtractIso_Click(object sender, RoutedEventArgs e)
        {
            bool isPt = LocalizationService.Instance.IsPtBr;
            string isoPath = TxtIsoPath.Text.Trim();
            string destPath = TxtExtractDest.Text.Trim();

            if (string.IsNullOrWhiteSpace(isoPath) || !File.Exists(isoPath))
            {
                MessageBox.Show(
                    isPt ? "Por favor, selecione um arquivo ISO válido do Windows 11 primeiro." : "Please select a valid Windows 11 ISO file first.",
                    isPt ? "ISO Não Encontrada" : "ISO Not Found",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(destPath))
            {
                MessageBox.Show(
                    isPt ? "Por favor, especifique uma pasta de destino para a extração." : "Please specify a destination folder for extraction.",
                    isPt ? "Destino Inválido" : "Invalid Destination",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            BtnExtractIso.IsEnabled = false;
            BtnCancelExtract.IsEnabled = true;
            ExtractProgressPanel.Visibility = Visibility.Visible;
            ExtractProgressBar.Value = 0;
            TxtExtractStatus.Text = isPt ? "Montando imagem ISO..." : "Mounting ISO image...";
            TxtExtractPercent.Text = "0%";
            TxtGlobalStatus.Text = isPt ? "Montando e extraindo arquivos da ISO..." : "Mounting and extracting ISO files...";

            _extractCts = new CancellationTokenSource();

            try
            {
                string? drive = await IsoManagerService.MountIsoAsync(isoPath);
                if (string.IsNullOrWhiteSpace(drive))
                {
                    MessageBox.Show(
                        isPt ? "Falha ao montar a imagem ISO ou identificar a unidade." : "Failed to mount ISO image or identify virtual drive.",
                        isPt ? "Erro na Montagem" : "Mount Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
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
                        MessageBox.Show(
                            isPt ? $"Extração da ISO concluída com sucesso!\r\nExecutável de instalação pronto:\r\n{_detectedSetupExe}" : $"ISO extraction completed successfully!\r\nSetup executable ready:\r\n{_detectedSetupExe}",
                            isPt ? "Extração Concluída" : "Extraction Complete",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);
                        TxtGlobalStatus.Text = isPt ? "Extração da ISO finalizada com sucesso." : "ISO extraction finished successfully.";
                    }
                    else
                    {
                        MessageBox.Show(
                            isPt ? "A extração foi finalizada, mas nenhum executável (setupprep.exe ou setup.exe) foi encontrado na pasta de destino." : "Extraction finished, but no executable (setupprep.exe or setup.exe) was found in destination.",
                            isPt ? "Aviso" : "Warning",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning);
                    }
                }
            }
            catch (Exception ex)
            {
                LoggerService.Instance.LogError($"Extraction error: {ex.Message}");
                MessageBox.Show(
                    isPt ? $"Ocorreu um erro durante a extração:\r\n{ex.Message}" : $"An error occurred during extraction:\r\n{ex.Message}",
                    isPt ? "Erro" : "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
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
            bool isPt = LocalizationService.Instance.IsPtBr;
            _extractCts?.Cancel();
            TxtExtractStatus.Text = isPt ? "Cancelando extração..." : "Canceling extraction...";
            LoggerService.Instance.LogWarning("Extraction cancel requested by user.");
        }

        private void BtnLaunchSetup_Click(object sender, RoutedEventArgs e)
        {
            bool isPt = LocalizationService.Instance.IsPtBr;
            string destPath = TxtExtractDest.Text.Trim();
            string? setupExe = _detectedSetupExe ?? IsoManagerService.FindSetupExecutable(destPath);

            if (string.IsNullOrWhiteSpace(setupExe) || !File.Exists(setupExe))
            {
                var prompt = MessageBox.Show(
                    isPt ? "Executável do instalador não encontrado na pasta de destino.\r\nDeseja procurar manualmente pelo setup.exe ou setupprep.exe?" : "Setup executable not found in destination folder.\r\nWould you like to browse manually for setup.exe or setupprep.exe?",
                    isPt ? "Instalador Não Encontrado" : "Setup Not Found",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (prompt == MessageBoxResult.Yes)
                {
                    var ofd = new OpenFileDialog
                    {
                        Filter = isPt ? "Executável de Instalação (setup.exe, setupprep.exe)|*.exe" : "Setup Executable (setup.exe, setupprep.exe)|*.exe",
                        Title = isPt ? "Selecionar Executável de Instalação do Windows" : "Select Windows Setup Executable"
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

            string confirmMsg = isPt
                ? mode switch
                {
                    SetupMode.ServerUpgradeBypass => "Iniciar o instalador do Windows 11 no Modo Servidor (/product server)?\r\n\r\nEste método contorna as verificações de hardware para atualizações diretas com total segurança.",
                    SetupMode.StandardSetup       => "Iniciar a instalação do Windows 11 no Modo Padrão?",
                    SetupMode.CustomArgs          => $"Iniciar a instalação do Windows 11 com os argumentos personalizados:\r\n'{TxtCustomArgs.Text}'?",
                    _                             => "Iniciar o instalador do Windows 11?"
                }
                : mode switch
                {
                    SetupMode.ServerUpgradeBypass => "Launch Windows 11 setup in Server Mode (/product server)?\r\n\r\nThis method safely bypasses hardware gates for in-place upgrades.",
                    SetupMode.StandardSetup       => "Launch Windows 11 setup in Standard Mode?",
                    SetupMode.CustomArgs          => $"Launch Windows 11 setup with custom arguments:\r\n'{TxtCustomArgs.Text}'?",
                    _                             => "Launch Windows 11 setup?"
                };

            var confirm = MessageBox.Show(confirmMsg, isPt ? "Confirmar Inicialização" : "Confirm Launch", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (confirm == MessageBoxResult.Yes)
            {
                bool launched = SetupLauncherService.LaunchSetup(setupExe, mode, TxtCustomArgs.Text);
                if (launched)
                {
                    TxtGlobalStatus.Text = isPt ? "Instalador do Windows 11 iniciado com sucesso!" : "Windows 11 setup launched successfully!";
                    MessageBox.Show(
                        isPt ? "O instalador do Windows 11 foi iniciado.\r\n\r\nApós concluir toda a instalação, você poderá excluir com segurança a pasta de extração para liberar espaço." : "Windows 11 setup has been launched.\r\n\r\nAfter completing the installation, you may safely delete the extracted folder to free up disk space.",
                        isPt ? "Instalador em Execução" : "Setup Running",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
            }
        }

        #endregion

        #region Logs

        private void BtnCopyLogs_Click(object sender, RoutedEventArgs e)
        {
            bool isPt = LocalizationService.Instance.IsPtBr;
            var sb = new StringBuilder();
            foreach (var entry in LoggerService.Instance.Entries)
            {
                sb.AppendLine($"[{entry.FormattedTime}] [{entry.Level}] {entry.Message}");
            }

            try
            {
                Clipboard.SetText(sb.ToString());
                MessageBox.Show(
                    isPt ? "Logs copiados para a área de transferência com sucesso." : "Logs copied to clipboard successfully.",
                    isPt ? "Copiado" : "Copied",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    isPt ? $"Falha ao copiar logs: {ex.Message}" : $"Failed to copy logs: {ex.Message}",
                    isPt ? "Erro" : "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void BtnOpenLogFile_Click(object sender, RoutedEventArgs e)
        {
            bool isPt = LocalizationService.Instance.IsPtBr;
            string path = LoggerService.Instance.LogFilePath;
            if (File.Exists(path))
            {
                try
                {
                    Process.Start(new ProcessStartInfo("notepad.exe", $"\"{path}\"") { UseShellExecute = true });
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        isPt ? $"Não foi possível abrir o arquivo de log: {ex.Message}" : $"Could not open log file: {ex.Message}",
                        isPt ? "Erro" : "Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
            }
            else
            {
                MessageBox.Show(
                    isPt ? $"O arquivo de log ainda não foi criado em:\r\n{path}" : $"Log file not created yet at:\r\n{path}",
                    isPt ? "Aviso" : "Warning",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
        }

        private void BtnClearLogs_Click(object sender, RoutedEventArgs e)
        {
            bool isPt = LocalizationService.Instance.IsPtBr;
            LoggerService.Instance.Entries.Clear();
            LoggerService.Instance.LogInfo(isPt ? "Console de logs limpo." : "Log console cleared.");
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
