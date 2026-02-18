namespace MatthiasToolbox.Utilities.Shell.ProgressDialog
{
    /// <summary>Describes an action being performed.</summary>
    public enum OperationAction : uint
    {
        /// <summary>No action is being performed.</summary>
        None = 0,
        /// <summary>Files are being moved.</summary>
        Moving = (None + 1),
        /// <summary>Files are being copied.</summary>
        Copying = (Moving + 1),
        /// <summary>Files are being deleted.</summary>
        Recycling = (Copying + 1),
        /// <summary>A set of attributes are being applied to files.</summary>
        ApplyingAttributes = (Recycling + 1),
        /// <summary>A file is being downloaded from a remote source.</summary>
        Downloading = (ApplyingAttributes + 1),
        /// <summary>An Internet search is being performed.</summary>
        SearchingInternet = (Downloading + 1),
        /// <summary>A calculation is being performed.</summary>
        Calculating = (SearchingInternet + 1),
        /// <summary>A file is being uploaded to a remote source.</summary>
        Uploading = (Calculating + 1),
        /// <summary>A local search is being performed.</summary>
        SearchingFiles = (Uploading + 1),
        /// <summary>A deletion is being performed.</summary>
        Deleting = (SearchingFiles + 1),
        /// <summary>A renaming action is being performed.</summary>
        Renaming = (Deleting + 1),
        /// <summary>A formatting action is being performed.</summary>
        Formatting = (Renaming + 1)
    }
}