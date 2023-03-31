using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

public class RandomNumberGenerator : MonoBehaviour
{
    [SerializeField, ReadOnly]
    protected int counter;

    [SerializeField, ReadOnly]
    protected int seed;

    System.Random random;

    private void Awake()
    {

        // Logger.Log("Init rng with seed " + seed, this);
    }

    protected void Init()
    {
        if (seed == default)
        {
            seed = Random.Range(int.MinValue, int.MaxValue);
        }

        random = new System.Random(seed);
        // Debug.Log(transform.name + " Seed: " + seed, this);

        // Get back to the previous number
        var oldCounter = counter;
        counter = 0;
        while (counter < oldCounter)
        {
            NextInt();
        }
    }

    void OnEnable()
    {
        Init();
    }

    public int Seed => seed;

    public int NextInt() {
        var n = random.Next();
        // Debug.Log(counter + ": " + n, this);
        counter++;
        return n;
    }

    public int NextInt(int maxValue)
    {
        var n = random.Next(maxValue);
        // Debug.Log(counter + ": " + n, this);
        counter++;

        return n;
    }
    public int NextInt(int minValue, int maxValue)
    {
        var n = random.Next(minValue, maxValue);
        // Debug.Log(counter + ": " + n, this);
        counter++;

        return n;
    }
    public int NextInt(float minValue, float maxValue)
    {
        var n = random.Next((int)minValue, (int)maxValue);
        // Debug.Log(counter + ": " + n, this);
        counter++;

        return n;
    }
    public float Range()
    {
        var n = (float)random.NextDouble();
        // Debug.Log(counter + ": " + n, this);
        counter++;

        return n;
    }
    
    public float Range(float min, float max)
    {
        var n = (float)(random.NextDouble() * (max - min) + min);
        // Debug.Log(counter + ": " + n, this);
        counter++;

        return n;
    }

    internal ulong NextULong()
    {
        byte[] buf = new byte[8];
        var i1 = System.BitConverter.GetBytes(NextInt(int.MinValue, int.MaxValue));
        buf[0] = i1[0];
        buf[1] = i1[1];
        buf[2] = i1[2];
        buf[3] = i1[3];
        i1 = System.BitConverter.GetBytes(NextInt(int.MinValue, int.MaxValue));
        buf[4] = i1[0];
        buf[5] = i1[1];
        buf[6] = i1[2];
        buf[7] = i1[3];
        return System.BitConverter.ToUInt64(buf, 0);
    }
}
