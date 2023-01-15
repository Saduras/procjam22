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
    public float driftFactor = 0.95f;
    public float breakFactor = 3.0f;


    float accelerationInput;
    float steeringInput;

    float rotationAngle;

    Rigidbody rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();    
    }

    void Update()
    {
        accelerationInput = Input.GetAxis("Vertical");
        steeringInput = Input.GetAxis("Horizontal");
    }

    void FixedUpdate()
    {
        float forwardVelocityFactor =  Vector3.Dot(rb.velocity, transform.forward);

        // Engine acceleration
        float breakingInfluence = accelerationInput < 0.0f && forwardVelocityFactor > 0.0f ? breakFactor : 1.0f;
        Vector3 accelerationForce = transform.forward * accelerationInput * accelerationFactor * breakingInfluence;
        rb.AddForce(accelerationForce, ForceMode.Force);

        // Steering
        rotationAngle += steeringInput * turnFactor;
        rb.MoveRotation(Quaternion.AngleAxis(rotationAngle, Vector3.up));

        // Reduce orthogonal velocity
        Vector3 forwardVelocity = transform.forward * forwardVelocityFactor;
        Vector3 rightVelocity = transform.right * Vector3.Dot(rb.velocity, transform.right);
        rb.velocity = forwardVelocity + rightVelocity * driftFactor;
    }
}
