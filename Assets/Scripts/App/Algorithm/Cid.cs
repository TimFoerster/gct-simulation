
using System;

/**
*  Only used for internal calcuation
*/
[Serializable]
public struct Cid<T>
{
    public T id;
    public bool generated;

    public Cid(T id, bool generated)
    {
        this.id = id;
        this.generated = generated;
    }

}
