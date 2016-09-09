using System.Windows.Input;

namespace WoopsaAds.Commands
{
    /// <summary>
    /// Hides the main window.
    /// </summary>
    public class StartServerCommand : CommandBase<StartServerCommand>
    {
        public override void Execute(object parameter)
        {
            (GetTaskbarWindow(parameter)).controller.Restart();
            CommandManager.InvalidateRequerySuggested();
        }
        
        public override bool CanExecute(object parameter)
        {
            MainWindow win = GetTaskbarWindow(parameter);
            return win != null && !win.controller.isRunning;
        }
    }
}