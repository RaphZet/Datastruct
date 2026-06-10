using System.Collections.Generic;
using UnityEngine;

public class DijkstraPathfinder : MonoBehaviour
{
    [Header("Database")]
    public DijkstraGraphDatabase graphDatabase;
    public bool isUndirectedGraph = true;

    [Header("Visual Prefabs")]
    public GameObject nodePrefab;
    public Material nodeMaterial;
    public Material edgeMaterial;
    public Material pathMaterial;
    public Material textMaterial;

    [Header("Visual Settings")]
    public float nodeScale = 0.35f;
    public float edgeWidth = 0.045f;
    public float pathWidth = 0.12f;
    public float labelSize = 0.28f;
    public float weightLabelSize = 0.22f;
    public Vector3 labelOffset = new Vector3(0, 0.45f, 0);
    public Vector3 weightLabelOffset = new Vector3(0, 0.18f, 0);

    private readonly List<GameObject> spawnedObjects = new List<GameObject>();

    private void Start()
    {
        RunAndVisualize();
    }

    [ContextMenu("Run And Visualize")]
    public void RunAndVisualize()
    {
        ClearVisuals();

        if (graphDatabase == null)
        {
            Debug.LogError("no database");
            return;
        }

        DijkstraGraphSample sample = graphDatabase.GetSelectedSampleCopy();
        Dictionary<string, Vector3> nodePositions = new Dictionary<string, Vector3>();

        foreach (DijkstraNodeData node in sample.nodes)
        {
            nodePositions[node.id] = node.position;
            CreateNodeVisual(node);
        }

        foreach (DijkstraEdgeData edge in sample.edges)
            CreateEdgeVisual(edge, nodePositions);

        List<string> shortestPath = FindShortestPath(sample);
        float totalCost = CalculatePathCost(shortestPath, sample.edges);
        CreatePathLine(shortestPath, nodePositions);

        Debug.Log($"[Dijkstra] Sample: {sample.sampleName}");
        Debug.Log($"[Dijkstra] Path: {string.Join(" -> ", shortestPath)} | Total Weight: {totalCost}");
    }

    private List<string> FindShortestPath(DijkstraGraphSample sample)
    {
        Dictionary<string, float> distance = new Dictionary<string, float>();
        Dictionary<string, string> previous = new Dictionary<string, string>();
        List<string> unvisited = new List<string>();

        foreach (DijkstraNodeData node in sample.nodes)
        {
            distance[node.id] = Mathf.Infinity;
            previous[node.id] = null;
            unvisited.Add(node.id);
        }

        distance[sample.startNodeId] = 0f;

        while (unvisited.Count > 0)
        {
            string current = GetLowestDistanceNode(unvisited, distance);
            if (current == null)
                break;

            if (current == sample.targetNodeId)
                break;

            unvisited.Remove(current);

            foreach (DijkstraEdgeData edge in sample.edges)
            {
                string neighbor = null;

                if (edge.from == current)
                    neighbor = edge.to;
                else if (isUndirectedGraph && edge.to == current)
                    neighbor = edge.from;
                else
                    continue;

                float newDistance = distance[current] + edge.weight;
                if (newDistance < distance[neighbor])
                {
                    distance[neighbor] = newDistance;
                    previous[neighbor] = current;
                }
            }
        }

        return ReconstructPath(previous, sample.startNodeId, sample.targetNodeId);
    }

    private string GetLowestDistanceNode(List<string> unvisited, Dictionary<string, float> distance)
    {
        string bestNode = null;
        float bestDistance = Mathf.Infinity;

        foreach (string node in unvisited)
        {
            if (distance[node] < bestDistance)
            {
                bestDistance = distance[node];
                bestNode = node;
            }
        }

        return bestNode;
    }

    private List<string> ReconstructPath(Dictionary<string, string> previous, string start, string target)
    {
        List<string> path = new List<string>();
        string current = target;

        while (!string.IsNullOrEmpty(current))
        {
            path.Insert(0, current);
            current = previous.ContainsKey(current) ? previous[current] : null;
        }

        if (path.Count == 0 || path[0] != start)
            return new List<string>();

        return path;
    }

    private float CalculatePathCost(List<string> path, List<DijkstraEdgeData> edges)
    {
        float total = 0f;
        for (int i = 0; i < path.Count - 1; i++)
        {
            DijkstraEdgeData edge = edges.Find(e =>
                (e.from == path[i] && e.to == path[i + 1]) ||
                (isUndirectedGraph && e.from == path[i + 1] && e.to == path[i]));

            if (edge != null)
                total += edge.weight;
        }
        return total;
    }

    private void CreateNodeVisual(DijkstraNodeData node)
    {
        GameObject visual;
        if (nodePrefab != null)
            visual = Instantiate(nodePrefab, node.position, Quaternion.identity, transform);
        else
            visual = GameObject.CreatePrimitive(PrimitiveType.Sphere);

        visual.name = "Dijkstra Node " + node.id;
        visual.transform.SetParent(transform);
        visual.transform.position = node.position;
        visual.transform.localScale = Vector3.one * nodeScale;
        ApplyMaterial(visual, nodeMaterial);
        spawnedObjects.Add(visual);

        CreateText(node.id, node.position + labelOffset, labelSize, "Node Label " + node.id);
    }

    private void CreateEdgeVisual(DijkstraEdgeData edge, Dictionary<string, Vector3> nodePositions)
    {
        if (!nodePositions.ContainsKey(edge.from) || !nodePositions.ContainsKey(edge.to))
            return;

        Vector3 start = nodePositions[edge.from];
        Vector3 end = nodePositions[edge.to];
        CreateLine("Edge " + edge.from + " to " + edge.to, new List<Vector3> { start, end }, edgeWidth, edgeMaterial);

        Vector3 mid = (start + end) * 0.5f;
        CreateText(edge.weight.ToString("0"), mid + weightLabelOffset, weightLabelSize, "Weight " + edge.from + edge.to);
    }

    private void CreatePathLine(List<string> path, Dictionary<string, Vector3> nodePositions)
    {
        if (path == null || path.Count < 2)
            return;

        List<Vector3> points = new List<Vector3>();
        foreach (string nodeId in path)
        {
            if (nodePositions.ContainsKey(nodeId))
                points.Add(nodePositions[nodeId] + new Vector3(0, 0, -0.05f));
        }

        CreateLine("Dijkstra Shortest Path", points, pathWidth, pathMaterial);
    }

    private void CreateLine(string objectName, List<Vector3> points, float width, Material material)
    {
        GameObject lineObject = new GameObject(objectName);
        lineObject.transform.SetParent(transform);
        LineRenderer lineRenderer = lineObject.AddComponent<LineRenderer>();
        lineRenderer.positionCount = points.Count;
        lineRenderer.SetPositions(points.ToArray());
        lineRenderer.startWidth = width;
        lineRenderer.endWidth = width;
        lineRenderer.useWorldSpace = true;
        lineRenderer.material = material != null ? material : new Material(Shader.Find("Sprites/Default"));
        spawnedObjects.Add(lineObject);
    }

    private void CreateText(string content, Vector3 position, float size, string objectName)
    {
        GameObject textObject = new GameObject(objectName);
        textObject.transform.SetParent(transform);
        textObject.transform.position = position;
        TextMesh textMesh = textObject.AddComponent<TextMesh>();
        textMesh.text = content;
        textMesh.anchor = TextAnchor.MiddleCenter;
        textMesh.alignment = TextAlignment.Center;
        textMesh.characterSize = size;
        textMesh.fontSize = 64;
        if (textMaterial != null)
            textMesh.GetComponent<MeshRenderer>().material = textMaterial;
        spawnedObjects.Add(textObject);
    }

    private void ApplyMaterial(GameObject target, Material material)
    {
        Renderer renderer = target.GetComponent<Renderer>();
        if (renderer != null && material != null)
            renderer.material = material;
    }

    private void ClearVisuals()
    {
        for (int i = spawnedObjects.Count - 1; i >= 0; i--)
        {
            if (spawnedObjects[i] != null)
            {
                if (Application.isPlaying)
                    Destroy(spawnedObjects[i]);
                else
                    DestroyImmediate(spawnedObjects[i]);
            }
        }
        spawnedObjects.Clear();
    }
}
