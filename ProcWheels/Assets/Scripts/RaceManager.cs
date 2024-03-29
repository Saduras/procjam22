using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RaceManager : MonoBehaviour
{
    public GameObject carPrefab;
    public float spawnHeight = 0.3f;
    public TrackGenerator track;

    public GameObject editorUI;
    public GameObject raceUI;

    public CameraController cameraController;

    GameObject car;

    public void StartRace()
    {
        Vector3 startPosition = track.GetStart();
        startPosition += new Vector3(0.0f, spawnHeight, 0.0f);

        car = Instantiate(carPrefab, startPosition, Quaternion.identity);
        editorUI.SetActive(false);
        raceUI.SetActive(true);
        cameraController.SetTargetCar(car);
    }

    public void StopRace()
    {
        cameraController.ClearTarget();
        Destroy(car);
        editorUI.SetActive(true);
        raceUI.SetActive(false);
    }

    void Update()
    {
        if(Input.GetButton("Cancel"))
            StopRace();
    }
}
