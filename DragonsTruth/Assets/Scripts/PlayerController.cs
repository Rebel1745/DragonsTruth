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

        startGravityScale = rb.gravityScale;
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
    public bool CanSprint = true;
    public float SprintSpeed = 4f;
    float currentSpeed;

    // Jumping Variables
    float startGravityScale;
    public float JumpForce = 5f;
    private bool canJump = true;
    public float FallMultiplier = 2.5f;
    public float LowJumpMultiplier = 2f;
    bool isGrounded;
    public Transform GroundCheck;
    public float CheckRadius;
    public LayerMask WhatIsGround;
    public float GroundedRememberTime = 0.1f;
    float groundedRemember = 0;

    float moveInput;
    bool isFacingRight = true;

    // Animation
    public Animator anim;

    // Attack config
    public enum PLAYER_STATE {YELLOW, BLUE, RED, GREEN, ORANGE};
    PLAYER_STATE PlayerState = PLAYER_STATE.YELLOW;
    public Transform AttackSpawnPoint;
    public GameObject[] RangedAttackPrefabs;
    public GameObject[] ContinuousAttackPrefabs;

    public bool CanUseRangedAttack = true;
    bool rangedAttackAvailable = true;
    public float RangedAttackCooldown = 1f;
    private float rangedAttackCooldown = 0f;

    public bool CanUseContinuousAttack = true;
    bool continuousAttackAvailable = true;
    public float ContinuousAttackCooldown = 1f;
    private float continuousAttackCooldown = 0f;
    private float flt_attackTimer = 0f;
    private bool continuousAttacking = false;
    private GameObject currentContinuousPS = null;

    public bool CanUseMeleeAttack = false;
    bool meleeAttackAvailable = false;
    public float MeleeAttackCooldown = 1f;
    private float meleeAttackCooldown = 0f;

    //wall clinging, climbing, sliding, jumping
    public Transform FrontCheck;
    bool isTouchingFront = false;
    public bool CanWallClimb = true;
    public bool isWallClimb = false;
    public float WallClimbSpeed = 2f;
    public bool CanWallRun = true;
    bool isWallRun = false;
    public float WallRunSpeed = 4f;
    public bool CanWallJump = true;
    public float WallJumpUpForce = 5f;
    public float WallJumpSidewaysForce = 3f;
    private Vector2 wallJumpDir;
    private bool canMove = true;
    public float AfterJumpMovementTime = 0.1f;
    public float WallRememberTime = 0.1f;
    float wallRemember = 0;

    private void FixedUpdate()
    {
        DoMovement();
        CheckForm();
        UpdateAttack();
    }

    private void Update()
    {
        DoJumping();
        //moveInput = Input.GetAxisRaw("Horizontal");
        moveInput = pc.Land.Movement.ReadValue<float>();
    }

    #region Attacking
    void UpdateAttackCountdowns()
    {
        if (!rangedAttackAvailable)
            rangedAttackCooldown -= Time.deltaTime;

        if (rangedAttackCooldown <= 0f)
            rangedAttackAvailable = true;

        if (!continuousAttackAvailable)
            continuousAttackCooldown -= Time.deltaTime;

        if (continuousAttackCooldown <= 0f)
            continuousAttackAvailable = true;

        if (!meleeAttackAvailable)
            meleeAttackCooldown -= Time.deltaTime;

        if (meleeAttackCooldown <= 0f)
            meleeAttackAvailable = true;
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

        if(CanUseMeleeAttack && meleeAttackAvailable && pc.Land.MeleeAttack.ReadValue<float>() > 0.1f) {
            // melee attack
            anim.SetTrigger("Headbutt");
            meleeAttackAvailable = false;
            meleeAttackCooldown = MeleeAttackCooldown;
        }
    }

    public void TriggerRangedAttack()
    {
        // fire ranged attack fireball
        GameObject projectile = (GameObject)Instantiate(RangedAttackPrefabs[(int)PlayerState], AttackSpawnPoint.position, AttackSpawnPoint.rotation);
        //projectile.transform.Rotate(0f, 0f, 90f);
        rangedAttackAvailable = false;
        rangedAttackCooldown = RangedAttackCooldown;
    }

    void RangedAttack()
    {
        if (!CanUseRangedAttack || !rangedAttackAvailable)
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
        if (!CanUseContinuousAttack || continuousAttackAvailable)
            return;

        anim.SetTrigger("ContinuousAttackWarmup");
        continuousAttacking = true;
        // start continuous attack (particle effect)
        currentContinuousPS = (GameObject)Instantiate(ContinuousAttackPrefabs[(int)PlayerState], AttackSpawnPoint.position, AttackSpawnPoint.rotation);
        currentContinuousPS.transform.Rotate(0f, 90f, 0f);
        currentContinuousPS.GetComponent<ParticleSystem>().Play();
        currentContinuousPS.transform.parent = this.transform;
        continuousAttackAvailable = false;
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

    void CheckForm()
    {
        if (pc.Land.ChangeForm_Yellow.ReadValue<float>() == 1f && PlayerState != PLAYER_STATE.YELLOW)
            PlayerState = PLAYER_STATE.YELLOW;
        if (pc.Land.ChangeForm_Blue.ReadValue<float>() == 1f && PlayerState != PLAYER_STATE.BLUE)
            PlayerState = PLAYER_STATE.BLUE;
        if (pc.Land.ChangeForm_Red.ReadValue<float>() == 1f && PlayerState != PLAYER_STATE.RED)
            PlayerState = PLAYER_STATE.RED;
        if (pc.Land.ChangeForm_Green.ReadValue<float>() == 1f && PlayerState != PLAYER_STATE.GREEN)
            PlayerState = PLAYER_STATE.GREEN;
    }

    #region Jumping
    void DoJumping()
    {
        // if we are on the ground, we can jump
        isGrounded = Physics2D.OverlapCircle(GroundCheck.position, CheckRadius, WhatIsGround);
        isTouchingFront = Physics2D.OverlapCircle(FrontCheck.position, CheckRadius, WhatIsGround);

        if (isTouchingFront)
        {
            wallRemember = WallRememberTime;
            wallJumpDir = isFacingRight ? Vector2.left : Vector2.right;
        }

        #region Wall Grab / climb
        if (CanWallClimb && isTouchingFront && pc.Land.WallGrab.ReadValue<float>() > 0)
        {
            isWallClimb = true;
            // stay static if we press nothing
            rb.gravityScale = 0f;
            rb.velocity = new Vector2(rb.velocity.x, 0f);

            // move up / down if we are holding the keys
            float climbInput = pc.Land.WallClimb.ReadValue<float>();
            rb.velocity = new Vector2(rb.velocity.x, climbInput * WallClimbSpeed);
        }
        else
        {
            isWallClimb = false;
            rb.gravityScale = startGravityScale;
        }

        if(CanWallRun && isTouchingFront && pc.Land.Sprint.ReadValue<float>() > 0)
        {
            if (!CanWallClimb && moveInput == 0)
                return;
            isWallRun = true;
            rb.gravityScale = 0f;
            float wallRunInput = isFacingRight ? moveInput : -moveInput;
            rb.velocity = new Vector2(rb.velocity.x, wallRunInput * WallRunSpeed);
        }
        else
        {
            isWallRun = false;
            rb.gravityScale = startGravityScale;
        }
        #endregion

        #region Wall Jump
        // if we arent on the ground but are on the wall then we walljump
        if (!isGrounded && wallRemember > 0)
        {
            if (pc.Land.Jump.triggered)
            {
                Debug.Log("Jump");
                StartCoroutine(DisableMovement(AfterJumpMovementTime));
                rb.velocity = (wallJumpDir * WallJumpSidewaysForce) + (Vector2.up * WallJumpUpForce);
                if(isTouchingFront)
                    Flip();
            }
        }
        #endregion

        groundedRemember -= Time.deltaTime;
        wallRemember -= Time.deltaTime;

        if (isGrounded)
        {
            // setting this allows us to be considered 'grounded' for a time after not being grounded. Coyote time!
            groundedRemember = GroundedRememberTime;

            anim.SetBool("Jump", false);
            anim.SetBool("Fall", false);
        }

        if (groundedRemember > 0 && canJump && pc.Land.Jump.ReadValue<float>() == 1f)
        {
            groundedRemember = 0;
            //rb.velocity = Vector2.up * JumpForce;
            rb.velocity = new Vector2(rb.velocity.x, JumpForce);
            anim.SetBool("Jump", true);
            canJump = false;
        }

        if (pc.Land.Jump.ReadValue<float>() == 0)
        {
            if(!canJump)
                canJump = true;
        }

        if (!isGrounded && rb.velocity.y < 0)
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
    }
    #endregion

    #region Movement
    void DoMovement()
    {
        if (!canMove)
            return;

        #region Movement
        //check if sprinting and change current speed
        if(CanSprint && pc.Land.Sprint.ReadValue<float>() > 0)
        {
            currentSpeed = SprintSpeed;
        }
        else
        {
            currentSpeed = Speed;
        }

        rb.velocity = new Vector2(moveInput * currentSpeed, rb.velocity.y);        
        anim.SetFloat("Speed", Mathf.Abs(moveInput));
        #endregion

        #region Flipping
        // If the input is moving the player right and the player is facing left...
        if (rb.velocity.x > 0 && !isFacingRight)
        {
            // ... flip the player.
            Flip();
        }
        // Otherwise if the input is moving the player left and the player is facing right...
        else if (rb.velocity.x < 0 && isFacingRight)
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
        if (isWallClimb)
            return;

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

    IEnumerator DisableMovement(float time)
    {
        canMove = false;
        yield return new WaitForSeconds(time);
        canMove = true;
    }

}