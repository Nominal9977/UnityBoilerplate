using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FlowFieldGUI : MonoBehaviour
{
    [Header("Map Generation Reference")]
    public MapGeneration mapGeneration;

    [Header("Map Dimensions")]
    public InputField mapWidthInput;
    public InputField mapHeightInput;
    public Button updateMapButton;

    [Header("Start Point Inputs")]
    public InputField startXInput;
    public InputField startYInput;

    [Header("Target Point Inputs")]
    public InputField targetXInput;
    public InputField targetYInput;

    [Header("Influence Point Inputs")]
    public InputField influenceXInput;
    public InputField influenceYInput;
    public InputField influenceStrengthInput;

    [Header("Buttons")]
    public Button addInfluenceButton;
    public Button regenerateButton;
    public Button clearAllInfluenceButton;
    public Button randomizeButton;

    [Header("Auto Regenerate Settings")]
    public bool autoRegenerateOnPointChange = true;

    [Header("Influence Point List (ScrollView)")]
    public ScrollRect influenceScrollRect;
    public Transform influenceListContent;
    public GameObject influenceListItemPrefab;

    [Header("UI Feedback")]
    public Text statusText;

    private List<GameObject> influenceListItems = new List<GameObject>();

    private void Start()
    {
        if (mapGeneration == null)
        {
            mapGeneration = FindObjectOfType<MapGeneration>();
        }
        InitializeInputFields();
        SetupButtonListeners();
        UpdateInfluencePointList();
        UpdateStatusText("Ready");
    }

    void InitializeInputFields()
    {
        if (mapGeneration != null)
        {
            mapWidthInput.text = mapGeneration.mapWidth.ToString();
            mapHeightInput.text = mapGeneration.mapHeight.ToString();

            startXInput.text = mapGeneration.startPoint.x.ToString();
            startYInput.text = mapGeneration.startPoint.y.ToString();
            targetXInput.text = mapGeneration.targetPoint.x.ToString();
            targetYInput.text = mapGeneration.targetPoint.y.ToString();
        }
        influenceXInput.text = "0";
        influenceYInput.text = "0";
        influenceStrengthInput.text = "1";
    }

    void SetupButtonListeners()
    {
        if (updateMapButton != null)
        {
            updateMapButton.onClick.AddListener(UpdateMapDimensions);
            Debug.Log("? Update Map button listener added");
        }
        else
        {
            Debug.LogWarning("? Update Map button is not assigned in inspector!");
        }

        mapWidthInput.onValueChanged.AddListener(ValidateNumericInput);
        mapHeightInput.onValueChanged.AddListener(ValidateNumericInput);

        startXInput.onEndEdit.AddListener(_ => UpdateStartPoint());
        startYInput.onEndEdit.AddListener(_ => UpdateStartPoint());

        targetXInput.onEndEdit.AddListener(_ => UpdateTargetPoint());
        targetYInput.onEndEdit.AddListener(_ => UpdateTargetPoint());

        addInfluenceButton.onClick.AddListener(AddInfluencePoint);
        regenerateButton.onClick.AddListener(RegenerateFlowField);

        if (clearAllInfluenceButton != null)
        {
            clearAllInfluenceButton.onClick.AddListener(ClearAllInfluencePoints);
        }

        if (randomizeButton != null)
        {
            randomizeButton.onClick.AddListener(RandomizeInfluencePoints);
            Debug.Log("? Randomize button listener added");
        }
        else
        {
            Debug.LogWarning("? Randomize button is not assigned in inspector!");
        }

        influenceXInput.onValueChanged.AddListener(ValidateNumericInput);
        influenceYInput.onValueChanged.AddListener(ValidateNumericInput);
        influenceStrengthInput.onValueChanged.AddListener(ValidateNumericInput);
        startXInput.onValueChanged.AddListener(ValidateNumericInput);
        startYInput.onValueChanged.AddListener(ValidateNumericInput);
        targetXInput.onValueChanged.AddListener(ValidateNumericInput);
        targetYInput.onValueChanged.AddListener(ValidateNumericInput);
    }

    void ValidateNumericInput(string input)
    {
    }

    void UpdateMapDimensions()
    {
        try
        {
            int width = int.Parse(mapWidthInput.text);
            int height = int.Parse(mapHeightInput.text);

            if (width <= 0 || height <= 0)
            {
                UpdateStatusText("Error: Map dimensions must be greater than 0");
                return;
            }

            if (width > 100 || height > 100)
            {
                UpdateStatusText("Warning: Large map sizes may impact performance");
            }

            Debug.Log($"Updating map dimensions to: {width}x{height}");

            mapGeneration.SetMapDimensions(width, height);

            RefreshGUI();
            UpdateInfluencePointList();

            UpdateStatusText($"Map size updated to {width}x{height}. All influence points cleared.");

            if (autoRegenerateOnPointChange)
            {
                RegenerateFlowField();
            }
        }
        catch (System.Exception e)
        {
            UpdateStatusText("Error: Invalid map dimensions");
            Debug.LogError($"Error updating map dimensions: {e.Message}");
        }
    }

    void UpdateStartPoint()
    {
        try
        {
            float x = float.Parse(startXInput.text);
            float y = float.Parse(startYInput.text);
            if (x < 0 || x >= mapGeneration.mapWidth || y < 0 || y >= mapGeneration.mapHeight)
            {
                UpdateStatusText($"Warning: Start point ({x}, {y}) is outside map bounds!");
            }
            mapGeneration.SetStartPoint(new Vector2(x, y));
            UpdateInfluencePointList();
            UpdateStatusText($"Start point updated to ({x}, {y})");

            if (autoRegenerateOnPointChange)
            {
                RegenerateFlowField();
            }
        }
        catch (System.Exception e)
        {
            UpdateStatusText("Error: Invalid start point input");
        }
    }

    void UpdateTargetPoint()
    {
        try
        {
            float x = float.Parse(targetXInput.text);
            float y = float.Parse(targetYInput.text);
            if (x < 0 || x >= mapGeneration.mapWidth || y < 0 || y >= mapGeneration.mapHeight)
            {
                UpdateStatusText($"Warning: Target point ({x}, {y}) is outside map bounds!");
            }
            mapGeneration.SetTargetPoint(new Vector2(x, y));
            UpdateInfluencePointList();
            UpdateStatusText($"Target point updated to ({x}, {y})");

            if (autoRegenerateOnPointChange)
            {
                RegenerateFlowField();
            }
        }
        catch (System.Exception e)
        {
            UpdateStatusText("Error: Invalid target point input");
        }
    }

    void AddInfluencePoint()
    {
        try
        {
            float x = float.Parse(influenceXInput.text);
            float y = float.Parse(influenceYInput.text);
            float strength = float.Parse(influenceStrengthInput.text);
            bool isAttraction = false;

            if (strength <= 0)
            {
                UpdateStatusText("Error: Strength must be greater than 0");
                return;
            }

            if (x < 0 || x >= mapGeneration.mapWidth || y < 0 || y >= mapGeneration.mapHeight)
            {
                UpdateStatusText($"Error: Position ({x}, {y}) is outside map bounds!");
                return;
            }

            mapGeneration.AddInfluencePointWithoutRegen(
                new Vector2(x, y),
                strength,
                isAttraction
            );

            UpdateInfluencePointList();

            influenceXInput.text = "0";
            influenceYInput.text = "0";
            influenceStrengthInput.text = "1";

            UpdateStatusText($"Added influence point at ({x}, {y}) with strength {strength}");

            if (autoRegenerateOnPointChange)
            {
                RegenerateFlowField();
            }
        }
        catch (System.Exception e)
        {
            UpdateStatusText("Error: Invalid influence point input");
            Debug.LogError(e.Message);
        }
    }

    void RandomizeInfluencePoints()
    {
        try
        {
            Debug.Log("=== RandomizeInfluencePoints() START ===");

            ClearAllInfluencePointsInternal();
            Debug.Log("Cleared all custom influence points");

            int randomPointCount = Random.Range(3, 8);
            Debug.Log($"Generating {randomPointCount} random influence points");

            List<Vector2> usedPositions = new List<Vector2>();
            usedPositions.Add(mapGeneration.startPoint);
            usedPositions.Add(mapGeneration.targetPoint);

            int pointsAdded = 0;

            for (int i = 0; i < randomPointCount; i++)
            {
                int randomX = Random.Range(0, mapGeneration.mapWidth);
                int randomY = Random.Range(0, mapGeneration.mapHeight);
                Vector2 randomPos = new Vector2(randomX, randomY);

                bool positionUsed = false;
                foreach (Vector2 usedPos in usedPositions)
                {
                    if (Vector2.Distance(randomPos, usedPos) < 0.5f)
                    {
                        positionUsed = true;
                        break;
                    }
                }

                if (positionUsed)
                {
                    Debug.Log($"Position ({randomX}, {randomY}) already used, skipping");
                    continue;
                }

                usedPositions.Add(randomPos);

                float randomStrength = Random.Range(1f, 10.1f);

                mapGeneration.AddInfluencePointWithoutRegen(randomPos, randomStrength, false);
                pointsAdded++;

                Debug.Log($"Added random point {pointsAdded}: ({randomX}, {randomY}) | Strength: {randomStrength:F1}");
            }

            Debug.Log("Setting target point strength to 10");
            mapGeneration.SetTargetPointStrength(10f);

            UpdateInfluencePointList();
            UpdateStatusText($"Randomized: Added {pointsAdded} random repulsion points");
            Debug.Log("=== RandomizeInfluencePoints() COMPLETE ===\n");

            if (autoRegenerateOnPointChange)
            {
                RegenerateFlowField();
            }
        }
        catch (System.Exception e)
        {
            UpdateStatusText($"Error: Failed to randomize influence points");
            Debug.LogError($"RandomizeInfluencePoints error: {e.Message}");
        }
    }

    void UpdateInfluencePointList()
    {
        ClearInfluenceListItems();
        if (mapGeneration == null) return;

        var influencePoints = mapGeneration.GetInfluencePoints();
        foreach (var point in influencePoints)
        {
            CreateInfluenceListItem(point);
        }

        if (influenceScrollRect != null)
        {
            Canvas.ForceUpdateCanvases();
            influenceScrollRect.verticalNormalizedPosition = 1f;
        }

        UpdateStatusText($"Displaying {influencePoints.Count} influence points");
    }

    void CreateInfluenceListItem(FlowUtility.InfluencePoint point)
    {
        if (influenceListItemPrefab == null || influenceListContent == null)
        {
            return;
        }
        GameObject listItem = Instantiate(influenceListItemPrefab, influenceListContent);
        influenceListItems.Add(listItem);

        TextMeshProUGUI itemText = listItem.GetComponentInChildren<TextMeshProUGUI>();

        if (itemText != null)
        {
            string typeText;
            if (point.IsAttraction)
            {
                typeText = "TARGET (Attraction)";
                itemText.color = Color.green;
            }
            else
            {
                bool isStartPoint = Vector2.Distance(point.Position, mapGeneration.startPoint) < 0.1f;
                if (isStartPoint)
                {
                    typeText = "START (Repulsion)";
                    itemText.color = Color.blue;
                }
                else
                {
                    typeText = "REPULSION";
                    itemText.color = Color.red;
                }
            }
            itemText.text = $"({point.Position.x:F1}, {point.Position.y:F1}) | Strength: {point.Strength:F1} | {typeText}";
        }

        Button deleteButton = listItem.GetComponentInChildren<Button>();
        if (deleteButton != null)
        {
            bool isStartPoint = !point.IsAttraction && Vector2.Distance(point.Position, mapGeneration.startPoint) < 0.1f;
            bool isTargetPoint = point.IsAttraction && Vector2.Distance(point.Position, mapGeneration.targetPoint) < 0.1f;

            if (isStartPoint || isTargetPoint)
            {
                deleteButton.gameObject.SetActive(false);
                Debug.Log($"Hiding delete button for {(isStartPoint ? "START" : "TARGET")} point");
            }
            else
            {
                deleteButton.gameObject.SetActive(true);

                var pointCopy = point;
                deleteButton.onClick.AddListener(() => RemoveInfluencePoint(pointCopy));

                Text deleteButtonText = deleteButton.GetComponentInChildren<Text>();
                if (deleteButtonText != null)
                {
                    deleteButtonText.text = "X";
                }
            }
        }

        AddHoverEffects(listItem);
    }

    void AddHoverEffects(GameObject listItem)
    {
        var eventTrigger = listItem.GetComponent<UnityEngine.EventSystems.EventTrigger>();
        if (eventTrigger == null)
        {
            eventTrigger = listItem.AddComponent<UnityEngine.EventSystems.EventTrigger>();
        }
    }

    void RemoveInfluencePoint(FlowUtility.InfluencePoint point)
    {
        if (mapGeneration != null)
        {
            mapGeneration.RemoveInfluencePointWithoutRegen(point);
            UpdateInfluencePointList();
            UpdateStatusText($"Removed influence point at ({point.Position.x:F1}, {point.Position.y:F1})");

            if (autoRegenerateOnPointChange)
            {
                RegenerateFlowField();
            }
        }
    }

    void ClearInfluenceListItems()
    {
        foreach (GameObject item in influenceListItems)
        {
            if (item != null)
                Destroy(item);
        }
        influenceListItems.Clear();
    }

    void ClearAllInfluencePoints()
    {
        ClearAllInfluencePointsInternal();
        if (autoRegenerateOnPointChange)
        {
            RegenerateFlowField();
        }
    }

    void ClearAllInfluencePointsInternal()
    {
        if (mapGeneration != null)
        {
            var allPoints = mapGeneration.GetInfluencePoints();
            var pointsToRemove = new List<FlowUtility.InfluencePoint>();
            foreach (var point in allPoints)
            {
                bool isStartPoint = !point.IsAttraction && Vector2.Distance(point.Position, mapGeneration.startPoint) < 0.1f;
                bool isTargetPoint = point.IsAttraction && Vector2.Distance(point.Position, mapGeneration.targetPoint) < 0.1f;
                if (!isStartPoint && !isTargetPoint)
                {
                    pointsToRemove.Add(point);
                }
            }

            foreach (var point in pointsToRemove)
            {
                mapGeneration.RemoveInfluencePointWithoutRegen(point);
            }

            UpdateInfluencePointList();
            UpdateStatusText($"Cleared {pointsToRemove.Count} repulsion points");
        }
    }

    void RegenerateFlowField()
    {
        if (mapGeneration != null)
        {
            mapGeneration.RegenerateFlowField();
            UpdateStatusText("Flow field regenerated");
        }
    }

    void UpdateStatusText(string message)
    {
        if (statusText != null)
        {
            statusText.text = message;
        }
    }

    public void RefreshGUI()
    {
        if (mapGeneration != null)
        {
            mapWidthInput.text = mapGeneration.mapWidth.ToString();
            mapHeightInput.text = mapGeneration.mapHeight.ToString();
            startXInput.text = mapGeneration.startPoint.x.ToString();
            startYInput.text = mapGeneration.startPoint.y.ToString();
            targetXInput.text = mapGeneration.targetPoint.x.ToString();
            targetYInput.text = mapGeneration.targetPoint.y.ToString();
            UpdateInfluencePointList();
        }
    }

    void OnDestroy()
    {
        ClearInfluenceListItems();
    }
}