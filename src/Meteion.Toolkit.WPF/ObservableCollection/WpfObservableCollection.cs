using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Reflection;

namespace System.Windows.Data;

/// <summary>
/// A WPF-aware <see cref="ObservableRangeCollection{T}"/>. WPF's <see cref="CollectionView"/> does not support
/// range notifications, so for multi-item changes this collection refreshes bound views instead of raising the
/// range event they would reject.
/// </summary>
/// <typeparam name="T">The type of the items in the collection.</typeparam>
public class WpfObservableRangeCollection<T> : ObservableRangeCollection<T>
{
    // Pending notifications while a range operation is running; null when events are raised immediately.
    // Distinct from the base class's private field of the same name.
    DeferredEventsCollection _deferredEvents;

    /// <summary>
    /// Initializes a new, empty instance of the <see cref="WpfObservableRangeCollection{T}"/> class.
    /// </summary>
    public WpfObservableRangeCollection()
    {
    }

    /// <summary>
    /// Initializes a new instance that contains the elements copied from the specified collection.
    /// </summary>
    /// <param name="collection">The collection whose elements are copied to the new collection.</param>
    public WpfObservableRangeCollection(IEnumerable<T> collection) : base(collection)
    {
    }

    /// <summary>
    /// Initializes a new instance that contains the elements copied from the specified list.
    /// </summary>
    /// <param name="list">The list whose elements are copied to the new collection.</param>
    public WpfObservableRangeCollection(List<T> list) : base(list)
    {
    }


    /// <inheritdoc />
    /// <remarks>
    /// Range changes are delivered to <see cref="CollectionView"/> listeners as a refresh, because those listeners
    /// throw on multi-item notifications; every other listener receives the original event.
    /// </remarks>
    protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
    {
        // Reads the base class's private deferred-event list through reflection. This local shadows the field of
        // the same name declared in this class.
        var _deferredEvents = (ICollection<NotifyCollectionChangedEventArgs>)typeof(ObservableRangeCollection<T>).GetField("_deferredEvents", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(this);
        if (_deferredEvents != null)
        {
            _deferredEvents.Add(e);
            return;
        }

        foreach (var handler in GetHandlers())
            if (IsRange(e) && handler.Target is CollectionView cv)
                cv.Refresh();
            else
                handler(this, e);
    }

    /// <inheritdoc />
    /// <remarks>
    /// Disposing the returned object raises the queued events, refreshing each <see cref="CollectionView"/> once.
    /// </remarks>
    protected override IDisposable DeferEvents() => new DeferredEventsCollection(this);

    // True when the event covers more than one item, which CollectionView cannot process.
    bool IsRange(NotifyCollectionChangedEventArgs e) => e.NewItems?.Count > 1 || e.OldItems?.Count > 1;

    // Reads the CollectionChanged invocation list through reflection so each handler can be targeted individually.
    IEnumerable<NotifyCollectionChangedEventHandler> GetHandlers()
    {
        var info = typeof(ObservableCollection<T>).GetField(nameof(CollectionChanged), BindingFlags.Instance | BindingFlags.NonPublic);
        var @event = (MulticastDelegate)info.GetValue(this);
        return @event?.GetInvocationList()
          .Cast<NotifyCollectionChangedEventHandler>()
          .Distinct()
          ?? Enumerable.Empty<NotifyCollectionChangedEventHandler>();
    }

    class DeferredEventsCollection : List<NotifyCollectionChangedEventArgs>, IDisposable
    {
        private readonly WpfObservableRangeCollection<T> _collection;
        public DeferredEventsCollection(WpfObservableRangeCollection<T> collection)
        {
            Debug.Assert(collection != null);
            Debug.Assert(collection._deferredEvents == null);
            _collection = collection;
            _collection._deferredEvents = this;
        }

        public void Dispose()
        {
            _collection._deferredEvents = null;

            // Replay every queued event to ordinary listeners, but refresh each CollectionView only once.
            var handlers = _collection
              .GetHandlers()
              .ToLookup(h => h.Target is CollectionView);

            foreach (var handler in handlers[false])
                foreach (var e in this)
                    handler(_collection, e);

            foreach (var cv in handlers[true]
              .Select(h => h.Target)
              .Cast<CollectionView>()
              .Distinct())
                cv.Refresh();
        }
    }
}
