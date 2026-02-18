using System.Windows;
using System.Windows.Media;
using MatthiasToolbox.Basics.Interfaces;
using MatthiasToolbox.Basics.Datastructures.Graph;
using MatthiasToolbox.Presentation;
using MatthiasToolbox.Semantics.Metamodel;
using System.ComponentModel;

namespace MatthiasToolbox.OntologyEditor.Semantics
{
    /// <summary>
    /// ConceptVertex is a Concept for use in a visible IGraph2D
    /// </summary>
    public class ConceptTemplate : IVertex<Point>, IColors<Color>, IOpacity
    {
        #region prop

        public double Opacity { get; set; }

        #region INamedElement

        public string Name
        {
            get;
            set;
        }

        #endregion
        #region IPosition<Point>

        /// <summary>
        /// Gets or sets the position.
        /// </summary>
        /// <value>The position.</value>
        public Point Position
        {
            get; set;
        }

        #endregion
        #region IColors<Color>

        public Color BackgroundColor { get; set; }

        public Color ForegroundColor { get; set; }

        #endregion

        public Concept BaseConcept { get; set; }

        #endregion
        #region ctor
        
        /// <summary>
        /// Initializes a new instance of the <see cref="ConceptTemplate"/> class.
        /// </summary>
        /// <param name="name">The name.</param>
        public ConceptTemplate()
        {
            BackgroundColor = Colors.LightGray;
            ForegroundColor = Colors.Black;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ConceptTemplate"/> class.
        /// </summary>
        /// <param name="name">The name.</param>
        public ConceptTemplate(string name) : this()
        {
            this.Name = name;
        }

        public ConceptTemplate(Concept baseConcept)
            : this(baseConcept.Name + " Subconcept")
        {
            this.BaseConcept = baseConcept;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ConceptTemplate"/> class.
        /// </summary>
        /// <param name="name">The name.</param>
        public ConceptTemplate(Concept baseConcept, Color backColor)
            : this(baseConcept)
        {
            BackgroundColor = backColor;
        }

        public ConceptTemplate(string name, Concept baseConcept, Color backColor)
            : this(baseConcept, backColor)
        {
            this.Name = name;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ConceptTemplate"/> class.
        /// </summary>
        /// <param name="name">The name.</param>
        public ConceptTemplate(Concept baseConcept, Color backColor, Color foreColor)
            : this(baseConcept)
        {
            BackgroundColor = backColor;
            ForegroundColor = foreColor;
        }

        public ConceptTemplate(string name, Concept baseConcept, Color backColor, Color foreColor)
            : this(baseConcept, backColor, foreColor)
        {
            this.Name = name;
        }

        #endregion
    }
}
