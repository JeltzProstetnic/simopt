using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Data.Linq.Mapping;
using MatthiasToolbox.Basics.Interfaces;
using MatthiasToolbox.Basics.Datastructures.Graph;
using MatthiasToolbox.Semantics.Interfaces;
using System.Windows;
using System.Windows.Media;
using MatthiasToolbox.Semantics.Metamodel.Layout;

namespace MatthiasToolbox.Semantics.Metamodel
{
    public partial class Concept : INotifyPropertyChanged
    {
        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion

        #region prop

        public View ViewContext { get; set; }

        public Point Position
        {
            get
            {
                if (ViewContext == null) return new Point(0, 0);
                if (ViewContext.ConceptLayouts.ContainsKey(this))
                    return new Point(ViewContext.ConceptLayouts[this].X, ViewContext.ConceptLayouts[this].Y);
                else return new Point(0, 0);
            }
            set
            {
                if (ViewContext == null) throw new NullReferenceException("ViewContext not set.");
                ViewContext.ConceptLayouts[this].X = value.X;
                ViewContext.ConceptLayouts[this].Y = value.Y;
                DataContext.SubmitChanges();
            }
        }

        public Size Size
        {
            get
            {
                if (ViewContext == null) return new Size(100, 100);
                if (ViewContext.ConceptLayouts.ContainsKey(this))
                    return new Size(ViewContext.ConceptLayouts[this].Width, ViewContext.ConceptLayouts[this].Height);
                else return new Size(100, 100);
            }
            set
            {
                if (ViewContext == null) throw new NullReferenceException("ViewContext not set.");
                ViewContext.ConceptLayouts[this].Width = value.Width;
                ViewContext.ConceptLayouts[this].Height = value.Height;
                DataContext.SubmitChanges();
            }
        }

        public Color BackgroundColor
        {
            get
            {
                if (ViewContext == null) return Colors.Transparent;
                if (ViewContext.ConceptLayouts.ContainsKey(this))
                {
                    System.Drawing.Color c = System.Drawing.ColorTranslator.FromHtml(ViewContext.ConceptLayouts[this].BackgroundColor);
                    return Color.FromArgb(c.A, c.R, c.G, c.B);
                }
                else return Colors.Transparent;
            }
            set
            {
                if (ViewContext == null) throw new NullReferenceException("ViewContext not set.");
                ViewContext.ConceptLayouts[this].BackgroundColor = value.ToString(); // String strHtmlColor = System.Drawing.ColorTranslator.ToHtml(c);
                DataContext.SubmitChanges();
                OnPropertyChanged("BackgroundColor");
            }
        }

        public Color ForegroundColor
        {
            get
            {
                if (ViewContext == null) return Colors.Black;
                if (ViewContext.ConceptLayouts.ContainsKey(this))
                {
                    System.Drawing.Color c = System.Drawing.ColorTranslator.FromHtml(ViewContext.ConceptLayouts[this].ForegroundColor);
                    return Color.FromArgb(c.A, c.R, c.G, c.B);
                }
                else return Colors.Black;
            }
            set
            {
                if (ViewContext == null) throw new NullReferenceException("ViewContext not set.");
                ViewContext.ConceptLayouts[this].ForegroundColor = value.ToString(); // String strHtmlColor = System.Drawing.ColorTranslator.ToHtml(c);
                DataContext.SubmitChanges();
                OnPropertyChanged("ForegroundColor");
            }
        }

        #region ITitle

        public string Title
        {
            get
            {
                return Name;
            }
            set
            {
                Name = value;
            }
        }

        #endregion

        #endregion
        #region ctor

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