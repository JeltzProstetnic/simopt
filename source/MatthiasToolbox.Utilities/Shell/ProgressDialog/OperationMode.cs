namespace MatthiasToolbox.Utilities.Shell.ProgressDialog
{
    /// <summary>Indicates opeartions mode.</summary>
    public enum OperationMode : uint
    {
        None = 0x0,
        /// <summary>Indicates operation is running.</summary>
        Run = 0x1,
        /// <summary>Indicates pre-flight mode, calculating operation time.</summary>
        Preflight = 0x2,
        /// <summary>Indicates operation is rolling back, undo has been selected.</summary>
        Undoing = 0x4,
        /// <summary>Indicates error dialogs are blocking progress from completing.</summary>
        ErrorsBlocking = 0x8,
        /// <summary>Indicates length of the operation is indeterminate.</summary
        /// <remarks>Don't show a timer, progressbar is in marquee mode.</remarks>
        Indeterminate = 0x10
    }
}