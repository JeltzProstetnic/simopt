using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using MatthiasToolbox.Logging;
using Point = System.Windows.Point;

namespace MatthiasToolbox.Presentation.Utilities
{
    /// <summary>
    /// Contains Methods to help finding Objects in the WPF Visual Tree.
    /// </summary>
    public class WPFTreeHelper
    {
        #region Methods 

        /// <summary>
        /// Finds the child control.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="outerDepObj">The outer dep obj.</param>
        /// <returns></returns>
        public static T FindChildControl<T>(DependencyObject outerDepObj) where T : DependencyObject {
            T child = null;
            for (int index = 0; index < VisualTreeHelper.GetChildrenCount(outerDepObj); index++) {
                DependencyObject depObj = VisualTreeHelper.GetChild(outerDepObj, index);
                
                if (depObj is T) {
                    FrameworkElement fe = depObj as FrameworkElement;
                    if (fe != null) {
                        child = depObj as T;
                    }
                }
                else if (VisualTreeHelper.GetChildrenCount(depObj) > 0) {
                    child = FindChildControl<T>(depObj);
                }
                if (child != null) {
                    break;
                }
            }
            return child;
        }

        /// <summary>
        /// Finds the child control.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="outerDepObj">The outer dep obj.</param>
        /// <param name="name">The name.</param>
        /// <returns></returns>
        public static T FindChildControl<T>(DependencyObject outerDepObj, String name) where T : DependencyObject {
            T child = null;
            for (int index = 0; index < VisualTreeHelper.GetChildrenCount(outerDepObj); index++) {
                DependencyObject depObj = VisualTreeHelper.GetChild(outerDepObj, index);
                if (depObj is T) {
                    FrameworkElement fe = depObj as FrameworkElement;
                    if (fe != null) {
                        if (name == null) {
                            child = depObj as T;
                        }
                        else if (fe.Name.Equals(name)) {
                            child = depObj as T;
                        }
                    }
                }
                else if (VisualTreeHelper.GetChildrenCount(depObj) > 0) {
                    child = FindChildControl<T>(depObj, name);
                }
                if (child != null) {
                    break;
                }
            }
            return child;
        }

        /// <summary>
        /// Finds the parent/ancestor control.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="innerDepObj">The inner dep obj.</param>
        /// <returns></returns>
        public static T FindParentControl<T>(DependencyObject innerDepObj) where T : DependencyObject {
            T parent = null;
            //get the parent object
            DependencyObject depObj = VisualTreeHelper.GetParent(innerDepObj);
            if (depObj == null)
                return parent;
            if (depObj is T) {
                //if the object is the searched type T, return it
                parent = depObj as T;
            }
            else {
                //Traverse the tree further till the first parent of type T is found
                parent = FindParentControl<T>(depObj);
            }
            return parent;
        }

        /// <summary>
        /// Finds the parent control.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="innerDepObj">The inner dep obj.</param>
        /// <param name="lastParentToSearch">The last parent to search.</param>
        /// <returns></returns>
        public static T FindParentControl<T>(DependencyObject innerDepObj, DependencyObject lastParentToSearch) where T : DependencyObject
        {
            T parent = null;
            //get the parent object
            DependencyObject depObj = VisualTreeHelper.GetParent(innerDepObj);
            if (depObj == null)
                return parent;
            if (depObj is T)
            {
                //if the object is the searched type T, return it
                parent = depObj as T;
            }
            else if (depObj == lastParentToSearch)
            {
                return parent;
            }
            else
            {
                //Traverse the tree further till the first parent of type T is found
                parent = FindParentControl<T>(depObj, lastParentToSearch);
            }
            return parent;
        }

        /// <summary>
        /// Finds the parent control.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="innerDepObj">The inner dep obj.</param>
        /// <param name="lastParentToSearch">The last parent to search.</param>
        /// <returns></returns>
        public static DependencyObject FindParentControlByInterface<T>(DependencyObject innerDepObj, DependencyObject lastParentToSearch)
        {
            DependencyObject parent = null;
            //get the parent object
            DependencyObject depObj = VisualTreeHelper.GetParent(innerDepObj);
            if (depObj == null)
                return parent;
            if (depObj is T)
            {
                //if the object is the searched type T, return it
                parent = depObj;
            }
            else if (depObj == lastParentToSearch)
            {
                return parent;
            }
            else
            {
                //Traverse the tree further till the first parent of type T is found
                parent = FindParentControlByInterface<T>(depObj, lastParentToSearch);
            }
            return parent;
        }

        /// <summary>
        /// Finds the parent with specified type.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="type">The type.</param>
        /// <returns></returns>
        public static T FindParentType<T>(DependencyObject type) where T : DependencyObject
        {
            T t = null;
            FrameworkElement par = ((FrameworkElement)type).Parent as FrameworkElement;
            if (par == null)
                return t;
            if (par is T)
            {
                t = par as T;
            }
            else
            {
                t = FindParentType<T>(par);
            }
            return t;
        }

        /// <summary>
        /// Finds the visual child.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj">The obj.</param>
        /// <returns></returns>
        public static T FindVisualChild<T>(DependencyObject obj) where T : DependencyObject {
            int num = VisualTreeHelper.GetChildrenCount(obj);
            for (int i = 0; i < num; i++) {
                DependencyObject child = VisualTreeHelper.GetChild(obj, i);
                if (child != null && child is T)
                    return (T)child;
                T childOfChild = FindVisualChild<T>(child);
                if (childOfChild != null)
                    return childOfChild;
            }
            return null;
        }

        /// <summary>
        /// Gets the context menu.
        /// </summary>
        /// <param name="contextItem">The context item.</param>
        /// <returns></returns>
        public static ContextMenu GetContextMenu(object contextItem) {
            MenuItem menuItem = contextItem as MenuItem;
            if (menuItem == null)
                return null;
            return menuItem.Parent as ContextMenu;
        }

        /// <summary>
        /// Gets the panel from context.
        /// </summary>
        /// <param name="contextItem">The context item.</param>
        /// <returns></returns>
        public static WrapPanel GetPanelFromContext(object contextItem) {
            ContextMenu menu = GetContextMenu(contextItem);
            if (menu == null)
                return null;

            // get parent of contextmenue placementtarget
            WrapPanel item = menu.PlacementTarget as WrapPanel;
            if (item == null)
                return null;
            return item;
        }

        /// <summary>
        /// Gets the tree item from context.
        /// </summary>
        /// <param name="contextItem">The context item.</param>
        /// <returns></returns>
        public static TreeViewItem GetTreeItemFromContext(object contextItem) {
            ContextMenu menu = GetContextMenu(contextItem);
            if (menu == null)
                return null;
            // get parent of contextmenue placementtarget
            TreeViewItem item = FindParentControl<TreeViewItem>(menu.PlacementTarget);
            if (item == null)
                return null;
            return item;
        }

       /// <summary>
        /// Loads the picture.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns></returns>
        public static BitmapImage LoadPicture(string path) {
            string fullPath = Path.Combine(Environment.CurrentDirectory, @"Images\" + path);

            BitmapImage bi = new BitmapImage();
            bi.BeginInit();
            bi.UriSource = new Uri(fullPath, UriKind.RelativeOrAbsolute);
            bi.EndInit();
            bi.CacheOption = BitmapCacheOption.OnLoad;

            return bi;
        }

        /// <summary>
        /// Loads the picture from full path.
        /// </summary>
        /// <param name="fullPath">The full path.</param>
        /// <returns></returns>
        public static BitmapImage LoadPictureFromFullPath(string fullPath) {
            try {
                BitmapImage bi = new BitmapImage();
                bi.BeginInit();
                bi.UriSource = new Uri(fullPath, UriKind.RelativeOrAbsolute);
                bi.EndInit();
                bi.CacheOption = BitmapCacheOption.OnLoad;
                return bi;

            }
            catch (Exception ex) {
                Logger.Log<ERROR>("WPFTreeHelper.LoadPictureFromFullPath", ex);
                return null;
            }
        }

       /// <summary>
        /// Sets the tree view selected item.
        /// </summary>
        /// <param name="control">The control.</param>
        /// <param name="item">The item.</param>
        public static void SetTreeViewSelectedItem(ref System.Windows.Controls.TreeView control, object item) {
            try {
                DependencyObject dObject = control
                    .ItemContainerGenerator
                    .ContainerFromItem(item);

                //uncomment the following line if UI updates are unnecessary
                ((TreeViewItem)dObject).IsSelected = true;

                MethodInfo selectMethod =
                   typeof(TreeViewItem).GetMethod("Select",
                   BindingFlags.NonPublic | BindingFlags.Instance);

                selectMethod.Invoke(dObject, new object[] { true });
            }
            catch (Exception ex) {
                Logger.Log<ERROR>("WPFTreeHelper.SetTreeViewSelectedItem", ex);
            }
        }

		#endregion Methods 
    }
}
