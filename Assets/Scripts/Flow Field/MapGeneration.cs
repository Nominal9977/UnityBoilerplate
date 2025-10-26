using System.Collections.Generic;
using UnityEngine;
using static FlowFieldGenerator;

public class MapGeneration : MonoBehaviour
{
    public int mapWidth = 10;
    public int mapHeight = 10;
    public float cellSize = 1f;
    public float cellPadding = 0.1f;
    public GameObject cellPrefab;
    public GameObject arrowPrefab;
    public GameObject startPointMarker;
    public GameObject targetPointMarker;
    public GameObject influencePointMarkerPrefab;

    // Start and Target Points
    public Vector2 startPoint = new Vector2(0, 0);
    public Vector2 targetPoint = new Vector2(9, 9);

    private List<GameObject> mapCells = new List<GameObject>();
    private List<GameObject> flowFieldArrows = new List<GameObject>();
    public List<InfluencePoint> influencePoints = new List<InfluencePoint>();
    private List<GameObject> influencePointMarkers = new List<GameObject>();

    private FlowFieldGenerator flowFieldGenerator = new FlowFieldGenerator();
    private Vector2[,] flowField;

    void Start()
    {
        GenerateMap();
        CreateStartAndTargetMarkers();
        GenerateInfluencePoints();
        GenerateFlowField();
        VisualizeFlowField();
    }

    void CreateStartAndTargetMarkers()
    {
       

        if (startPointMarker != null)
        {
            Vector3 startPosition = new Vector3(
                startPoint.x * (cellSize + cellPadding),
                0.2f,
                startPoint.y * (cellSize + cellPadding)
            );
            startPointMarker = Instantiate(startPointMarker, startPosition, Quaternion.identity, transform);
            startPointMarker.name = "Start Point";
        }

        if (targetPointMarker != null)
        {
            Vector3 targetPosition = new Vector3(
                targetPoint.x * (cellSize + cellPadding),
                0.2f,
                targetPoint.y * (cellSize + cellPadding)
            );
            targetPointMarker = Instantiate(targetPointMarker, targetPosition, Quaternion.identity, transform);
            targetPointMarker.name = "Target Point";
        }
    }

    void GenerateInfluencePoints()
    {
        influencePoints.Clear();

        influencePoints.Add(new InfluencePoint(
            startPoint,
            1f,
            false 
        ));

        influencePoints.Add(new InfluencePoint(
            targetPoint,
            2f,
            true  
        ));
    }

    private void CreateInfluencePointMarker(InfluencePoint point)
    {
        if (influencePointMarkerPrefab == null)
        {
            Debug.LogWarning("No influence point marker prefab assigned!");
            return;
        }

        Vector3 markerPosition = new Vector3(
            point.Position.x * (cellSize + cellPadding),
            0.2f,
            point.Position.y * (cellSize + cellPadding)
        );

        GameObject marker = Instantiate(
            influencePointMarkerPrefab,
            markerPosition,
            Quaternion.identity,
            transform
        );

        marker.name = $"Influence Point ({point.Position.x}, {point.Position.y})";

        // Optional: Color coding
        Renderer renderer = marker.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material.color = point.IsAttraction ? Color.green : Color.red;
        }

        influencePointMarkers.Add(marker);
    }

    // Method to clear influence point markers
    private void ClearInfluencePointMarkers()
    {
        foreach (GameObject marker in influencePointMarkers)
        {
            if (marker != null)
                Destroy(marker);
        }
        influencePointMarkers.Clear();
    }

    // Update existing methods
    public void AddInfluencePoint(Vector2 position, float strength, bool isAttraction)
    {
        InfluencePoint newPoint = new InfluencePoint(position, strength, isAttraction);
        influencePoints.Add(newPoint);
        CreateInfluencePointMarker(newPoint);
        RegenerateFlowField();
    }

    public void RemoveInfluencePoint(InfluencePoint point)
    {
        influencePoints.Remove(point);
        RegenerateFlowField();
    }

    public void RegenerateFlowField()
    {
        // Clear existing influence point markers
        ClearInfluencePointMarkers();

        // Regenerate flow field components
        GenerateFlowField();
        VisualizeFlowField();
    }
    public void SetStartAndTargetPoints(Vector2 start, Vector2 target)
    {
        startPoint = start;
        targetPoint = target;
        CreateStartAndTargetMarkers();
        GenerateInfluencePoints();
        GenerateFlowField();
        VisualizeFlowField();
    }

    public void SetStartPoint(Vector2 newStartPoint)
    {
        startPoint = newStartPoint;
        CreateStartAndTargetMarkers();
        RegenerateFlowField();
    }

    public void SetTargetPoint(Vector2 newTargetPoint)
    {
        targetPoint = newTargetPoint;
        CreateStartAndTargetMarkers();
        RegenerateFlowField();
    }

    

    public List<InfluencePoint> GetInfluencePoints()
    {
        return new List<InfluencePoint>(influencePoints);
    }

    void GenerateMap()
    {
        // Clear existing cells
        foreach (GameObject cell in mapCells)
        {
            if (cell != null)
                Destroy(cell);
        }
        mapCells.Clear();

        // Generate map cells
        for (int x = 0; x < mapWidth; x++)
        {
            for (int y = 0; y < mapHeight; y++)
            {
                // Calculate position with padding
                Vector3 cellPosition = new Vector3(
                    x * (cellSize + cellPadding),  // X with padding
                    0,                             // Ground level
                    y * (cellSize + cellPadding)   // Y with padding
                );

                GameObject cell = Instantiate(
                    cellPrefab,
                    cellPosition,
                    Quaternion.identity,
                    transform
                );

                // Adjust cell scale to account for padding
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
        // Add map corner and center points
        influencePoints.Add(new FlowFieldGenerator.InfluencePoint(
            new Vector2(0, 0),  // Bottom left corner
            1f,
            false  // Repulsion
        ));

        influencePoints.Add(new FlowFieldGenerator.InfluencePoint(
            new Vector2(
                mapWidth * (cellSize + cellPadding),
                mapHeight * (cellSize + cellPadding)
            ),  // Top right corner
            1f,
            true   // Attraction
        ));

        influencePoints.Add(new FlowFieldGenerator.InfluencePoint(
            new Vector2(
                mapWidth * (cellSize + cellPadding) / 2,
                mapHeight * (cellSize + cellPadding) / 2
            ),  // Map center
            1.5f,
            true   // Attraction
        ));
    }

    void GenerateFlowField()
    {
        // Generate flow field based on influence points
        flowField = flowFieldGenerator.GenerateFlowField(
            mapWidth,
            mapHeight,
            influencePoints
        );
    }

    void VisualizeFlowField()
    {
        // Clear existing arrows
        foreach (GameObject arrow in flowFieldArrows)
        {
            if (arrow != null)
                Destroy(arrow);
        }
        flowFieldArrows.Clear();

        // Create an arrow for each cell
        for (int x = 0; x < mapWidth; x++)
        {
            for (int y = 0; y < mapHeight; y++)
            {
                // Calculate cell position
                Vector3 cellPosition = new Vector3(
                    x * (cellSize + cellPadding),
                    0,
                    y * (cellSize + cellPadding)
                );

                // Get flow vector for this cell
                Vector2 flowVector = flowField[x, y];

                // Instantiate arrow
                GameObject arrow = Instantiate(
                    arrowPrefab,
                    cellPosition + Vector3.up * 0.1f,  // Slight lift to be visible
                    Quaternion.identity,
                    transform
                );

                // Rotate arrow to match flow direction
                arrow.transform.rotation = Quaternion.LookRotation(
                    new Vector3(flowVector.x, 0, flowVector.y)
                );

                // Scale arrow based on vector magnitude
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
        GenerateInfluencePoints();
        GenerateFlowField();
        VisualizeFlowField();
    }
}
