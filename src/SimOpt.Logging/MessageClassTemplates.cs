using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SimOpt.Logging
{
	/// <summary>
	/// A message class for status information. The default severity associated with this class is 0.
	/// </summary>
    public class STATUS { }
    
    /// <summary>
    /// A message class for general information. The default severity associated with this class is 1.
    /// </summary>
    public class INFO { }
    
    /// <summary>
    /// A message class for warnings. The default severity associated with this class is 2.
    /// </summary>
    public class WARN { }
    
    /// <summary>
    /// A message class for errors. The default severity associated with this class is 3.
    /// </summary>
    public class ERROR { }
    
    /// <summary>
    /// A message class for fatal errors. The default severity associated with this class is 4.
    /// </summary>
    public class FATAL { }
}
