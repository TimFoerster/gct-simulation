using System;
using System.Linq;
using UnityEngine;

[Serializable]
public struct Vector2d
{

    public double x;
    public double y;

    public Vector2d(double x, double y)
    {
        this.x = x;
        this.y = y;
    }

    public static Vector2d Zero { get => new Vector2d(0, 0); }

    public double X { get => x; }

    public double Y { get => y; }

    public static Vector2d operator +(Vector2d a, Vector2d b)
    {
        return new Vector2d(a.x + b.x, a.y + b.y);
    }

    public static Vector2d operator *(float a, Vector2d b)
    {
        return new Vector2d(a * b.x, a * b.y);
    }

    public static Vector2d operator *(Vector2d b, double a)
    {
        return new Vector2d(a * b.x, a * b.y);
    }

    public static Vector2d operator *(Vector2d a, Vector2d b)
    {
        return new Vector2d(a.x * b.x, a.y * b.y);
    }

    internal double Length() => Math.Sqrt(x * x + y * y);

    internal Vector2d Normalize()
    {
        var length = Length();
        return new Vector2d(x / length, y / length);
    }

    internal Vector2 AsVector2() => new Vector2((float)x, (float)y);

    internal Double Magnitude()
     =>  System.Math.Sqrt(x * x + y * y);

}

static class CidCalculator
{

    public const double deg2rad = Math.PI / 180d;
    public const double rad2deg = 180d / Math.PI;

    const ulong n_cis = ulong.MaxValue;
    // n_cis in Deg
    const double n_cisDegRatio = 360d / n_cis;
    // 360° / n_cis in Rad
    //const double n_cisRadRatio = n_cisDegRatio * deg2rad;
    // => 2pi / n_cis
    const double n_cisRadRatio = (2d * Math.PI) / n_cis;

    //const double n_cisValueRatio = n_cis / 360d * rad2deg;
    const double n_cisValueRatio = n_cis / (2d * Math.PI);

    public static ulong meanBySumVector(System.Collections.Generic.IEnumerable<ulong> values)
    {
        if (values.Count() == 0)
        {
            return 0;
        }

        var sumVector = Vector2d.Zero;
        foreach(var val in values)
        {
            sumVector += ulongToVector(val);
        }

        return vectorToUlong(sumVector);

    }

    public static double UlongToRad(ulong value)
    {
        return n_cisRadRatio * value;
    }
    public static double UlongToDeg(ulong value)
    {
        return n_cisDegRatio * value;
    }

    public static Vector2d ulongToVector(ulong value)
    {
        var rad = UlongToRad(value);
        return new Vector2d(
            Math.Cos(rad),
            Math.Sin(rad)
        );
    }

    public static ulong vectorToUlong(Vector2d val)
    {
        return (ulong)((Math.Atan2(val.Y, val.X)) * n_cisValueRatio);
    }
}
