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
        GenerateTrack(tiles);

        // Destroy previous track
        Clear();

        // Spawn new track
        SpawnPrefabs(tiles);
    }

    void GenerateTrack(TileType[,] tiles)
    {
        // TODO do something cool here
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
