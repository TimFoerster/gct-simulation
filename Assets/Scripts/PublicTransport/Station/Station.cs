using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.AI;

public class Station : MonoBehaviour
{
    [SerializeField] public StationPlatform platformN;
    [SerializeField] public StationPlatform platformS;
    [SerializeField] public GameObject objectToSpawn;

    [SerializeField] public SpawnEdge[] Spawns;
    [SerializeField] public Exit[] Exits;
    [SerializeField] public GameObject Persons;
    [SerializeField] public TextMeshPro Text;
    [SerializeField] public NavMeshSurface2d NavMesh;
    [SerializeField] public Camera Camera;

    public int Index;

    public NavMeshData NavMeshData { get => NavMesh.navMeshData; set => NavMesh.navMeshData = value; }

    public float SpawnModifier = 1;

    void Awake()
    {
        Spawns = GetComponentsInChildren<SpawnEdge>();
        Exits = GetComponentsInChildren<Exit>();
        NavMesh = GetComponentInChildren<NavMeshSurface2d>();
        UpdateMesh();
        Text.text = transform.name;
    }

    internal void UpdateMesh()
    {
        NavMesh.BuildNavMesh();
    }

}
