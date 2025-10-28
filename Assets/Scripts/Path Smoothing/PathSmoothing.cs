using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Smoother
{
    // Constructor With Offset Point
    public Smoother(MapGeneration map_gen, Vector3 start_point, Vector3 offset_point, Vector3 end_point) {
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
    }

    // Constructor Without Offset Point
    public Smoother(MapGeneration map_gen, Vector3 start_point, Vector3 end_point) {
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
    }

    // Constuctor Defined Variables
    public Vector3 start_point;
    public Vector3 offset_point;
    public Vector3 end_point;

    // Changing Variables
    public float current_time = 0.0f;
    public Vector3 current_position;
    public string type = "";

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

    int smoother_index = 0;
    int last_exit = (int)Directions.South;

    [SerializeField] MapGeneration map_gen;
    public void SetPath(List<Node> path) {
        // Update Path
        this.node_list = path;

        // first node in list will have start point (duh)
        // loop through and setup logic per unit, based on a start enxit and potional offset for bezier curves
        // used scotts hashmap to check for wall blocks which will form diagonals


        // variables
        // last "exit" state (current moving direction):
        // - normal (any of 4 cardinal directions)
        // - diagonal (any of 4 sub-cardinal directions)

        // frame checks
        // if exit normal

        List<int> exit_directions = new List<int>(node_list.Count);
        exit_directions.Add(last_exit);

        // Calculate Direction For Each Point In Path
        for (int i = 0; i < node_list.Count - 2; i++) {
            Vector3 current_node_position = node_list[i].position;
            Vector3 next_node_position = node_list[i+1].position;
            Vector3 far_node_position = node_list[i+2].position;

            Vector3 difference_a = next_node_position - current_node_position;
            Vector3 difference_b = far_node_position - next_node_position;

            // Check If Starting Diagonal, Otherwise Turn Normally
            int exit_direction_a = DifferenceToNormalDirectionEnum(difference_a);
            int exit_direction_b = DifferenceToNormalDirectionEnum(difference_b);

            if (last_exit <= (int)Directions.East) // Normal Direction
            {
                if(difference_a == difference_b)
                {
                    // Check If Continue Straight
                    int opposite_last_exit = DirectionEnumToOppositeDirectionEnum(last_exit);
                    Smoother next_path = new Smoother(map_gen, current_node_position - DirectionEnumToDirection(opposite_last_exit)/2, next_node_position - difference_a/2);
                    final_path.Add(next_path);
                    next_path.type = "Normal Straight";
                } else 
                {
                    // Check If Starting A Diagonal, Otherwise Turn Normally
                    bool went_diagonal = false;

                    // We Know We Are Turning, So We Check If We Are Staying Oriented
                    if(exit_direction_b == last_exit)
                    {
                        // Check If Opposite To Exit Is A "Wall"
                        int opposite_last_exit = DirectionEnumToOppositeDirectionEnum((int)last_exit);
                        Vector3 opposite_last_exit_direction = DirectionEnumToDirection(opposite_last_exit);
                        Vector2 oppposite_last_exit_index = new Vector2(current_node_position.x + opposite_last_exit_direction.x, current_node_position.z + opposite_last_exit_direction.z);
                        
                        bool wall_exists = MapGeneration.IsPositionBlocked(oppposite_last_exit_index);

                        if(!wall_exists)
                        {
                            Vector3 start_point = current_node_position - DirectionEnumToDirection(opposite_last_exit)/2;
                            Vector3 offset_point = current_node_position - (difference_a / 4) + (opposite_last_exit_direction / 4);
                            Vector3 end_point = current_node_position + (difference_a / 2) + (opposite_last_exit_direction / 2);
                            Smoother next_path = new Smoother(map_gen, start_point, offset_point, end_point);
                            next_path.type = "Starting Diagonal (Type A)";
                            final_path.Add(next_path);
                            went_diagonal = true;
                            i += 1;
                            last_exit = NormalDirectionEnumTo45CounterClockwiseEnum(last_exit, "Start Diagonal");
                        }
                    } else if(NormalDirectionEnumTo90CounterClockwiseEnum(exit_direction_b) == last_exit)
                    {
                        // Check If Opposite To Exit Is A "Wall"
                        int opposite_last_exit = DirectionEnumToOppositeDirectionEnum(exit_direction_b);
                        Vector3 opposite_last_exit_direction = DirectionEnumToDirection(opposite_last_exit);
                        Vector2 oppposite_last_exit_index = new Vector2(current_node_position.x + opposite_last_exit_direction.x, current_node_position.z + opposite_last_exit_direction.z);

                        bool wall_exists = MapGeneration.IsPositionBlocked(oppposite_last_exit_index);

                        if (!wall_exists)
                        {
                            Vector3 start_point = current_node_position - DirectionEnumToDirection(opposite_last_exit) / 2;
                            Vector3 offset_point = current_node_position - (difference_a / 4) + (opposite_last_exit_direction / 4);
                            Vector3 end_point = current_node_position + (difference_a / 2) + (opposite_last_exit_direction / 2);
                            Smoother next_path = new Smoother(map_gen, start_point, offset_point, end_point);
                            next_path.type = "Starting Diagonal (Type B)";
                            final_path.Add(next_path);
                            went_diagonal = true;
                            i += 1;
                            last_exit = NormalDirectionEnumTo45ClockwiseEnum(last_exit);
                        }
                    }

                    // No Other Option Besides Normal Turn
                    if(went_diagonal == false)
                    {
                        int opposite_last_exit = DirectionEnumToOppositeDirectionEnum((int)last_exit);
                        Vector3 start_point = current_node_position - DirectionEnumToDirection(opposite_last_exit)/2;
                        Vector3 end_point = current_node_position + (difference_a / 2);
                        Smoother next_path = new Smoother(map_gen, start_point, current_node_position, end_point);
                        next_path.type = "Turning";
                        final_path.Add(next_path);
                        last_exit = exit_direction_a;
                    }
                }

                // cases
                // -Keep going straight
                // -turn left or right
                // -shift to diagonal left or right
            } else // Diagonal Direction
            {
                int left_direction = NormalDirectionEnumTo45CounterClockwiseEnum(last_exit, "Continue Diagonal");
                int right_direction = NormalDirectionEnumTo45ClockwiseEnum(last_exit);
                bool staying_diagonal = false;

                if (exit_direction_a == left_direction)
                {
                    if(exit_direction_b == right_direction)
                    {
                        // Check If Opposite To Exit Is A "Wall"
                        int opposite_last_exit = DirectionEnumToOppositeDirectionEnum((int)last_exit);
                        Vector3 exit_b_direction = DirectionEnumToDirection(exit_direction_b);
                        Vector3 opposite_last_exit_direction = DirectionEnumToDirection(opposite_last_exit);
                        Vector2 oppposite_last_exit_index = new Vector2(current_node_position.x + exit_b_direction.x, current_node_position.z + exit_b_direction.z);

                        bool wall_exists = MapGeneration.IsPositionBlocked(oppposite_last_exit_index);

                        if (!wall_exists)
                        {
                            Vector3 start_point = current_node_position - DirectionEnumToDirection(opposite_last_exit) / 2;
                            Vector3 end_point = current_node_position + (difference_a / 2) + (opposite_last_exit_direction / 2);
                            Smoother next_path = new Smoother(map_gen, start_point, end_point);
                            next_path.type = "Continue Diagonal A";
                            final_path.Add(next_path);
                            staying_diagonal = true;
                            i += 1;
                        }
                    }
                }


                if (exit_direction_a == right_direction)
                {
                    if (exit_direction_b == left_direction)
                    {
                        // Check If Opposite To Exit Is A "Wall"
                        int opposite_last_exit = DirectionEnumToOppositeDirectionEnum((int)last_exit);
                        Vector3 exit_b_direction = DirectionEnumToDirection(exit_direction_b);
                        Vector3 opposite_last_exit_direction = DirectionEnumToDirection(opposite_last_exit);
                        Vector2 oppposite_last_exit_index = new Vector2(current_node_position.x + exit_b_direction.x, current_node_position.y + exit_b_direction.y);

                        bool wall_exists = MapGeneration.IsPositionBlocked(oppposite_last_exit_index);

                        if (!wall_exists)
                        {
                            Vector3 start_point = current_node_position - DirectionEnumToDirection(opposite_last_exit) / 2;
                            Vector3 end_point = current_node_position + (difference_a / 2) + (opposite_last_exit_direction / 2);
                            Smoother next_path = new Smoother(map_gen, start_point, end_point);
                            next_path.type = "Continue Diagonal B";
                            final_path.Add(next_path);
                            staying_diagonal = true;
                            i += 1;
                        }
                    }
                }

                if(staying_diagonal == false)
                {
                    Vector3 start_point = current_node_position - DirectionEnumToDirection(last_exit) / 2;
                    Vector3 end_point = next_node_position - difference_a / 2;
                    Smoother next_path = new Smoother(map_gen, start_point, end_point);
                    next_path.type = "End Diagonal";
                    final_path.Add(next_path);
                    last_exit = exit_direction_a;
                }
            }
        }

        
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

    // Start is called before the first frame update
    void Start()
    {
        // Setup Start Position
        Vector3 current_position = new Vector3();
        
        // Setup Direction List
        List<Vector3> directions = new List<Vector3>();

        // Add Cardinal Directions
        directions.Add(new Vector3(2, 0, 0));
        directions.Add(new Vector3(-2, 0, 0));
        directions.Add(new Vector3(0, 0, 2));
        directions.Add(new Vector3(0, 0, -2));

        Vector3 last_direction = new Vector3();

        // Generate A Random Amount Of Nodes
        for (int i = 0; i < 100; i++)
        { 
            // Make Node, Set Position, & Add To Node List
            Node new_node = new Node();
            new_node.position = current_position;
            node_list.Add(new_node);

            // Pick A Random Of 4 Directions (x and z)
            Vector3 next_direction = new Vector3(2, 0, 0);// directions[Random.Range(0, directions.Count)];
            //while(next_direction == -last_direction)
            //{
              //  next_direction = directions[Random.Range(0, directions.Count)];
            //}

            // Add To New Position
            current_position += next_direction;
        }

        // Set Path With Nodes
        SetPath(node_list);
    }

    // Update is called once per frame
    void Update()
    {
        if (final_path.Count == 0 || smoother_index >= final_path.Count)
        {
            print("RETURNING UPDATE EARLY");
            return;
        }

        // Get Current Smoother
        Smoother current_smoother = final_path[smoother_index];

        current_smoother.Update(Time.deltaTime/1.5f);
        if(current_smoother.finished)
        {
            smoother_index += 1;
            Smoother next = final_path[smoother_index];
            print(next.type + "S: " + next.start_point + " E: " + next.end_point);
        }

        transform.position = current_smoother.current_position;
    }
}
