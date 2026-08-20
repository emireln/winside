using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using System.Windows;
using Winside.Models;

namespace Winside.Services
{
    public class LoggerService
    {
        private static readonly Lazy<LoggerService> _instance = new(() => new LoggerService());
        public static LoggerService Instance => _instance.Value;

        public ObservableCollection<LogEntry> Entries { get; } = new();
        public string LogFilePath { get; }

        private readonly object _fileLock = new();

        private LoggerService()
        {
            string desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            LogFilePath = Path.Combine(desktop, "Winside_install_log.txt");

            try
            {
                lock (_fileLock)
                {
                    File.WriteAllText(LogFilePath, "=== Emir Tech Tools - Winside Deployment Suite Log ===\r\n\r\n", Encoding.UTF8);
                }
            }
            catch
            {
                // Fallback to local app data if desktop is not writable
                string localApp = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                LogFilePath = Path.Combine(localApp, "Winside_install_log.txt");
            }
        }

        public void Log(string message, LogLevel level = LogLevel.Info)
        {
            var entry = new LogEntry
            {
                Message = message,
                Level = level
            };

            // Post to UI thread
            Application.Current?.Dispatcher.BeginInvoke(() =>
            {
                Entries.Add(entry);
                // Keep max 1000 items in memory
                if (Entries.Count > 1000)
                {
                    Entries.RemoveAt(0);
                }
            });

            // Write to file asynchronously / safely
            string line = $"[{entry.Timestamp:yyyy-MM-dd HH:mm:ss}] [{level.ToString().ToUpper()}] {message}\r\n";
            try
            {
                lock (_fileLock)
                {
                    File.AppendAllText(LogFilePath, line, Encoding.UTF8);
                }
            }
            catch
            {
                // Suppress disk write error in logger
            }
        }

        public void LogInfo(string message) => Log(message, LogLevel.Info);
        public void LogSuccess(string message) => Log(message, LogLevel.Success);
        public void LogWarning(string message) => Log(message, LogLevel.Warning);
        public void LogError(string message) => Log(message, LogLevel.Error);
    }
}
