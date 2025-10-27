using UnityEngine;
using System.Collections.Generic;

public class FlowUtility : MonoBehaviour
{
    public struct InfluencePoint
    {
        public Vector2 Position;
        public float Strength;
        public bool IsAttraction;
        public InfluencePoint(Vector2 position, float strength, bool isAttraction = true)
        {
            Position = position;
            Strength = strength;
            IsAttraction = isAttraction;
        }
    }

    /// <summary>
    /// Generate a flow field based on influence points
    /// OPTIMIZED: Minimal logging for performance
    /// </summary>
    public static Vector2[,] GenerateFlowField(int width, int height, List<InfluencePoint> influencePoints)
    {
        Debug.Log("========================================");
        Debug.Log($">>> FlowUtility.GenerateFlowField() START <<<");
        Debug.Log($"    Grid: {width}x{height} ({width * height} cells)");
        Debug.Log($"    Influence Points: {influencePoints.Count}");
        Debug.Log("========================================");

        if (width <= 0 || height <= 0)
        {
            Debug.LogError($"FlowUtility.GenerateFlowField() - Invalid dimensions: {width}x{height}");
            return null;
        }

        if (influencePoints == null || influencePoints.Count == 0)
        {
            Debug.LogError("FlowUtility.GenerateFlowField() - No influence points provided");
            return null;
        }

        System.Diagnostics.Stopwatch timer = System.Diagnostics.Stopwatch.StartNew();

        Vector2[,] flowField = new Vector2[width, height];
        int totalCells = width * height;

        // Calculate all cells
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Vector2 currentPos = new Vector2(x, y);
                flowField[x, y] = CalculateFlowVectorAtPoint(currentPos, influencePoints);
            }
        }

        timer.Stop();

        Debug.Log($"FlowUtility.GenerateFlowField() - Calculated {totalCells} cells in {timer.ElapsedMilliseconds}ms");
        Debug.Log("<<< FlowUtility.GenerateFlowField() COMPLETE <<<");
        Debug.Log("========================================");

        return flowField;
    }

    /// <summary>
    /// Calculate the flow vector at a specific point based on all influence points
    /// OPTIMIZED: NO logging to avoid performance hit per cell
    /// </summary>
    private static Vector2 CalculateFlowVectorAtPoint(Vector2 point, List<InfluencePoint> influencePoints)
    {
        Vector2 resultantFlow = Vector2.zero;

        foreach (var influencePoint in influencePoints)
        {
            // Calculate vector from current point to influence point
            Vector2 vectorToInfluence = point - influencePoint.Position;

            // Calculate distance
            float distance = vectorToInfluence.magnitude;

            // Calculate falloff (how much influence decreases with distance)
            float falloff = 1f / (distance * distance + 1f);

            // Determine direction: attraction points pull toward them, repulsion points push away
            float directionMultiplier = influencePoint.IsAttraction ? 1 : -1;

            // Calculate the influence vector
            Vector2 influenceVector = vectorToInfluence.normalized *
                influencePoint.Strength *
                falloff *
                directionMultiplier;

            // Add to resultant flow
            resultantFlow += influenceVector;
        }

        // Normalize the resultant flow to get the final direction
        return resultantFlow.normalized;
    }
}