using System;
using System.Windows;
using System.Windows.Threading;
using Winside.Services;

namespace Winside
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            DispatcherUnhandledException += App_DispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        }

        private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            LoggerService.Instance.LogError($"Unhandled UI Exception: {e.Exception}");
            MessageBox.Show($"An unexpected error occurred:\n{e.Exception.Message}", "Winside Error", MessageBoxButton.OK, MessageBoxImage.Error);
            e.Handled = true;
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is Exception ex)
            {
                LoggerService.Instance.LogError($"Fatal Application Exception: {ex}");
            }
        }
    }
}
