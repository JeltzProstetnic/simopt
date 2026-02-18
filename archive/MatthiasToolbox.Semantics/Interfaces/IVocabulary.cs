using System.Collections.Generic;
using MatthiasToolbox.Basics.Interfaces;
using MatthiasToolbox.Semantics.Model;

namespace MatthiasToolbox.Semantics.Interfaces
{
    /// <summary>
    /// IVocabulary is a list of terms that have been enumerated explicitly.
    /// This controlled vocabulary is the registration
    /// authority for all terms. 
    /// The IVocabulary only contains terms with unique names.
    /// </summary>
    public interface IVocabulary : INamedElement, IContainer<ITerm>
    {
        /// <summary>
        /// Gets all the terms in the Vocabulary.
        /// </summary>
        /// <value>The terms in the Vocabulary.</value>
        IEnumerable<ITerm> Terms { get; }

        /// <summary>
        /// Adds the synonym to the Vocabulary.
        /// </summary>
        /// <param name="baseTerm">The base term.</param>
        /// <param name="synonym">The synonym.</param>
        /// <returns></returns>
        bool AddSynonym(ITerm baseTerm, ITerm synonym);

        /// <summary>
        /// All synonyms / aliases for this term.
        /// Each listed synonym denotes the same as this entry.
        /// </summary>
        /// <param name="word">The term.</param>
        /// <returns></returns>
        IEnumerable<ITerm> GetSynonyms(string word);
    }
}