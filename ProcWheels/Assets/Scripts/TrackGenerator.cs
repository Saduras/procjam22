using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

enum TileType
{
    None,
    StraightNS,
    StraightEW,
    CornerSE,
    CornerSW,
    CornerNE,
    CornerNW,
}

enum Direction
{
    Unset,
    North,
    East,
    South,
    West,
}

struct Connector
{
    public int x;
    public int z;
    public Direction direction;

    public Connector(int x, int z, Direction direction)
    {
        this.x = x;
        this.z = z;
        this.direction = direction;
    }
}

struct Point
{
    public int x;
    public int z;
    
    public Point(int x, int z)
    {
        this.x = x;
        this.z = z;
    }

    public override bool Equals(object o) => o is Point && this == (Point)o;
    public override int GetHashCode() => x.GetHashCode() ^ z.GetHashCode();
    public static bool operator ==(Point lhs, Point rhs) => lhs.x == rhs.x && lhs.z == rhs.z;
    public static bool operator !=(Point lhs, Point rhs) => !(lhs == rhs);

    public override string ToString()
    {
        return String.Format("({0}, {1})", x, z);
    }
}

public class TrackGenerator : MonoBehaviour
{
    public GameObject roadStraightPrefab;
    public GameObject roadCornerSmallPrefab;

    TileType[,] roadTiles;
    List<Point[]> debugPaths;

    public int scanX = 0;
    public int scanZ = 0;
    public int scanSize = 2;

    public void Clear()
    {
        if (Application.isPlaying)
            foreach (Transform childTransform in this.transform)
                Destroy(childTransform.gameObject);
        else
            // Source: https://stackoverflow.com/questions/38120084/how-can-we-destroy-child-objects-in-edit-modeunity3d
            for (int i = this.transform.childCount; i > 0; --i)
                DestroyImmediate(this.transform.GetChild(0).gameObject);
    }

    public void Generate()
    {
        // Start track
        roadTiles = new TileType[,]{
            {TileType.CornerSE, TileType.StraightEW, TileType.CornerSW },
            {TileType.StraightNS, TileType.None, TileType.StraightNS },
            {TileType.CornerNE, TileType.StraightEW, TileType.CornerNW },
        };

        // Modify track
        GenerateTrack(ref roadTiles);

        Connector[] connectors = ScanForConnectors(roadTiles, scanX, scanZ, scanSize);
        // To Local space
        for (int i = 0; i < connectors.Length; i++)
            connectors[i] = new Connector(connectors[i].x - scanX, connectors[i].z - scanZ, connectors[i].direction);
        GenerateRoad(scanSize, connectors);

        // Destroy previous track
        Clear();

        // Spawn new track
        SpawnPrefabs(roadTiles);
    }

    void GenerateTrack(ref TileType[,] tiles)
    {
        // TODO do something cool here

        int rowInsertCount = UnityEngine.Random.Range(0, 4);
        for (int i = 0; i < rowInsertCount; i++)
        {
            // Debug.LogFormat("Insert row at index {0}", 1);
            InsertRow(ref tiles, 1);
        }

        int columnInsertCount = UnityEngine.Random.Range(0, 4);
        for (int i = 0; i < columnInsertCount; i++)
        {
            // Debug.LogFormat("Insert column at index {0}", 1);
            InsertColumn(ref tiles, 1);
        }
    }

    void InsertRow(ref TileType[,] tiles, int rowIndex)
    {
        // Validate if insertion at row index is possible
        bool valid = true;
        for (int x = 0; x < tiles.GetLength(1); x++)
            valid &= tiles[rowIndex, x] == TileType.None || tiles[rowIndex, x] == TileType.StraightNS;

        if (!valid)
        {
            Debug.LogFormat("ERROR: Row {0} is invalid for insertion. Only StraightNS and None tiles are allowed!", rowIndex);
            return;
        }

        TileType[,] newTiles = new TileType[tiles.GetLength(0) + 1, tiles.GetLength(1)];
        for (int z = 0; z < newTiles.GetLength(0); z++)
        {
            int copyFromRow = (z > rowIndex) ? z - 1 : z;
            for (int x = 0; x < newTiles.GetLength(1); x++)
                newTiles[z, x] = tiles[copyFromRow, x];
        }
        tiles = newTiles;
    }

    void InsertColumn(ref TileType[,] tiles, int columnIndex)
    {
        // Validate if insertion at column index is possible
        bool valid = true;
        for (int z = 0; z < tiles.GetLength(0); z++)
            valid &= tiles[z, columnIndex] == TileType.None || tiles[z, columnIndex] == TileType.StraightEW;

        if (!valid)
        {
            Debug.LogFormat("ERROR: Column {0} is invalid for insertion. Only StraightEW and None tiles are allowed!", columnIndex);
            return;
        }

        TileType[,] newTiles = new TileType[tiles.GetLength(0), tiles.GetLength(1) + 1];
        for (int x = 0; x < newTiles.GetLength(1); x++)
        {
            int copyFromColumn = (x > columnIndex) ? x - 1 : x;
            for (int z = 0; z < newTiles.GetLength(0); z++)
                newTiles[z, x] = tiles[z, copyFromColumn];
        }
        tiles = newTiles;
    }

    Connector[] ScanForConnectors(TileType[,] tiles, int scan_x, int scan_z, int scan_size)
    {
        if(scan_x + scan_size > tiles.GetLength(1) || scan_z + scan_size > tiles.GetLength(0)
         || scan_x < 0 || scan_z < 0)
            throw new IndexOutOfRangeException("Scan area is outside tile map!");

        List<Connector> entries = new List<Connector>();

        // Check north edge
        for (int x = scan_x; x < scan_x + scan_size; x++)
        {
            TileType tile = tiles[scan_z, x];
            if(tile == TileType.StraightNS || tile == TileType.CornerNE || tile == TileType.CornerNW)
                entries.Add(new Connector(x, scan_z, Direction.North));
        }

        // Check south edge
        for (int x = scan_x; x < scan_x + scan_size; x++)
        {
            TileType tile = tiles[scan_z + scan_size - 1, x];
            if(tile == TileType.StraightNS || tile == TileType.CornerSE || tile == TileType.CornerSW)
                entries.Add(new Connector(x, scan_z + scan_size - 1, Direction.South));
        }

        // Check east edge
        for (int z = scan_z; z < scan_z + scan_size; z++)
        {
            TileType tile = tiles[z, scan_x + scan_size - 1];
            if(tile == TileType.StraightEW || tile == TileType.CornerSE || tile == TileType.CornerNE)
                entries.Add(new Connector(scan_x + scan_size - 1, z, Direction.East));
        }

        // Check west edge
        for (int z = scan_z; z < scan_z + scan_size; z++)
        {
            TileType tile = tiles[z, scan_x];
            if(tile == TileType.StraightEW || tile == TileType.CornerSW || tile == TileType.CornerNW)
                entries.Add(new Connector(scan_x, z, Direction.West));
        }

        return entries.ToArray();
    }

    TileType[,] GenerateRoad(int patchSize, Connector[] connectors)
    {
        TileType[,] road = new TileType[patchSize,patchSize];

        if(connectors.Length != 2)
            throw new ArgumentException("Only two connectors are supported!");

        Direction[,,] tiles = new Direction[2,2,2];

        List<Point[]> paths = new List<Point[]>();
        FindPath(new Point(connectors[0].x, connectors[0].z), 
                null,
                new Point(connectors[1].x, connectors[1].z), 
                patchSize, 
                ref paths);

        debugPaths = paths;

        foreach (var path in paths)
            Debug.LogFormat("Path: {0}", string.Join(",", path.Select(x => x.ToString()).ToArray()));

        return road;
    }

    Point Move(Point p, Direction d)
    {
        switch(d)
        {
            case Direction.North: return new Point(p.x, p.z - 1);
            case Direction.South: return new Point(p.x, p.z + 1);
            case Direction.West: return new Point(p.x - 1, p.z);
            case Direction.East: return new Point(p.x + 1, p.z);
            default: throw new ArgumentException(String.Format("Invalid direction: {0}", d));
        }
    }

    Direction Mirror(Direction d)
    {
        switch(d)
        {
            case Direction.North: return Direction.South;
            case Direction.South: return Direction.North;
            case Direction.West: return Direction.East;
            case Direction.East: return Direction.West;
            case Direction.Unset: return Direction.Unset;
            default: throw new ArgumentException(String.Format("Invalid direction: {0}", d));
        }
    }

    void FindPath(Point current, List<Point> currentPath, Point goal, int size, ref List<Point[]> results)
    {
        Debug.LogFormat("{0} {1} {2}", current, currentPath, goal);

        // Reach goal point
        if(current == goal)
        {
            results.Add(currentPath.ToArray());
            return;
        }

        if(currentPath is null)
            currentPath = new List<Point>{current};

        foreach (Direction direction in Enum.GetValues(typeof(Direction)))
        {
            if(direction == Direction.Unset)
                continue;

            Debug.LogFormat("Trying direction {0}", direction);

            Point newPoint = Move(current, direction);

            // Check if out of bounds
            if(newPoint.x < 0 || newPoint.x >= size || newPoint.z < 0 || newPoint.z >= size)
                continue;

            // Don't walk in cycles
            if(currentPath.Contains(newPoint))
                continue;

            List<Point> newPath = new List<Point>(currentPath);
            newPath.Add(newPoint);
            FindPath(newPoint, newPath, goal, size, ref results);
        }
    }

    void SpawnPrefabs(TileType[,] tiles)
    {
        float gridSize = 10.0f;

        for (int z = 0; z < tiles.GetLength(0); z++)
            for (int x = 0; x < tiles.GetLength(1); x++)
            {
                TileType tile = tiles[z, x];
                GameObject road = null;
                switch (tile)
                {
                    case TileType.None:
                        break;
                    case TileType.StraightNS:
                        road = Instantiate(roadStraightPrefab, new Vector3(x * gridSize, 0.0f, -z * gridSize), Quaternion.identity, this.transform);
                        road.name = String.Format("{0} ({1}, {2})", tile, x, z);
                        break;
                    case TileType.StraightEW:
                        road = Instantiate(roadStraightPrefab, new Vector3(x * gridSize, 0.0f, -z * gridSize), Quaternion.Euler(0.0f, 90.0f, 0.0f), this.transform);
                        road.name = String.Format("{0} ({1}, {2})", tile, x, z);
                        break;
                    case TileType.CornerSE:
                        road = Instantiate(roadCornerSmallPrefab, new Vector3(x * gridSize, 0.0f, -z * gridSize), Quaternion.identity, this.transform);
                        road.name = String.Format("{0} ({1}, {2})", tile, x, z);
                        break;
                    case TileType.CornerSW:
                        road = Instantiate(roadCornerSmallPrefab, new Vector3(x * gridSize, 0.0f, -z * gridSize), Quaternion.Euler(0.0f, 90.0f, 0.0f), this.transform);
                        road.name = String.Format("{0} ({1}, {2})", tile, x, z);
                        break;
                    case TileType.CornerNE:
                        road = Instantiate(roadCornerSmallPrefab, new Vector3(x * gridSize, 0.0f, -z * gridSize), Quaternion.Euler(0.0f, -90.0f, 0.0f), this.transform);
                        road.name = String.Format("{0} ({1}, {2})", tile, x, z);
                        break;
                    case TileType.CornerNW:
                        road = Instantiate(roadCornerSmallPrefab, new Vector3(x * gridSize, 0.0f, -z * gridSize), Quaternion.Euler(0.0f, 180.0f, 0.0f), this.transform);
                        road.name = String.Format("{0} ({1}, {2})", tile, x, z);
                        break;
                    default:
                        throw new ArgumentException(string.Format("Unsupported tile type {0}", tile));
                }
            }
    }

    void OnDrawGizmos()
    {
        if(roadTiles == null)
            return;

        float gridSize = 10.0f;

        Gizmos.color = Color.yellow;

        for (int x = 0; x < scanSize; x++)
            for (int z = 0; z < scanSize; z++)
                Gizmos.DrawWireCube(new Vector3(gridSize * (scanX + x), 0.0f, -gridSize * (scanZ + z)), new Vector3(gridSize, 0.5f, gridSize));

        float yOffset = 0.5f;
        Gizmos.color = Color.red;
        Connector[] connectors = ScanForConnectors(roadTiles, scanX, scanZ, scanSize);
        foreach (var connector in connectors)
        {
            float offsetX = 0;
            float offsetZ = 0;
            switch (connector.direction)
            {
                case Direction.North:
                    offsetZ = -0.5f;
                    break;
                case Direction.East:
                    offsetX = 0.5f;
                    break;
                case Direction.South:
                    offsetZ = 0.5f;
                    break;
                case Direction.West:
                    offsetX = -0.5f;
                    break;
            }
            Gizmos.DrawWireSphere(new Vector3(gridSize * (connector.x + offsetX), yOffset, -gridSize * (connector.z + offsetZ)), 1.0f);
            Gizmos.DrawLine(new Vector3(gridSize * (connector.x + offsetX), yOffset , -gridSize * (connector.z + offsetZ)), new Vector3(gridSize * (connector.x), yOffset, -gridSize * (connector.z)));
        }

        if(debugPaths is not null)
        {
            var points = debugPaths[0];
            for (int i = 0; i < points.Length; i++)
            {
                Gizmos.DrawWireSphere(new Vector3(gridSize * (points[i].x + scanX), yOffset, -gridSize * (points[i].z + scanZ)), 1.0f);
                if(i > 0)
                {
                    Gizmos.DrawLine(new Vector3(gridSize * (points[i].x + scanX), yOffset, -gridSize * (points[i].z + scanZ)), new Vector3(gridSize * (points[i-1].x + scanX), yOffset, -gridSize * (points[i-1].z + scanZ)));
                }
            }
        }

    }
}
