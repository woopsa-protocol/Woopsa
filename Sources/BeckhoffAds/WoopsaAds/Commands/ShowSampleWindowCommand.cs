using System.Windows.Input;

namespace WoopsaAds.Commands
{
    /// <summary>
    /// Shows the main window.
    /// </summary>
    public class ShowSampleWindowCommand : CommandBase<ShowSampleWindowCommand>
    {
        public override void Execute(object parameter)
        {
            GetTaskbarWindow(parameter).Show();
            CommandManager.InvalidateRequerySuggested();
            GetTaskbarWindow(parameter).Focus();
        }
        
        public override bool CanExecute(object parameter)
        {
            return true;
        }
    }
}