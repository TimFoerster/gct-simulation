
public interface IAlgorithmFactory<T>
{
    IAlgorithm<T> CreateAlgorithm(RandomNumberGenerator rng);
}
