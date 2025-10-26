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
    public Toggle influenceTypeToggle;

    [Header("Buttons")]
    public Button addInfluenceButton;
    public Button regenerateButton;

    [Header("Influence Point List")]
    public Transform influenceListContent;
    public GameObject influenceListItemPrefab;

    private void Start()
    {
        if (mapGeneration == null)
        {
            mapGeneration = FindObjectOfType<MapGeneration>();
        }

        InitializeInputFields();
        SetupButtonListeners();
    }

    void InitializeInputFields()
    {
        startXInput.text = mapGeneration.startPoint.x.ToString();
        startYInput.text = mapGeneration.startPoint.y.ToString();

        targetXInput.text = mapGeneration.targetPoint.x.ToString();
        targetYInput.text = mapGeneration.targetPoint.y.ToString();
        influenceXInput.text = "0";
        influenceYInput.text = "0";
        influenceStrengthInput.text = "1";
        influenceTypeToggle.isOn = true;
    }

    void SetupButtonListeners()
    {
        startXInput.onEndEdit.AddListener(_ => UpdateStartPoint());
        startYInput.onEndEdit.AddListener(_ => UpdateStartPoint());


        targetXInput.onEndEdit.AddListener(_ => UpdateTargetPoint());
        targetYInput.onEndEdit.AddListener(_ => UpdateTargetPoint());

        addInfluenceButton.onClick.AddListener(AddInfluencePoint);
        regenerateButton.onClick.AddListener(RegenerateFlowField);
    }

    void UpdateStartPoint()
    {
        try
        {
            float x = float.Parse(startXInput.text);
            float y = float.Parse(startYInput.text);
            mapGeneration.SetStartPoint(new Vector2(x, y));
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Invalid start point input: {e.Message}");
        }
    }

    void UpdateTargetPoint()
    {
        try
        {
            float x = float.Parse(targetXInput.text);
            float y = float.Parse(targetYInput.text);
            mapGeneration.SetTargetPoint(new Vector2(x, y));
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Invalid target point input: {e.Message}");
        }
    }

    void AddInfluencePoint()
    {
        try
        {
            float x = float.Parse(influenceXInput.text);
            float y = float.Parse(influenceYInput.text);
            float strength = float.Parse(influenceStrengthInput.text);
            bool isAttraction = influenceTypeToggle.isOn;

            mapGeneration.AddInfluencePoint(
                new Vector2(x, y), 
                strength, 
                isAttraction
            );

            UpdateInfluencePointList();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Invalid influence point input: {e.Message}");
        }
    }

    void UpdateInfluencePointList()
    {
        foreach (Transform child in influenceListContent)
        {
            Destroy(child.gameObject);
        }

        foreach (var point in mapGeneration.GetInfluencePoints())
        {
            GameObject listItem = Instantiate(influenceListItemPrefab, influenceListContent);
            
            Text itemText = listItem.GetComponentInChildren<Text>();
            itemText.text = $"({point.Position.x}, {point.Position.y}) - Strength: {point.Strength} - " +
                            $"{(point.IsAttraction ? "Attraction" : "Repulsion")}";

            Button deleteButton = listItem.GetComponentInChildren<Button>();
            if (deleteButton != null)
            {
                deleteButton.onClick.AddListener(() => 
                {
                    mapGeneration.RemoveInfluencePoint(point);
                    UpdateInfluencePointList();
                });
            }
        }
    }

    void RegenerateFlowField()
    {
        mapGeneration.RegenerateFlowField();
    }
}