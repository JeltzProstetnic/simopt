using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MatthiasToolbox.Presentation.Interfaces;
using MatthiasToolbox.Basics.Datastructures.Graph;
using MatthiasToolbox.GraphDesigner.DiagramDesigner;
using System.Windows;

namespace MatthiasToolbox.GraphDesigner.Events
{
    public class OptionsDialogEventArgs
    {
        /// <summary>
        /// Create an instance of OptionsDialogEventArgs.
        /// </summary>
        /// <param name="selectedObjects">An array of ISelectables</param>
        public OptionsDialogEventArgs(ISelectable[] selectedObjects)
        {
            this.SelectedObjects = selectedObjects;

            if (selectedObjects.Count() == 1)
            {
                if (selectedObjects[0] is Connection)
                {
                    SelectedEdge = (selectedObjects[0] as Connection).Edge;
                }
                else if (selectedObjects[0] is DesignerItem)
                {
                    SelectedVertex = (selectedObjects[0] as DesignerItem).Vertex;
                }

                //if (selectedObjects[0] is IVertex)
                //    SelectedVertex = selectedObjects[0] as IVertex;
                //else if (selectedObjects[0] is IEdge)
                //    SelectedEdge = selectedObjects[0] as IEdge;
            }
        }

        /// <summary>
        /// All objects which were selected when the menu point "Options" was clicked.
        /// </summary>
        public ISelectable[] SelectedObjects { get; private set; }

        /// <summary>
        /// In case of exactly one selected vertex this holds the instance.
        /// </summary>
        public IVertex<Point> SelectedVertex { get; private set; }

        /// <summary>
        /// In case of exactly one selected edge this holds the instance.
        /// </summary>
        public IEdge<Point> SelectedEdge { get; private set; }
    }
}
