
public interface IAlgorithm<T>
{
    T Init();
    (T, int[]) CalculateNextCid(T currentCid, BLEReceiver receiver);
    void SetRng(RandomNumberGenerator rng);
}
