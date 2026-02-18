using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel;
using MatthiasToolbox.Indexer.Service;

namespace MatthiasToolbox.Indexer.Interfaces
{
    public interface IUpdateCallback
    {
        [OperationContract(IsOneWay = true)]
        void ReportProgress(int progressPercentage, ulong currentItemSize);

        [OperationContract(IsOneWay = true)]
        void NotifyStart(ulong numberOfFiles, ulong totalSize);

        [OperationContract(IsOneWay = true)]
        void NotifyFinish(bool wasCancelled);
    }
}
