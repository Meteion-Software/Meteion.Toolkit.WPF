using System.Collections.Specialized;
using System.ComponentModel;

namespace System.Collections.ObjectModel;

public interface IObservableRangeCollection<T> : ICollection<T>, IEnumerable<T>, INotifyCollectionChanged, INotifyPropertyChanged
{
    void AddRange(IEnumerable<T> collection);
    void InsertRange(int index, IEnumerable<T> collection);

    void RemoveRange(IEnumerable<T> collection);
    int RemoveAll(Predicate<T> match);
    int RemoveAll(int index, int count, Predicate<T> match);
    void RemoveRange(int index, int count);
    void ReplaceRange(IEnumerable<T> collection);
    void ReplaceRange(IEnumerable<T> collection, IEqualityComparer<T> comparer);
    void ReplaceRange(int index, int count, IEnumerable<T> collection);
    void ReplaceRange(int index, int count, IEnumerable<T> collection, IEqualityComparer<T> comparer);
}
