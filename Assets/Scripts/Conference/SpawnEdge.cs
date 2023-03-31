using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(EdgeCollider2D))]
public class SpawnEdge : MonoBehaviour
{

    public enum SpawnType
    {
        Outside,
        Inside
    }

    public float spawnRate = 1;
    EdgeCollider2D edgeCollider;
    
    public SpawnType spawnType;

    void Awake()
    {
        edgeCollider = GetComponent<EdgeCollider2D>();  
    }

    public Vector3 RandomPosition(RandomNumberGenerator rng)
    {
        return Utils.RandomPositionInBounds(edgeCollider.bounds, rng);
    }

}
