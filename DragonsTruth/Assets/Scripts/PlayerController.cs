using System.Collections;
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

    // Attack config
    public Transform AttackSpawnPoint;
    public GameObject RangedAttackPrefab;
    public GameObject ContinuousAttackPrefab;

    private bool CanUseRangedAttack = true;
    public float RangedAttackCooldown = 1f;
    private float rangedAttackCooldown = 0f;

    private bool CanUseContinuousAttack = true;
    public float ContinuousAttackCooldown = 1f;
    private float continuousAttackCooldown = 0f;

    private bool CanUseMeleeAttack = true;
    public float MeleeAttackCooldown = 1f;
    private float meleeAttackCooldown = 0f;

    private void FixedUpdate()
    {
        //moveInput = Input.GetAxisRaw("Horizontal");
        moveInput = pc.Land.Movement.ReadValue<float>();
        rb.velocity = new Vector2(moveInput * currentSpeed, rb.velocity.y);
        anim.SetFloat("Speed", Mathf.Abs(moveInput));
    }

    private void Update()
    {
        DoMovement();
        UpdateAttack();
    }

    void UpdateAttackCountdowns()
    {
        if (!CanUseRangedAttack)
            rangedAttackCooldown -= Time.deltaTime;

        if (rangedAttackCooldown <= 0f)
            CanUseRangedAttack = true;

        if (!CanUseContinuousAttack)
            continuousAttackCooldown -= Time.deltaTime;

        if (continuousAttackCooldown <= 0f)
            CanUseContinuousAttack = true;

        if (!CanUseMeleeAttack)
            meleeAttackCooldown -= Time.deltaTime;

        if (meleeAttackCooldown <= 0f)
            CanUseMeleeAttack = true;
    }

    void UpdateAttack()
    {
        UpdateAttackCountdowns();

        if (pc.Land.RangedAttack.ReadValue<float>() > 0.1f && CanUseRangedAttack)
        {
            anim.SetTrigger("RangedAttack");
        }
        else if (pc.Land.ContinuousAttack.ReadValue<float>() > 0.1f && CanUseContinuousAttack)
        {
            ContinuousAttack();
        }
        else if(pc.Land.MeleeAttack.ReadValue<float>() > 0.1f) {
            // melee attack
            CanUseMeleeAttack = false;
            meleeAttackCooldown = MeleeAttackCooldown;
        }
    }

    void ContinuousAttack()
    {
        // start continuous attack (particle effect)
        GameObject continuousPS = (GameObject)Instantiate(ContinuousAttackPrefab, AttackSpawnPoint.position, AttackSpawnPoint.rotation);
        continuousPS.transform.Rotate(0f, 90f, 0f);
        continuousPS.GetComponent<ParticleSystem>().Play();
        continuousPS.transform.parent = this.transform;
        CanUseContinuousAttack = false;
        continuousAttackCooldown = ContinuousAttackCooldown;
    }

    public void RangedAttack()
    {
        // fire ranged attack fireball
        GameObject projectile = (GameObject)Instantiate(RangedAttackPrefab, AttackSpawnPoint.position, AttackSpawnPoint.rotation);
        projectile.transform.Rotate(0f, 90f, 0f);
        CanUseRangedAttack = false;
        rangedAttackCooldown = RangedAttackCooldown;
    }

    void DoMovement()
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
        /*Vector3 theScale = transform.localScale;
        theScale.x *= -1;
        transform.localScale = theScale;*/

        transform.Rotate(0f, 180f, 0f);

        //sr.flipX = !sr.flipX;
    }




}
