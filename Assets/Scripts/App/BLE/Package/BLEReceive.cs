[System.Serializable]
public struct BLEReceive<T>
{
    public BLEAdvPackage<T> package;
    public float distance;
    public bool generated;
    public ulong continuation;

    public BLEReceive(BLEBroadcast<T> broadcast, float distance, ulong continuation)
    {
        this.package = broadcast.package;
        this.distance = distance;
        this.generated = broadcast.generated;
        this.continuation = continuation;
    }
}
