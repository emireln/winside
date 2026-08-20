using System;

namespace Winside.Models
{
    public enum LogLevel
    {
        Info,
        Success,
        Warning,
        Error
    }

    public class LogEntry
    {
        public DateTime Timestamp { get; } = DateTime.Now;
        public string Message { get; set; } = string.Empty;
        public LogLevel Level { get; set; } = LogLevel.Info;

        public string FormattedTime => Timestamp.ToString("HH:mm:ss");

        public string LevelColor => Level switch
        {
            LogLevel.Success => "#2ecc71",
            LogLevel.Warning => "#f39c12",
            LogLevel.Error   => "#e74c3c",
            _                => "#95a5a6"
        };
    }
}
