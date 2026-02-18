using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows.Media;
using MatthiasToolbox.Basics.Interfaces;
using MatthiasToolbox.Basics.Datastructures.Graph;
using System.Data.Linq.Mapping;
using System.Windows;
using MatthiasToolbox.Semantics.Interfaces;
using MatthiasToolbox.Semantics.Utilities;
using MatthiasToolbox.Semantics.Metamodel.Layout;

namespace MatthiasToolbox.Semantics.Metamodel
{
    public partial class Relation : ILINQTable, IEdge<Point>, IColors<Color>, INotifyPropertyChanged
    {
        #region prop

        public View ViewContext { get; set; }

        #region IEdge<Point>

        public bool IsDirected { get; set; }

        public IVertex<Point> Vertex1
        {
            get { return Members.Item1; }
            set
            {
                Members = new Tuple<Concept, Concept>((Concept)value, Members.Item2);
                OnPropertyChanged("Vertex1");
            }
        }

        public IVertex<Point> Vertex2
        {
            get { return Members.Item2; }
            set
            {
                Members = new Tuple<Concept, Concept>(Members.Item1, (Concept)value);
                OnPropertyChanged("Vertex2");
            }
        }

        public IEnumerable<Point> Path
        {
            get
            {
                if (ViewContext == null) yield break;
                if (ViewContext.RelationLayouts.ContainsKey(this))
                {
                    string data = ViewContext.RelationLayouts[this].PathData;
                    string[] points = data.Split('|');
                    foreach (string s in points)
                        yield return Point.Parse(s);
                }
                else yield break;
            }
            set
            {
                if (ViewContext == null) throw new NullReferenceException("ViewContext not set.");

                StringBuilder sb = new StringBuilder();
                foreach (Point p in value)
                {
                    sb.Append(p.ToString());
                    sb.Append("|");
                }
                sb.Remove(sb.Length - 1, 1);

                ViewContext.RelationLayouts[this].PathData = sb.ToString();
                DataContext.SubmitChanges();
                OnPropertyChanged("Path");
            }
        }

        #endregion
        #region IColor<Color>

        public Color BackgroundColor { get { return LineColor; } set { LineColor = value; } }

        public Color ForegroundColor { get { return TextColor; } set { TextColor = value; } }

        public Color LineColor
        {
            get
            {
                if (ViewContext == null) return Colors.Transparent;
                if (ViewContext.RelationLayouts.ContainsKey(this))
                {
                    System.Drawing.Color c = System.Drawing.ColorTranslator.FromHtml(ViewContext.RelationLayouts[this].LineColor);
                    return Color.FromArgb(c.A, c.R, c.G, c.B);
                }
                else return Colors.Transparent;
            }
            set
            {
                if (ViewContext == null) throw new NullReferenceException("ViewContext not set.");
                ViewContext.RelationLayouts[this].LineColor = value.ToString(); // String strHtmlColor = System.Drawing.ColorTranslator.ToHtml(c);
                DataContext.SubmitChanges();
                OnPropertyChanged("BackgroundColor");
            }
        }

        public Color TextColor
        {
            get
            {
                if (ViewContext == null) return Colors.Black;
                if (ViewContext.RelationLayouts.ContainsKey(this))
                {
                    System.Drawing.Color c = System.Drawing.ColorTranslator.FromHtml(ViewContext.RelationLayouts[this].TextColor);
                    return Color.FromArgb(c.A, c.R, c.G, c.B);
                }
                else return Colors.Black;
            }
            set
            {
                if (ViewContext == null) throw new NullReferenceException("ViewContext not set.");
                ViewContext.RelationLayouts[this].TextColor = value.ToString(); // String strHtmlColor = System.Drawing.ColorTranslator.ToHtml(c);
                DataContext.SubmitChanges();
                OnPropertyChanged("ForegroundColor");
            }
        }

        public int StartCaps
        {
            get
            {
                if (ViewContext == null) return 0;
                if (ViewContext.RelationLayouts.ContainsKey(this))
                {
                    return ViewContext.RelationLayouts[this].StartCaps;
                }
                else return 0;
            }
            set
            {
                if (ViewContext == null) throw new NullReferenceException("ViewContext not set.");
                ViewContext.RelationLayouts[this].StartCaps = value;
                DataContext.SubmitChanges();
                OnPropertyChanged("StartCaps");
            }
        }

        public int EndCaps
        {
            get
            {
                if (ViewContext == null) return 0;
                if (ViewContext.RelationLayouts.ContainsKey(this))
                {
                    return ViewContext.RelationLayouts[this].EndCaps;
                }
                else return 0;
            }
            set
            {
                if (ViewContext == null) throw new NullReferenceException("ViewContext not set.");
                ViewContext.RelationLayouts[this].EndCaps = value;
                DataContext.SubmitChanges();
                OnPropertyChanged("EndCaps");
            }
        }

        public int LineType
        {
            get
            {
                if (ViewContext == null) return 0;
                if (ViewContext.RelationLayouts.ContainsKey(this))
                {
                    return ViewContext.RelationLayouts[this].LineType;
                }
                else return 0;
            }
            set
            {
                if (ViewContext == null) throw new NullReferenceException("ViewContext not set.");
                ViewContext.RelationLayouts[this].LineType = value;
                DataContext.SubmitChanges();
                OnPropertyChanged("LineType");
            }
        }

        #endregion

        #endregion
        #region impl

        #region INotifyPropertyChanged

        protected virtual void OnPropertyChanged(string propertyName)
        {
            if (this.PropertyChanged != null)
                this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion

        #endregion
    }
}