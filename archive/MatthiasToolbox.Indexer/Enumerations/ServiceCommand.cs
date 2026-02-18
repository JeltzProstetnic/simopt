using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MatthiasToolbox.Indexer.Enumerations
{
    /// <summary>
    /// Commands which can be sent to the service application.
    /// </summary>
    public enum ServiceCommand
    {
        /// <summary>
        /// Test if the service is responsive.
        /// </summary>
        Test = 129,

        /// <summary>
        /// Update the index.
        /// </summary>
        Update = 130,

        /// <summary>
        /// Cleanup the database.
        /// </summary>
        Cleanup = 131,

        /// <summary>
        /// Shut the service down.
        /// </summary>
        Shutdown = 132,

        /// <summary>
        /// Stop indexing.
        /// </summary>
        Cancel = 133
    }
}