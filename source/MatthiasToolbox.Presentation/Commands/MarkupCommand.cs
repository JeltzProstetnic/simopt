using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;

namespace MatthiasToolbox.Presentation.Commands
{
    /// <summary>
    /// Basic implementation of the <see cref="ICommand"/>
    /// interface, which is also accessible as a markup
    /// extension.
    /// </summary>
    public abstract class MarkupCommand<T> : MarkupExtension, ICommand
        where T : class, ICommand, new()
    {
        #region over

        /// <summary>
        /// Gets a shared command instance.
        /// </summary>
        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            if (command == null) command = new T();
            return command;
        }

        #endregion
        #region cvar

        /// <summary>
        /// A singleton instance.
        /// </summary>
        private static T command;

        #endregion
        #region evnt

        /// <summary>
        /// Fires when changes occur that affect whether
        /// or not the command should execute.
        /// </summary>
        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        #endregion
        #region prop

        public static bool IsDesignMode
        {
            get
            {
                return (bool)
                  DependencyPropertyDescriptor.FromProperty(DesignerProperties.IsInDesignModeProperty, typeof(FrameworkElement))
                      .Metadata.DefaultValue;
            }
        }

        #endregion
        #region impl

        /// <summary>
        /// Defines the method to be called when the command is invoked.
        /// </summary>
        /// <param name="parameter">Data used by the command.
        /// If the command does not require data to be passed,
        /// this object can be set to null.
        /// </param>
        public abstract void Execute(object parameter);

        /// <summary>
        /// Defines the method that determines whether the command
        /// can execute in its current state.
        /// </summary>
        /// <returns>
        /// This default implementation always returns true.
        /// </returns>
        /// <param name="parameter">Data used by the command.  
        /// If the command does not require data to be passed,
        /// this object can be set to null.
        /// </param>
        public virtual bool CanExecute(object parameter)
        {
            return IsDesignMode ? false : true;
        }

        /// <summary>
        /// Resolves the window that owns the given class.
        /// </summary>
        /// <param name="commandParameter"></param>
        /// <returns></returns>
        protected Window GetParentWindow(object commandParameter)
        {
            if (IsDesignMode) return null;
            var tb = commandParameter as DependencyObject;
            return tb == null ? null : tb.FindVisualParentIncludeContentElements<Window>();
        }

        #endregion
    }
}