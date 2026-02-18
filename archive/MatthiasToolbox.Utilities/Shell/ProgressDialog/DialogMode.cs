using System;

namespace MatthiasToolbox.Utilities.Shell.ProgressDialog
{
    [Flags]
    public enum DialogMode : uint
    {
        /// <summary>Normal progress dialog box behavior.</summary>
        Normal = 0x00000000,
        /// <summary>The progress dialog box will be modal to the window specified by hwndParent. By default, a progress dialog box is modeless.</summary>
        Modal = 0x00000001,
        /// <summary>Automatically estimate the remaining time and display the estimate on line 3. </summary>
        /// <remarks>If this flag is set, IProgressDialog.SetLine can be used only to display text on lines 1 and 2.</remarks>
        AutoTime = 0x00000002,
        /// <summary>Do not show the "time remaining" text.</summary>
        NoTime = 0x00000004,
        /// <summary>Do not display a minimize button on the dialog box's caption bar.</summary>
        NoMinimize = 0x00000008,
        /// <summary>Do not display a progress bar.</summary>
        /// <remarks>Typically, an application can quantitatively determine how much of the operation remains and periodically pass that value to IProgressDialog.SetProgress. The progress dialog box uses this information to update its progress bar. This flag is typically set when the calling application must wait for an operation to finish, but does not have any quantitative information it can use to update the dialog box.</remarks>
        NoProgressBar = 0x00000010,
        /// <summary>Use marquee progress.</summary>
        /// <remarks>comctl32 v6 required.</remarks>
        MarqueeProgress = 0x00000020,
        /// <summary>No cancel button (operation cannot be canceled).</summary>
        /// <remarks>Use sparingly.</remarks>
        NoCancel = 0x00000040,
        /// <summary>Add a pause button (operation can be paused)</summary>
        EnablePause = 0x00000080,
        /// <summary>The operation can be undone in the dialog.</summary>
        /// <remarks>The Stop button becomes Undo.</remarks>
        AllowUndo = 0x00000100,
        /// <summary>Don't display the path of source file in progress dialog.</summary>
        DontDisplaySourcePath = 0x00000200,
        /// <summary>Don't display the path of destination file in progress dialog.</summary>
        DontDisplayDestPath = 0x00000400
    }
}