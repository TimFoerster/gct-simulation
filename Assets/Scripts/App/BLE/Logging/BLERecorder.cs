using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Serializable]
public struct BLERecord<T>
{
    public ulong index;
    public T message;
    public Vector3 position;
}

[Serializable]
public class BLERecorder<T>
{
    private ulong index = 1;
    private int syncMessages = -1;

    List<BLERecord<T>> data = new List<BLERecord<T>>();

    public void Add(Transform entity, T message)
    {
        data.Add(new BLERecord<T>
        {
            index = index,
            message = message,
            position = entity.position
        });

        index++;
    }

    public int Count => data.Count;

    public bool IsSynching => syncMessages >= 0;

    public BLERecord<T>[] ToSync()
    {
        var array = data.ToArray();
        syncMessages = array.Length;
        return array;
    }

    public void SyncResult(bool result)
    {
        if (result && syncMessages > 0)
            data.RemoveRange(0, syncMessages);
        syncMessages = -1;
    }

    internal void Flush()
    {
        data.Clear();
    }
}
