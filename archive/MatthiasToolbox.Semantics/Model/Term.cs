using System.Collections.Generic;
using System.Linq;
using MatthiasToolbox.Semantics.Interfaces;
using MatthiasToolbox.Semantics.Utilities;

namespace MatthiasToolbox.Semantics.Model
{
    public class Term : ITerm
    {
        #region cvar

        private SynSet _synSet;
        private readonly string _name;

        #endregion

        #region prop

        #region ITerm

        /// <summary>
        /// The unique name of this term.
        /// </summary>
        /// <value></value>
        public string Name
        {
            get { return _name; }
        }

        /// <summary>
        /// Retrurns true if this is the preferred / main term in this synset.
        /// </summary>
        /// <value></value>
        public bool IsPreferredTerm { get; set; }

        /// <summary>
        /// The preferred name of this term.
        /// </summary>
        /// <value></value>
        public string PreferredName
        {
            get
            {
                return (from term in Synonyms
                        where term.IsPreferredTerm
                        select term).FirstOrDefault().Name;
            }
        }

        /// <summary>
        /// Retrieve the associated SynSet.
        /// The setter is a one time setter. Once set, the value cannot be changed.
        /// </summary>
        public SynSet SynSet 
        {
            get 
            {
                return _synSet;
            }
            internal set 
            {
                if(value != null && _synSet == null)
                    _synSet = value;
            }
        }

        /// <summary>
        /// All synonyms / aliases for this term. If no synset is
        /// associated this will return null.
        /// Each listed synonym denotes the same as this entry.
        /// </summary>
        /// <value></value>
        public IEnumerable<ITerm> Synonyms
        {
            get 
            {
                if (_synSet != null) return _synSet.Members;
                else return null;
            }
        }

        /// <summary>
        /// All synonyms / aliases for this term including the preferred name.
        /// </summary>
        /// <value></value>
        public IEnumerable<string> Names
        {
            get {
                return Synonyms.Select(t => t.Name);
            }
        }

        /// <summary>
        /// A formal, unique definition of this term.
        /// </summary>
        /// <value></value>
        public string Definition { get; set; }


        /// <summary>
        /// Each listed coordinate term shares a hypernym with this entry.
        /// </summary>
        /// <value>The coordinate terms.</value>
        public IEnumerable<ITerm> CoordinateTerms
        {
            get { return Synonyms; }
        }

        #endregion

        #endregion

        #region ctor

        /// <summary>
        /// Initializes a new instance of the <see cref="Term"/> class.
        /// </summary>
        /// <param name="name">The unique name of term.</param>
        public Term(string name)
        {
            _name = name;
            // _synSet = new SynSet(this);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Term"/> class.
        /// </summary>
        /// <param name="name">The unique name of term.</param>
        /// <param name="synSet">The synset that contains the term.</param>
        public Term(string name, SynSet synSet)
        {
            _name = name;
            _synSet = synSet;
        }

        #endregion
        
        public static implicit operator Term(string s) 
        {
        	return new Term(s);
        }
    }
}