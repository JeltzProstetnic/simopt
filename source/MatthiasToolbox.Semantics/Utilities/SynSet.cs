using System;
using System.Collections.Generic;
using MatthiasToolbox.Semantics.Interfaces;
using MatthiasToolbox.Semantics.Model;

namespace MatthiasToolbox.Semantics.Utilities
{
    public class SynSet
    {
        private readonly Dictionary<string, ITerm> _terms;

        public IEnumerable<string> MemberNames { get { foreach (ITerm t in _terms.Values) yield return t.Name; } }

        public IEnumerable<ITerm> Members { get { return _terms.Values; } }

        public ITerm this[string name]
        {
            get
            {
                string trimmedName = name.Trim();
                if (_terms == null || String.IsNullOrEmpty(trimmedName) || !_terms.ContainsKey(trimmedName)) return null;
                else return _terms[trimmedName];
            }
            set
            {
                string trimmedName = name.Trim();
                if (_terms == null || String.IsNullOrEmpty(trimmedName) || _terms.ContainsKey(trimmedName))
                    return;

                _terms[trimmedName] = value;
            }
        }

        public SynSet() { _terms = new Dictionary<string, ITerm>(); }

        public SynSet(params ITerm[] terms)
            : this()
        {
            foreach (ITerm t in terms) this._terms[t.Name] = t;
        }

        /// <summary>
        /// TODO  Semantics - comment
        /// </summary>
        /// <param name="term"></param>
        /// <returns></returns>
        public bool Add(Term term) 
        {
            if (_terms.ContainsKey(term.Name) || term.SynSet != null) return false;
            _terms[term.Name] = term;
            term.SynSet = this;
            return true;
        }
    }
}