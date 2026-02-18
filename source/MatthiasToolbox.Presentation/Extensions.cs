using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Security.Permissions;
using System.Windows.Threading;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.Windows.Controls;
using System.Windows.Input;
using System.Threading;
using MatthiasToolbox.Presentation.Datagrid;
// using MatthiasToolbox.Utilities.Office;
using MatthiasToolbox.Basics.Interfaces;
using MatthiasToolbox.Presentation.Utilities;
using MatthiasToolbox.Basics.Datastructures.Graph;
using System.Collections;
using System.Windows.Data;

namespace MatthiasToolbox.Presentation
{
    public static class Extensions
    {
        #region xtrn

        [System.Runtime.InteropServices.DllImportAttribute("user32.dll", EntryPoint = "SetCursorPos")]
        [return: System.Runtime.InteropServices.MarshalAsAttribute(System.Runtime.InteropServices.UnmanagedType.Bool)]
        public static extern bool SetCursorPos(int X, int Y);

        #endregion
        #region cvar

        private static byte[] tmpPixels = new byte[1];

        #endregion
        #region impl
        
        #region Point

        public static IPoint2D<double> ToIPoint(this Point value) 
        {
            return new WindowsPointWrapper(value);
        }

        public static Point ToWindowsPoint(this IPoint2D<int> value)
        {
            return new Point(value.X, value.Y);
        }

        public static Point ToWindowsPoint(this IPoint3D<int> value)
        {
            return new Point(value.X, value.Y);
        }

        public static Point ToWindowsPoint(this IPoint2D<double> value) 
        {
            return new Point(value.X, value.Y);
        }

        public static Point ToWindowsPoint(this IPoint3D<double> value)
        {
            return new Point(value.X, value.Y);
        }

        public static System.Drawing.Point ToDrawingPoint(this IPoint2D<int> value)
        {
            return new System.Drawing.Point(value.X, value.Y);
        }

        public static System.Drawing.Point ToDrawingPoint(this IPoint3D<int> value)
        {
            return new System.Drawing.Point(value.X, value.Y);
        }

        public static System.Drawing.Point ToDrawingPoint(this IPoint2D<double> value)
        {
            return new System.Drawing.Point(Convert.ToInt32(value.X), Convert.ToInt32(value.Y));
        }

        public static System.Drawing.Point ToDrawingPoint(this IPoint3D<double> value)
        {
            return new System.Drawing.Point(Convert.ToInt32(value.X), Convert.ToInt32(value.Y));
        }

        #endregion
        #region Window

        [SecurityPermissionAttribute(SecurityAction.Demand, Flags = SecurityPermissionFlag.UnmanagedCode)]
        public static void DoEvents(this Window window)
        {
            DispatcherFrame frame = new DispatcherFrame();
            Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Background,
                new DispatcherOperationCallback(ExitFrame), frame);
            Dispatcher.PushFrame(frame);
        }

        [SecurityPermissionAttribute(SecurityAction.Demand, Flags = SecurityPermissionFlag.UnmanagedCode)]
        public static void DoEvents(this UserControl control)
        {
            DispatcherFrame frame = new DispatcherFrame();
            Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Background,
                new DispatcherOperationCallback(ExitFrame), frame);
            Dispatcher.PushFrame(frame);
        }

        private static object ExitFrame(object f)
        {
            ((DispatcherFrame)f).Continue = false;
            return null;
        }

        #endregion
        #region Vertex

        public static double GetX(this IVertex<Point> obj)
        {
            return obj.Position.X;
        }

        public static double GetY(this IVertex<Point> obj)
        {
            return obj.Position.Y;
        }

        public static void SetX(this IVertex<Point> obj, double x)
        {
            obj.Position = new Point(x, obj.GetY());
        }

        public static void SetY(this IVertex<Point> obj, double y)
        {
            obj.Position = new Point(obj.GetY(), y);
        }

        #endregion
        #region DataGrid

        ///// <summary>
        ///// Export data from <paramref name="grid"/> to Excel
        ///// </summary>
        ///// <param name="grid"></param>
        //public static void OpenInExcel(this DataGrid grid)
        //{
        //    Thread thread = new Thread(o => Excel.OpenWithExcel(o as object[,]));
        //    thread.Start(ExportHelper.GetExportData(grid));
        //}

        #endregion
        #region MouseDevice

        /// <summary>
        /// Set the mouse position to the given coordinates using the windows API.
        /// The coordinates in Point position will be converted to int and 
        /// interpreted as absolute screen coordinates.
        /// </summary>
        /// <param name="device">The mouse device</param>
        /// <param name="position">Target mouse position in absolute screen coordinates</param>
        public static void SetPosition(this MouseDevice device, Point position)
        {
            SetCursorPos((int)position.X, (int)position.Y);
        }

        #endregion
        #region BitmapSource

        public static bool IsPixelTransparent(this BitmapSource src, int x, int y)
        {
            try
            {
                if (src.Format == PixelFormats.Indexed4)
                {
                    src.CopyPixels(new Int32Rect(x, y, 1, 1), tmpPixels, 2753, 0);
                    return (src.Palette.Colors[tmpPixels[0] >> 4]).A == 0;
                }
                else if (src.Format == PixelFormats.Indexed1)
                {
                    int stride = (src.PixelWidth / src.Format.BitsPerPixel);
                    src.CopyPixels(new Int32Rect(x, y, 1, 1), tmpPixels, stride, 0);
                    return (src.Palette.Colors[tmpPixels[0] >> 7]).A == 0;
                }
                else
                {
                    return true;
                }
            }
            catch
            {
                return true;
            }
        }

        #endregion
        #region DependencyObject

        /// <summary>
        /// Enumerate the Dependencyproperties with bindings set.
        /// </summary>
        /// <param name="element">The DependencyObject to search for bound properties.</param>
        /// <returns>All DependencyProperties that have a binding set.</returns>
        public static IEnumerable EnumerateDependencyProperties(this DependencyObject element)
        {
            LocalValueEnumerator lve = element.GetLocalValueEnumerator();

            while (lve.MoveNext())
            {
                LocalValueEntry entry = lve.Current;
                if (BindingOperations.IsDataBound(element, entry.Property))
                {
                    yield return entry.Property;
                }
            }
        }

        public static T FindVisualChild<T>(this DependencyObject obj) where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(obj); i += 1)
            {
                DependencyObject child = VisualTreeHelper.GetChild(obj, i);
                if (child != null && child is T) return (T)child;
                else
                {
                    T childOfChild = FindVisualChild<T>(child);
                    if (childOfChild != null) return childOfChild;
                }
            }
            return null;
        }

        /// <summary>
        /// Finds a parent of a given item on the visual tree.
        /// </summary>
        /// <typeparam name="T">The type of the queried item.</typeparam>
        /// <param name="child">A direct or indirect child of the queried item.</param>
        /// <returns>The first parent item that matches the submitted type parameter. 
        /// If not matching item can be found, a null reference is being returned.</returns>
        public static T FindVisualParent<T>(this DependencyObject child)
          where T : DependencyObject
        {
            // get parent item
            DependencyObject parentObject = VisualTreeHelper.GetParent(child);

            // we’ve reached the end of the tree
            if (parentObject == null) return null;

            // check if the parent matches the type we’re looking for
            T parent = parentObject as T;
            if (parent != null)
            {
                return parent;
            }
            else
            {
                // use recursion to proceed with next level
                return FindVisualParent<T>(parentObject);
            }
        }

        /// <summary>
        /// Finds a parent of a given item on the visual tree.
        /// </summary>
        /// <typeparam name="T">The type of the queried item.</typeparam>
        /// <param name="child">A direct or indirect child of the queried item.</param>
        /// <returns>The first parent item that matches the submitted type parameter. 
        /// If not matching item can be found, a null reference is being returned.</returns>
        public static T FindVisualParentIncludeContentElements<T>(this DependencyObject child)
          where T : DependencyObject
        {
            // get parent item
            DependencyObject parentObject = GetParentObject(child);

            // we’ve reached the end of the tree
            if (parentObject == null) return null;

            // check if the parent matches the type we’re looking for
            T parent = parentObject as T;
            if (parent != null)
            {
                return parent;
            }
            else
            {
                // use recursion to proceed with next level
                return FindVisualParent<T>(parentObject);
            }
        }

        /// <summary>
        /// This method is an alternative to WPF's
        /// <see cref="VisualTreeHelper.GetParent"/> method, which also
        /// supports content elements. Keep in mind that for content element,
        /// this method falls back to the logical tree of the element!
        /// </summary>
        /// <param name="child">The item to be processed.</param>
        /// <returns>The submitted item's parent, if available. Otherwise
        /// null.</returns>
        public static DependencyObject GetParentObject(DependencyObject child)
        {
            if (child == null) return null;
            ContentElement contentElement = child as ContentElement;

            if (contentElement != null)
            {
                DependencyObject parent = ContentOperations.GetParent(contentElement);
                if (parent != null) return parent;

                FrameworkContentElement fce = contentElement as FrameworkContentElement;
                return fce != null ? fce.Parent : null;
            }

            //if it's not a ContentElement, rely on VisualTreeHelper
            return VisualTreeHelper.GetParent(child);
        }

        #endregion
        
        #endregion
    }
}