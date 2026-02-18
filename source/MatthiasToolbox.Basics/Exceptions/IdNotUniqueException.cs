using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MatthiasToolbox.Basics.Exceptions
{
    public class IdNotUniqueException : Exception
    {
        public IdNotUniqueException(string id, object other, string message = "") 
            : base("The ID " + id +
            " was already given to " + other.ToString() + ".\n" + message)
        { }

        public IdNotUniqueException(string id, string message = "")
            : base("The ID " + id +
            " was already given to another object.\n" + message)
        { }

        public IdNotUniqueException(int id, object other, string message = "")
            : base("The ID " + id.ToString() +
            " was already given to " + other.ToString() + ".\n" + message)
        { }

        public IdNotUniqueException(int id, string message = "")
            : base("The ID " + id.ToString() +
            " was already given to another object.\n" + message)
        { }

        public IdNotUniqueException(long id, object other, string message = "")
            : base("The ID " + id.ToString() +
            " was already given to " + other.ToString() + ".\n" + message)
        { }

        public IdNotUniqueException(long id, string message = "")
            : base("The ID " + id.ToString() +
            " was already given to another object.\n" + message)
        { }
    }
}
