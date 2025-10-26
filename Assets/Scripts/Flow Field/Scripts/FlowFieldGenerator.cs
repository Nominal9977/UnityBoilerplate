using UnityEngine;
using System.Collections.Generic;
public static class FlowFieldGenerator
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
    public static Vector2[,] GenerateFlowField(int width, int height, List<InfluencePoint> influencePoints)
    {
        Vector2[,] flowField = new Vector2[width, height];
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Vector2 currentPos = new Vector2(x, y);
                flowField[x, y] = CalculateFlowVectorAtPoint(currentPos, influencePoints);
            }
        }
        return flowField;
    }
    private static Vector2 CalculateFlowVectorAtPoint(Vector2 point, List<InfluencePoint> influencePoints)
    {
        Vector2 resultantFlow = Vector2.zero;
        foreach (var influencePoint in influencePoints)
        {
            Vector2 vectorToInfluence = point - influencePoint.Position;
            float distance = vectorToInfluence.magnitude;
            float falloff = 1f / (distance * distance + 1f);
            float directionMultiplier = influencePoint.IsAttraction ? 1 : -1;
            Vector2 influenceVector = vectorToInfluence.normalized *
                influencePoint.Strength *
                falloff *
                directionMultiplier;
            resultantFlow += influenceVector;
        }
        return resultantFlow.normalized;
    }
}