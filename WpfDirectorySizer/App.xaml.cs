using System.Windows;
using System.Windows.Threading;

namespace WpfDirectorySizer
{
    public partial class App : Application
    {
        // Global Exception handler in case some unhandleds get through
        void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            MessageBox.Show(e.Exception.Message, "Exception", MessageBoxButton.OK, MessageBoxImage.Exclamation);
            e.Handled = true;
        }
    }
}
