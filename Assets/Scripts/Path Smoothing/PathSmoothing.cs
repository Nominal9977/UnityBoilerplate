using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Smoother
{
    // Constructor With Offset Point
    public Smoother(Vector3 start_point, Vector3 offset_point, Vector3 end_point) {
        this.start_point = start_point;
        this.offset_point = offset_point;
        this.end_point = end_point;
        has_offset_point = true;
    }

    // Constructor Without Offset Point
    public Smoother(Vector3 start_point, Vector3 end_point) {
        this.start_point = start_point;
        this.end_point = end_point;
        has_offset_point = false;
    }

    // Constuctor Defined Variables
    public Vector3 start_point;
    public Vector3 offset_point;
    public Vector3 end_point;

    // Changing Variables
    public float current_time = 0.0f;
    public Vector3 current_position;

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

enum NormalDirections
{
    North = 1,
    South,
    West,
    East,
    NormalDirection
}

enum DiagonalDirections
{
    North_East = NormalDirections.NormalDirection,
    North_West,
    South_East,
    South_West,
    DiagonalDirection
}

enum TurnDirection
{
    Left = DiagonalDirections.DiagonalDirection,
    Right,
    Straight
}
public class PathSmoothing : MonoBehaviour
{
    List<Node> node_list = new List<Node>();
    List<Smoother> final_path = new List<Smoother>();

    int smoother_index = 0;
    int last_exit = (int)NormalDirections.South;

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
        for (int i = 0; i < node_list.Count - 1; i++) {
            Vector3 current_node_position = node_list[i].position;
            Vector3 next_node_position = node_list[i+1].position;
            Vector3 far_node_position = node_list[i+2].position;

            Vector3 difference_a = next_node_position - current_node_position;
            Vector3 difference_b = far_node_position - next_node_position;

            if (last_exit <= (int)NormalDirections.East) // Normal Direction
            {
                if(difference_a == difference_b)
                {
                    // Check If Continue Straight
                    Smoother next_path = new Smoother(current_node_position - DirectionEnumToDirection(last_exit), next_node_position - difference_a/2);
                    final_path.Add(next_path);
                } else 
                {
                    // Check If Starting Diagonal, Otherwise Turn Normally
                    int exit_direction_a = DifferenceToNormalDirectionEnum(difference_a);
                    int exit_direction_b = DifferenceToNormalDirectionEnum(difference_b);

                    // We Know We Are Turning, So We Check If We Are Staying Oriented
                    if(exit_direction_b == last_exit)
                    {

                    }

                    // see if next exit direction is the opposite (right if left, left if right)

                }

                // cases
                // -Keep going straight
                // -turn left or right
                // -shift to diagonal left or right
            } else // Diagonal Direction
            {
                // cases
                // - continue diagonal
                // - fall out left or right (back to normal direction)
            }
        }

        
    }
    public Vector3 DirectionEnumToDirection(int direction)
    {
        switch(direction)
        {
            case (int)NormalDirections.North:
                return new Vector3(0, 0, 1);
            case (int)NormalDirections.South:
                return new Vector3(0, 0, -1);
            case (int)NormalDirections.East:
                return new Vector3(1, 0, 0);
            case (int)NormalDirections.West:
                return new Vector3(-1, 0, 0);
            case (int)DiagonalDirections.North_East:
                return new Vector3(1, 0, 1);
            case (int)DiagonalDirections.North_West:
                return new Vector3(-1, 0, 1);
            case (int)DiagonalDirections.South_East:
                return new Vector3(1, 0, -1);
            case (int)DiagonalDirections.South_West:
                return new Vector3(-1, 0, -1);
            default:
                Debug.LogError("SHOULD NEVER REACH");
                break;
        }
        return new Vector3();
    }

    public int DifferenceToNormalDirectionEnum(Vector3 difference)
    {
        if(difference.x > 0)
        {
            return (int)NormalDirections.East;
        } else
        {
            return (int)NormalDirections.West;
        }

        if(difference.z > 0)
        {
            return (int)NormalDirections.North;
        } else
        {
            return (int)NormalDirections.South;
        }
    } 

    public Vector3 DifferenceToDirection()

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
            current_path.Add(new_node);

            // Pick A Random Of 4 Directions (x and z)
            Vector3 next_direction = directions[Random.Range(0, directions.Count)];
            while(next_direction == -last_direction)
            {
                next_direction = directions[Random.Range(0, directions.Count)];
            }

            // Add To New Position
            current_position += next_direction;
        }

        // Set Path With Nodes
        SetPath(current_path);
    }

    // Update is called once per frame
    void Update()
    {
        // Get Current Smoother
        Smoother current_smoother = final_path[smoother_index];

        current_smoother.Update(Time.deltaTime);
        if(current_smoother.finished)
        {
            smoother_index += 1;
        }

        transform.position = current_smoother.current_position;
    }
}
