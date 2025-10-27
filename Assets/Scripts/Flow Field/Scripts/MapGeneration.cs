using System.Collections.Generic;
using UnityEngine;

public class MapGeneration : MonoBehaviour
{
    public int mapWidth = 10;
    public int mapHeight = 10;
    public float cellSize = 1f;
    public float cellPadding = 0.1f;
    public GameObject cellPrefab;
    public GameObject arrowPrefab;
    
    [Header("Marker Prefabs")]
    public GameObject startPointMarkerPrefab;
    public GameObject targetPointMarkerPrefab;
    public GameObject repulsionPointMarkerPrefab;
    
    public static  Vector2 startPoint = new Vector2(0, 0);
    public static Vector2 targetPoint = new Vector2(9, 9);

    private List<GameObject> mapCells = new List<GameObject>();
    private List<GameObject> flowFieldArrows = new List<GameObject>();
    private Dictionary<string, GameObject> arrowMap = new Dictionary<string, GameObject>();
    
    public List<FlowUtility.InfluencePoint> influencePoints = new List<FlowUtility.InfluencePoint>();
    private List<GameObject> influencePointMarkers = new List<GameObject>();
    private Dictionary<Vector2, GameObject> influenceMarkerMap = new Dictionary<Vector2, GameObject>();
    private HashSet<Vector2> influencePointPositions = new HashSet<Vector2>();
    private GameObject startPointMarkerInstance;
    private GameObject targetPointMarkerInstance;
    public static Vector2[,] flowField;

    private bool isUpdating = false;

    void Start()
    {
        if (mapWidth <= 0 || mapHeight <= 0)
        {
            Debug.LogError("MapGeneration: Invalid map dimensions!");
            return;
        }

        InitializeInfluencePoints();
        GenerateMap();
        CreateAllMarkers();
        GenerateAndVisualizeFlowField();
    }

    void CreateAllMarkers()
    {
        if (isUpdating) return;
        CreateStartAndTargetMarkers();
        CreateAllInfluencePointMarkers();
    }

    void CreateAllInfluencePointMarkers()
    {
        if (isUpdating) return;
        
        // Only remove markers that no longer exist
        var pointsToKeep = new HashSet<Vector2>();
        foreach (var point in influencePoints)
        {
            // Skip start and target points
            if (Vector2.Distance(point.Position, startPoint) < 0.1f ||
                Vector2.Distance(point.Position, targetPoint) < 0.1f)
            {
                continue;
            }
            pointsToKeep.Add(point.Position);
        }

        // Remove markers for deleted points
        var keysToRemove = new List<Vector2>();
        foreach (var kvp in influenceMarkerMap)
        {
            if (!pointsToKeep.Contains(kvp.Key))
            {
                if (kvp.Value != null)
                    Destroy(kvp.Value);
                keysToRemove.Add(kvp.Key);
            }
        }
        foreach (var key in keysToRemove)
        {
            influenceMarkerMap.Remove(key);
            influencePointMarkers.Remove(influenceMarkerMap[key]);
        }

        // Create markers for new points
        foreach (var point in influencePoints)
        {
            if (Vector2.Distance(point.Position, startPoint) < 0.1f ||
                Vector2.Distance(point.Position, targetPoint) < 0.1f)
            {
                continue;
            }

            if (!influenceMarkerMap.ContainsKey(point.Position))
            {
                CreateInfluencePointMarker(point);
            }
        }
    }

    void CreateStartAndTargetMarkers()
    {
        if (isUpdating) return;
        DestroyStartAndTargetMarkers();
        if (startPointMarkerPrefab != null)
        {
            Vector3 startPosition = new Vector3(
                startPoint.x * (cellSize + cellPadding),
                0.2f,
                startPoint.y * (cellSize + cellPadding)
            );
            startPointMarkerInstance = Instantiate(startPointMarkerPrefab, startPosition, Quaternion.identity, transform);
            startPointMarkerInstance.name = "Start Point";
        }
        if (targetPointMarkerPrefab != null)
        {
            Vector3 targetPosition = new Vector3(
                targetPoint.x * (cellSize + cellPadding),
                0.2f,
                targetPoint.y * (cellSize + cellPadding)
            );
            targetPointMarkerInstance = Instantiate(targetPointMarkerPrefab, targetPosition, Quaternion.identity, transform);
            targetPointMarkerInstance.name = "Target Point";
        }
    }

    void DestroyStartAndTargetMarkers()
    {
        if (startPointMarkerInstance != null)
        {
            Destroy(startPointMarkerInstance);
            startPointMarkerInstance = null;
        }
        if (targetPointMarkerInstance != null)
        {
            Destroy(targetPointMarkerInstance);
            targetPointMarkerInstance = null;
        }
    }

    void InitializeInfluencePoints()
    {
        influencePoints.Clear();
        influencePointPositions.Clear();
        influencePoints.Add(new FlowUtility.InfluencePoint(startPoint, 1f, false));
        influencePointPositions.Add(startPoint);
        influencePoints.Add(new FlowUtility.InfluencePoint(targetPoint, 2f, true));
        influencePointPositions.Add(targetPoint);
    }

    private void CreateInfluencePointMarker(FlowUtility.InfluencePoint point)
    {
        if (repulsionPointMarkerPrefab == null)
            return;

        Vector3 markerPosition = new Vector3(
            point.Position.x * (cellSize + cellPadding),
            0.2f,
            point.Position.y * (cellSize + cellPadding)
        );
        GameObject marker = Instantiate(
            repulsionPointMarkerPrefab,
            markerPosition,
            Quaternion.identity,
            transform
        );
        marker.name = $"Repulsion Point ({point.Position.x}, {point.Position.y})";
        influencePointMarkers.Add(marker);
        influenceMarkerMap[point.Position] = marker;
    }

    /// <summary>
    /// Add influence point WITHOUT regenerating the flow field
    /// Call RegenerateFlowField() manually when ready to update
    /// </summary>
    public void AddInfluencePointWithoutRegen(Vector2 position, float strength, bool isAttraction)
    {
        if (isUpdating) return;
        if (isAttraction && Vector2.Distance(position, targetPoint) > 0.1f)
        {
            isAttraction = false;
        }
        FlowUtility.InfluencePoint newPoint = new FlowUtility.InfluencePoint(position, strength, isAttraction);
        influencePoints.Add(newPoint);
        influencePointPositions.Add(position);
        
        // Only create marker if it's not start/target point
        if (!(Vector2.Distance(position, startPoint) < 0.1f || Vector2.Distance(position, targetPoint) < 0.1f))
        {
            CreateInfluencePointMarker(newPoint);
        }
        
        // NO GenerateAndVisualizeFlowField() - that's called manually
    }

    /// <summary>
    /// Add influence point AND regenerate (old behavior, kept for compatibility)
    /// </summary>
    public void AddInfluencePoint(Vector2 position, float strength, bool isAttraction)
    {
        AddInfluencePointWithoutRegen(position, strength, isAttraction);
        GenerateAndVisualizeFlowField();
    }

    /// <summary>
    /// Remove influence point WITHOUT regenerating the flow field
    /// Call RegenerateFlowField() manually when ready to update
    /// </summary>
    public void RemoveInfluencePointWithoutRegen(FlowUtility.InfluencePoint point)
    {
        if (isUpdating) return;
        influencePoints.Remove(point);
        influencePointPositions.Remove(point.Position);
        
        // Remove marker if it exists
        if (influenceMarkerMap.ContainsKey(point.Position))
        {
            if (influenceMarkerMap[point.Position] != null)
                Destroy(influenceMarkerMap[point.Position]);
            influenceMarkerMap.Remove(point.Position);
        }
        
        // NO GenerateAndVisualizeFlowField() - that's called manually
    }

    /// <summary>
    /// Remove influence point AND regenerate (old behavior, kept for compatibility)
    /// </summary>
    public void RemoveInfluencePoint(FlowUtility.InfluencePoint point)
    {
        RemoveInfluencePointWithoutRegen(point);
        GenerateAndVisualizeFlowField();
    }

    public void RegenerateFlowField()
    {
        if (isUpdating) return;
        CreateAllMarkers();
        GenerateAndVisualizeFlowField();
    }

    public void SetStartAndTargetPoints(Vector2 start, Vector2 target)
    {
        if (isUpdating) return;
        isUpdating = true;
        startPoint = start;
        targetPoint = target;
        ReInitializeAllInfluencePoints();
        CreateAllMarkers();
        GenerateAndVisualizeFlowField();
        isUpdating = false;
    }

    public void SetStartPoint(Vector2 newStartPoint)
    {
        if (isUpdating) return;
        if (Vector2.Distance(newStartPoint, startPoint) < 0.01f) return;

        isUpdating = true;
        RemoveOldStartPoint();
        startPoint = newStartPoint;
        AddNewStartPoint();
        CreateAllMarkers();
        GenerateAndVisualizeFlowField();
        isUpdating = false;
    }

    public void SetTargetPoint(Vector2 newTargetPoint)
    {
        if (isUpdating) return;
        if (Vector2.Distance(newTargetPoint, targetPoint) < 0.01f) return;

        isUpdating = true;
        RemoveOldTargetPoint();
        targetPoint = newTargetPoint;
        AddNewTargetPoint();
        CreateAllMarkers();
        GenerateAndVisualizeFlowField();
        isUpdating = false;
    }

    void RemoveOldStartPoint()
    {
        for (int i = influencePoints.Count - 1; i >= 0; i--)
        {
            var point = influencePoints[i];
            bool isOldStartPoint = !point.IsAttraction && Vector2.Distance(point.Position, startPoint) < 0.1f;
            if (isOldStartPoint)
            {
                influencePointPositions.Remove(point.Position);
                influencePoints.RemoveAt(i);
                break;
            }
        }
    }

    void RemoveOldTargetPoint()
    {
        for (int i = influencePoints.Count - 1; i >= 0; i--)
        {
            var point = influencePoints[i];
            bool isOldTargetPoint = point.IsAttraction && Vector2.Distance(point.Position, targetPoint) < 0.1f;
            if (isOldTargetPoint)
            {
                influencePointPositions.Remove(point.Position);
                influencePoints.RemoveAt(i);
                break;
            }
        }
    }

    void AddNewStartPoint()
    {
        influencePoints.Add(new FlowUtility.InfluencePoint(startPoint, 1f, false));
        influencePointPositions.Add(startPoint);
    }

    void AddNewTargetPoint()
    {
        influencePoints.Add(new FlowUtility.InfluencePoint(targetPoint, 2f, true));
        influencePointPositions.Add(targetPoint);
    }

    void ReInitializeAllInfluencePoints()
    {
        List<FlowUtility.InfluencePoint> customPoints = new List<FlowUtility.InfluencePoint>();
        foreach (var point in influencePoints)
        {
            bool isStartPoint = !point.IsAttraction && Vector2.Distance(point.Position, startPoint) < 0.1f;
            bool isTargetPoint = point.IsAttraction && Vector2.Distance(point.Position, targetPoint) < 0.1f;
            if (!isStartPoint && !isTargetPoint)
            {
                customPoints.Add(point);
            }
        }
        InitializeInfluencePoints();
        foreach (var customPoint in customPoints)
        {
            influencePoints.Add(customPoint);
            influencePointPositions.Add(customPoint.Position);
        }
    }

    public List<FlowUtility.InfluencePoint> GetInfluencePoints()
    {
        return new List<FlowUtility.InfluencePoint>(influencePoints);
    }

    public bool IsPositionBlocked(Vector2 position)
    {
        return influencePointPositions.Contains(position);
    }

    public bool IsPositionBlockedWithinRadius(Vector2 position, float radius = 0.5f)
    {
        foreach (var influencePos in influencePointPositions)
        {
            if (Vector2.Distance(position, influencePos) <= radius)
            {
                return true;
            }
        }
        return false;
    }

    void GenerateMap()
    {
        foreach (GameObject cell in mapCells)
        {
            if (cell != null)
                Destroy(cell);
        }
        mapCells.Clear();
        for (int x = 0; x < mapWidth; x++)
        {
            for (int y = 0; y < mapHeight; y++)
            {
                Vector3 cellPosition = new Vector3(
                    x * (cellSize + cellPadding),
                    0,
                    y * (cellSize + cellPadding)
                );
                GameObject cell = Instantiate(
                    cellPrefab,
                    cellPosition,
                    Quaternion.identity,
                    transform
                );
                Vector3 originalScale = cell.transform.localScale;
                cell.transform.localScale = new Vector3(
                    originalScale.x * (cellSize / (cellSize + cellPadding)),
                    originalScale.y,
                    originalScale.z * (cellSize / (cellSize + cellPadding))
                );
                cell.name = $"Cell_{x}_{y}";
                mapCells.Add(cell);
            }
        }
    }

    void GenerateAndVisualizeFlowField()
    {
        if (isUpdating) return;
        if (mapWidth <= 0 || mapHeight <= 0 || influencePoints.Count == 0) return;

        flowField = FlowUtility.GenerateFlowField(mapWidth, mapHeight, influencePoints);
        VisualizeFlowField();
    }

    void VisualizeFlowField()
    {
        if (flowField == null) return;

        float arrowScale = cellSize * 0.8f;

        for (int x = 0; x < mapWidth; x++)
        {
            for (int y = 0; y < mapHeight; y++)
            {
                string arrowKey = $"Arrow_{x}_{y}";
                Vector3 cellPosition = new Vector3(
                    x * (cellSize + cellPadding),
                    0,
                    y * (cellSize + cellPadding)
                );
                Vector2 flowVector = flowField[x, y];

                GameObject arrow;
                
                // Reuse existing arrow if it exists
                if (arrowMap.ContainsKey(arrowKey))
                {
                    arrow = arrowMap[arrowKey];
                    if (arrow == null)
                    {
                        // Arrow was destroyed, create new one
                        arrow = Instantiate(
                            arrowPrefab,
                            cellPosition + Vector3.up * 0.1f,
                            Quaternion.identity,
                            transform
                        );
                        arrowMap[arrowKey] = arrow;
                        flowFieldArrows.Add(arrow);
                    }
                }
                else
                {
                    // Create new arrow on first run
                    arrow = Instantiate(
                        arrowPrefab,
                        cellPosition + Vector3.up * 0.1f,
                        Quaternion.identity,
                        transform
                    );
                    arrowMap[arrowKey] = arrow;
                    flowFieldArrows.Add(arrow);
                }

                // UPDATE existing arrow data instead of recreating
                arrow.transform.position = cellPosition + Vector3.up * 0.1f;
                arrow.transform.rotation = Quaternion.LookRotation(
                    new Vector3(flowVector.x, 0, flowVector.y)
                );
                arrow.transform.localScale = new Vector3(arrowScale, arrowScale, arrowScale);
            }
        }
    }

    public Vector3 GridToWorldPosition(int gridX, int gridY)
    {
        return new Vector3(
            gridX * (cellSize + cellPadding),
            0,
            gridY * (cellSize + cellPadding)
        );
    }

    public void RegenerateMap()
    {
        if (isUpdating) return;
        GenerateMap();
        ReInitializeAllInfluencePoints();
        CreateAllMarkers();
        GenerateAndVisualizeFlowField();
    }

    void OnDestroy()
    {
        DestroyStartAndTargetMarkers();
        
        foreach (GameObject marker in influencePointMarkers)
        {
            if (marker != null)
                Destroy(marker);
        }
        influencePointMarkers.Clear();
    }
}