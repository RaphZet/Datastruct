using UnityEngine;

[CreateAssetMenu(fileName = "AStarGridDatabase", menuName = "Shortest Path/A Star Grid Database v7")]
public class AStarGridDatabase : ScriptableObject
{
    [Header("Grid Size")]
    [Min(2)] public int columns = 5;
    [Min(2)] public int rows = 8;

    [Header("Random Blocked Cells")]
    [Min(0)] public int blockedCount = 8;
    [Tooltip("Jika aktif, susunan blocked cell berubah setiap Play. Jika nonaktif, hasil random tetap sama setiap Play.")]
    public bool randomizeEveryPlay = true;
    [Range(1, 200)] public int maxGenerationAttempts = 50;

    [Header("Source and Target")]
    [Tooltip("Source selalu ada di row paling bawah.")]
    public int sourceColumn = 0;
    [Tooltip("Target selalu ada di row paling atas.")]
    public int targetColumn = 0;
    public bool randomizeSourceColumn = false;
    public bool randomizeTargetColumn = false;

    [Header("A* Movement")]
    public bool allowDiagonalMovement = true;
    public bool preventCornerCutting = true;
    public float straightMoveCost = 10f;
    public float diagonalMoveCost = 14f;

    [Header("Visual Materials - assign Shader Graph materials here")]
    public Material unblockedMaterial;
    public Material blockedMaterial;
    public Material sourceMaterial;
    public Material targetMaterial;
    public Material pathMaterial;
    public Material visitedMaterial;
    public Material gridLineMaterial;
    public Material textMaterial;

    [Header("Visual Settings")]
    public float cellSize = 1f;
    public float cellGap = 0.04f;
    public float cellHeight = 0.05f;
    public float pathLineWidth = 0.10f;

    [Header("Labels / Numbers")]
    [Tooltip("Nomor column di atas grid dan nomor row di kiri grid.")]
    public bool showAxisLabels = true;
    [Tooltip("Koordinat di dalam semua cell. Default false supaya tidak numpuk.")]
    public bool showCoordinateLabels = false;
    [Tooltip("Koordinat hanya di cell yang dilewati shortest path. Lebih rapi untuk presentasi.")]
    public bool showPathCoordinateLabels = true;
    [Tooltip("Ukuran tulisan. 0.08 - 0.14 biasanya aman untuk cellSize 1.")]
    [Range(0.02f, 0.3f)] public float labelSize = 0.12f;
    public float labelHeightOffset = 0.12f;
    public Color axisLabelColor = Color.white;
    public Color cellLabelColor = Color.black;
    public Color pathLabelColor = Color.black;

    [Header("Debug Visualization")]
    public bool showVisitedCells = false;

    private void OnValidate()
    {
        columns = Mathf.Max(2, columns);
        rows = Mathf.Max(2, rows);
        sourceColumn = Mathf.Clamp(sourceColumn, 0, columns - 1);
        targetColumn = Mathf.Clamp(targetColumn, 0, columns - 1);
        blockedCount = Mathf.Clamp(blockedCount, 0, Mathf.Max(0, (columns * rows) - 2));
        straightMoveCost = Mathf.Max(0.01f, straightMoveCost);
        diagonalMoveCost = Mathf.Max(0.01f, diagonalMoveCost);
        cellSize = Mathf.Max(0.1f, cellSize);
        cellGap = Mathf.Clamp(cellGap, 0f, cellSize * 0.5f);
        pathLineWidth = Mathf.Max(0.01f, pathLineWidth);
        labelSize = Mathf.Clamp(labelSize, 0.02f, 0.3f);
    }
}
