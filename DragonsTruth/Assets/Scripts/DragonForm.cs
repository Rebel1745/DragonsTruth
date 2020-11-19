using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Dragon Form", menuName = "Dragon Form")]
public class DragonForm : ScriptableObject
{
    public string DragonFormName;

    [Space]
    [Header("Movement")]
    public float Speed;
    public bool CanSprint;
    public float SprintSpeed;

    [Space]
    [Header("Jumping")]
    public float JumpForce;
    public int NumberOfJumps;
    public float FallMultiplier;
    public float LowJumpMultiplier;

    [Space]
    [Header("Attacks")]
    public bool CanUseRangedAttack;
    public float RangedAttackCooldown;
    public GameObject RangedAttackPrefab;
    public bool CanUseContinuousAttack;
    public float ContinuousAttackMaxDuration;
    public float ContinuousAttackCooldown;
    public GameObject ContinuousAttackPrefab;
    public bool CanMeleeAttack;
    public float MeleeAttackCooldown;
    public bool CanGroundSlam;
    public float GroundSlamSpeed;
    public float PreSlamHoverTime;
    public float SlamMinHeight;

    [Space]
    [Header("Wall Interactions")]
    public bool CanWallClimb;
    public float WallClimbSpeed;
    public bool CanWallRun;
    public float WallRunSpeed;
    public bool CanWallJump;
    public float WallJumpUpForce;
    public float WallJumpSidewaysForce;
    public float AfterJumpMovementTime;

    [Space]
    [Header("Dashing")]
    public bool CanDash;
    public float DashSpeed;
    public int NumberOfDashes;
    public float DashDuration;

    [Space]
    [Header("Flying / gliding")]
    public bool CanFly;
    public float FlyUpForce;
    public float FlyingCooldownTime;

}
