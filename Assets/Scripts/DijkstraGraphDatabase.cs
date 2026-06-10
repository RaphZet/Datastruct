using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "DijkstraGraphDatabase", menuName = "Shortest Path/Dijkstra Graph Database v7")]
public class DijkstraGraphDatabase : ScriptableObject
{
    [Header("Sample Selection")]
    public int sampleIndex = 0;

    [Header("Auto Layout")]
    public bool useAutoLayout = true;
    public float autoLayoutRadius = 4f;

    [Header("Graph Samples")]
    public List<DijkstraGraphSample> samples = new List<DijkstraGraphSample>();

    private void OnValidate()
    {
        if (samples == null || samples.Count == 0)
            CreateDefaultSamples();

        sampleIndex = Mathf.Clamp(sampleIndex, 0, Mathf.Max(0, samples.Count - 1));
    }

    public DijkstraGraphSample GetSelectedSampleCopy()
    {
        if (samples == null || samples.Count == 0)
            CreateDefaultSamples();

        sampleIndex = Mathf.Clamp(sampleIndex, 0, samples.Count - 1);
        DijkstraGraphSample source = samples[sampleIndex];
        DijkstraGraphSample copy = source.Clone();

        if (useAutoLayout)
            ApplyCircularAutoLayout(copy.nodes, autoLayoutRadius);

        return copy;
    }

    private void ApplyCircularAutoLayout(List<DijkstraNodeData> nodes, float radius)
    {
        if (nodes == null || nodes.Count == 0)
            return;

        for (int i = 0; i < nodes.Count; i++)
        {
            float angle = (Mathf.PI * 2f * i / nodes.Count) + Mathf.PI * 0.5f;
            float x = Mathf.Cos(angle) * radius;
            float y = Mathf.Sin(angle) * radius;
            nodes[i].position = new Vector3(x, y, 0f);
        }
    }

    [ContextMenu("Create Default Samples")]
    public void CreateDefaultSamples()
    {
        samples = new List<DijkstraGraphSample>();

        DijkstraGraphSample simple = new DijkstraGraphSample
        {
            sampleName = "Simple Weighted Graph",
            startNodeId = "A",
            targetNodeId = "F",
            nodes = new List<DijkstraNodeData>
            {
                new DijkstraNodeData("A", new Vector3(-4, 1, 0)),
                new DijkstraNodeData("B", new Vector3(-2, 2, 0)),
                new DijkstraNodeData("C", new Vector3(-2, 0, 0)),
                new DijkstraNodeData("D", new Vector3(0, 1, 0)),
                new DijkstraNodeData("E", new Vector3(2, 1, 0)),
                new DijkstraNodeData("F", new Vector3(4, 1, 0))
            },
            edges = new List<DijkstraEdgeData>
            {
                new DijkstraEdgeData("A", "B", 4),
                new DijkstraEdgeData("A", "C", 2),
                new DijkstraEdgeData("C", "B", 1),
                new DijkstraEdgeData("B", "D", 5),
                new DijkstraEdgeData("C", "D", 8),
                new DijkstraEdgeData("D", "E", 2),
                new DijkstraEdgeData("E", "F", 1),
                new DijkstraEdgeData("D", "F", 6)
            }
        };
        samples.Add(simple);

        DijkstraGraphSample city = new DijkstraGraphSample
        {
            sampleName = "City Route Graph",
            startNodeId = "Home",
            targetNodeId = "Campus",
            nodes = new List<DijkstraNodeData>
            {
                new DijkstraNodeData("Home", new Vector3(-4, -1, 0)),
                new DijkstraNodeData("Mall", new Vector3(-2, 1.5f, 0)),
                new DijkstraNodeData("Park", new Vector3(-1, -1.5f, 0)),
                new DijkstraNodeData("Station", new Vector3(1, 1.5f, 0)),
                new DijkstraNodeData("Library", new Vector3(2, -1, 0)),
                new DijkstraNodeData("Campus", new Vector3(4, 0, 0))
            },
            edges = new List<DijkstraEdgeData>
            {
                new DijkstraEdgeData("Home", "Mall", 7),
                new DijkstraEdgeData("Home", "Park", 3),
                new DijkstraEdgeData("Park", "Mall", 2),
                new DijkstraEdgeData("Mall", "Station", 4),
                new DijkstraEdgeData("Park", "Library", 6),
                new DijkstraEdgeData("Station", "Campus", 5),
                new DijkstraEdgeData("Library", "Campus", 2),
                new DijkstraEdgeData("Station", "Library", 1)
            }
        };
        samples.Add(city);

        DijkstraGraphSample dungeon = new DijkstraGraphSample
        {
            sampleName = "Dungeon Room Graph",
            startNodeId = "Spawn",
            targetNodeId = "Boss",
            nodes = new List<DijkstraNodeData>
            {
                new DijkstraNodeData("Spawn", Vector3.zero),
                new DijkstraNodeData("RoomA", Vector3.zero),
                new DijkstraNodeData("RoomB", Vector3.zero),
                new DijkstraNodeData("Chest", Vector3.zero),
                new DijkstraNodeData("Trap", Vector3.zero),
                new DijkstraNodeData("Boss", Vector3.zero)
            },
            edges = new List<DijkstraEdgeData>
            {
                new DijkstraEdgeData("Spawn", "RoomA", 2),
                new DijkstraEdgeData("Spawn", "RoomB", 6),
                new DijkstraEdgeData("RoomA", "Chest", 2),
                new DijkstraEdgeData("Chest", "Boss", 5),
                new DijkstraEdgeData("RoomB", "Trap", 1),
                new DijkstraEdgeData("Trap", "Boss", 9),
                new DijkstraEdgeData("RoomA", "RoomB", 3)
            }
        };
        samples.Add(dungeon);
    }
}

[Serializable]
public class DijkstraGraphSample
{
    public string sampleName;
    public string startNodeId;
    public string targetNodeId;
    public List<DijkstraNodeData> nodes = new List<DijkstraNodeData>();
    public List<DijkstraEdgeData> edges = new List<DijkstraEdgeData>();

    public DijkstraGraphSample Clone()
    {
        DijkstraGraphSample copy = new DijkstraGraphSample
        {
            sampleName = sampleName,
            startNodeId = startNodeId,
            targetNodeId = targetNodeId,
            nodes = new List<DijkstraNodeData>(),
            edges = new List<DijkstraEdgeData>()
        };

        foreach (DijkstraNodeData node in nodes)
            copy.nodes.Add(new DijkstraNodeData(node.id, node.position));

        foreach (DijkstraEdgeData edge in edges)
            copy.edges.Add(new DijkstraEdgeData(edge.from, edge.to, edge.weight));

        return copy;
    }
}

[Serializable]
public class DijkstraNodeData
{
    public string id;
    public Vector3 position;

    public DijkstraNodeData(string id, Vector3 position)
    {
        this.id = id;
        this.position = position;
    }
}

[Serializable]
public class DijkstraEdgeData
{
    public string from;
    public string to;
    public float weight;

    public DijkstraEdgeData(string from, string to, float weight)
    {
        this.from = from;
        this.to = to;
        this.weight = weight;
    }
}
