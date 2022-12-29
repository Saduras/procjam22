using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraController : MonoBehaviour
{
    public float racingOrthographicSize = 6.0f;
    public Transform offset;

    public float smoothZoomStep = 0.1f;
    public float smoothStepPosition = 0.4f;

    Vector3 startPosition;
    float startSize;
    new Camera camera;

    Vector3 worldOffset;
    GameObject targetCar;
    Vector3 targetPosition;
    float targetSize;

    void Start()
    {
        camera = GetComponent<Camera>();
        startPosition = transform.position;
        startSize = camera.orthographicSize;
        worldOffset = startPosition - offset.position;
        ClearTarget();
    }

    public void SetTargetCar(GameObject car)
    {
        targetCar = car;
    }

    public void ClearTarget()
    {
        targetCar = null;
        targetPosition = startPosition;
        targetSize = startSize;
    }

    void FixedUpdate()
    {
        if(targetCar)
        {
            // Update target
            targetPosition = targetCar.transform.position + worldOffset;
            targetSize = racingOrthographicSize;
        }

        // Follow target
        this.transform.position = Vector3.Lerp(this.transform.position, targetPosition, smoothStepPosition);
        camera.orthographicSize = Mathf.Lerp(camera.orthographicSize, targetSize, smoothZoomStep);
    }
}
