using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

enum TileType
{
    None,
    StraightNS,
    StraightWE,
    CornerSE,
    CornerSW,
    CornerNE,
    CornerNW,
}

enum Direction
{
    Unset,
    North,
    West,
    East,
    South,
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

struct DirectedPoint
{
    public Point point;
    public Direction direction;

    public DirectedPoint(Point point, Direction direction)
    {
        this.point = point;
        this.direction = direction;
    }

    public override string ToString()
    {
        return String.Format("{0} {1}", point, direction);
    }
}

public class TrackGenerator : MonoBehaviour
{
    public GameObject roadStraightPrefab;
    public GameObject roadCornerSmallPrefab;

    public int rows = 4;
    public int columns = 4;
    public int patchSize = 2;

    TileType[,] roadTiles;
    List<DirectedPoint[]> debugPaths;
    Connector[] debugConnectors;

    public void OnRowsChanged(String newText)
    {
        int newValue = 0;
        if(int.TryParse(newText, out newValue))
            rows = newValue;
    }

    public void OnColumnsChanged(String newText)
    {
        int newValue = 0;
        if(int.TryParse(newText, out newValue))
            columns = newValue;
    }
    
    public void OnPatchSizeChanged(String newText)
    {
        int newValue = 0;
        if(int.TryParse(newText, out newValue))
            patchSize = newValue;
    }

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
            {TileType.CornerSE, TileType.StraightWE, TileType.CornerSW },
            {TileType.StraightNS, TileType.None, TileType.StraightNS },
            {TileType.CornerNE, TileType.StraightWE, TileType.CornerNW },
        };

        try
        {
            // Modify track
            GenerateTrack(ref roadTiles);
        }
        catch (Exception e)
        {
            Debug.LogError(e.Message);
        }

        // Destroy previous track
        Clear();

        // Spawn new track
        StopAllCoroutines();
        StartCoroutine("SpawnPrefabs");
    }

    static TileType GetTileFromDirections(Direction direction1, Direction direction2)
    {
        switch (direction1)
        {
            case Direction.North:
                switch (direction2)
                {
                    case Direction.West: return TileType.CornerNW;
                    case Direction.East: return TileType.CornerNE;
                    case Direction.South: return TileType.StraightNS;
                }
                break;
            case Direction.West:
                switch (direction2)
                {
                    case Direction.North: return TileType.CornerNW;
                    case Direction.East: return TileType.StraightWE;
                    case Direction.South: return TileType.CornerSW;
                }
                break;
            case Direction.East:
                switch (direction2)
                {
                    case Direction.North: return TileType.CornerNE;
                    case Direction.West: return TileType.StraightWE;
                    case Direction.South: return TileType.CornerSE;
                }
                break;
            case Direction.South:
                switch (direction2)
                {
                    case Direction.North: return TileType.StraightNS;
                    case Direction.West: return TileType.CornerSW;
                    case Direction.East: return TileType.CornerSE;
                }
                break;
        }

        // Invalid combination of directions
        return TileType.None;
    }

    void GenerateTrack(ref TileType[,] tiles)
    {
        int rowInsertCount = rows - 3;
        for (int i = 0; i < rowInsertCount; i++)
        {
            InsertRow(ref tiles, 1);
        }

        int columnInsertCount = columns - 3;
        for (int i = 0; i < columnInsertCount; i++)
        {
            InsertColumn(ref tiles, 1);
        }

        // Iterate over all square patches and mutate them to random variants
        for (int x = 0; x <= tiles.GetLength(1) - patchSize; x += patchSize)
        {
            for (int z = 0; z <= tiles.GetLength(0) - patchSize; z += patchSize)
            {
                DirectedPoint[] connectors = ScanForConnectors(tiles, x, z, patchSize);
                
                // To Local space
                for (int i = 0; i < connectors.Length; i++)
                    connectors[i] = new DirectedPoint(new Point(connectors[i].point.x - x, connectors[i].point.z - z), connectors[i].direction);

                if (connectors.Length == 0)
                    continue;

                // Clear old tiles
                for (int lx = 0; lx < patchSize; lx++)
                    for (int lz = 0; lz < patchSize; lz++)
                        tiles[lz + z, lx + x] = TileType.None;

                List<DirectedPoint[]> paths = new List<DirectedPoint[]>();
                FindPath(new Point(connectors[0].point.x, connectors[0].point.z),
                        null,
                        new Point(connectors[1].point.x, connectors[1].point.z),
                        patchSize,
                        ref paths);

                if (paths.Count() == 0)
                    continue;

                Direction lastDirection = connectors[0].direction;
                DirectedPoint[] path = paths[UnityEngine.Random.Range(0, paths.Count())];
                for (int i = 0; i < path.Length; i++)
                {
                    Point worldPoint = new Point(path[i].point.x + x, path[i].point.z + z);
                    Direction nextDirection = (i < path.Length - 1) ? Mirror(path[i + 1].direction) : connectors[1].direction;
                    tiles[worldPoint.z, worldPoint.x] = GetTileFromDirections(lastDirection, nextDirection);
                    lastDirection = Mirror(nextDirection);
                }
            }
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
            Debug.LogErrorFormat("Row {0} is invalid for insertion. Only StraightNS and None tiles are allowed!", rowIndex);
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
            valid &= tiles[z, columnIndex] == TileType.None || tiles[z, columnIndex] == TileType.StraightWE;

        if (!valid)
        {
            Debug.LogErrorFormat("Column {0} is invalid for insertion. Only StraightEW and None tiles are allowed!", columnIndex);
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

    DirectedPoint[] ScanForConnectors(TileType[,] tiles, int scan_x, int scan_z, int scan_size)
    {
        if (scan_x + scan_size > tiles.GetLength(1) || scan_z + scan_size > tiles.GetLength(0)
         || scan_x < 0 || scan_z < 0)
            throw new IndexOutOfRangeException(String.Format("Scan area is outside tile map! Tiles dimensions: {0}x{1} x: {2} z:{3} size:{4}", tiles.GetLength(0), tiles.GetLength(1), scan_x, scan_z, scan_size));

        List<DirectedPoint> entries = new List<DirectedPoint>();

        // Check north edge
        for (int x = scan_x; x < scan_x + scan_size; x++)
        {
            TileType tile = tiles[scan_z, x];
            if (tile == TileType.StraightNS || tile == TileType.CornerNE || tile == TileType.CornerNW)
                entries.Add(new DirectedPoint(new Point(x, scan_z), Direction.North));
        }

        // Check south edge
        for (int x = scan_x; x < scan_x + scan_size; x++)
        {
            TileType tile = tiles[scan_z + scan_size - 1, x];
            if (tile == TileType.StraightNS || tile == TileType.CornerSE || tile == TileType.CornerSW)
                entries.Add(new DirectedPoint(new Point(x, scan_z + scan_size - 1), Direction.South));
        }

        // Check east edge
        for (int z = scan_z; z < scan_z + scan_size; z++)
        {
            TileType tile = tiles[z, scan_x + scan_size - 1];
            if (tile == TileType.StraightWE || tile == TileType.CornerSE || tile == TileType.CornerNE)
                entries.Add(new DirectedPoint(new Point(scan_x + scan_size - 1, z), Direction.East));
        }

        // Check west edge
        for (int z = scan_z; z < scan_z + scan_size; z++)
        {
            TileType tile = tiles[z, scan_x];
            if (tile == TileType.StraightWE || tile == TileType.CornerSW || tile == TileType.CornerNW)
                entries.Add(new DirectedPoint(new Point(scan_x, z), Direction.West));
        }

        return entries.ToArray();
    }

    TileType[,] GenerateRoad(int patchSize, Connector[] connectors)
    {
        TileType[,] road = new TileType[patchSize, patchSize];

        if (connectors.Length != 2)
            throw new ArgumentException("Only two connectors are supported!");

        Direction[,,] tiles = new Direction[2, 2, 2];

        List<DirectedPoint[]> paths = new List<DirectedPoint[]>();
        FindPath(new Point(connectors[0].x, connectors[0].z),
                null,
                new Point(connectors[1].x, connectors[1].z),
                patchSize,
                ref paths);

        debugPaths = paths;

        

        return road;
    }

    Point Move(Point p, Direction d)
    {
        switch (d)
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
        switch (d)
        {
            case Direction.North: return Direction.South;
            case Direction.South: return Direction.North;
            case Direction.West: return Direction.East;
            case Direction.East: return Direction.West;
            case Direction.Unset: return Direction.Unset;
            default: throw new ArgumentException(String.Format("Invalid direction: {0}", d));
        }
    }

    void FindPath(Point current, List<DirectedPoint> currentPath, Point goal, int size, ref List<DirectedPoint[]> results)
    {
        // Reach goal point
        if (current == goal)
        {
            results.Add(currentPath.ToArray());
            return;
        }

        if (currentPath is null)
            currentPath = new List<DirectedPoint> { new DirectedPoint(current, Direction.Unset) };

        foreach (Direction direction in Enum.GetValues(typeof(Direction)))
        {
            if (direction == Direction.Unset)
                continue;

            Point newPoint = Move(current, direction);

            // Check if out of bounds
            if (newPoint.x < 0 || newPoint.x >= size || newPoint.z < 0 || newPoint.z >= size)
                continue;

            // Don't walk in cycles
            if (currentPath.Any(step => step.point == newPoint))
                continue;
            
            // Deep copy path
            List<DirectedPoint> newPath = currentPath.ConvertAll(step => new DirectedPoint(step.point, step.direction));
            newPath.Add(new DirectedPoint(newPoint, Mirror(direction)));
            FindPath(newPoint, newPath, goal, size, ref results);
        }
    }

    IEnumerator SpawnPrefabs()
    {
        float gridSize = 10.0f;

        for (int z = 0; z < roadTiles.GetLength(0); z++)
            for (int x = 0; x < roadTiles.GetLength(1); x++)
            {
                TileType tile = roadTiles [z, x];
                GameObject road = null;
                switch (tile)
                {
                    case TileType.None:
                        break;
                    case TileType.StraightNS:
                        road = Instantiate(roadStraightPrefab, new Vector3(x * gridSize, 0.0f, -z * gridSize), Quaternion.identity, this.transform);
                        road.name = String.Format("{0} ({1}, {2})", tile, x, z);
                        break;
                    case TileType.StraightWE:
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
                yield return new WaitForSeconds(0.02f);
            }
    }
}
