using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel;

namespace MatthiasToolbox.Indexer.Interfaces
{
    [ServiceContract(Namespace = "http://MatthiasToolbox.Indexer.Interfaces", SessionMode=SessionMode.Required, CallbackContract=typeof(IUpdateCallback))]
    public interface IUpdateService
    {
        /// <summary>
        /// Determine if the service is currently performing an update.
        /// </summary>
        /// <returns>True if the service is currently performing an update.</returns>
        [OperationContract]
        bool IsUpdateRunning();

        /// <summary>
        /// Start an update if possible.
        /// </summary>
        /// <returns>success flag</returns>
        [OperationContract]
        bool StartUpdate();

        /// <summary>
        /// Request cancellation of the current updating process if applicable.
        /// </summary>
        [OperationContract(IsOneWay = true)]
        void CancelUpdate();

        /// <summary>
        /// This operation contract is not One-Way because we want to 
        /// be sure cleanup is finished before doing other stuff.
        /// </summary>
        [OperationContract]
        void Cleanup();

        //[OperationContract (IsInitiating=true)]
        //void Open();

        //[OperationContract(IsTerminating=true, IsOneWay=true)]
        //void Close();
    }
}
