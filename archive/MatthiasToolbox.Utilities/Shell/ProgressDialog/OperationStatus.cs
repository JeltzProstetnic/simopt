namespace MatthiasToolbox.Utilities.Shell.ProgressDialog
{
    /// <summary>Provides operation status values.</summary>
    public enum OperationStatus : uint
    {
        /// <summary>Operation is running, no user intervention.</summary>
        Running = 0x1,
        /// <summary>Operation has been paused by the user.</summary>
        Paused = 0x2,
        /// <summary>Operation has been canceled by the user - now go undo.</summary>
        Cancelled = 0x3,
        /// <summary>Operation has been stopped by the user - terminate completely.</summary>
        Stopped = 0x4,
        /// <summary>Operation has gone as far as it can go without throwing error dialogs.</summary>
        Errors = 0x5
    }
}