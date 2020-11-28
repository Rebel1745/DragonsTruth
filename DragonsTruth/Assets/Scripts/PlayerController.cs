using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    #region Startup
    private void Awake()
    {
        rb = GetComponentInChildren<Rigidbody2D>();
        sr = GetComponentInChildren<SpriteRenderer>();

        if (rb == null)
            Debug.LogError("No RigidBody found");
        if (sr == null)
            Debug.LogError("No SpriteRenderer found");

        pc = new PlayerControls();
    }

    private void Start()
    {
        LoadForm(StartingForm);
        currentSpeed = Speed;

        startGravityScale = rb.gravityScale;

        jumpsLeft = NumberOfJumps;
        dashesLeft = NumberOfDashes;
    }

    private void OnEnable()
    {
        pc.Enable();
    }

    private void OnDisable()
    {
        pc.Disable();
    }
    #endregion

    #region Variables
    [HideInInspector]
    public Rigidbody2D rb;
    SpriteRenderer sr;
    PlayerControls pc;

    [Header("Player Forms")]
    public DragonForm[] DragonForms;
    public int StartingForm = 0;
    int currentForm;

    [Space]
    [Header("Abilities")]
    public bool CanUseRangedAttack = true;
    public bool CanUseContinuousAttack = true;
    public bool CanMeleeAttack = false;
    public bool CanGroundSlam = false;
    public bool CanWallClimb = false;
    public bool CanWallRun = false;
    public bool CanWallJump = false;
    public bool CanDash = false;
    public bool CanSprint = false;
    public bool CanFly = false;

    [Space]
    [Header("Movement")]
    public float Speed = 3f;
    public float SprintSpeed = 4f;
    float currentSpeed;
    float moveInput;
    bool isFacingRight = true;

    public Animator anim;

    [Space]
    [Header("Jumping")]
    public float JumpForce = 5f;
    private bool canJump = true;
    public int NumberOfJumps = 2;
    public int jumpsLeft;
    public float FallMultiplier = 2.5f;
    public float LowJumpMultiplier = 2f;
    bool isGrounded;
    public Transform GroundCheck;
    public float CheckRadius;
    public LayerMask WhatIsGround;
    public float GroundedRememberTime = 0.1f;
    float groundedRemember = 0;
    float startGravityScale;

    [Space]
    [Header("Attacks")]
    public Transform AttackSpawnPoint;
    public GameObject RangedAttackPrefab;
    public GameObject ContinuousAttackPrefab;

    bool rangedAttackAvailable = true;
    public float RangedAttackCooldown = 1f;
    float rangedAttackCooldown = 0f;

    bool continuousAttackAvailable = true;
    public float ContinuousAttackMaxDuration = 2f;
    float continuousAttackDuration = 0f;
    public float ContinuousAttackCooldown = 1f;
    float continuousAttackCooldown = 0f;
    bool continuousAttacking = false;
    private GameObject currentContinuousPS = null;
    bool isAttacking = false;
    public float TimeBeforeAttackStarts = 0.5f;
    float continuousAttackTimer = 0;

    bool meleeAttackAvailable = false;
    public float MeleeAttackCooldown = 1f;
    private float meleeAttackCooldown = 0f;

    [Space]
    [Header("Ground Slam")]
    public float GroundSlamSpeed = 20f;
    public float PreSlamHoverTime = 1f;
    public float SlamMinHeight = 2f;
    bool isGroundSlam = false;

    [Space]
    [Header("Wall Interactions")]
    public Transform FrontCheck;
    bool isTouchingFront = false;
    public bool isWallClimb = false;
    public float WallClimbSpeed = 2f;
    bool isWallRun = false;
    public float WallRunSpeed = 4f;
    public float WallJumpUpForce = 5f;
    public float WallJumpSidewaysForce = 3f;
    private Vector2 wallJumpDir;
    private bool canMove = true;
    public float AfterJumpMovementTime = 0.1f;
    public float WallRememberTime = 0.1f;
    float wallRemember = 0;

    [Space]
    [Header("Dashing")]
    bool isDashing = false;
    public float DashSpeed = 50f;
    public int NumberOfDashes = 1;
    int dashesLeft;
    public float DashDuration = 0.1f;

    [Space]
    [Header("Flying / gliding")]
    public float FlyUpForce = 0.5f; // low number for glide, high to fly
    public float FlyingCooldownTime = 0.1f;
    float flyingCooldown;
    #endregion

    #region Update Funtions
    private void FixedUpdate()
    {
        DoMovement();
        CheckForm();
    }

    private void Update()
    {
        DoJumping();
        DoDashing();
        DoFlying();
        UpdateAttack();
        CheckGroundSlam();
        moveInput = pc.Land.Movement.ReadValue<float>();
    }
    #endregion

    #region Ground Slam
    void CheckGroundSlam()
    {
        if (isGrounded)
            isGroundSlam = false;

        if (!CanGroundSlam && isGroundSlam)
            return;

        RaycastHit2D hit = Physics2D.Raycast(GroundCheck.position, Vector2.down, SlamMinHeight, WhatIsGround);

        if (!hit && pc.Land.Crouch.triggered)
        {
            isGroundSlam = true;
            // stay static 
            rb.bodyType = RigidbodyType2D.Static;
            Invoke("DoGroundSlam", PreSlamHoverTime);
        }
    }

    void DoGroundSlam()
    {
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.velocity = new Vector2(0f, Mathf.Abs(GroundSlamSpeed) * -1);
    }
    #endregion

    #region Flying
    void DoFlying()
    {
        if (flyingCooldown > 0)
            flyingCooldown -= Time.deltaTime;

        // check to see if we are falling
        if(CanFly && jumpsLeft == 0 && !isGrounded && rb.velocity.y < 0 && flyingCooldown <= 0)
        {
            if (pc.Land.Jump.triggered)
            {
                rb.velocity = new Vector2(rb.velocity.x, FlyUpForce);
                flyingCooldown = FlyingCooldownTime;
            }
        }
    }
    #endregion

    #region Dashing
    void DoDashing()
    {
        if(CanDash && dashesLeft > 0 && !isDashing && (pc.Land.DashLeft.triggered || pc.Land.DashRight.triggered))
        {
            isDashing = true;
            Vector2 dir;
            dir = pc.Land.DashRight.triggered ? Vector2.right : Vector2.left;
            rb.velocity = dir * DashSpeed;
            dashesLeft--;

            Invoke("StopDash", DashDuration);
        }
    }

    void StopDash()
    {
        isDashing = false;
        currentSpeed = Speed;
    }
    #endregion

    #region Attacking
    void UpdateAttackCountdowns()
    {
        if (!rangedAttackAvailable)
            rangedAttackCooldown -= Time.deltaTime;

        if (rangedAttackCooldown <= 0f)
            rangedAttackAvailable = true;
        
        if (continuousAttacking)
        {
            if (continuousAttackDuration > 0)
                continuousAttackDuration -= Time.deltaTime;

            if (continuousAttackDuration <= 0)
            {
                StopContinuousAttack();
            }
        }
        else
        {
            if(!continuousAttackAvailable)
            continuousAttackCooldown -= Time.deltaTime;

            if (continuousAttackCooldown <= 0f)
                continuousAttackAvailable = true;
        }        

        if (!meleeAttackAvailable)
            meleeAttackCooldown -= Time.deltaTime;

        if (meleeAttackCooldown <= 0f)
            meleeAttackAvailable = true;
        
    }

    void InitiateAttack()
    {
        // set flag to true to signify we are pressing the attack button
        isAttacking = true;
        continuousAttackTimer = 0;
    }

    void EndAttack()
    {
        // time limit for continuous attack not reached, perform ranged attack
        if (continuousAttackTimer <= TimeBeforeAttackStarts)
        {
            RangedAttack();
        }
        if(continuousAttacking)
            StopContinuousAttack();

        isAttacking = false;
        continuousAttacking = false;
    }

    void UpdateAttack()
    {
        UpdateAttackCountdowns();

        // if we press the attack button, initiate
        if (pc.Land.Attack.triggered)
        {
            InitiateAttack();
        }

        if (isAttacking)
        {
            continuousAttackTimer += Time.deltaTime;

            if (continuousAttackTimer >= TimeBeforeAttackStarts && !continuousAttacking)
            {
                // been holding down the attack button, start continuous attack
                ContinuousAttack();
                return;
            }

            if (isAttacking && pc.Land.Attack.ReadValue<float>() == 0f)
            {
                // stopped pressing therefore stop attacking
                EndAttack();
            }
        }

        if(CanMeleeAttack && meleeAttackAvailable && pc.Land.MeleeAttack.ReadValue<float>() > 0.1f) {
            // melee attack
            anim.SetTrigger("Headbutt");
            meleeAttackAvailable = false;
            meleeAttackCooldown = MeleeAttackCooldown;
        }
    }

    public void TriggerRangedAttack()
    {
        // fire ranged attack fireball
        GameObject projectile = (GameObject)Instantiate(RangedAttackPrefab, AttackSpawnPoint.position, AttackSpawnPoint.rotation);
        rangedAttackAvailable = false;
        rangedAttackCooldown = RangedAttackCooldown;
    }

    void RangedAttack()
    {
        if (!CanUseRangedAttack || !rangedAttackAvailable)
        {
            isAttacking = false;
            return;
        }

        anim.SetTrigger("RangedAttack");
    }

    void ContinuousAttack()
    {
        if (!CanUseContinuousAttack || !continuousAttackAvailable)
        {
            // needs to be called to reset flags
            StopContinuousAttack();
            return;
        }

        continuousAttackAvailable = false;
        anim.SetTrigger("ContinuousAttackWarmup");
        continuousAttacking = true;
        continuousAttackDuration = ContinuousAttackMaxDuration;
        // start continuous attack (particle effect)
        currentContinuousPS = (GameObject)Instantiate(ContinuousAttackPrefab, AttackSpawnPoint.position, AttackSpawnPoint.rotation);
        currentContinuousPS.transform.Rotate(0f, 90f, 0f);
        currentContinuousPS.GetComponent<ParticleSystem>().Play();
        currentContinuousPS.transform.parent = this.transform;
    }

    public void PlayContinuousAttack()
    {
        anim.SetTrigger("ContinuousAttack");
    }

    void StopContinuousAttack()
    {
        if (continuousAttacking)
        {
            continuousAttackCooldown = ContinuousAttackCooldown;
            anim.SetTrigger("StopContinuousAttack");
            if (currentContinuousPS)
            {
                currentContinuousPS.GetComponent<ParticleSystem>().Stop();
                Destroy(currentContinuousPS);
            }
            continuousAttackDuration = 0f;
        }        
        continuousAttacking = false;
        isAttacking = false;
    }
    #endregion

    #region Dragon Form
    void CheckForm()
    {
        if (pc.Land.ChangeForm_Yellow.ReadValue<float>() == 1f && currentForm != 0)
            LoadForm(0);
        if (pc.Land.ChangeForm_Blue.ReadValue<float>() == 1f && currentForm != 1)
            LoadForm(1);
        if (pc.Land.ChangeForm_Red.ReadValue<float>() == 1f && currentForm != 2)
            LoadForm(2);
        if (pc.Land.ChangeForm_Green.ReadValue<float>() == 1f && currentForm != 3)
            LoadForm(3);
    }

    void LoadForm(int form)
    {
        currentForm = form;
        DragonForm df = DragonForms[form];

        Speed = df.Speed;
        CanSprint = df.CanSprint;
        SprintSpeed = df.SprintSpeed;

        JumpForce = df.JumpForce;
        NumberOfJumps = df.NumberOfJumps;
        FallMultiplier = df.FallMultiplier;
        LowJumpMultiplier = df.LowJumpMultiplier;

        CanUseRangedAttack = df.CanUseRangedAttack;
        RangedAttackCooldown = df.RangedAttackCooldown;
        RangedAttackPrefab = df.RangedAttackPrefab;
        CanUseContinuousAttack = df.CanUseContinuousAttack;
        ContinuousAttackMaxDuration = df.ContinuousAttackMaxDuration;
        ContinuousAttackCooldown = df.ContinuousAttackCooldown;
        ContinuousAttackPrefab = df.ContinuousAttackPrefab;
        CanMeleeAttack = df.CanMeleeAttack;
        MeleeAttackCooldown = df.MeleeAttackCooldown;

        CanGroundSlam = df.CanGroundSlam;
        GroundSlamSpeed = df.GroundSlamSpeed;
        PreSlamHoverTime = df.PreSlamHoverTime;
        SlamMinHeight = df.SlamMinHeight;

        CanWallClimb = df.CanWallClimb;
        WallClimbSpeed = df.WallClimbSpeed;

        CanWallRun = df.CanWallRun;
        WallRunSpeed = df.WallRunSpeed;

        CanWallJump = df.CanWallJump;
        WallJumpUpForce = df.WallJumpUpForce;
        WallJumpSidewaysForce = df.WallJumpSidewaysForce;
        AfterJumpMovementTime = df.AfterJumpMovementTime;

        CanDash = df.CanDash;
        DashSpeed = df.DashSpeed;
        NumberOfDashes = df.NumberOfDashes;
        DashDuration = df.DashDuration;

        CanFly = df.CanFly;
        FlyUpForce = df.FlyUpForce;
        FlyingCooldownTime = df.FlyingCooldownTime;
    }

    #endregion

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

        if(isGrounded || isTouchingFront)
        {
            jumpsLeft = NumberOfJumps;
            dashesLeft = NumberOfDashes;
        }

        if (((groundedRemember > 0 && canJump) || jumpsLeft > 0) && pc.Land.Jump.triggered)
        {
            groundedRemember = 0;
            rb.velocity = new Vector2(rb.velocity.x, JumpForce);
            anim.SetBool("Jump", true);
            canJump = false;
            jumpsLeft--;
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
        if (!canMove || isDashing || isGroundSlam)
            return;
        
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

    IEnumerator DisableMovement(float time)
    {
        canMove = false;
        yield return new WaitForSeconds(time);
        canMove = true;
    }
    #endregion

    #region Flip Function
    private void Flip()
    {
        if (isWallClimb)
            return;

        // Switch the way the player is labelled as facing.
        isFacingRight = !isFacingRight;

        transform.Rotate(0f, 180f, 0f);
    }
    #endregion

}