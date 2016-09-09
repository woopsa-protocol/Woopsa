using System.Threading;
using System.Windows;
using System.Windows.Threading;

namespace WoopsaAds
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public WoopsaAdsController controller;

        public static object appLock = new object();
        public static bool isExiting = false;
        protected override void OnStartup(StartupEventArgs e)
        {
            Current.DispatcherUnhandledException += OnDispatcherUnhandledException;
            base.OnStartup(e);
            controller = new WoopsaAdsController();
            if (controller.runAtStartUp)
                Thread.Sleep(5000);
            MainWindow = new MainWindow(controller);
            ((MainWindow)MainWindow).InitWindow();
            controller.Load();
        }

        public void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            string errorMessage = string.Format("An unhandled exception occurred: {0}", e.Exception.Message);
            MessageBox.Show(errorMessage, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            e.Handled = true;
            ((MainWindow)MainWindow).diagnostic.AddException(e.Exception);
        }

        protected override void OnExit(ExitEventArgs e)
        {
                base.OnExit(e);
                lock (appLock)
                {
                    isExiting = true;
                }
                controller.ShutDown();
        }
    }
}
