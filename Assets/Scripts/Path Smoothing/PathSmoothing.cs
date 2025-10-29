using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;
using UnityEngine;

public class Smoother
{
    // Constructor With Offset Point
    public Smoother(MapGeneration map_gen, Vector3 start_point, Vector3 offset_point, Vector3 end_point, PathSmoothing path_smoothing) {
        this.start_point = new Vector3(
            start_point.x * (map_gen.cellPadding + map_gen.cellSize),
            0,
            start_point.z * (map_gen.cellPadding + map_gen.cellSize)
        );
        this.offset_point = new Vector3(
            offset_point.x * (map_gen.cellPadding + map_gen.cellSize),
            0,
            offset_point.z * (map_gen.cellPadding + map_gen.cellSize)
        );
        this.end_point = new Vector3(
            end_point.x * (map_gen.cellPadding + map_gen.cellSize),
            0,
            end_point.z * (map_gen.cellPadding + map_gen.cellSize)
        );
        has_offset_point = true;
        pathsmoothing = path_smoothing;
        pathsmoothing.SpawnPoints(this.start_point, this.offset_point, this.end_point);
    }

    // Constructor Without Offset Point
    public Smoother(MapGeneration map_gen, Vector3 start_point, Vector3 end_point, PathSmoothing path_smoothing) {
        this.start_point = new Vector3(
            start_point.x * (map_gen.cellPadding + map_gen.cellSize),
            0,
            start_point.z * (map_gen.cellPadding + map_gen.cellSize)
        );
        this.end_point = new Vector3(
            end_point.x * (map_gen.cellPadding + map_gen.cellSize),
            0,
            end_point.z * (map_gen.cellPadding + map_gen.cellSize)
        );
        has_offset_point = false;
        pathsmoothing = path_smoothing;
        pathsmoothing.SpawnPoints(this.start_point, this.end_point);
    }

    // Constuctor Defined Variables
    public Vector3 start_point;
    public Vector3 offset_point;
    public Vector3 end_point;

    // Changing Variables
    public float current_time = 0.0f;
    public Vector3 current_position;
    public string type = "";

    public PathSmoothing pathsmoothing;

    bool has_offset_point = false;
    public bool finished = false;

    public void Update(float delta_time) {
        // Increment the current time by the delta time
        current_time += delta_time;

        if(current_time >= 1)
        {
            current_time = 1;
            finished = true;
        }

        // Calculate The Current Position (Based On The Smoother Curve)
        if (has_offset_point) {
            // Get Point On First Line (Start -> Offset)
            Vector3 point_0 = Vector3.Lerp(start_point, offset_point, current_time);

            // Get Point On Second Line (Offset -> End)
            Vector3 point_1 = Vector3.Lerp(offset_point, end_point, current_time);
            
            // Get Point On Line Between Point 0 And Point 1
            current_position = Vector3.Lerp(point_0, point_1, current_time);
        } else {
            // Get Point On Line (Start -> End)
            current_position = Vector3.Lerp(start_point, end_point, current_time);
        }
    }
}

enum Directions
{
    North = 1,
    South,
    West,
    East,
    North_East,
    North_West,
    South_East,
    South_West,
    DiagonalDirection
}

public class PathSmoothing : MonoBehaviour
{
    List<Node> node_list = new List<Node>();
    List<Smoother> final_path = new List<Smoother>();
    List<GameObject> start_points = new List<GameObject>();
    List<GameObject> bezier_points = new List<GameObject>();
    List<GameObject> end_points = new List<GameObject>();

    int smoother_index = 0;
    int last_exit_enum = (int)Directions.South;
    int creating_smoother_index = 0;

    [SerializeField] MapGeneration map_gen;
    [SerializeField] GameObject point_prefab;
    [SerializeField] GameObject bezier_point_prefab;
    public void SetPath(List<Node> path) {
        // Update Path
        this.node_list = path;

        // Calculate Direction For Each Point In Path
        for (int i = 0; i < node_list.Count - 2; i++) {
            // Get Next 3 Postitions
            Vector3 current_node_position = node_list[i].position;
            Vector3 next_node_position = node_list[i+1].position;
            Vector3 far_node_position = node_list[i+2].position;

            // Get Distance Between These Next Postiions
            Vector3 difference_a = next_node_position - current_node_position;
            Vector3 difference_b = far_node_position - next_node_position;

            // Get Node Exit Directions 
            int exit_direction_a_enum = DifferenceToNormalDirectionEnum(difference_a);
            int exit_direction_b_enum = DifferenceToNormalDirectionEnum(difference_b);
            Vector3 exit_direction_a = DirectionEnumToDirection(exit_direction_a_enum);
            Vector3 exit_direction_b = DirectionEnumToDirection(exit_direction_b_enum);

            // Get Current Entry Direction Enum & Direction Vector
            int entry_direction_enum = DirectionEnumToOppositeDirectionEnum(last_exit_enum);
            Vector3 entry_direction = DirectionEnumToDirection(entry_direction_enum);

            Debug.Log("CALCULATING (" + creating_smoother_index + ") " + DirectionEnumToString(entry_direction_enum) + " ENTRY\nFUTURE EXITS: " + DirectionEnumToString(exit_direction_a_enum) + " ... " + DirectionEnumToString(exit_direction_b_enum));

            // If Entering From Normal Direction (Not Diagonal)
            if (last_exit_enum <= (int)Directions.East)
            {
                // Check If Starting A Diagonal, Otherwise Turn Normally
                bool went_diagonal = false;

                // Get Node (Index Space Index) Ahead Of Entry
                Vector2 entry_ahead_index = new Vector2(current_node_position.x + entry_direction.x, current_node_position.z + entry_direction.z);
                
                // Use This To Determine If A Wall Exists There
                if (!MapGeneration.IsPositionBlocked(entry_ahead_index))
                {
                    // Check Two Different Scenarios In Which Diagonal Movement Is Started
                    if (exit_direction_b_enum == last_exit_enum && exit_direction_a_enum == NormalDirectionEnumTo90ClockwiseEnum(entry_direction_enum))
                    {
                        // Get Start, Offset (Bezier), & End Points
                        Vector3 start_point = current_node_position + entry_direction / 2;
                        Vector3 offset_point = current_node_position - (entry_direction / 5);
                        Vector3 end_point = current_node_position + (difference_a / 2) - (entry_direction / 2);

                        // Create Smoother
                        Smoother next_path = new Smoother(map_gen, start_point, offset_point, end_point, this);
                        next_path.type = "Starting Diagonal (Type A)";

                        // Add To Final Path
                        final_path.Add(next_path);

                        // Say That We Did Go Diagonal For Future Code
                        went_diagonal = true;

                        // Index By 1 More Since We Will Be "Skipping" Next Node
                        i += 1;

                        // Update Last Exit Since It Changed
                        last_exit_enum = NormalDirectionEnumTo45CounterClockwiseEnum(last_exit_enum, "Start Diagonal");

                        // Print Result
                        Debug.Log("--> RESULT - START DIAGONAL A | EXIT " + DirectionEnumToString(last_exit_enum));

                    }
                    else if (NormalDirectionEnumTo90CounterClockwiseEnum(exit_direction_b_enum) == last_exit_enum && exit_direction_a_enum == last_exit_enum)
                    {
                        // Get Start, Offset (Bezier), & Exit Points
                        Vector3 start_point = current_node_position + entry_direction / 2;
                        Vector3 offset_point = current_node_position + (difference_a / 5);
                        Vector3 end_point = current_node_position - (entry_direction / 2) + (exit_direction_b / 2);

                        // Create Smoother
                        Smoother next_path = new Smoother(map_gen, start_point, offset_point, end_point, this);
                        next_path.type = "Starting Diagonal (Type B)";

                        // Add To Final Path
                        final_path.Add(next_path);

                        // Say That We Did Go Diagonal For Future Code
                        went_diagonal = true;

                        // Increment Index Since We Will Be "Skipping" Next Node
                        i += 1;

                        // Update Last Exit Since It Changed
                        last_exit_enum = NormalDirectionEnumTo45ClockwiseEnum(last_exit_enum);

                        // Print Result
                        Debug.Log("--> RESULT - START DIAGONAL B | EXIT " + DirectionEnumToString(last_exit_enum));
                    }
                }

                // If Didn't Start Diagonal
                if (went_diagonal == false)
                {
                    // Check If We Are Going Straight, Otherwise Turn
                    if (exit_direction_a_enum == DirectionEnumToOppositeDirectionEnum(entry_direction_enum))
                    {
                        // Get Start & End Points
                        Vector3 start_point = current_node_position + DirectionEnumToDirection(entry_direction_enum) / 2;
                        Vector3 end_point = next_node_position - difference_a / 2;

                        // Create Smoother
                        Smoother next_path = new Smoother(map_gen, start_point, end_point, this);
                        next_path.type = "Normal Straight";

                        // Add To Final Path
                        final_path.Add(next_path);

                        // Print Result
                        Debug.Log("--> RESULT - STRAIGHT | EXIT " + DirectionEnumToString(last_exit_enum));
                    }
                    else
                    {
                        // Get Start & End Points (Offset (Bezier) Is Nodes Center)
                        Vector3 start_point = current_node_position + entry_direction /2;
                        Vector3 end_point = current_node_position + (difference_a / 2);

                        // Create Smoother
                        Smoother next_path = new Smoother(map_gen, start_point, current_node_position, end_point, this);
                        next_path.type = "Turning";

                        // Add To Final Path
                        final_path.Add(next_path);

                        // Update Last Exit Since It Change
                        last_exit_enum = exit_direction_a_enum;

                        // Print Result
                        print("--> RESULT - TURNING | EXIT " + DirectionEnumToString(last_exit_enum));
                    }
                }
            } 
            else // Starting Diagonal Direction
            {
                int left_direction = NormalDirectionEnumTo45CounterClockwiseEnum(last_exit_enum, "Continue Diagonal");
                int right_direction = NormalDirectionEnumTo45ClockwiseEnum(last_exit_enum);
                bool staying_diagonal = false;

                // Get Node (Index Space Index) Ahead Of Entry
                Vector2 entry_ahead_index = new Vector2(current_node_position.x + entry_direction.x, current_node_position.z + entry_direction.z);
                
                // Use This To Determine If A Wall Exists There
                if (!MapGeneration.IsPositionBlocked(entry_ahead_index))
                {
                    // Check Two Different Scenarios In Which Diagonal Movement Is Ended
                    if (exit_direction_a_enum == left_direction && exit_direction_b_enum == right_direction)
                    {
                        // Calculate Start & End Points
                        Vector3 start_point = current_node_position + entry_direction / 2;
                        Vector3 end_point = current_node_position - (entry_direction / 2);

                        // Create Smoother
                        Smoother next_path = new Smoother(map_gen, start_point, end_point, this);
                        next_path.type = "Continue Diagonal A";

                        // Add to Final Path
                        final_path.Add(next_path);

                        // Say That We Ended Up Staying Diagonal For Future Code
                        staying_diagonal = true;

                        // Increment Index Since We Will Be "Skipping" Next Node
                        i += 1;
                    }
                    else if (exit_direction_a_enum == right_direction && exit_direction_b_enum == left_direction)
                    {
                        // Create Start And End Points
                        Vector3 start_point = current_node_position + entry_direction / 2;
                        Vector3 end_point = current_node_position - (entry_direction / 2);

                        // Create Smoother
                        Smoother next_path = new Smoother(map_gen, start_point, end_point, this);
                        next_path.type = "Continue Diagonal B";

                        // Add To Final Path
                        final_path.Add(next_path);

                        // Say That We Ended Up Staying Diagonal For Future Code
                        staying_diagonal = true;

                        // Increment Index Since We Will Be "Skipping" Next Node
                        i += 1;
                    }
                }

                // If We Are Falling Out Of Going Diagonal
                if(staying_diagonal == false)
                {
                    // Set Start, Offset (Bezier), & End Points
                    Vector3 start_point = current_node_position - DirectionEnumToDirection(last_exit_enum) / 2;
                    Vector3 offset_point = current_node_position - (difference_a / 5); 
                    Vector3 end_point = next_node_position - difference_a / 2;

                    // Create Smoother
                    Smoother next_path = new Smoother(map_gen, start_point, offset_point, end_point, this);
                    next_path.type = "End Diagonal";
                    
                    // Add To Final Path
                    final_path.Add(next_path);

                    // Update Last Exit
                    last_exit_enum = exit_direction_a_enum;

                    // Print Result
                    print("--> RESULT - STOPPING DIAGONAL | EXIT " + DirectionEnumToString(last_exit_enum));
                }
            }
            creating_smoother_index++;

        }

    }
    
    public void SpawnPoints(Vector3 position_start, Vector3 position_end)
    {
        GameObject start_point = Instantiate(point_prefab, position_start, Quaternion.identity);
        start_point.name = "Start Point " + creating_smoother_index;
        GameObject end_point = Instantiate(point_prefab, position_end, Quaternion.identity);
        end_point.name = "End Point " + creating_smoother_index;

        start_points.Add(start_point);
        end_points.Add(end_point);
    }

    public void SpawnPoints(Vector3 position_start, Vector3 position_offset, Vector3 position_end)
    {
        GameObject start_point = Instantiate(point_prefab, position_start, Quaternion.identity);
        start_point.name = "Start Point " + creating_smoother_index;
        GameObject bezier_point = Instantiate(bezier_point_prefab, position_offset, Quaternion.identity);
        bezier_point.name = "Bezier Point " + creating_smoother_index;
        GameObject end_point = Instantiate(point_prefab, position_end, Quaternion.identity);
        end_point.name = "End Point " + creating_smoother_index;

        start_points.Add(start_point);
        bezier_points.Add(bezier_point);
        end_points.Add(end_point);
    }

    public void UpdateBezier(int index) {

    }
    
    public Vector3 DirectionEnumToDirection(int direction)
    {
        switch(direction)
        {
            case (int)Directions.North:
                return new Vector3(0, 0, 1);
            case (int)Directions.South:
                return new Vector3(0, 0, -1);
            case (int)Directions.East:
                return new Vector3(1, 0, 0);
            case (int)Directions.West:
                return new Vector3(-1, 0, 0);
            case (int)Directions.North_East:
                return new Vector3(1, 0, 1);
            case (int)Directions.North_West:
                return new Vector3(-1, 0, 1);
            case (int)Directions.South_East:
                return new Vector3(1, 0, -1);
            case (int)Directions.South_West:
                return new Vector3(-1, 0, -1);
            default:
                Debug.LogError("SHOULD NEVER REACH - DirectionEnumToDirection");
                break;
        }
        return new Vector3();
    }

    public int DifferenceToNormalDirectionEnum(Vector3 difference)
    {
        if(difference.x > 0)
        {
            return (int)Directions.East;
        }
        if(difference.x < 0) {
            return (int)Directions.West;
        }

        if(difference.z > 0)
        {
            return (int)Directions.North;
        } else
        {
            return (int)Directions.South;
        }
    } 

    public int NormalDirectionEnumTo90CounterClockwiseEnum(int direction)
    {
        switch (direction)
        {
            case (int)Directions.North:
                return (int)Directions.West;
            case (int)Directions.East:
                return (int)Directions.North;
            case (int)Directions.South:
                return (int)Directions.East;
            case (int)Directions.West:
                return (int)Directions.South;
            default:
                Debug.LogError("SHOULD NEVER REACH - NormalDirectionEnumTo90CounterClockwiseEnum  - INPUT: " + direction);
                break;
        }
        return (int)Directions.East;
    }

    public int NormalDirectionEnumTo90ClockwiseEnum(int direction)
    {
        switch (direction)
        {
            case (int)Directions.North:
                return (int)Directions.East;
            case (int)Directions.East:
                return (int)Directions.South;
            case (int)Directions.South:
                return (int)Directions.West;
            case (int)Directions.West:
                return (int)Directions.North;
            default:
                Debug.LogError("SHOULD NEVER REACH - NormalDirectionEnumTo90CounterClockwiseEnum  - INPUT: " + direction);
                break;
        }
        return (int)Directions.East;
    }

    public int NormalDirectionEnumTo45CounterClockwiseEnum(int direction, string from)
    {
        switch (direction)
        {
            case (int)Directions.North:
                return (int)Directions.North_West;
            case (int)Directions.East:
                return (int)Directions.North_East;
            case (int)Directions.South:
                return (int)Directions.South_East;
            case (int)Directions.West:
                return (int)Directions.South_West;
            case (int)Directions.North_West:
                return (int)Directions.West;
            case (int)Directions.North_East:
                return (int)Directions.North;
            case (int)Directions.South_East:
                return (int)Directions.East;
            case (int)Directions.South_West:
                return (int)Directions.South;
            default:
                Debug.LogError("SHOULD NEVER REACH - NormalDirectionEnumTo45CounterClockwiseEnum - INPUT: " + direction + " CALLED BY: " + from);
                break;
        }
        return (int)Directions.East;
    }

    public int NormalDirectionEnumTo45ClockwiseEnum(int direction)
    {
        switch (direction)
        {
            case (int)Directions.North:
                return (int)Directions.North_East;
            case (int)Directions.East:
                return (int)Directions.South_East;
            case (int)Directions.South:
                return (int)Directions.South_West;
            case (int)Directions.West:
                return (int)Directions.North_West;
            case (int)Directions.North_West:
                return (int)Directions.North;
            case (int)Directions.North_East:
                return (int)Directions.East;
            case (int)Directions.South_East:
                return (int)Directions.South;
            case (int)Directions.South_West:
                return (int)Directions.West;
            default:
                Debug.LogError("SHOULD NEVER REACH - NormalDirectionEnumTo90CounterClockwiseEnum");
                break;
        }
        return (int)Directions.East;
    }

    public int DirectionEnumToOppositeDirectionEnum(int direction)
    {
        switch (direction)
        {
            case (int)Directions.North:
                return (int)Directions.South;
            case (int)Directions.South:
                return (int)Directions.North;
            case (int)Directions.East:
                return (int)Directions.West;
            case (int)Directions.West:
                return (int)Directions.East;
            case (int)Directions.North_East:
                return (int)Directions.South_West;
            case (int)Directions.North_West:
                return (int)Directions.South_East;
            case (int)Directions.South_East:
                return (int)Directions.North_West;
            case (int)Directions.South_West:
                return (int)Directions.North_East;
        }
        return (int)Directions.North;
    }

    public string DirectionEnumToString(int direction)
    {
        switch (direction)
        {
            case (int)Directions.North:
                return "-NORTH-";
            case (int)Directions.South:
                return "-SOUTH-";
            case (int)Directions.East:
                return "-EAST-";
            case (int)Directions.West:
                return "-WEST-";
            case (int)Directions.North_East:
                return "-NORTH_EAST-";
            case (int)Directions.North_West:
                return "-NORTH_WEST-";
            case (int)Directions.South_East:
                return "-SOUTH_EAST-";
            case (int)Directions.South_West:
                return "-SOUTH_WEST-";
            default:
                return "DIRECTION NOT FOUND OH SHIT";
        }
    }

    private void AddNewNode(Vector3 _position)
    {
        // Make Node, Set Position, & Add To Node List
        Node new_node = new Node();
        new_node.position = _position;
        node_list.Add(new_node);
    }
    // Start is called before the first frame update
    void Start()
    {
        // Setup Start Position
        Vector3 current_position = new Vector3();
        
        // Setup Direction List
        List<Vector3> directions = new List<Vector3>();

        // Add Sample Direction List
        directions.Add(new Vector3(-1, 0, 0));
        directions.Add(new Vector3(0, 0, 1));
        directions.Add(new Vector3(-1, 0, 0));
        directions.Add(new Vector3(0, 0, 1));
        directions.Add(new Vector3(-1, 0, 0));
        directions.Add(new Vector3(0, 0, 1));
        directions.Add(new Vector3(-1, 0, 0));
        directions.Add(new Vector3(0, 0, 1));
        directions.Add(new Vector3(-1, 0, 0));
        directions.Add(new Vector3(0, 0, 1));
        directions.Add(new Vector3(-1, 0, 0));
        directions.Add(new Vector3(-1, 0, 0));
        directions.Add(new Vector3(-1, 0, 0));
        directions.Add(new Vector3(-1, 0, 0));
        directions.Add(new Vector3(0, 0, 1));
        directions.Add(new Vector3(0, 0, 1));
        directions.Add(new Vector3(0, 0, 1));
        directions.Add(new Vector3(1, 0, 0));
        directions.Add(new Vector3(0, 0, 1));
        directions.Add(new Vector3(1, 0, 0));
        directions.Add(new Vector3(0, 0, 1));
        directions.Add(new Vector3(1, 0, 0));
        directions.Add(new Vector3(0, 0, 1));
        directions.Add(new Vector3(1, 0, 0));


        int last_direction = 0;

        // Generate A Random Amount Of Nodes
        for (int i = 0; i < directions.Count; i++)
        {
            current_position += directions[i];
            AddNewNode(current_position);
        }

        // Set Path With Nodes
        SetPath(node_list);

        // Print First Node Set
        Smoother next = final_path[smoother_index];
        print(next.type + " S: " + next.start_point + " E: " + next.end_point);
    }

    // Update is called once per frame
    void Update()
    {
        if (final_path.Count == 0 || smoother_index >= final_path.Count)
        {
            return;
        }
         
        // Get Current Smoother
        Smoother current_smoother = final_path[smoother_index];

        current_smoother.Update(Time.deltaTime);
        if(current_smoother.finished)
        {
            smoother_index += 1;
            Smoother next = final_path[smoother_index];
            print(next.type + " S: " + next.start_point + " E: " + next.end_point);
        }

        transform.position = current_smoother.current_position;
    }
}
