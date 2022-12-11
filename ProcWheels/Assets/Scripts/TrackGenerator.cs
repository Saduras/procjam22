using System;
using System.Collections.Generic;
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

public class TrackGenerator : MonoBehaviour
{
    public GameObject roadStraightPrefab;
    public GameObject roadCornerSmallPrefab;

    TileType[,] roadTiles;

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
            Debug.LogFormat("Insert row at index {0}", 1);
            InsertRow(ref tiles, 1);
        }

        int columnInsertCount = UnityEngine.Random.Range(0, 4);
        for (int i = 0; i < columnInsertCount; i++)
        {
            Debug.LogFormat("Insert column at index {0}", 1);
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
            Gizmos.DrawWireSphere(new Vector3(gridSize * (connector.x + offsetX), 0.0f, -gridSize * (connector.z + offsetZ)), 1.0f);
        }
    }
}
