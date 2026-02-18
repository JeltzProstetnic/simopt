using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Threading;

namespace MatthiasToolbox.Presentation.Utilities.Collections
{
    /// <summary>
    /// An ObservableCollection&lt;T&gt; enhanced with capability of free threading.
    /// </summary>
    [Serializable]
    public class BindableCollection<T> : ObservableCollection<T>
    {
        /// <summary>
        /// Initializes a new instance of the<see cref="BindingCollection&lt;T&gt;">BindingCollection</see>.
        /// </summary>
        public BindableCollection() : base() { }

        /// <summary>
        /// Initializes a new instance of the<see cref="BindingCollection&lt;T&gt;">BindingCollection</see>
        /// class that contains elements copied from the specified List&lt;T&gt;.
        /// </summary>
        /// <param name="list">The list from which the elements are copied.</param>
        /// <exception cref="System.ArgumentNullException">The list parameter cannot be null.</exception>
        public BindableCollection(List<T> list) : base(list) { }

        /// <summary>
        /// Initializes a new instance of the<see cref="BindingCollection&lt;T&gt;">BindingCollection</see>
        /// class that contains elements copied from the specified IEnumerable&lt;T&gt;.
        /// </summary>
        /// <param name="list">The list from which the elements are copied.</param>
        /// <exception cref="System.ArgumentNullException">The list parameter cannot be null.</exception>
        public BindableCollection(IEnumerable<T> list)
        {
            if (list == null) throw new ArgumentOutOfRangeException("The list parameter cannot be null.");
            foreach (T item in list)
            {
                this.Items.Add(item);
            }
        }

        /// <summary>
        /// Occurs when an item is added, removed, changed, moved, or the entire list is refreshed.
        /// </summary>
        public override event NotifyCollectionChangedEventHandler CollectionChanged;

        protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            if (this.CollectionChanged != null)
            {
                using (IDisposable disposable = this.BlockReentrancy())
                {
                    foreach (Delegate del in this.CollectionChanged.GetInvocationList())
                    {
                        NotifyCollectionChangedEventHandler handler = (NotifyCollectionChangedEventHandler)del;
                        DispatcherObject dispatcherInvoker = del.Target as DispatcherObject;
                        ISynchronizeInvoke syncInvoker = del.Target as ISynchronizeInvoke;
                        if (dispatcherInvoker != null)
                        {
                            // We are running inside DispatcherSynchronizationContext,
                            // so we should invoke the event handler in the correct dispatcher.
                            dispatcherInvoker.Dispatcher.Invoke(DispatcherPriority.Normal, new ThreadStart(delegate
                            {
                                handler(this, e);
                            }));
                        }
                        else if (syncInvoker != null)
                        {
                            // We are running inside WindowsFormsSynchronizationContext,
                            // so we should invoke the event handler in the correct context.
                            syncInvoker.Invoke(del, new Object[] { this, e });
                        }
                        else
                        {
                            // We are running in free threaded context, so just directly invoke the event handler.
                            handler(this, e);
                        }
                    }
                }
            }
        }
    }

}
