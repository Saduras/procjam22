using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RaceManager : MonoBehaviour
{
    public GameObject carPrefab;
    public TrackGenerator track;

    public GameObject editorUI;
    public GameObject raceUI;

    GameObject car;

    public void StartRace()
    {
        Vector3 startPosition = track.GetStart();
        startPosition += new Vector3(0.0f, 1.0f, 0.0f);

        car = Instantiate(carPrefab, startPosition, Quaternion.identity);
        editorUI.SetActive(false);
        raceUI.SetActive(true);
    }

    public void StopRace()
    {
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
