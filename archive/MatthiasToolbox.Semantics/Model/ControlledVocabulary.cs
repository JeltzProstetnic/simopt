using System;
using System.Collections.Generic;
using System.Linq;
using MatthiasToolbox.Semantics.Interfaces;
using MatthiasToolbox.Semantics.Utilities;

namespace MatthiasToolbox.Semantics.Model
{
    public class ControlledVocabulary : IVocabulary
    {
        private readonly List<SynSet> _synSets; //List of all Terms of the Vocabularyin SynSets.

        #region cvar

        #endregion
        #region prop

        #region IVocabulary

        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>The name.</value>
        public string Name { get; private set; }

        /// <summary>
        /// Gets all the terms in the Vocabulary.
        /// </summary>
        /// <value>The terms in the Vocabulary.</value>
        public IEnumerable<ITerm> Terms
        {
            get { return Content; }
        }

        /// <summary>
        /// Adds the synonym to the Vocabulary SynSet of the base term.
        /// </summary>
        /// <param name="baseTerm">The base term.</param>
        /// <param name="synonym">The synonym.</param>
        /// <returns></returns>
        public bool AddSynonym(ITerm baseTerm, ITerm synonym)
        {
            if (_synSets.Any(s => s[synonym.Name] != null))
            {
                return false;
            }

            //Get SynSet of the base term
            SynSet synSet = GetSynset(baseTerm.Name);
            if (synSet == null)
                return false;

            //add synonym
            synSet[synonym.Name] = synonym;

            return true;
        }

        /// <summary>
        /// All synonyms / aliases for this term.
        /// Each listed synonym denotes the same as this entry.
        /// </summary>
        /// <param name="word">The term.</param>
        /// <returns></returns>
        public IEnumerable<ITerm> GetSynonyms(string word)
        {
            return GetSynset(word).Members;
        }

        #endregion

        #endregion
        #region ctor

        /// <summary>
        /// Initializes a new instance of the <see cref="ControlledVocabulary"/> class.
        /// </summary>
        private ControlledVocabulary()
        {
            _synSets = new List<SynSet>();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ControlledVocabulary"/> class.
        /// </summary>
        /// <param name="name">The name of the Vocabulary.</param>
        public ControlledVocabulary(string name)
            : this()
        {
            Name = name;
        }

        #endregion

        /// <summary>
        /// Gets the synset that contains the Term.
        /// </summary>
        /// <param name="word">The term name.</param>
        /// <returns></returns>
        private SynSet GetSynset(string word)
        {
            return (from synSet in _synSets where synSet[word] != null select synSet).FirstOrDefault();
        }

        #region impl

        #region IContainer<SynSet>

        /// <summary>
        /// All elements within this container.
        /// </summary>
        public IEnumerable<ITerm> Content
        {
            get {
                return _synSets.SelectMany(synSet => synSet.Members);
            }
        }

        /// <summary>
        /// Adds a new synset.
        /// </summary>
        /// <param name="element">The element.</param>
        /// <returns></returns>
        public bool Add(ITerm element)
        {
            if (_synSets.Any(synSet => synSet[element.Name] != null))
            {
                return false;
            }

            SynSet syn = new SynSet();
            syn[element.Name] = element;
            _synSets.Add(syn);

            return true;
        }

        /// <summary>
        /// Removes the specified element. Removes the SynSet if it is empty after removing the Term.
        /// </summary>
        /// <param name="element">The element to remove.</param>
        /// <returns></returns>
        public bool Remove(ITerm element)
        {
            var synSets = from s in _synSets where s[element.Name] != null select s;
            if (!synSets.Any())
                return false;

            List<SynSet> setsToRemove = new List<SynSet>(); //list of synsets to remove
            foreach (var set in synSets)
            {
                set[element.Name] = null;
                if(set.Members.Count() == 0)
                    setsToRemove.Add(set);
            }

            //remove all empty synsets
            foreach (var set in setsToRemove)
            {
                _synSets.Remove(set);
            }

            return true;
        }

        /// <summary>
        /// Gets the element by the unique name.
        /// </summary>
        /// <param name="word">The unique name.</param>
        /// <returns></returns>
        public ITerm GetElement(string word)
        {
            return (from synSet in _synSets where synSet[word] != null select synSet[word]).FirstOrDefault();
        }

        /// <summary>
        /// Finds the element ignoring case sensitivity.
        /// </summary>
        /// <param name="word">The word.</param>
        /// <param name="searchDefinitions">if set to <c>true</c> search in term definitions.</param>
        /// <returns></returns>
        public IEnumerable<ITerm> FindElement(string word, bool searchDefinitions)
        {
            foreach (SynSet synSet in Content)
            {
                foreach (var term in synSet.Members)
                {
                    if (term.Name.ToLower().Contains(word))
                        if (!searchDefinitions)
                            yield return term;
                        else if (term.Definition.ToLower().Contains(word))
                            yield return term;
                }
            }
        }

        #endregion

        #endregion
    }
}