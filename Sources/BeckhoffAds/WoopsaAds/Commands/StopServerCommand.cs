using System.Windows.Input;

namespace WoopsaAds.Commands
{
    /// <summary>
    /// Hides the main window.
    /// </summary>
    public class StopServerCommand : CommandBase<StopServerCommand>
    {
        public override void Execute(object parameter)
        {
            (GetTaskbarWindow(parameter)).controller.ShutDown();
            CommandManager.InvalidateRequerySuggested();
        }
        
        public override bool CanExecute(object parameter)
        {
            MainWindow win = GetTaskbarWindow(parameter);
            return win != null && win.controller.isRunning;
        }
    }
}