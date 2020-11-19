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
        lr = GetComponent<LineRenderer>();

        if (rb == null)
            Debug.LogError("No RigidBody found");
        if (sr == null)
            Debug.LogError("No SpriteRenderer found");
        if (lr == null)
            Debug.LogError("No LineRenderer found");

        pc = new PlayerControls();

        // when either attack is performed, fire off a function
        pc.Land.RangedAttack.performed += x => RangedAttack();
        pc.Land.ContinuousAttack.performed += x => ContinuousAttack();
    }

    private void Start()
    {
        currentSpeed = Speed;

        startGravityScale = rb.gravityScale;

        jumpsLeft = NumberOfJumps;
        dashesLeft = NumberOfDashes;

        lr.enabled = false;
        lr.positionCount = 0;

        if (!cam)
            cam = Camera.main;
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

    [Header("Abilities")]
    public bool CanUseRangedAttack = true;
    public bool CanUseContinuousAttack = true;
    public bool CanWallClimb = false;
    public bool CanWallRun = false;
    public bool CanWallJump = false;
    public bool CanDash = false;
    public bool CanSprint = false;
    public bool CanUseMeleeAttack = false;
    public bool CanFly = false;
    public bool CanGroundSlam = false;
    public bool CanGrapple = true;

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
    public GameObject[] RangedAttackPrefabs;
    public GameObject[] ContinuousAttackPrefabs;

    public enum PLAYER_STATE {YELLOW, BLUE, RED, GREEN, ORANGE};
    PLAYER_STATE PlayerState = PLAYER_STATE.YELLOW;

    bool rangedAttackAvailable = true;
    public float RangedAttackCooldown = 1f;
    private float rangedAttackCooldown = 0f;

    bool continuousAttackAvailable = true;
    public float ContinuousAttackMaxDuration = 2f;
    private float continuousAttackDuration = 0f;
    public float ContinuousAttackCooldown = 1f;
    private float continuousAttackCooldown = 0f;
    private float flt_attackTimer = 0f;
    private bool continuousAttacking = false;
    private GameObject currentContinuousPS = null;

    bool meleeAttackAvailable = false;
    public float MeleeAttackCooldown = 1f;
    private float meleeAttackCooldown = 0f;

    [Space]
    [Header("Ground Slam")]
    public float GroundSlamSpeed = 20f;
    public float PreSlamHoverTime = 1f;
    bool isGroundSlam = false;
    public float RayCheckLength = 2f;

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
    public float flyingCooldown;

    [Space]
    [Header("Grappling")]
    public Camera cam;
    public Transform GrapplePoint;
    bool isGrappling = false;
    public float MaxGrappleLength = 10f;
    public float MinGrappleLength = 1f;
    public float DefaultGrappleOffset = 0.2f;
    public bool UsingController = false;
    public LineRenderer lr;
    private DistanceJoint2D dj;
    Vector3 currentGrapplePoint;
    #endregion

    #region Update Funtions
    private void FixedUpdate()
    {
        DoMovement();
        CheckForm();
        UpdateAttack();
    }

    private void Update()
    {
        DoJumping();
        DoDashing();
        DoFlying();
        CheckGroundSlam();
        CheckGrapple();
        moveInput = pc.Land.Movement.ReadValue<float>();
    }
    #endregion

    #region Grappling
    void CheckGrapple()
    {
        if (!CanGrapple)
            return;

        if (pc.Land.Grapple.triggered)
        {
            Vector3 mousePos, grappleDir;

            if (UsingController)
            {
                Vector2 stickPos = pc.Land.GrappleDirection.ReadValue<Vector2>();
                grappleDir = new Vector3(stickPos.x, stickPos.y, 0f);
            }
            else
            {
                mousePos = cam.ScreenToWorldPoint(pc.Land.GrappleDirection.ReadValue<Vector2>());
                grappleDir = mousePos - GrapplePoint.position;
            }

            if (grappleDir.y > 0 && !isGrappling)
            {
                RaycastHit2D hit = Physics2D.Raycast(GrapplePoint.position, grappleDir, MaxGrappleLength, WhatIsGround);

                if (hit)
                {
                    isGrappling = true;
                    currentGrapplePoint = hit.point;
                    // create a distance joint and anchor it to hit point
                    dj = gameObject.AddComponent<DistanceJoint2D>();
                    dj.enableCollision = true;
                    dj.connectedAnchor = hit.point;
                    dj.distance = Mathf.Max(MinGrappleLength, hit.point.y - GrapplePoint.position.y - DefaultGrappleOffset);

                    // setup line renderer 'rope'
                    lr.enabled = true;
                    lr.positionCount = 2;
                    lr.SetPosition(0, GrapplePoint.position);
                    lr.SetPosition(1, hit.point);
                }
            }
        }

        // update the line renderer
        if (isGrappling)
        {
            lr.SetPosition(0, GrapplePoint.position);
            lr.SetPosition(1, currentGrapplePoint);
        }

        if(pc.Land.Grapple.ReadValue<float>() == 0)
        {
            StopGrapple();
        }
    }

    void StopGrapple()
    {
        DestroyImmediate(dj);
        isGrappling = false;
        lr.positionCount = 0;
        lr.enabled = false;
    }
    #endregion

    #region Ground Slam
    void CheckGroundSlam()
    {
        if (isGrounded)
            isGroundSlam = false;

        if (!CanGroundSlam && isGroundSlam)
            return;

        RaycastHit2D hit = Physics2D.Raycast(GroundCheck.position, Vector2.down, RayCheckLength, WhatIsGround);

        if (!hit && pc.Land.Crouch.triggered)
        {
            Debug.Log("Slam");
            isGroundSlam = true;
            // stay static 
            rb.bodyType = RigidbodyType2D.Static;
            Invoke("DoGroundSlam", PreSlamHoverTime);
        }
    }

    void DoGroundSlam()
    {
        Debug.Log("Slamming");
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

        if (!continuousAttackAvailable)
            continuousAttackCooldown -= Time.deltaTime;

        if (continuousAttackCooldown <= 0f)
            continuousAttackAvailable = true;

        if (!meleeAttackAvailable)
            meleeAttackCooldown -= Time.deltaTime;

        if (meleeAttackCooldown <= 0f)
            meleeAttackAvailable = true;

        if (continuousAttacking)
        {
            if (continuousAttackDuration > 0)
                continuousAttackDuration -= Time.deltaTime;

            if (continuousAttackDuration <= 0)
            {
                StopContinuousAttack();
                return;
            }
        }
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
        Debug.Log("Continous");
        if (!CanUseContinuousAttack || !continuousAttackAvailable)
            return;
        
        anim.SetTrigger("ContinuousAttackWarmup");
        continuousAttacking = true;
        continuousAttackDuration = ContinuousAttackMaxDuration;
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
        continuousAttackDuration = 0f;
    }

    void UpdateAttackTimer()
    {
        if (flt_attackTimer >= 0)
        {
            flt_attackTimer -= Time.deltaTime;
        }
    }
    #endregion

    #region Dragon Form
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

        // Multiply the player's x local scale by -1.
        /*Vector3 theScale = transform.localScale;
        theScale.x *= -1;
        transform.localScale = theScale;*/

        transform.Rotate(0f, 180f, 0f);

        //sr.flipX = !sr.flipX;
    }
    #endregion

    private void OnDrawGizmos()
    {
        Gizmos.DrawWireSphere(GrapplePoint.position, MaxGrappleLength);
        Gizmos.DrawSphere(currentGrapplePoint, 0.5f);
    }

}