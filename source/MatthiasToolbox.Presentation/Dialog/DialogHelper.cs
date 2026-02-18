using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MatthiasToolbox.Logging;
using System.Windows;

namespace MatthiasToolbox.Presentation.Dialog
{
    public class DialogHelper
    {
        /// <summary>
        /// automate some try and retry proccess with a wpf message box
        /// </summary>
        /// <param name="operation"></param>
        /// <param name="operationName"></param>
        /// <param name="failedMessage"></param>
        /// <param name="successStatusMessage"></param>
        /// <param name="failedMessageTitle"></param>
        /// <param name="attemptFailedStatusMessage"></param>
        /// <param name="failedStatusMessage"></param>
        /// <returns></returns>
        public static bool RetryOperation(Func<bool> operation,
            string operationName,
            string failedMessage,
            string failedMessageTitle = null,
            string successStatusMessage = null,
            string attemptFailedStatusMessage = null,
            string failedStatusMessage = null)
        {
            bool success;

            string opName = operationName;
            if (string.IsNullOrEmpty(opName)) opName = "Unknown operation";

            string title = failedMessageTitle;
            if (string.IsNullOrEmpty(title)) title = opName + " failed.";

            // first attempt
            try
            {
                success = operation.Invoke();
            }
            catch (Exception ex)
            {
                Logger.Log("MatthiasToolbox.Presentation.Dialog.DialogHelper", "Error while trying to run " + opName, ex);
                success = false;
            }

            if (!success) // first attempt failed
            {
                if (!string.IsNullOrEmpty(attemptFailedStatusMessage)) Logger.Log<STATUS>(attemptFailedStatusMessage);

                // repeat until user loses interest or patience
                while (MessageBox.Show(failedMessage, failedMessageTitle, MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    try
                    {
                        success = operation.Invoke();
                    }
                    catch (Exception ex)
                    {
                        Logger.Log("MatthiasToolbox.Presentation.Dialog.DialogHelper", "Error while trying to run " + opName, ex);
                        success = false;
                    }
                    if (success) break;
                }
                if (!success && !string.IsNullOrEmpty(failedStatusMessage)) Logger.Log<STATUS>(failedStatusMessage);
                if (success && !string.IsNullOrEmpty(successStatusMessage)) Logger.Log<STATUS>(successStatusMessage);
            }

            // that's it, no way back.
            return success;
        }

        /// <summary>
        /// automate some try and retry proccess with a wpf message box
        /// </summary>
        /// <param name="operation"></param>
        /// <param name="operationName"></param>
        /// <returns></returns>
        public static bool RetryOperation(Func<bool> operation, string operationName)
        {
            return RetryOperation(operation, 
                operationName, 
                operationName + " failed. Do you want to retry?", 
                operationName + " failed.",
                operationName + " succeded.", 
                operationName + " attempt failed.",
                operationName + " failed.");
        }
    }
}
