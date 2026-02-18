using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

namespace MatthiasToolbox.Utilities.Shell.ProgressDialog
{
    [ComImport]
    [Guid("EBBC7C04-315E-11d2-B62F-006097DF5BD4")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface IProgressDialog
    {

        /// <summary>
        /// Starts the progress dialog box.
        /// </summary>
        /// <param name="hwndParent">A handle to the dialog box's parent window.</param>
        /// <param name="punkEnableModless">Reserved. Set to null.</param>
        /// <param name="dwFlags">Flags that control the operation of the progress dialog box. </param>
        /// <param name="pvResevered">Reserved. Set to IntPtr.Zero</param>
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void StartProgressDialog(
                IntPtr hwndParent, //HWND
                [MarshalAs(UnmanagedType.IUnknown)]    object punkEnableModless, //IUnknown
                DialogMode dwFlags,  //DWORD
                IntPtr pvResevered //LPCVOID
                );

        /// <summary>
        /// Stops the progress dialog box and removes it from the screen.
        /// </summary>
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void StopProgressDialog();

        /// <summary>
        /// Sets the title of the progress dialog box.
        /// </summary>
        /// <param name="pwzTitle">A pointer to a null-terminated Unicode string that contains the dialog box title.</param>
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void SetTitle(
                [MarshalAs(UnmanagedType.LPWStr)] string pwzTitle //LPCWSTR
                );

        /// <summary>
        /// Specifies an Audio-Video Interleaved (AVI) clip that runs in the dialog box. Note: Note  This method is not supported in Windows Vista or later versions.
        /// </summary>
        /// <param name="hInstAnimation">An instance handle to the module from which the AVI resource should be loaded.</param>
        /// <param name="idAnimation">An AVI resource identifier. To create this value, use the MAKEINTRESOURCE macro. The control loads the AVI resource from the module specified by hInstAnimation.</param>
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void SetAnimation(
                IntPtr hInstAnimation, //HINSTANCE
                ushort idAnimation //UINT
                );

        /// <summary>
        /// Checks whether the user has canceled the operation.
        /// </summary>
        /// <returns>TRUE if the user has cancelled the operation; otherwise, FALSE.</returns>
        /// <remarks>
        /// The system does not send a message to the application when the user clicks the Cancel button.
        /// You must periodically use this function to poll the progress dialog box object to determine
        /// whether the operation has been canceled.
        /// </remarks>
        [PreserveSig]
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        [return: MarshalAs(UnmanagedType.Bool)]
        bool HasUserCancelled();

        /// <summary>
        /// Updates the progress dialog box with the current state of the operation.
        /// </summary>
        /// <param name="dwCompleted">An application-defined value that indicates what proportion of the operation has been completed at the time the method was called.</param>
        /// <param name="dwTotal">An application-defined value that specifies what value dwCompleted will have when the operation is complete.</param>
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void SetProgress(
                uint dwCompleted, //DWORD
                uint dwTotal //DWORD
                );

        /// <summary>
        /// Updates the progress dialog box with the current state of the operation.
        /// </summary>
        /// <param name="ullCompleted">An application-defined value that indicates what proportion of the operation has been completed at the time the method was called.</param>
        /// <param name="ullTotal">An application-defined value that specifies what value ullCompleted will have when the operation is complete.</param>
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void SetProgress64(
                ulong ullCompleted, //ULONGLONG
                ulong ullTotal //ULONGLONG
                );

        /// <summary>
        /// Displays a message in the progress dialog.
        /// </summary>
        /// <param name="dwLineNum">The line number on which the text is to be displayed. Currently there are three lines—1, 2, and 3. If the PROGDLG_AUTOTIME flag was included in the dwFlags parameter when IProgressDialog.StartProgressDialog was called, only lines 1 and 2 can be used. The estimated time will be displayed on line 3.</param>
        /// <param name="pwzString">A null-terminated Unicode string that contains the text.</param>
        /// <param name="fCompactPath">TRUE to have path strings compacted if they are too large to fit on a line. The paths are compacted with PathCompactPath.</param>
        /// <param name="pvResevered"> Reserved. Set to IntPtr.Zero.</param>
        /// <remarks>This function is typically used to display a message such as "Item XXX is now being processed." typically, messages are displayed on lines 1 and 2, with line 3 reserved for the estimated time.</remarks>
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void SetLine(
                uint dwLineNum, //DWORD
                [MarshalAs(UnmanagedType.LPWStr)] string pwzString, //LPCWSTR
                [MarshalAs(UnmanagedType.VariantBool)] bool fCompactPath, //BOOL
                IntPtr pvResevered //LPCVOID
                );

        /// <summary>
        /// Sets a message to be displayed if the user cancels the operation.
        /// </summary>
        /// <param name="pwzCancelMsg">A pointer to a null-terminated Unicode string that contains the message to be displayed.</param>
        /// <param name="pvResevered">Reserved. Set to NULL.</param>
        /// <remarks>Even though the user clicks Cancel, the application cannot immediately call
        /// IProgressDialog.StopProgressDialog to close the dialog box. The application must wait until the
        /// next time it calls IProgressDialog.HasUserCancelled to discover that the user has canceled the
        /// operation. Since this delay might be significant, the progress dialog box provides the user with
        /// immediate feedback by clearing text lines 1 and 2 and displaying the cancel message on line 3.
        /// The message is intended to let the user know that the delay is normal and that the progress dialog
        /// box will be closed shortly.
        /// It is typically is set to something like "Please wait while ...". </remarks>
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void SetCancelMsg(
                [MarshalAs(UnmanagedType.LPWStr)] string pwzCancelMsg, //LPCWSTR
                IntPtr pvResevered //LPCVOID
                );

        /// <summary>
        /// Resets the progress dialog box timer to zero.
        /// </summary>
        /// <param name="dwTimerAction">Flags that indicate the action to be taken by the timer.</param>
        /// <param name="pvResevered">Reserved. Set to NULL.</param>
        /// <remarks>
        /// The timer is used to estimate the remaining time. It is started when your application
        /// calls IProgressDialog.StartProgressDialog. Unless your application will start immediately,
        /// it should call Timer just before starting the operation.
        /// This practice ensures that the time estimates will be as accurate as possible. This method
        /// should not be called after the first call to IProgressDialog.SetProgress.</remarks>
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void Timer(
                TimerAction dwTimerAction, //DWORD
                IntPtr pvResevered //LPCVOID
                );
    }
}
