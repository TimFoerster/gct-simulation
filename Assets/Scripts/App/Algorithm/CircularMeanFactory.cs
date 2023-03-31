using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class CircularMeanFactory : IAlgorithmFactory<Cid<ulong>>
{
    bool ranged;
    double sigma;

    public CircularMeanFactory(bool ranged = true, double sigma = 32)
    {
        this.ranged = ranged;
        this.sigma = sigma;
    }

    public IAlgorithm<Cid<ulong>> CreateAlgorithm(RandomNumberGenerator rng)
    {
        return new CircularMean(rng, ranged, sigma);
    }
}
