using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;

namespace MatthiasToolbox.Utilities.Shell.ProgressDialog
{
    /// <summary>Displays a progress dialog box that shows an action being performed on items.</summary>
    public class OperationsProgressDialog : ProgressDialog
    {
        private readonly IOperationsProgressDialog instance;
        private OperationAction action;
        private OperationMode mode;
        private ulong size;
        private ulong totalSize;
        private ulong count;
        private ulong totalCount;
        private string sourcePath;
        private string targetPath;

        public OperationsProgressDialog()
            : base()
        {
            if (!Environment.OSVersion.IsAtLeastWindowsVista()) throw new InvalidOperationException("The IOperationsProgressDialog interface is only avaliable for Windows Vista and above.");
            instance = (IOperationsProgressDialog)coclassObject;
        }

        /// <summary>Gets or sets which progress dialog operation is occuring.</summary>
        public OperationAction Action
        {
            get
            {
                return action;
            }
            set
            {
                action = value;
                if (IsVisible)
                    instance.SetOperation(action);
            }
        }

        /// <summary>Gets or sets progress dialog operations mode.</summary>
        public OperationMode Mode
        {
            get
            {
                return mode;
            }
            set
            {
                mode = value;
                if (IsVisible)
                    instance.SetMode(mode);
            }
        }

        /// <summary>Gets operation status for progress dialog.</summary>
        public OperationStatus Status
        {
            get
            {
                OperationStatus value;
                instance.GetOperationStatus(out value);
                return value;
            }
        }

        /// <summary>
        /// Gets or sets a value that indicates the operation
        /// can be paused and adds a pause button.
        /// </summary>
        public bool HasPauseButton
        {
            get { return GetOption((uint)DialogMode.EnablePause); }
            set { SetOption((uint)DialogMode.EnablePause, value); }
        }

        /// <summary>
        /// Gets or sets a value that indicates the operation
        /// can be undone in the dialog or not.
        /// </summary>
        public bool AllowsUndo
        {
            get { return GetOption((uint)DialogMode.AllowUndo); }
            set { SetOption((uint)DialogMode.AllowUndo, value); }
        }

        /// <summary>
        /// Gets or sets the source shell item path or parsing name
        /// to generate from/to display for the progress dialog.
        /// </summary>
        public string SourcePath
        {
            get
            {
                return sourcePath;
            }
            set
            {
                sourcePath = value;
                UpdateLocations();
            }
        }

        /// <summary>
        /// Gets or sets the target shell item path or parsing name
        /// to generate from/to display for the progress dialog.
        /// </summary>
        public string TargetPath
        {
            get
            {
                return targetPath;
            }
            set
            {
                targetPath = value;
                UpdateLocations();
            }
        }

        /// <summary>
        /// Gets or sets a value that indicates whether the path
        /// of source item should be displayed in the progress dialog.
        /// </summary>
        public bool DisplaysSourcePath
        {
            get { return !GetOption((uint)DialogMode.DontDisplaySourcePath); }
            set { SetOption((uint)DialogMode.DontDisplaySourcePath, !value); }
        }

        /// <summary>
        /// Gets or sets a value that indicates whether the path
        /// of target item should be displayed in the progress dialog.
        /// </summary>
        public bool DisplaysTargetPath
        {
            get { return !GetOption((uint)DialogMode.DontDisplayDestPath); }
            set { SetOption((uint)DialogMode.DontDisplayDestPath, !value); }
        }

        /// <summary>
        /// Always true.
        /// </summary>
        public override bool EstimatesTimeRemaining
        {
            get
            {
                return true;
            }
            set
            {
            }
        }

        public override bool ProgressBarIsMarquee
        {
            get
            {
                return Mode == OperationMode.Indeterminate;
            }
            set
            {
                Mode = value ? OperationMode.Indeterminate : OperationMode.Run;
            }
        }

        public override bool IsCancelled
        {
            get { return Status == OperationStatus.Cancelled; }
        }

        /// <summary>Gets or sets the current size in bytes, used for showing progress in bytes.</summary>
        public ulong Size
        {
            get
            {
                return size;
            }
            set
            {
                size = value;
                UpdateProgress();
            }
        }

        /// <summary>Gets or sets the total size in bytes, used for showing progress in bytes.</summary>
        public ulong TotalSize
        {
            get
            {
                return totalSize;
            }
            set
            {
                totalSize = value;
                UpdateProgress();
            }
        }

        /// <summary>Gets or sets the number of current items, used for showing progress in items.</summary>
        public ulong Count
        {
            get
            {
                return count;
            }
            set
            {
                count = value;
                UpdateProgress();
            }
        }

        /// <summary>Gets or sets the total number of items, used for showing progress in items.</summary>
        public ulong TotalCount
        {
            get
            {
                return totalCount;
            }
            set
            {
                totalCount = value;
                UpdateProgress();
            }
        }

        public override void Reset()
        {
            base.Reset();
            action = OperationAction.None;
            mode = OperationMode.None;
            size = 0ul;
            totalSize = 0ul;
            count = 0ul;
            totalCount = 0ul;
        }

        protected override void RunDialogImpl(IntPtr hwndOwner)
        {
            instance.StartProgressDialog(hwndOwner, (DialogMode)Options);
            if (action != OperationAction.None)
                instance.SetOperation(action);
            if (mode != OperationMode.None)
                instance.SetMode(mode);
            UpdateLocations();
            UpdateProgress();
        }

        protected override void UpdateProgress()
        {
            if (IsVisible)
                instance.UpdateProgress(Value, TotalValue, Size, TotalSize, Count, TotalCount);
        }

        private void UpdateLocations()
        {
            if (!IsVisible)
                return;

            var sourceItem = IntPtr.Zero;
            var targetItem = IntPtr.Zero;
            try
            {
                var interfaceId = new Guid(IID_IShellItem);

                SHCreateItemFromParsingName(sourcePath, IntPtr.Zero, ref interfaceId, out sourceItem);
                SHCreateItemFromParsingName(targetPath, IntPtr.Zero, ref interfaceId, out targetItem);

                instance.UpdateLocations(sourceItem, targetItem, IntPtr.Zero);
            }
            finally
            {
                if (sourceItem != IntPtr.Zero)
                    Marshal.Release(sourceItem);
                if (targetItem != IntPtr.Zero)
                    Marshal.Release(targetItem);
            }
        }

        private const string IID_IShellItem = "43826D1E-E718-42EE-BC55-A1E261C37BFE";

        [DllImport("shell32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern uint SHCreateItemFromParsingName(
                [MarshalAs(UnmanagedType.LPWStr)] string path,
                IntPtr pbc,
                ref Guid riid,
                out IntPtr shellItem);
    }
}