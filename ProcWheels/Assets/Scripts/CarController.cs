using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Setup based on PrettyFlyGames 2D Arcade TopDownCarController https://www.youtube.com/watch?v=DVHcOS1E5OQ&ab_channel=PrettyFlyGames
[RequireComponent(typeof(Rigidbody))]
public class CarController : MonoBehaviour
{
    [Header("Car Settings")]
    public float accelerationFactor = 30.0f;
    public float turnFactor = 3.5f;
    public float maxSteeringAtSpeed = 10.0f;
    public float driftFactor = 0.95f;
    public float breakFactor = 3.0f;

    [Header("Visualization")]
    public float maxSteerAngle = 30.0f;
    public Transform leftFrontWheel;
    public Transform rightFrontWheel;
    public GameObject breakLight;


    float accelerationInput;
    float steeringInput;
    bool isBreaking;

    float rotationAngle;

    Rigidbody rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        // Get input
        accelerationInput = Input.GetAxis("Vertical");
        steeringInput = Input.GetAxis("Horizontal");
        isBreaking = accelerationInput < 0.0f && Vector3.Dot(rb.velocity, transform.forward) > 0.0f;

        // Rotate front wheels
        leftFrontWheel.localRotation = Quaternion.AngleAxis(steeringInput * maxSteerAngle, Vector3.up);
        rightFrontWheel.localRotation = Quaternion.AngleAxis(steeringInput * maxSteerAngle, Vector3.up);

        // Toggle break light
        breakLight.SetActive(isBreaking);
    }

    void FixedUpdate()
    {
        // Engine acceleration
        float breakingInfluence = isBreaking ? breakFactor : 1.0f;
        Vector3 accelerationForce = transform.forward * accelerationInput * accelerationFactor * breakingInfluence;
        rb.AddForce(accelerationForce, ForceMode.Force);

        // Steering
        float steeringLimitBySpeed = Mathf.Clamp01(rb.velocity.magnitude / maxSteeringAtSpeed);
        rotationAngle += steeringInput * turnFactor * steeringLimitBySpeed;
        rb.MoveRotation(Quaternion.AngleAxis(rotationAngle, Vector3.up));

        // Reduce orthogonal velocity
        Vector3 forwardVelocity = transform.forward * Vector3.Dot(rb.velocity, transform.forward);
        Vector3 rightVelocity = transform.right * Vector3.Dot(rb.velocity, transform.right);
        rb.velocity = forwardVelocity + rightVelocity * driftFactor;
    }
}
