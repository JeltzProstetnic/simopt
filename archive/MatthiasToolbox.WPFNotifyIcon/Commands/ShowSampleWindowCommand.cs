using System.Windows;
using System.Windows.Input;
using MatthiasToolbox.Presentation.Commands;

namespace MatthiasToolbox.WPFNotifyIcon.Commands
{
    /// <summary>
    /// Shows the main window.
    /// </summary>
    public class ShowSampleWindowCommand : MarkupCommand<ShowSampleWindowCommand>// CommandBase<ShowSampleWindowCommand>
    {
        public override void Execute(object parameter)
        {
            // GetTaskbarWindow(parameter).Show();
            GetParentWindow(parameter).Show();
            CommandManager.InvalidateRequerySuggested();
        }


        public override bool CanExecute(object parameter)
        {
            // Window win = GetTaskbarWindow(parameter);
            Window win = GetParentWindow(parameter);
            return win != null && !win.IsVisible;
        }
    }
}