﻿using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(CapsuleCollider2D))]

public class RigidbodyFPS2D : MonoBehaviour
{
    #region Variables (private)

    private bool grounded = false;
    public bool Grounded { get { return grounded; } }
    private Vector2 groundVelocity;
    private CapsuleCollider2D capsule;
    private Rigidbody2D rb;
    private CollisionSinkingDetector sinkingDetector;
    private bool sinkingSuspended = true;

    // Inputs Cache
    private bool jumpFlag = false;
    private int jumpFlagCountdown = 0;
    private int jumpFlagTimer = 10;
    private bool isJumping = false;

    #endregion

    #region Properties (public)

    // Speeds
    public float walkSpeed = 8.0f;
    //public float walkBackwardSpeed = 4.0f;
    //public float runSpeed = 14.0f;
    //public float runBackwardSpeed = 6.0f;
    //public float sidestepSpeed = 8.0f;
    //public float runSidestepSpeed = 12.0f;
    public float maxVelocityChange = 10.0f;

    // Air
    private float inAirControl = 0.0f;
    //public float jumpHeight = 2.0f;
    public float jumpSpeed = 10f;

    // Can Flags
    public bool canRunSidestep = true;
    public bool canJump = true;
    public bool canRun = true;

    #endregion

    #region Unity event functions

    /// <summary>
    /// Use for initialization
    /// </summary>
    void Awake()
    {
        capsule = GetComponent<CapsuleCollider2D>();
        rb = GetComponent<Rigidbody2D>();
        rb.freezeRotation = true;
        sinkingDetector = GetComponentInChildren<CollisionSinkingDetector>();
        sinkingDetector.TriggerStay += OnSinkingStay;
        //sinkingDetector.TriggerLeave += OnSinkingLeave;
        //rb.useGravity = true;
    }

    /// <summary>
    /// Use this for initialization
    /// </summary>
    //void Start()
    //{

    //}

    /// <summary>
    /// Update is called once per frame
    /// </summary>
    void Update()
    {
        // Cache the input
        if (Input.GetButtonDown("Jump"))
        {
            jumpFlagCountdown = jumpFlagTimer;
            jumpFlag = true;
        }
        if (jumpFlag)
        {
            jumpFlagCountdown--;
            if (jumpFlagCountdown == 0) jumpFlag = false;
        }
    }

    /// <summary>
    /// Update for physics
    /// </summary>
    void FixedUpdate()
    {
        // Cache de input
        var inputVector = new Vector2(Input.GetAxis("Horizontal"), 0);// Input.GetAxis("Vertical"));

        // On the ground
        if (grounded && !isJumping)
        {
            // Apply a force that attempts to reach our target velocity
            var velocityChange = CalculateVelocityChange(inputVector);
            rb.AddForce(velocityChange * rb.mass, ForceMode2D.Impulse);

            // Jump
            if (canJump && jumpFlag)
            {
                jumpFlag = false;
                rb.velocity = rb.velocity - (Vector2)Vector3.Normalize(rb.position) * jumpSpeed;// new Vector2(rb.velocity.x, rb.velocity.y + CalculateJumpVerticalSpeed());
                sinkingSuspended = true;
                sinkingDetector.ColliderEnabled = false;
                isJumping = true;
            }
            

            // By setting the grounded to false in every FixedUpdate we avoid
            // checking if the character is not grounded on OnCollisionExit()
            grounded = false;
        }
        // In mid-air
        else
        {
            isJumping = false;

            // Uses the input vector to affect the mid air direction
            var velocityChange = transform.TransformDirection(inputVector) * inAirControl;
            rb.AddForce(velocityChange * rb.mass, ForceMode2D.Impulse);
        }

        transform.rotation = Quaternion.AngleAxis(Mathf.Atan2(transform.position.x, -transform.position.y) * Mathf.Rad2Deg, Vector3.forward);
    }

    void OnSinkingStay(Collider2D other)
    {
        grounded = true && !sinkingSuspended;
    }

    //void OnSinkingLeave(Collider2D other)
    //{
    //    sinkingSuspended = false;
    //    sinkingDetector.ColliderEnabled = true;
    //}

    // Unparent if we are no longer standing on our parent
    void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.transform == transform.parent)
            transform.parent = null;
    }

    // If there are collisions check if the character is grounded
    void OnCollisionStay2D(Collision2D col)
    {
        TrackGrounded(col);
    }

    void OnCollisionEnter2D(Collision2D col)
    {
        TrackGrounded(col);
    }

    #endregion

    #region Methods

    // From the user input calculate using the set up speeds the velocity change
    private Vector2 CalculateVelocityChange(Vector2 inputVector)
    {
        // Calculate how fast we should be moving
        var relativeVelocity = (Vector2)transform.TransformDirection(inputVector);
        //if (inputVector.y > 0)
        //{
        //    relativeVelocity.y *= (canRun && Input.GetKey(KeyCode.LeftShift)/*GetButton("Sprint")*/) ? runSpeed : walkSpeed;
        //}
        //else
        //{
        //    relativeVelocity.y *= (canRun && Input.GetKey(KeyCode.LeftShift)/*GetButton("Sprint")*/) ? runBackwardSpeed : walkBackwardSpeed;
        //}
        //relativeVelocity.x *= (canRunSidestep && Input.GetKey(KeyCode.LeftShift)/*GetButton("Sprint")*/) ? runSidestepSpeed : sidestepSpeed;
        relativeVelocity *= walkSpeed;// (canRun && Input.GetKey(KeyCode.LeftShift)) ? runSpeed : walkSpeed;

        // Calcualte the delta velocity
        var currRelativeVelocity = rb.velocity - groundVelocity;
        var velocityChange = relativeVelocity - currRelativeVelocity;
        velocityChange.x = Mathf.Clamp(velocityChange.x, -maxVelocityChange, maxVelocityChange);
        velocityChange.y = Mathf.Clamp(velocityChange.y, -maxVelocityChange, maxVelocityChange);
        //velocityChange.y = 0;

        return velocityChange;
    }

    // From the jump height and gravity we deduce the upwards speed for the character to reach at the apex.
    //private float CalculateJumpVerticalSpeed()
    //{
    //    return Mathf.Sqrt(2f * jumpHeight * Mathf.Abs(Physics.gravity.y));
    //}

    // Check if the base of the capsule is colliding to track if it's grounded
    private void TrackGrounded(Collision2D collision)
    {
        if (!isJumping)
        {
            var minRadius = capsule.transform.position.magnitude + capsule.size.y * 0.5f - capsule.size.x * 0.45f;// capsule.bounds. + capsule.size.x * .9f;
            foreach (var contact in collision.contacts)
            {
                if (contact.point.magnitude > minRadius)
                {
                    if (isKinematic(collision))
                    {
                        // Get the ground velocity and we parent to it
                        groundVelocity = collision.rigidbody.velocity;
                        transform.parent = collision.transform;
                    }
                    else if (isStatic(collision))
                    {
                        // Just parent to it since it's static
                        transform.parent = collision.transform;
                    }
                    else
                    {
                        // We are standing over a dinamic object,
                        // set the groundVelocity to Zero to avoid jiggers and extreme accelerations
                        groundVelocity = Vector2.zero;
                    }

                    // Esta en el suelo
                    grounded = true;
                    sinkingSuspended = false;
                    sinkingDetector.ColliderEnabled = true;
                }

                break;
            }
        }
    }

    private bool isKinematic(Collision2D collision)
    {
        return isKinematic(collision.transform);
    }

    private bool isKinematic(Transform transform)
    {
        Rigidbody trrb = transform.GetComponent<Rigidbody>();
        return trrb != null && trrb.isKinematic;
    }

    private bool isStatic(Collision2D collision)
    {
        return isStatic(collision.transform);
    }

    private bool isStatic(Transform transform)
    {
        return transform.gameObject.isStatic;
    }

    #endregion
}