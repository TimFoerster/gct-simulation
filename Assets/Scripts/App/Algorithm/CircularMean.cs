using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using UnityEngine;

public class CircularMean : IAlgorithm<Cid<ulong>>
{
    bool ranged;
    private double sigma;

    RandomNumberGenerator rng;

    public CircularMean(RandomNumberGenerator rng, bool ranged = true, double sigma = 32)
    {
        this.ranged = ranged;
        this.sigma = sigma;
        this.rng = rng;
    }

    public Cid<ulong> Init()
    {
        return new Cid<ulong>(getRandom(), true);
    }

    public void SetRng(RandomNumberGenerator rng) => this.rng = rng;

    public (Cid<ulong>, int[]) CalculateNextCid(Cid<ulong> currentCid, BLEReceiver receiver)
    {
        var receivedPackages = receiver.GetLastReceivedMessagesBySender();
        if (receivedPackages.Count() == 0)
        {
            return (Init(), new int[0]);
        }

        var distPos = new List<BigInteger>();
        var distNeg = new List<BigInteger>();
        BigInteger sumNeg = new BigInteger(0);
        BigInteger sumPos = new BigInteger(0);
        foreach (var item in receivedPackages)
        {
            var dist = distanceCIS(currentCid.id, item.package.cid, item.distance);
            if (dist < 0)
            {
                distNeg.Add(dist);
                sumNeg += dist;
            }
            else
            {
                distPos.Add(dist);
                sumPos += dist;

            }
            // Logger.Log(currentCid.ToString("N0") + "\n" + item.cid.ToString("N0") + " ("+item.dist.ToString()+") => " + dist.ToString("N0"));

        }

        BigInteger add;
        if (sumNeg + sumPos < 0)
        {
            add = distNeg.Count > 0 ? sumNeg / distNeg.Count : 0;
        }
        else
        {
            add = distPos.Count > 0 ? sumPos / distPos.Count : 0;
        }

        //uint newId = (uint)(mappedId + (add < 0 ? (uint)~add : (uint)add));
        var newId = (ulong)((add + currentCid.id) & 0xFFFFFFFFFFFFFFFF);
        // Logger.Log($"newId: \n{currentCid.ToString("N0")} + \n{add.ToString("N0")} ({(add % ulong.MaxValue).ToString("N0")}) = \n{newId.ToString("N0")}");

        return (new Cid<ulong>(newId, false), receivedPackages.Select(r => r.package.uuid).ToArray());
    }

    public ulong getRandom() => rng.NextULong();


    private double distanceFunction(float dist) => Math.Exp(-(dist * dist) / sigma);

    private long distanceCIS(ulong lcid, ulong rcid, float dist)
    {
        var diff = (long)(rcid - lcid);

        // TODO overflow
        /*
        if (diff > (maxNumber/2))
        {
            diff = -maxNumber + diff;
        }*/

        // Logger.Log($"d: {rcid.ToString("N0")} - {lcid.ToString("N0")} = {diff.ToString("N0")}");
        if (ranged)
        {
            /**
            // https://stackoverflow.com/questions/52917342/how-to-multiply-a-biginteger-with-a-decimal 
            var d = new Decimal(distanceFunction(dist));
            var f = Fraction(d);

            diff = diff * f.numerator / f.denominator;*/
            diff = (long)( (double)diff * distanceFunction(dist));
        }

        return diff;
    }
    (BigInteger numerator, BigInteger denominator) Fraction(decimal d)
    {
        int[] bits = decimal.GetBits(d);
        BigInteger numerator = (1 - ((bits[3] >> 30) & 2)) *
                               unchecked(((BigInteger)(uint)bits[2] << 64) |
                                         ((BigInteger)(uint)bits[1] << 32) |
                                          (BigInteger)(uint)bits[0]);
        BigInteger denominator = BigInteger.Pow(10, (bits[3] >> 16) & 0xff);
        return (numerator, denominator);
    }
}

