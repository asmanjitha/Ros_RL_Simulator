using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Experimental.GlobalIllumination;

public class MazeGenerator : MonoBehaviour
{
    public Vector3 cubeSize = new Vector3(7f, 0.1f, 7f); // Cube size (width, height, depth)
    public int mazeWidth = 20;  // Maze max width (Must be an ODD number)
    public int mazeHeight = 20; // Maze max height (Must be an ODD number)
    public GameObject wallPrefab; // Assign a cube prefab in Unity
    public GameObject floorPrefab;
    public GameObject pointLight;
    public int pointCount = 1; // Number of random points
    public List<Vector3> randomPoints = new List<Vector3>(); // Stores random walkable points
    public GameObject rescue;

    public int numObjects = 10;
    public int lightingLevel = 1;

    private int[,] maze; // Maze grid: 0 = path, 1 = wall
    private Vector2Int entrance;
    private Vector2Int exit;

    public GameObject tb3;
    public GameObject avatar;

    private static readonly Vector2Int[] directions = 
    {
        new Vector2Int(0, 2), new Vector2Int(2, 0), new Vector2Int(0, -2), new Vector2Int(-2, 0)
    };

    void Start()
    {
        // GenerateMaze();
    }

    public void GenerateMaze()
    {
        if (mazeWidth % 2 == 0) mazeWidth -= 1;  // Ensure odd size
        if (mazeHeight % 2 == 0) mazeHeight -= 1;

        maze = new int[mazeWidth, mazeHeight];
        randomPoints.Clear(); // Clear previous points

        // Initialize maze with walls
        for (int x = 0; x < mazeWidth; x++)
        {
            for (int y = 0; y < mazeHeight; y++)
            {
                maze[x, y] = 1; // Set all to walls
            }
        }

        // Select a random starting point (must be odd indices)
        int startX = Random.Range(1, mazeWidth - 1);
        int startY = Random.Range(1, mazeHeight - 1);
        startX = (startX % 2 == 0) ? startX + 1 : startX;
        startY = (startY % 2 == 0) ? startY + 1 : startY;

        // Depth-First Search Maze Generation
        CarveMaze(startX, startY);

        // Set Entrance and Exit
        // SetEntranceAndExit();

        // Generate Random Walkable Points
        GenerateRandomWalkablePoints();

        // Instantiate Maze
        BuildMaze();
    }

    private void CarveMaze(int x, int y)
    {
        maze[x, y] = 0; // Mark as a path

        List<Vector2Int> shuffledDirections = new List<Vector2Int>(directions);
        Shuffle(shuffledDirections);

        foreach (Vector2Int dir in shuffledDirections)
        {
            int nx = x + dir.x;
            int ny = y + dir.y;
            if (IsInBounds(nx, ny) && maze[nx, ny] == 1)
            {
                int wallX = x + dir.x / 2;
                int wallY = y + dir.y / 2;

                maze[wallX, wallY] = 0; // Remove wall
                CarveMaze(nx, ny);
            }
        }
    }

    private void SetEntranceAndExit()
    {
        entrance = new Vector2Int(0, 1); // Always at (0, 1)
        exit = new Vector2Int(mazeWidth - 1, mazeHeight - 2); // Exit at (maxWidth-1, maxHeight-2)

        maze[entrance.x, entrance.y] = 0;
        maze[exit.x, exit.y] = 0;
    }

    private void GenerateRandomWalkablePoints()
    {
        List<Vector2Int> walkableSpaces = new List<Vector2Int>();

        // Collect all walkable cells
        for (int x = 1; x < mazeWidth - 1; x++)
        {
            for (int y = 1; y < mazeHeight - 1; y++)
            {
                if (maze[x, y] == 0 && new Vector2Int(x, y) != entrance && new Vector2Int(x, y) != exit)
                {
                    walkableSpaces.Add(new Vector2Int(x, y));
                    Instantiate(floorPrefab, new Vector3(x*cubeSize.x,-(cubeSize.x / 2),y*cubeSize.z), Quaternion.identity,  gameObject.transform);
                    // Instantiate(floorPrefab, new Vector3(x*cubeSize.x,2.9f,y*cubeSize.z), Quaternion.identity,  gameObject.transform);
                    GameObject light = Instantiate(pointLight, new Vector3(x*cubeSize.x,1,y*cubeSize.z), Quaternion.identity, gameObject.transform);
                    if(lightingLevel == 1){
                        light.GetComponent<Light>().range = 1.0f;
                    }else{
                        light.GetComponent<Light>().range = 10.0f;
                    }
                    
                }
            }
        }

        Instantiate(floorPrefab, new Vector3(0,-1,7), Quaternion.identity,  gameObject.transform);
        // Instantiate(floorPrefab, new Vector3(mazeWidth - 1,-1,  mazeHeight - 2), Quaternion.identity,  gameObject.transform);

        // Select 'randomPointCount' unique random points
        for (int i = 0; i < pointCount && walkableSpaces.Count > 0; i++)
        {
            int index = Random.Range(0, walkableSpaces.Count);
            Vector2Int chosenPoint = walkableSpaces[index];
            walkableSpaces.RemoveAt(index);

            Vector3 worldPosition = new Vector3(chosenPoint.x * cubeSize.x, -0.85f, chosenPoint.y * cubeSize.z);
            randomPoints.Add(worldPosition);
            Instantiate(rescue, worldPosition, Quaternion.identity);
        }

        // --------------------Initiate Avatar and TB3-----------------------
        int avatarPosID = Random.Range(0, walkableSpaces.Count);
        Vector2Int avatarPos = walkableSpaces[avatarPosID];
        walkableSpaces.RemoveAt(avatarPosID);
        Vector3 worldPos = new Vector3(avatarPos.x * cubeSize.x, -0.85f, avatarPos.y * cubeSize.z);
        Instantiate(tb3, worldPos + new Vector3(0.5f, 0, 0), Quaternion.identity);
        Instantiate(avatar, worldPos + new Vector3(-0.5f, 0, 0), Quaternion.identity);

    }

    private void BuildMaze()
    {
        for (int x = 0; x < mazeWidth; x++)
        {
            for (int y = 0; y < mazeHeight; y++)
            {
                if (maze[x, y] == 1)
                {
                    Vector3 position = new Vector3(x * cubeSize.x, cubeSize.y / 2, y * cubeSize.z);
                    Instantiate(wallPrefab, position, Quaternion.identity, gameObject.transform);
                }
            }
        }
    }

    private bool IsInBounds(int x, int y)
    {
        return x > 0 && y > 0 && x < mazeWidth - 1 && y < mazeHeight - 1;
    }

    private void Shuffle<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            T temp = list[i];
            list[i] = list[j];
            list[j] = temp;
        }
    }
}
