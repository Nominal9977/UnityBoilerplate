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
    public Vector2 startPoint = new Vector2(0, 0);
    public Vector2 targetPoint = new Vector2(9, 9);
    private List<GameObject> mapCells = new List<GameObject>();
    private List<GameObject> flowFieldArrows = new List<GameObject>();
    public List<FlowFieldGenerator.InfluencePoint> influencePoints = new List<FlowFieldGenerator.InfluencePoint>();
    public static List<GameObject> influencePointMarkers = new List<GameObject>();
    public static HashSet<Vector2> influencePointPositions = new HashSet<Vector2>();
    private GameObject startPointMarkerInstance;
    private GameObject targetPointMarkerInstance;
    public static Vector2[,] flowField;
    void Start()
    {
        InitializeInfluencePoints();
        GenerateMap();
        CreateAllMarkers();
        GenerateAndVisualizeFlowField();
    }
    void CreateAllMarkers()
    {
        CreateStartAndTargetMarkers();
        CreateAllInfluencePointMarkers();
    }
    void CreateAllInfluencePointMarkers()
    {
        ClearInfluencePointMarkers();
        foreach (var point in influencePoints)
        {
            CreateInfluencePointMarker(point);
        }
    }
    void CreateStartAndTargetMarkers()
    {
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
        influencePoints.Add(new FlowFieldGenerator.InfluencePoint(
            startPoint,
            1f,
            false
        ));
        influencePointPositions.Add(startPoint);
        influencePoints.Add(new FlowFieldGenerator.InfluencePoint(
            targetPoint,
            2f,
            true
        ));
        influencePointPositions.Add(targetPoint);
    }
    private void CreateInfluencePointMarker(FlowFieldGenerator.InfluencePoint point)
    {
        if (Vector2.Distance(point.Position, startPoint) < 0.1f ||
            Vector2.Distance(point.Position, targetPoint) < 0.1f)
        {
            return;
        }
        if (repulsionPointMarkerPrefab == null)
        {
            return;
        }
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
    }
    private void ClearInfluencePointMarkers()
    {
        foreach (GameObject marker in influencePointMarkers)
        {
            if (marker != null)
                Destroy(marker);
        }
        influencePointMarkers.Clear();
    }
    public void AddInfluencePoint(Vector2 position, float strength, bool isAttraction)
    {
        if (isAttraction && Vector2.Distance(position, targetPoint) > 0.1f)
        {
            isAttraction = false;
        }
        FlowFieldGenerator.InfluencePoint newPoint = new FlowFieldGenerator.InfluencePoint(position, strength, isAttraction);
        influencePoints.Add(newPoint);
        influencePointPositions.Add(position);
        CreateInfluencePointMarker(newPoint);
        GenerateAndVisualizeFlowField();
    }
    public void RemoveInfluencePoint(FlowFieldGenerator.InfluencePoint point)
    {
        influencePoints.Remove(point);
        influencePointPositions.Remove(point.Position);
        CreateAllInfluencePointMarkers();
        GenerateAndVisualizeFlowField();
    }
    public void RegenerateFlowField()
    {
        CreateAllMarkers();
        GenerateAndVisualizeFlowField();
    }
    public void SetStartAndTargetPoints(Vector2 start, Vector2 target)
    {
        startPoint = start;
        targetPoint = target;
        ReInitializeAllInfluencePoints();
        CreateAllMarkers();
        GenerateAndVisualizeFlowField();
    }
    public void SetStartPoint(Vector2 newStartPoint)
    {
        RemoveOldStartPoint();
        startPoint = newStartPoint;
        AddNewStartPoint();
        CreateAllMarkers();
        GenerateAndVisualizeFlowField();
    }
    public void SetTargetPoint(Vector2 newTargetPoint)
    {
        RemoveOldTargetPoint();
        targetPoint = newTargetPoint;
        AddNewTargetPoint();
        CreateAllMarkers();
        GenerateAndVisualizeFlowField();
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
        influencePoints.Add(new FlowFieldGenerator.InfluencePoint(startPoint, 1f, false));
        influencePointPositions.Add(startPoint);
    }
    void AddNewTargetPoint()
    {
        influencePoints.Add(new FlowFieldGenerator.InfluencePoint(targetPoint, 2f, true));
        influencePointPositions.Add(targetPoint);
    }
    void ReInitializeAllInfluencePoints()
    {
        List<FlowFieldGenerator.InfluencePoint> customPoints = new List<FlowFieldGenerator.InfluencePoint>();
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
    public List<FlowFieldGenerator.InfluencePoint> GetInfluencePoints()
    {
        return new List<FlowFieldGenerator.InfluencePoint>(influencePoints);
    }
    public static bool IsPositionBlocked(Vector2 position)
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
    void AddStrategicPoints()
    {
        influencePoints.Add(new FlowFieldGenerator.InfluencePoint(
            new Vector2(0, 0),
            1f,
            false
        ));
        influencePoints.Add(new FlowFieldGenerator.InfluencePoint(
            new Vector2(
                mapWidth * (cellSize + cellPadding),
                mapHeight * (cellSize + cellPadding)
            ),
            1f,
            true
        ));
        influencePoints.Add(new FlowFieldGenerator.InfluencePoint(
            new Vector2(
                mapWidth * (cellSize + cellPadding) / 2,
                mapHeight * (cellSize + cellPadding) / 2
            ),
            1.5f,
            true
        ));
    }
    void GenerateAndVisualizeFlowField()
    {
        flowField = FlowFieldGenerator.GenerateFlowField(
            mapWidth,
            mapHeight,
            influencePoints
        );
        VisualizeFlowField();
    }
    void VisualizeFlowField()
    {
        foreach (GameObject arrow in flowFieldArrows)
        {
            if (arrow != null)
                Destroy(arrow);
        }
        flowFieldArrows.Clear();
        for (int x = 0; x < mapWidth; x++)
        {
            for (int y = 0; y < mapHeight; y++)
            {
                Vector3 cellPosition = new Vector3(
                    x * (cellSize + cellPadding),
                    0,
                    y * (cellSize + cellPadding)
                );
                Vector2 flowVector = flowField[x, y];
                GameObject arrow = Instantiate(
                    arrowPrefab,
                    cellPosition + Vector3.up * 0.1f,
                    Quaternion.identity,
                    transform
                );
                arrow.transform.rotation = Quaternion.LookRotation(
                    new Vector3(flowVector.x, 0, flowVector.y)
                );
                float arrowScale = cellSize * 0.8f;
                arrow.transform.localScale = new Vector3(
                    arrowScale,
                    arrowScale,
                    arrowScale
                );
                arrow.name = $"Arrow_{x}_{y}";
                flowFieldArrows.Add(arrow);
            }
        }
    }
    void OnDrawGizmosSelected()
    {
        if (influencePoints == null) return;
        foreach (var point in influencePoints)
        {
            Gizmos.color = point.IsAttraction ? Color.green : Color.red;
            Gizmos.DrawSphere(
                new Vector3(point.Position.x, 0, point.Position.y),
                0.2f
            );
        }
    }
    public void RegenerateMap()
    {
        GenerateMap();
        ReInitializeAllInfluencePoints();
        CreateAllMarkers();
        GenerateAndVisualizeFlowField();
    }
    void OnDestroy()
    {
        DestroyStartAndTargetMarkers();
        ClearInfluencePointMarkers();
    }
}