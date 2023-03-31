
public struct BLEAdvPackage<T>
{
    public int uuid;
    public T cid;
    public float globalTime;

    public BLEAdvPackage(int uuid, T cid, float globalTime)
    {
        this.uuid = uuid;
        this.cid = cid;
        this.globalTime = globalTime;
    }

    public string value => cid.ToString();

}
