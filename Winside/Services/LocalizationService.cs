using System;

namespace Winside.Services
{
    public enum AppLanguage
    {
        PtBr,
        EnUs
    }

    public class LocalizationService
    {
        private static readonly Lazy<LocalizationService> _instance = new(() => new LocalizationService());
        public static LocalizationService Instance => _instance.Value;

        public AppLanguage CurrentLanguage { get; private set; } = AppLanguage.PtBr;

        public event Action? LanguageChanged;

        public void SetLanguage(AppLanguage language)
        {
            if (CurrentLanguage != language)
            {
                CurrentLanguage = language;
                LanguageChanged?.Invoke();
            }
        }

        public void ToggleLanguage()
        {
            SetLanguage(CurrentLanguage == AppLanguage.PtBr ? AppLanguage.EnUs : AppLanguage.PtBr);
        }

        public bool IsPtBr => CurrentLanguage == AppLanguage.PtBr;
    }
}
