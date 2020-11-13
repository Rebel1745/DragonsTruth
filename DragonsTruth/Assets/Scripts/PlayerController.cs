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

        // when either attack is performed, fire off a function
        pc.Land.RangedAttack.performed += x => RangedAttack();
        pc.Land.ContinuousAttack.performed += x => ContinuousAttack();
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
    private bool canJump = true;
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
    private float flt_attackTimer = 0f;
    private bool continuousAttacking = false;
    private GameObject currentContinuousPS = null;

    private bool CanUseMeleeAttack = true;
    public float MeleeAttackCooldown = 1f;
    private float meleeAttackCooldown = 0f;

    private void FixedUpdate()
    {
        //moveInput = Input.GetAxisRaw("Horizontal");
        moveInput = pc.Land.Movement.ReadValue<float>();
    }

    private void Update()
    {
        DoMovement();
        UpdateAttack();
    }

    #region Attacking
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

        // controls the difference between a button press, and a hold for attacks
        UpdateAttackTimer();

        // check to see if we have released the attack button, if so, cancel continuous attack
        if (continuousAttacking)
        {
            if (pc.Land.ContinuousAttack.ReadValue<float>() == 0f)
            {
                StopContinuousAttack();
            }
        }

        if(pc.Land.MeleeAttack.ReadValue<float>() > 0.1f) {
            // melee attack
            anim.SetTrigger("Headbutt");
            CanUseMeleeAttack = false;
            meleeAttackCooldown = MeleeAttackCooldown;
        }
    }

    public void TriggerRangedAttack()
    {
        // fire ranged attack fireball
        GameObject projectile = (GameObject)Instantiate(RangedAttackPrefab, AttackSpawnPoint.position, AttackSpawnPoint.rotation);
        projectile.transform.Rotate(0f, 90f, 0f);
        CanUseRangedAttack = false;
        rangedAttackCooldown = RangedAttackCooldown;
    }

    void RangedAttack()
    {
        if (!CanUseRangedAttack)
            return;

        // must be too high, can't workout why this works but it stops ranged attack from being fired if we are holding down the attack button
        if (flt_attackTimer <= 0)
        {
            flt_attackTimer = 0.4f;
            return;
        }

        anim.SetTrigger("RangedAttack");
    }

    void ContinuousAttack()
    {
        anim.SetTrigger("ContinuousAttackWarmup");
        continuousAttacking = true;
        // start continuous attack (particle effect)
        currentContinuousPS = (GameObject)Instantiate(ContinuousAttackPrefab, AttackSpawnPoint.position, AttackSpawnPoint.rotation);
        currentContinuousPS.transform.Rotate(0f, 90f, 0f);
        currentContinuousPS.GetComponent<ParticleSystem>().Play();
        currentContinuousPS.transform.parent = this.transform;
        CanUseContinuousAttack = false;
        continuousAttackCooldown = ContinuousAttackCooldown;
    }

    public void PlayContinuousAttack()
    {
        anim.SetTrigger("ContinuousAttack");
    }

    void StopContinuousAttack()
    {
        anim.SetTrigger("StopContinuousAttack");
        // for some reason the action needs to be restarted to work more than once
        // otherwise after performing the action it never returns to the waiting state
        pc.Land.ContinuousAttack.Disable();
        pc.Land.ContinuousAttack.Enable();
        continuousAttacking = false;
        currentContinuousPS.GetComponent<ParticleSystem>().Stop();
    }

    void UpdateAttackTimer()
    {
        if (flt_attackTimer >= 0)
        {
            flt_attackTimer -= Time.deltaTime;
        }
    }
    #endregion

    #region Movement
    void DoMovement()
    {
        #region Movement
        // use the moveInput value set in Update to move the character
        rb.velocity = new Vector2(moveInput * currentSpeed, rb.velocity.y);
        anim.SetFloat("Speed", Mathf.Abs(moveInput));
        #endregion

        #region Jumping
        // if we are on the ground, we can jump
        isGrounded = Physics2D.OverlapCircle(GroundCheck.position, CheckRadius, WhatIsGround);

        // Jump
        if (isGrounded)
        {
            anim.SetBool("Jump", false);
            anim.SetBool("Fall", false);
        }

        if (isGrounded && canJump && pc.Land.Jump.ReadValue<float>() == 1f)
        {
            rb.velocity = Vector2.up * JumpForce;
            anim.SetBool("Jump", true);
            canJump = false;
        }

        if (!canJump && pc.Land.Jump.ReadValue<float>() == 0)
            canJump = true;

        if(!isGrounded && rb.velocity.y < 0)
        {
            anim.SetBool("Jump", false);
            anim.SetBool("Fall", true);
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
        #endregion

        #region Flipping
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
        #endregion
    }
    #endregion

    #region Flip Function
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
    #endregion

}
