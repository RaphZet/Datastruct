using System.Collections.Generic;
using UnityEngine;

public class AStarGridPathfinder : MonoBehaviour
{
    [Header("Database")]
    public AStarGridDatabase gridDatabase;

    [Header("Optional Prefab")]
    public GameObject cellPrefab;

    private readonly List<GameObject> spawnedObjects = new List<GameObject>();
    private AStarCell[,] grid;
    private Vector2Int source;
    private Vector2Int target;
    private List<Vector2Int> lastPath = new List<Vector2Int>();
    private HashSet<Vector2Int> visitedCells = new HashSet<Vector2Int>();
    private HashSet<Vector2Int> pathCells = new HashSet<Vector2Int>();

    private Material fallbackUnblocked;
    private Material fallbackBlocked;
    private Material fallbackSource;
    private Material fallbackTarget;
    private Material fallbackVisited;
    private Material fallbackPath;
    private Material fallbackGridLine;

    private void Start()
    {
        GenerateAndVisualize();
    }

    [ContextMenu("Generate And Visualize")]
    public void GenerateAndVisualize()
    {
        ClearVisuals();

        if (gridDatabase == null)
        {
            Debug.LogError("No database");
            return;
        }

        GenerateGridWithValidPath();
        pathCells = new HashSet<Vector2Int>(lastPath);
        DrawGrid();
        DrawGridLines();
        DrawPath();
        DrawLabels();

        Debug.Log($"[A*] Grid: {gridDatabase.columns}x{gridDatabase.rows} | Blocked: {gridDatabase.blockedCount}");
        Debug.Log($"[A*] Source: ({source.x + 1},{source.y + 1}) Target: ({target.x + 1},{target.y + 1})");
        Debug.Log($"[A*] Path: {FormatPath(lastPath)}");
    }

    private void GenerateGridWithValidPath()
    {
        int attempts = Mathf.Max(1, gridDatabase.maxGenerationAttempts);
        for (int attempt = 0; attempt < attempts; attempt++)
        {
            GenerateGrid(attempt);
            lastPath = FindPath(source, target);
            if (lastPath.Count > 0)
                return;
        }

        Debug.LogWarning("A* gagal membuat random blocked cells yang punya path valid. Blocked cells dikurangi otomatis.");
        int originalBlocked = gridDatabase.blockedCount;
        int safeBlockedCount = Mathf.Max(0, originalBlocked / 2);
        GenerateGrid(999, safeBlockedCount);
        lastPath = FindPath(source, target);
    }

    private void GenerateGrid(int attemptOffset, int overrideBlockedCount = -1)
    {
        int columns = gridDatabase.columns;
        int rows = gridDatabase.rows;
        grid = new AStarCell[columns, rows];
        visitedCells.Clear();

        int baseSeed = gridDatabase.randomizeEveryPlay ? System.Environment.TickCount : 777;
        System.Random random = new System.Random(baseSeed + attemptOffset);

        int sourceColumn = gridDatabase.randomizeSourceColumn ? random.Next(0, columns) : gridDatabase.sourceColumn;
        int targetColumn = gridDatabase.randomizeTargetColumn ? random.Next(0, columns) : gridDatabase.targetColumn;

        source = new Vector2Int(sourceColumn, rows - 1);
        target = new Vector2Int(targetColumn, 0);

        for (int x = 0; x < columns; x++)
        {
            for (int y = 0; y < rows; y++)
                grid[x, y] = new AStarCell(new Vector2Int(x, y));
        }

        int blockedTarget = overrideBlockedCount >= 0 ? overrideBlockedCount : gridDatabase.blockedCount;
        blockedTarget = Mathf.Clamp(blockedTarget, 0, (columns * rows) - 2);

        int placed = 0;
        int guard = 0;
        while (placed < blockedTarget && guard < columns * rows * 25)
        {
            guard++;
            int x = random.Next(0, columns);
            int y = random.Next(0, rows);
            Vector2Int candidate = new Vector2Int(x, y);

            if (candidate == source || candidate == target)
                continue;

            if (grid[x, y].blocked)
                continue;

            grid[x, y].blocked = true;
            placed++;
        }
    }

    private List<Vector2Int> FindPath(Vector2Int start, Vector2Int end)
    {
        List<AStarCell> openList = new List<AStarCell>();
        HashSet<AStarCell> closedSet = new HashSet<AStarCell>();

        AStarCell startCell = grid[start.x, start.y];
        AStarCell targetCell = grid[end.x, end.y];

        ResetPathValues();
        startCell.gCost = 0f;
        startCell.hCost = GetHeuristicCost(startCell.position, targetCell.position);
        openList.Add(startCell);

        while (openList.Count > 0)
        {
            AStarCell current = GetLowestFCostCell(openList);
            openList.Remove(current);
            closedSet.Add(current);
            visitedCells.Add(current.position);

            if (current == targetCell)
                return ReconstructPath(targetCell);

            foreach (AStarCell neighbor in GetNeighbors(current))
            {
                if (neighbor.blocked || closedSet.Contains(neighbor))
                    continue;

                float tentativeGCost = current.gCost + GetMoveCost(current.position, neighbor.position);
                if (tentativeGCost < neighbor.gCost)
                {
                    neighbor.parent = current;
                    neighbor.gCost = tentativeGCost;
                    neighbor.hCost = GetHeuristicCost(neighbor.position, targetCell.position);

                    if (!openList.Contains(neighbor))
                        openList.Add(neighbor);
                }
            }
        }

        return new List<Vector2Int>();
    }

    private void ResetPathValues()
    {
        for (int x = 0; x < gridDatabase.columns; x++)
        {
            for (int y = 0; y < gridDatabase.rows; y++)
            {
                grid[x, y].gCost = Mathf.Infinity;
                grid[x, y].hCost = 0f;
                grid[x, y].parent = null;
            }
        }
    }

    private AStarCell GetLowestFCostCell(List<AStarCell> cells)
    {
        AStarCell best = cells[0];
        for (int i = 1; i < cells.Count; i++)
        {
            bool lowerFCost = cells[i].FCost < best.FCost;
            bool sameFButLowerH = Mathf.Approximately(cells[i].FCost, best.FCost) && cells[i].hCost < best.hCost;
            if (lowerFCost || sameFButLowerH)
                best = cells[i];
        }
        return best;
    }

    private List<AStarCell> GetNeighbors(AStarCell cell)
    {
        List<AStarCell> neighbors = new List<AStarCell>();

        AddNeighbor(neighbors, cell.position.x, cell.position.y - 1);
        AddNeighbor(neighbors, cell.position.x, cell.position.y + 1);
        AddNeighbor(neighbors, cell.position.x - 1, cell.position.y);
        AddNeighbor(neighbors, cell.position.x + 1, cell.position.y);

        if (gridDatabase.allowDiagonalMovement)
        {
            TryAddDiagonalNeighbor(neighbors, cell.position, -1, -1);
            TryAddDiagonalNeighbor(neighbors, cell.position, 1, -1);
            TryAddDiagonalNeighbor(neighbors, cell.position, -1, 1);
            TryAddDiagonalNeighbor(neighbors, cell.position, 1, 1);
        }

        return neighbors;
    }

    private void TryAddDiagonalNeighbor(List<AStarCell> neighbors, Vector2Int from, int dx, int dy)
    {
        int nx = from.x + dx;
        int ny = from.y + dy;

        if (!IsInsideGrid(nx, ny))
            return;

        if (gridDatabase.preventCornerCutting)
        {
            int sideX = from.x + dx;
            int sideY = from.y;
            int verticalX = from.x;
            int verticalY = from.y + dy;

            if (!IsInsideGrid(sideX, sideY) || !IsInsideGrid(verticalX, verticalY))
                return;

            if (grid[sideX, sideY].blocked || grid[verticalX, verticalY].blocked)
                return;
        }

        AddNeighbor(neighbors, nx, ny);
    }

    private void AddNeighbor(List<AStarCell> neighbors, int x, int y)
    {
        if (!IsInsideGrid(x, y))
            return;
        neighbors.Add(grid[x, y]);
    }

    private bool IsInsideGrid(int x, int y)
    {
        return x >= 0 && x < gridDatabase.columns && y >= 0 && y < gridDatabase.rows;
    }

    private float GetMoveCost(Vector2Int a, Vector2Int b)
    {
        bool diagonal = a.x != b.x && a.y != b.y;
        return diagonal ? gridDatabase.diagonalMoveCost : gridDatabase.straightMoveCost;
    }

    private float GetHeuristicCost(Vector2Int a, Vector2Int b)
    {
        int dx = Mathf.Abs(a.x - b.x);
        int dy = Mathf.Abs(a.y - b.y);

        if (gridDatabase.allowDiagonalMovement)
        {
            float diagonal = Mathf.Min(dx, dy);
            float straight = Mathf.Abs(dx - dy);
            return (gridDatabase.diagonalMoveCost * diagonal) + (gridDatabase.straightMoveCost * straight);
        }

        return gridDatabase.straightMoveCost * (dx + dy);
    }

    private List<Vector2Int> ReconstructPath(AStarCell targetCell)
    {
        List<Vector2Int> path = new List<Vector2Int>();
        AStarCell current = targetCell;

        while (current != null)
        {
            path.Insert(0, current.position);
            current = current.parent;
        }

        return path;
    }

    private void DrawGrid()
    {
        for (int x = 0; x < gridDatabase.columns; x++)
        {
            for (int y = 0; y < gridDatabase.rows; y++)
            {
                Vector2Int coord = new Vector2Int(x, y);
                GameObject cellObject = CreateCellObject(coord);
                Renderer renderer = cellObject.GetComponent<Renderer>();

                if (renderer != null)
                    renderer.material = GetCellMaterial(coord);
            }
        }
    }

    private GameObject CreateCellObject(Vector2Int coord)
    {
        Vector3 position = GridToWorld(coord);
        GameObject cellObject;

        if (cellPrefab != null)
            cellObject = Instantiate(cellPrefab, position, Quaternion.identity, transform);
        else
            cellObject = GameObject.CreatePrimitive(PrimitiveType.Cube);

        cellObject.name = $"Cell ({coord.x + 1},{coord.y + 1})";
        cellObject.transform.SetParent(transform);
        cellObject.transform.position = position;
        float size = gridDatabase.cellSize - gridDatabase.cellGap;
        cellObject.transform.localScale = new Vector3(size, gridDatabase.cellHeight, size);
        spawnedObjects.Add(cellObject);
        return cellObject;
    }

    private Material GetCellMaterial(Vector2Int coord)
    {
        if (coord == source)
            return gridDatabase.sourceMaterial != null ? gridDatabase.sourceMaterial : GetFallbackSource();

        if (coord == target)
            return gridDatabase.targetMaterial != null ? gridDatabase.targetMaterial : GetFallbackTarget();

        if (grid[coord.x, coord.y].blocked)
            return gridDatabase.blockedMaterial != null ? gridDatabase.blockedMaterial : GetFallbackBlocked();

        if (gridDatabase.showVisitedCells && visitedCells.Contains(coord))
            return gridDatabase.visitedMaterial != null ? gridDatabase.visitedMaterial : GetFallbackVisited();

        return gridDatabase.unblockedMaterial != null ? gridDatabase.unblockedMaterial : GetFallbackUnblocked();
    }

    private void DrawPath()
    {
        if (lastPath == null || lastPath.Count < 2)
            return;

        GameObject pathObject = new GameObject("A* Shortest Path Line");
        pathObject.transform.SetParent(transform);
        LineRenderer lineRenderer = pathObject.AddComponent<LineRenderer>();
        lineRenderer.positionCount = lastPath.Count;
        lineRenderer.startWidth = gridDatabase.pathLineWidth;
        lineRenderer.endWidth = gridDatabase.pathLineWidth;
        lineRenderer.useWorldSpace = true;
        lineRenderer.material = gridDatabase.pathMaterial != null ? gridDatabase.pathMaterial : GetFallbackPath();

        for (int i = 0; i < lastPath.Count; i++)
            lineRenderer.SetPosition(i, GridToWorld(lastPath[i]) + new Vector3(0, gridDatabase.labelHeightOffset + 0.08f, 0));

        spawnedObjects.Add(pathObject);
    }

    private void DrawLabels()
    {
        if (gridDatabase.showAxisLabels)
            DrawAxisLabels();

        if (gridDatabase.showCoordinateLabels)
            DrawAllCoordinateLabels();
        else if (gridDatabase.showPathCoordinateLabels)
            DrawPathCoordinateLabels();
    }

    private void DrawAllCoordinateLabels()
    {
        for (int x = 0; x < gridDatabase.columns; x++)
        {
            for (int y = 0; y < gridDatabase.rows; y++)
            {
                Vector2Int coord = new Vector2Int(x, y);
                if (grid[coord.x, coord.y].blocked)
                    continue;

                Vector3 textPos = GridToWorld(coord) + new Vector3(0, gridDatabase.labelHeightOffset + 0.12f, 0);
                CreateText($"{x + 1},{y + 1}", textPos, GetSafeLabelSize(), "Cell Coord Label", gridDatabase.cellLabelColor);
            }
        }
    }

    private void DrawPathCoordinateLabels()
    {
        for (int i = 0; i < lastPath.Count; i++)
        {
            Vector2Int coord = lastPath[i];
            Vector3 textPos = GridToWorld(coord) + new Vector3(0, gridDatabase.labelHeightOffset + 0.16f, 0);
            CreateText($"{coord.x + 1},{coord.y + 1}", textPos, GetSafeLabelSize(), "Path Coord Label", gridDatabase.pathLabelColor);
        }
    }

    private void DrawGridLines()
    {
        Material lineMaterial = gridDatabase.gridLineMaterial != null ? gridDatabase.gridLineMaterial : GetFallbackGridLine();
        float lineWidth = Mathf.Max(0.01f, gridDatabase.cellSize * 0.018f);
        float topY = gridDatabase.cellHeight * 0.65f;
        float half = gridDatabase.cellSize * 0.5f;

        float minX = GridToWorld(new Vector2Int(0, 0)).x - half;
        float maxX = GridToWorld(new Vector2Int(gridDatabase.columns - 1, 0)).x + half;
        float topZ = GridToWorld(new Vector2Int(0, 0)).z + half;
        float bottomZ = GridToWorld(new Vector2Int(0, gridDatabase.rows - 1)).z - half;

        for (int x = 0; x <= gridDatabase.columns; x++)
        {
            float lineX = minX + x * gridDatabase.cellSize;
            CreateLine($"Grid Vertical {x}", new Vector3(lineX, transform.position.y + topY, topZ), new Vector3(lineX, transform.position.y + topY, bottomZ), lineMaterial, lineWidth);
        }

        for (int y = 0; y <= gridDatabase.rows; y++)
        {
            float lineZ = topZ - y * gridDatabase.cellSize;
            CreateLine($"Grid Horizontal {y}", new Vector3(minX, transform.position.y + topY, lineZ), new Vector3(maxX, transform.position.y + topY, lineZ), lineMaterial, lineWidth);
        }
    }

    private void CreateLine(string objectName, Vector3 start, Vector3 end, Material material, float width)
    {
        GameObject lineObject = new GameObject(objectName);
        lineObject.transform.SetParent(transform);
        LineRenderer lr = lineObject.AddComponent<LineRenderer>();
        lr.positionCount = 2;
        lr.SetPosition(0, start);
        lr.SetPosition(1, end);
        lr.startWidth = width;
        lr.endWidth = width;
        lr.material = material;
        lr.useWorldSpace = true;
        spawnedObjects.Add(lineObject);
    }

    private void DrawAxisLabels()
    {
        float axisOffset = gridDatabase.cellSize * 0.76f;
        float topZ = GridToWorld(new Vector2Int(0, 0)).z + axisOffset;
        float leftX = GridToWorld(new Vector2Int(0, 0)).x - axisOffset;
        float textY = transform.position.y + gridDatabase.labelHeightOffset + 0.18f;
        float size = GetSafeLabelSize() * 1.35f;

        for (int x = 0; x < gridDatabase.columns; x++)
        {
            Vector3 cellWorld = GridToWorld(new Vector2Int(x, 0));
            CreateText((x + 1).ToString(), new Vector3(cellWorld.x, textY, topZ), size, "Column Label", gridDatabase.axisLabelColor);
        }

        for (int y = 0; y < gridDatabase.rows; y++)
        {
            Vector3 cellWorld = GridToWorld(new Vector2Int(0, y));
            CreateText((y + 1).ToString(), new Vector3(leftX, textY, cellWorld.z), size, "Row Label", gridDatabase.axisLabelColor);
        }
    }

    private Vector3 GridToWorld(Vector2Int coord)
    {
        float width = (gridDatabase.columns - 1) * gridDatabase.cellSize;
        float height = (gridDatabase.rows - 1) * gridDatabase.cellSize;
        float x = (coord.x * gridDatabase.cellSize) - width * 0.5f;
        float z = -(coord.y * gridDatabase.cellSize) + height * 0.5f;
        return transform.position + new Vector3(x, 0, z);
    }

    private float GetSafeLabelSize()
    {
        return Mathf.Min(gridDatabase.labelSize, gridDatabase.cellSize * 0.14f);
    }

    private void CreateText(string content, Vector3 position, float size, string objectName, Color color)
    {
        GameObject textObject = new GameObject(objectName + " " + content);
        textObject.transform.SetParent(transform);
        textObject.transform.position = position;
        textObject.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

        TextMesh textMesh = textObject.AddComponent<TextMesh>();
        textMesh.text = content;
        textMesh.anchor = TextAnchor.MiddleCenter;
        textMesh.alignment = TextAlignment.Center;
        textMesh.characterSize = size;
        textMesh.fontSize = 64;
        textMesh.color = color;

        MeshRenderer meshRenderer = textObject.GetComponent<MeshRenderer>();
        if (meshRenderer != null && gridDatabase.textMaterial != null)
            meshRenderer.material = gridDatabase.textMaterial;

        spawnedObjects.Add(textObject);
    }

    private string FormatPath(List<Vector2Int> path)
    {
        if (path == null || path.Count == 0)
            return "No valid path";

        List<string> parts = new List<string>();
        foreach (Vector2Int p in path)
            parts.Add($"({p.x + 1},{p.y + 1})");
        return string.Join(" -> ", parts);
    }

    private Material MakeMaterial(string name, Color color)
    {
        Material material = new Material(Shader.Find("Standard"));
        material.name = name;
        material.color = color;
        return material;
    }

    private Material GetFallbackUnblocked() => fallbackUnblocked ?? (fallbackUnblocked = MakeMaterial("Fallback Unblocked", Color.white));
    private Material GetFallbackBlocked() => fallbackBlocked ?? (fallbackBlocked = MakeMaterial("Fallback Blocked", Color.gray));
    private Material GetFallbackSource() => fallbackSource ?? (fallbackSource = MakeMaterial("Fallback Source", Color.green));
    private Material GetFallbackTarget() => fallbackTarget ?? (fallbackTarget = MakeMaterial("Fallback Target", Color.red));
    private Material GetFallbackVisited() => fallbackVisited ?? (fallbackVisited = MakeMaterial("Fallback Visited", new Color(0.5f, 0.75f, 1f)));
    private Material GetFallbackPath() => fallbackPath ?? (fallbackPath = MakeMaterial("Fallback Path", Color.black));
    private Material GetFallbackGridLine() => fallbackGridLine ?? (fallbackGridLine = MakeMaterial("Fallback Grid Line", Color.black));

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

public class AStarCell
{
    public Vector2Int position;
    public bool blocked;
    public float gCost;
    public float hCost;
    public AStarCell parent;

    public float FCost => gCost + hCost;

    public AStarCell(Vector2Int position)
    {
        this.position = position;
        blocked = false;
        gCost = Mathf.Infinity;
        hCost = 0f;
        parent = null;
    }
}
