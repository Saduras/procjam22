using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class CarController : MonoBehaviour
{
    public float topSpeed = 10.0f;
    public float topTurnSpeed = 30.0f;

    Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();    
    }

    void FixedUpdate()
    {
        float speed = Input.GetAxis("Vertical");
        float turn = Input.GetAxis("Horizontal");
        rb.velocity = transform.forward * speed * topSpeed + new Vector3(0.0f, rb.velocity.y, 0.0f);

        if(Mathf.Abs(speed) > 0.01f)
            rb.angularVelocity = transform.up * topTurnSpeed * Mathf.Deg2Rad * turn * Mathf.Sign(speed);
    }
}
