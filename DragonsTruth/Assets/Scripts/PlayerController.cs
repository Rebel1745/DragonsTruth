﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    private void Awake()
    {
        rb = GetComponentInChildren<Rigidbody2D>();
        sr = GetComponentInChildren<SpriteRenderer>();
        if (sr == null)
        {
            Debug.LogError("No sprite renderer found");
        }

        pc = new PlayerControls();
    }

    private void Start()
    {
        currentSpeed = Speed;
    }

    private void OnEnable()
    {
        pc.Enable();
    }

    private void OnDisable()
    {
        pc.Disable();
    }

    Rigidbody2D rb;
    SpriteRenderer sr;
    PlayerControls pc;

    public float Speed = 3f;
    public float SprintSpeed = 4f;
    float currentSpeed;

    public float JumpForce = 10f;
    public float FallMultiplier = 2.5f;
    public float LowJumpMultiplier = 2f;

    float moveInput;
    bool isFacingRight = true;

    public bool isGrounded;
    public Transform GroundCheck;
    public float CheckRadius;
    public LayerMask WhatIsGround;

    // Animation
    public Animator anim;

    private void FixedUpdate()
    {
        //moveInput = Input.GetAxisRaw("Horizontal");
        moveInput = pc.Land.Movement.ReadValue<float>();
        rb.velocity = new Vector2(moveInput * currentSpeed, rb.velocity.y);
        anim.SetFloat("Speed", Mathf.Abs(moveInput));
    }

    private void Update()
    {
        isGrounded = Physics2D.OverlapCircle(GroundCheck.position, CheckRadius, WhatIsGround);

        // Jump
        // if we are on the ground, we can jump
        if (isGrounded)
        {
            anim.SetBool("Jump", false);
        }
        else
            anim.SetBool("Jump", true);

        if (isGrounded && pc.Land.Jump.ReadValue<float>() == 1f)
        {
            rb.velocity = Vector2.up * JumpForce;
        }

        // if jumping or falling, alter gravity to allow for medium and large jumps
        if (rb.velocity.y < 0)
        {
            rb.velocity += Vector2.up * Physics2D.gravity.y * FallMultiplier * Time.deltaTime;
        }
        else if (rb.velocity.y > 0 && pc.Land.Jump.ReadValue<float>() == 0f)
        {
            rb.velocity += Vector2.up * Physics2D.gravity.y * LowJumpMultiplier * Time.deltaTime;
        }
        // END Jump
        
        // If the input is moving the player right and the player is facing left...
        if (moveInput > 0 && !isFacingRight)
        {
            // ... flip the player.
            Flip();
        }
        // Otherwise if the input is moving the player left and the player is facing right...
        else if (moveInput < 0 && isFacingRight)
        {
            // ... flip the player.
            Flip();
        }
    }

    private void Flip()
    {
        // Switch the way the player is labelled as facing.
        isFacingRight = !isFacingRight;

        // Multiply the player's x local scale by -1.
        Vector3 theScale = transform.localScale;
        theScale.x *= -1;
        transform.localScale = theScale;

        //sr.flipX = !sr.flipX;
    }
}
