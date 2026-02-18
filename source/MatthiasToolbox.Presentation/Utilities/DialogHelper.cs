using System;
using System.Collections;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace MatthiasToolbox.Presentation.Utilities
{
    /// <summary>
    /// Contains Extension Methods for DependencyObject
    /// - UpdateBAllSources: Update Binding sources of the DependencyObject when the Binding UpdateSourceTrigger is set explicit.
    /// </summary>
    public static class DialogHelper
    {
        ///<summary>
        /// Recursively processes a given dependency object and all its
        /// children, and updates sources of all objects that use a
        /// binding expression on a given property.
        ///
        /// The dependency object that marks a starting
        /// point. This could be a dialog window or a panel control that
        /// hosts bound controls.
        /// The properties to be updated if
        ///  or one of its childs provide it along
        /// with a binding expression.
        /// </summary>
        /// <returns>Returns true if all controls are valid, or false if one object has a validation error.</returns>
        public static bool UpdateBindingSources(DependencyObject obj)
        {
            bool isValid = UpdateBindingSources(obj, false);
            return isValid && UpdateBindingSources(obj, true);
        }

        public static bool UpdateAllSources(this DependencyObject obj)
        {
            return UpdateBindingSources(obj);
        }

        public static bool UpdateAllTargets(this DependencyObject obj)
        {
            return UpdateBindingTargets(obj);
        }

        #region private

        private static bool UpdateBindingSources(DependencyObject obj, bool doUpdate)
        {
            bool isValid = true;
            IEnumerable props = obj.EnumerateDependencyProperties();
            foreach (DependencyProperty p in props)
            {
                Binding b = BindingOperations.GetBinding(obj, p);
                if (b.UpdateSourceTrigger == UpdateSourceTrigger.Explicit)
                {
                    //check whether the submitted object provides a bound property
                    //that matches the property parameters
                    BindingExpression be = BindingOperations.GetBindingExpression(obj, p);


                    if (be != null)
                    {
                        if (be.Status == BindingStatus.PathError || be.Status == BindingStatus.Unattached ||
                            be.Status == BindingStatus.UpdateSourceError || be.Status == BindingStatus.UpdateTargetError)
                            continue;

                        if (doUpdate)
                        {
                            switch (b.Mode)
                            {
                                case BindingMode.OneTime:
                                case BindingMode.OneWay:
                                    be.UpdateTarget();
                                    break;
                                default:
                                    be.UpdateSource();
                                    break;
                            }
                        }
                        else be.ValidateWithoutUpdate();
                    }
                }
            }

            if (Validation.GetHasError(obj))
                return false;

            int count = VisualTreeHelper.GetChildrenCount(obj);
            for (int i = 0; i < count && isValid; i++)
            {
                //process child items recursively
                DependencyObject childObject = VisualTreeHelper.GetChild(obj, i);
                isValid = UpdateBindingSources(childObject, doUpdate);
            }

            return isValid;
        }


        private static bool UpdateBindingTargets(DependencyObject obj)
        {
            bool isValid = true;
            IEnumerable props = obj.EnumerateDependencyProperties();
            foreach (DependencyProperty p in props)
            {
                Binding b = BindingOperations.GetBinding(obj, p);

                //check whether the submitted object provides a bound property
                //that matches the property parameters
                BindingExpression be = BindingOperations.GetBindingExpression(obj, p);


                if (be != null)
                {
                    be.UpdateTarget();
                }
            }

            if (Validation.GetHasError(obj))
                return false;

            int count = VisualTreeHelper.GetChildrenCount(obj);
            for (int i = 0; i < count && isValid; i++)
            {
                //process child items recursively
                DependencyObject childObject = VisualTreeHelper.GetChild(obj, i);
                isValid = UpdateBindingTargets(childObject);
            }

            return isValid;
        }


        #endregion
    }
}