


using System;
using UnityEngine;

public class Utils
{


    public static Color U64ToRGB(ulong value)
    {
        //  color from 64 bits to 24 bits  => 3bytes (r,g,b)
        // take byte between 8 to 5
        var colorBytes = BitConverter.GetBytes(value);

        float r = colorBytes[7] / 255f;
        float g = colorBytes[6] / 255f;
        float b = colorBytes[5] / 255f;

        return new Color(r, g, b);
    }

    public static Color U64ToHSV(ulong value)
    {
        var colorBytes = BitConverter.GetBytes(value);
        float h = colorBytes[7] / 255f;
        //float s = colorBytes[6] / 255f;

        return Color.HSVToRGB(h, 1, 1);
    }

    public static Color U128ToColor(System.Numerics.BigInteger value)
    {
        //  color from 128 bits to 24 bits  => 3bytes (r,g,b)
        // take byte between 16 to 14
        var colorBytes = value.ToByteArray();

        float r = colorBytes.Length > 15 ? colorBytes[15] / 255f : 0f;
        float g = colorBytes.Length > 14 ? colorBytes[14] / 255f : 0f;
        float b = colorBytes.Length > 13  ? colorBytes[13] / 255f : 0f;

        return new Color(r,g,b);
    }

    public static T GetClosestBoundingBox<T>(T[] objects, Vector3 pos) where T : Component
    {

        float closestDist = float.MaxValue;
        T closestObject = null;

        foreach (var obj in objects)
        {
            var bounds = obj.GetComponent<Collider2D>().bounds;
            var closestPoint = bounds.ClosestPoint(pos);

            var dist = Vector3.Distance(pos, closestPoint);

            if (dist < closestDist)
            {
                closestDist = dist;
                closestObject = obj;
            }
        }

        return closestObject;
    } 

    public static Vector3 RandomPositionInBounds(Bounds bounds, float margin = .3f)
    {
        return new Vector3(
            UnityEngine.Random.Range(bounds.min.x + margin, bounds.max.x - margin),
            UnityEngine.Random.Range(bounds.min.y + margin, bounds.max.y - margin),
            0
        );
    }

    public static Vector3 RandomPositionInBounds(Bounds bounds, RandomNumberGenerator rng, float margin = .3f)
    {
        return new Vector3(
            rng.Range(bounds.min.x + margin, bounds.max.x - margin),
            rng.Range(bounds.min.y + margin, bounds.max.y - margin),
            0
        );
    }

    public static Vector3 GetRandomPointInsideCollider(Collider2D collider, RandomNumberGenerator rng, float margin = .3f)
    {
        if (collider is BoxCollider2D boxCollider)
        {
            Vector2 extents = boxCollider.size / 2f;
            Vector2 point = new Vector2(
                rng.Range(-extents.x + margin, extents.x - margin),
                rng.Range(-extents.y + margin, extents.y - margin)
            ) + collider.offset;
            return collider.transform.TransformPoint(point);
        }

        var pos = RandomPositionInBounds(collider.bounds, rng);
        return collider.bounds.ClosestPoint(pos);
    }



    /* 
     * Partial Scripts from Code Money 
     * @auhor unitycodemonkey.com
     */
    public static Vector3 GetMouseWorldPosition()
    {
        Vector3 vec = GetMouseWorldPositionWithZ(Input.mousePosition, Camera.main);
        vec.z = 0f;
        return vec;
    }

    public static Vector3 GetMouseWorldPositionWithZ()
    {
        return GetMouseWorldPositionWithZ(Input.mousePosition, Camera.main);
    }
    public static Vector3 GetMouseWorldPositionWithZ(Camera worldCamera)
    {
        return GetMouseWorldPositionWithZ(Input.mousePosition, worldCamera);
    }
    public static Vector3 GetMouseWorldPositionWithZ(Vector3 screenPosition, Camera worldCamera)
    {
        Vector3 worldPosition = worldCamera.ScreenToWorldPoint(screenPosition);
        return worldPosition;
    }

    public static T FindClosestObject<T>(T[] objects, Vector3 position) where T : MonoBehaviour
    {
        // find closest waiting area
        T closest = default;
        float closestDistance = float.PositiveInfinity;
        foreach (var area in objects)
        {
            var dist = Vector3.Distance(area.transform.position, position);
            if (dist < closestDistance)
            {
                closest = area;
                closestDistance = dist;
            }
        }

        return closest;
    }

    public static BoxCollider2D FindClosestObject(BoxCollider2D[] objects, Vector3 position)
    {
        // find closest waiting area
        BoxCollider2D closest = default;
        float closestDistance = float.PositiveInfinity;
        foreach (var area in objects)
        {
            var dist = area.bounds.SqrDistance(position);
            if (dist < closestDistance)
            {
                closest = area;
                closestDistance = dist;
            }
        }

        return closest;
    }


}
