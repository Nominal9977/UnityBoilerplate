using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FlowFieldGUI : MonoBehaviour
{
    [Header("Map Generation Reference")]
    public MapGeneration mapGeneration;

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
        // Validation logic if needed
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

            mapGeneration.AddInfluencePointWithoutRegen(
                new Vector2(x, y),
                strength,
                isAttraction
            );

            UpdateInfluencePointList();

            // Clear input fields
            influenceXInput.text = "0";
            influenceYInput.text = "0";
            influenceStrengthInput.text = "1";

            UpdateStatusText($"Added repulsion point at ({x}, {y}) with strength {strength} - Click 'Regenerate' to apply");
        }
        catch (System.Exception e)
        {
            UpdateStatusText("Error: Invalid influence point input");
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

        // FIND TEXTMESHPRO (not legacy Text)
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

        // Find the delete button
        Button deleteButton = listItem.GetComponentInChildren<Button>();
        if (deleteButton != null)
        {
            // Check if this is a start or target point
            bool isStartPoint = !point.IsAttraction && Vector2.Distance(point.Position, mapGeneration.startPoint) < 0.1f;
            bool isTargetPoint = point.IsAttraction && Vector2.Distance(point.Position, mapGeneration.targetPoint) < 0.1f;

            // Hide button for start/target points
            if (isStartPoint || isTargetPoint)
            {
                deleteButton.gameObject.SetActive(false);
                Debug.Log($"Hiding delete button for {(isStartPoint ? "START" : "TARGET")} point");
            }
            else
            {
                // Show button for custom repulsion points
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
            // Remove WITHOUT regenerating
            mapGeneration.RemoveInfluencePointWithoutRegen(point);
            UpdateInfluencePointList();
            UpdateStatusText($"Removed influence point at ({point.Position.x:F1}, {point.Position.y:F1}) - Click 'Regenerate' to apply");
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

            // Remove all without regenerating
            foreach (var point in pointsToRemove)
            {
                mapGeneration.RemoveInfluencePointWithoutRegen(point);
            }

            UpdateInfluencePointList();
            UpdateStatusText($"Cleared {pointsToRemove.Count} repulsion points - Click 'Regenerate' to apply");
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