﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Gamelogic.Extensions;
using System;

public class Gravity : GLMonoBehaviour
{
    private const string AFFECTED_TAG = "Affected";
    [TooltipAttribute("Rotation per second")]
    public float frequency;
    private GameObject[] affectedObjects;

    // Physics
    private float angularVelocity;

    void Start() {
        FindAffectedObjects();
    }
    void Update()
    {
        FindAffectedObjects();
    }

    private void FindAffectedObjects()
    {
        affectedObjects = GameObject.FindGameObjectsWithTag(AFFECTED_TAG);
    }

    void FixedUpdate()
    {
        angularVelocity = 2 * Mathf.PI * frequency;
        foreach (GameObject g in affectedObjects)
        {
            Attract(g);
        }
    }

    private void Attract(GameObject body)
    {

        Rigidbody2D rb = body.GetComponent<Rigidbody2D>();
        if (rb)
        {
            Transform t = body.transform;
            Vector3 gravityUp = t.position - transform.position;
            // Centripetal force
            Vector3 centripetalForce = rb.mass * gravityUp.magnitude * Mathf.Pow(angularVelocity, 2) * gravityUp;
            rb.AddForce(centripetalForce.WithZ(0));

            // Coriolis force
            Vector3 rotationVector = Vector3.back * angularVelocity;
            Vector3 velocityVector = rb.velocity;
            Vector3 coriolisForce = -2 * rb.mass * Vector3.Cross(rotationVector, velocityVector);
            rb.AddForce(coriolisForce);
        }
    }
}