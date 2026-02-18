namespace MatthiasToolbox.Utilities.Shell.ProgressDialog
{
    /// <summary>
    /// Flags that indicate the action to be taken by the IProgressDialog.Timer() method.
    /// </summary>
    public enum TimerAction: uint
	{
		/// <summary>Resets the timer to zero. Progress will be calculated from the time this method is called.</summary>
		Reset = 0x01,
		/// <summary>Progress has been suspended.</summary>
		Pause = 0x02,
		/// <summary>Progress has been resumed.</summary>
		Resume = 0x03
	}
}