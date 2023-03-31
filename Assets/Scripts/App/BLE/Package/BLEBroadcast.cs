
public struct BLEBroadcast<T>
{
    public BLEAdvPackage<T> package;
    public bool generated;

    public BLEBroadcast(BLEAdvPackage<T> package, bool generated)
    {
        this.package = package;
        this.generated = generated;
    }
}
