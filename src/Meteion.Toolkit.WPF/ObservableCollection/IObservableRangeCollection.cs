using System.Collections.Specialized;
using System.ComponentModel;

namespace System.Collections.ObjectModel;

/// <summary>
/// An observable collection that can add, remove and replace many items at once, raising a minimal number of
/// change notifications instead of one per item.
/// </summary>
/// <typeparam name="T">The type of the items in the collection.</typeparam>
public interface IObservableRangeCollection<T> : ICollection<T>, IEnumerable<T>, INotifyCollectionChanged, INotifyPropertyChanged
{
    /// <summary>Adds the items of a collection to the end of this collection.</summary>
    /// <param name="collection">The items to add.</param>
    void AddRange(IEnumerable<T> collection);

    /// <summary>Inserts the items of a collection at the specified index.</summary>
    /// <param name="index">The zero-based index at which the first new item is inserted.</param>
    /// <param name="collection">The items to insert.</param>
    void InsertRange(int index, IEnumerable<T> collection);

    /// <summary>Removes the first occurrence of each item of a collection from this collection.</summary>
    /// <param name="collection">The items to remove.</param>
    void RemoveRange(IEnumerable<T> collection);

    /// <summary>Removes all items that satisfy a predicate.</summary>
    /// <param name="match">The predicate that selects the items to remove.</param>
    /// <returns>The number of items removed.</returns>
    int RemoveAll(Predicate<T> match);

    /// <summary>Removes all items within a range that satisfy a predicate.</summary>
    /// <param name="index">The zero-based index at which the search starts.</param>
    /// <param name="count">The number of items to examine.</param>
    /// <param name="match">The predicate that selects the items to remove.</param>
    /// <returns>The number of items removed.</returns>
    int RemoveAll(int index, int count, Predicate<T> match);

    /// <summary>Removes a contiguous range of items.</summary>
    /// <param name="index">The zero-based index of the first item to remove.</param>
    /// <param name="count">The number of items to remove.</param>
    void RemoveRange(int index, int count);

    /// <summary>Replaces the entire contents of this collection.</summary>
    /// <param name="collection">The items the collection should contain afterwards.</param>
    void ReplaceRange(IEnumerable<T> collection);

    /// <summary>Replaces the entire contents of this collection, leaving equal items in equal positions untouched.</summary>
    /// <param name="collection">The items the collection should contain afterwards.</param>
    /// <param name="comparer">The comparer used to detect items that did not change.</param>
    void ReplaceRange(IEnumerable<T> collection, IEqualityComparer<T> comparer);

    /// <summary>Replaces a range of items with the items of a collection.</summary>
    /// <param name="index">The zero-based index at which the replacement starts.</param>
    /// <param name="count">The number of existing items to replace.</param>
    /// <param name="collection">The items to put in place of the removed range.</param>
    void ReplaceRange(int index, int count, IEnumerable<T> collection);

    /// <summary>Replaces a range of items, leaving equal items in equal positions untouched.</summary>
    /// <param name="index">The zero-based index at which the replacement starts.</param>
    /// <param name="count">The number of existing items to replace.</param>
    /// <param name="collection">The items to put in place of the removed range.</param>
    /// <param name="comparer">The comparer used to detect items that did not change.</param>
    void ReplaceRange(int index, int count, IEnumerable<T> collection, IEqualityComparer<T> comparer);
}
