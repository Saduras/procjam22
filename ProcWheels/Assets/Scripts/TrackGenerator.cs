using System;
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

public class TrackGenerator : MonoBehaviour
{
    public GameObject roadStraightPrefab;
    public GameObject roadCornerSmallPrefab;

    // Start is called before the first frame update
    void Start()
    {
        Generate();
    }
    public void Clear()
    {
        if(Application.isPlaying)
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
        TileType[,] tiles = new TileType[,]{
            {TileType.CornerSE, TileType.StraightEW, TileType.CornerSW },
            {TileType.StraightNS, TileType.None, TileType.StraightNS },
            {TileType.CornerNE, TileType.StraightEW, TileType.CornerNW },
        };

        // Modify track
        GenerateTrack(ref tiles);

        // Destroy previous track
        Clear();

        // Spawn new track
        SpawnPrefabs(tiles);
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

        if(!valid)
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

        if(!valid)
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
}
