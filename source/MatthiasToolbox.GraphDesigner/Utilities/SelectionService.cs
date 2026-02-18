using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using MatthiasToolbox.GraphDesigner.Events;
using MatthiasToolbox.GraphDesigner.Interfaces;
using MatthiasToolbox.Presentation.Interfaces;

namespace MatthiasToolbox.GraphDesigner.Utilities
{
    public class SelectionService
    {
        #region cvar

        private Canvas designerCanvas;

        private List<ISelectable> currentSelection;

        #endregion
        #region prop

        internal List<ISelectable> CurrentSelection
        {
            get
            {
                if (currentSelection == null)
                    currentSelection = new List<ISelectable>();

                return currentSelection;
            }
        }

        #endregion
        #region event

        public event EventHandler<EventArgs> SelectionServiceChanged;

        #endregion
        #region ctor

        public SelectionService(Canvas canvas)
        {
            this.designerCanvas = canvas;
        }

        #endregion
        #region impl

        #region public

        /// <summary>
        /// Select the item. Everything else will be unselected first.
        /// </summary>
        /// <param name="item"></param>
        public void SelectItem(ISelectable item)
        {
            this.ClearSelection();
            this.AddToSelection(item);
        }

        public void AddToSelection(ISelectable item)
        {
            if (item is IGroupable)
            {
                List<IGroupable> groupItems = GetGroupMembers(item as IGroupable);

                foreach (ISelectable groupItem in groupItems)
                {
                    groupItem.IsSelected = true;
                    CurrentSelection.Add(groupItem);
                }
            }
            else
            {
                item.IsSelected = true;
                CurrentSelection.Add(item);
            }

            InvokeSelectionChanged();
        }

        public void RemoveFromSelection(ISelectable item)
        {
            if (item is IGroupable)
            {
                List<IGroupable> groupItems = GetGroupMembers(item as IGroupable);

                foreach (ISelectable groupItem in groupItems)
                {
                    groupItem.IsSelected = false;
                    CurrentSelection.Remove(groupItem);
                }
            }
            else
            {
                item.IsSelected = false;
                CurrentSelection.Remove(item);
            }

            InvokeSelectionChanged();
        }

        public void ClearSelection()
        {
            CurrentSelection.ForEach(item => item.IsSelected = false);
            CurrentSelection.Clear();
            InvokeSelectionChanged();
        }

        private void InvokeSelectionChanged()
        {
            if (this.SelectionServiceChanged != null) this.SelectionServiceChanged.Invoke(this, new EventArgs());
        }

        public void SelectAll()
        {
            ClearSelection();
            CurrentSelection.AddRange(designerCanvas.Children.OfType<ISelectable>());
            CurrentSelection.ForEach(item => item.IsSelected = true);

            InvokeSelectionChanged();
        }

        #endregion
        #region internal

        internal List<IGroupable> GetGroupMembers(IGroupable item)
        {
            IEnumerable<IGroupable> list = designerCanvas.Children.OfType<IGroupable>();
            IGroupable rootItem = GetRoot(list, item);
            return GetGroupMembers(list, rootItem);
        }

        internal IGroupable GetGroupRoot(IGroupable item)
        {
            IEnumerable<IGroupable> list = designerCanvas.Children.OfType<IGroupable>();
            return GetRoot(list, item);
        }

        #endregion
        #region private

        private static IGroupable GetRoot(IEnumerable<IGroupable> list, IGroupable node)
        {
            if (node == null || node.ParentID == Guid.Empty)
            {
                return node;
            }
            else
            {
                foreach (IGroupable item in list)
                {
                    if (item.ID == node.ParentID)
                    {
                        return GetRoot(list, item);
                    }
                }
                return null;
            }
        }

        private static List<IGroupable> GetGroupMembers(IEnumerable<IGroupable> list, IGroupable parent)
        {
            List<IGroupable> groupMembers = new List<IGroupable>();
            groupMembers.Add(parent);

            var children = list.Where(node => node.ParentID == parent.ID);

            foreach (IGroupable child in children)
            {
                groupMembers.AddRange(GetGroupMembers(list, child));
            }

            return groupMembers;
        }

        #endregion

        #endregion
    }
}