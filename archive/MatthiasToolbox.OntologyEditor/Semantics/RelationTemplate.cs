using System;
using System.Collections.Generic;
using MatthiasToolbox.Basics.Datastructures.Graph;
using System.Windows;
using MatthiasToolbox.Basics.Interfaces;
using System.Windows.Media;
using MatthiasToolbox.Semantics.Interfaces;
using MatthiasToolbox.Semantics.Metamodel;
using MatthiasToolbox.Semantics.Utilities;

namespace MatthiasToolbox.OntologyEditor.Semantics
{
    public class RelationTemplate : IEdge<Point>, IColors<Color>
    {
        #region prop

        public Cardinality Cardinality { get; set; }

        #region IEdge

        public IVertex<Point> Vertex1
        {
            get;
            set;
        }

        public IVertex<Point> Vertex2
        {
            get;
            set;
        }

        public bool IsDirected
        {
            get;
            set;
        }

        public IEnumerable<Point> Path
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        #endregion
        #region INamedElement

        public string Name
        {
            get;
            set;
        }

        #endregion
        #region IColor<Color>

        public Color BackgroundColor { get; set; }

        public Color ForegroundColor { get; set; }

        #endregion

        #endregion
        #region ctor

        public RelationTemplate() : base() { Cardinality = Cardinality.AnyToAny; }

        public RelationTemplate(Relation r) : base() 
        {
            Name = r.Name;
            IsDirected = r.IsDirected;
            BackgroundColor = r.LineColor;
            ForegroundColor = r.TextColor;
            Cardinality = r.Cardinality;
        }

        #endregion
    }
}