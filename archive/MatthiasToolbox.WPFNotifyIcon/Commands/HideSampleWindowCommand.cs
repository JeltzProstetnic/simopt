using System.Windows;
using System.Windows.Input;
using MatthiasToolbox.Presentation.Commands;

namespace MatthiasToolbox.WPFNotifyIcon.Commands
{
    /// <summary>
    /// Hides the main window.
    /// </summary>
    public class HideSampleWindowCommand : MarkupCommand<HideSampleWindowCommand>
    {

        public override void Execute(object parameter)
        {
            GetParentWindow(parameter).Hide();
            CommandManager.InvalidateRequerySuggested();
        }


        public override bool CanExecute(object parameter)
        {
            Window win = GetParentWindow(parameter);
            return win != null && win.IsVisible;
        }


    }
}