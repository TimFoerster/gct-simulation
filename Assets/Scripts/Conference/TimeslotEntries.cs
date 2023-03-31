using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class TimeslotEntries<T> : IEnumerable<T>
{
    [SerializeField]
    List<T> list;

    public TimeslotEntries()
    {
        this.list = new List<T>();
    }

    public int Add(T value)
    {
        list.Add(value);
        return list.Count;
    }

    public void Clear()
    {
        list.Clear();
    }

    public bool Contains(T value)
    {
        return list.Contains(value);
    }

    public IEnumerator<T> GetEnumerator()
    {
        return ((IEnumerable<T>)list).GetEnumerator();
    }

    public void Remove(T value)
    {
        list.Remove(value);
    }

    public void RemoveAt(int index)
    {
        list.RemoveAt(index);
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return ((IEnumerable)list).GetEnumerator();
    }

}
