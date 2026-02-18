using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MatthiasToolbox.Basics.Interfaces;
using MatthiasToolbox.Basics.Datastructures.Collections;
using MatthiasToolbox.Semantics.Enumerations;

namespace MatthiasToolbox.Semantics.Interfaces
{
    public interface IRule : INamedElement
    {
        RuleContext ContextType { get; }
        INamedElement Context { get; }
        ReadOnlyDictionary<string, Type> Signature { get; }
        Type GetParameterType(string parameterName);
        bool Evaluate(IEnumerable<Tuple<string, object>> parameters);
    }
}