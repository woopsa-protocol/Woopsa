using System.Windows.Input;

namespace WoopsaAds.Commands
{
    /// <summary>
    /// Shows the main window.
    /// </summary>
    public class ShowDiagnosticWindowCommand : CommandBase<ShowDiagnosticWindowCommand>
    {
        public override void Execute(object parameter)
        {
            GetTaskbarWindow(parameter).diagnostic.Show();
            CommandManager.InvalidateRequerySuggested();
            GetTaskbarWindow(parameter).diagnostic.Focus();
        }

        public override bool CanExecute(object parameter)
        {
            return true;
        }
    }
}