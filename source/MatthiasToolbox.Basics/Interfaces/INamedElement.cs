using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace MatthiasToolbox.Basics.Interfaces
{
    public interface INamedElement
    {
        [XmlAttribute("Name")]
        string Name { get; }
    }
}