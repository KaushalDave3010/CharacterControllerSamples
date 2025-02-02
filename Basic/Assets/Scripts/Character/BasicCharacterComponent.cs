using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Unity.CharacterController;
using Unity.Physics.Authoring;

[Serializable]
public struct BasicCharacterComponent : IComponentData
{
    [Header("Movement")]
    public float RotationSharpness;
    public float GroundMaxSpeed;
    public float GroundedMovementSharpness;
    public float AirAcceleration;
    public float AirMaxSpeed;
    public float AirDrag;
    public float JumpSpeed;
    public float3 Gravity;
    public bool PreventAirAccelerationAgainstUngroundedHits;
    public int MaxJumpsInAir;
    public BasicStepAndSlopeHandlingParameters StepAndSlopeHandling;
    public bool AlwaysFaceCameraDirection;
    
    [Header("Tags")]
    public CustomPhysicsBodyTags IgnoreCollisionsTag;
    public CustomPhysicsBodyTags IgnoreGroundingTag;
    public CustomPhysicsBodyTags ZeroMassAgainstCharacterTag;
    public CustomPhysicsBodyTags InfiniteMassAgainstCharacterTag;
    public CustomPhysicsBodyTags IgnoreStepHandlingTag;
    
    public Entity ViewEntity;
    public Entity WeaponSocket;
    public Entity WeaponAnimationSocketEntity;
    
    [NonSerialized]
    public int CurrentJumpsInAir;

    public static BasicCharacterComponent GetDefault()
    {
        return new BasicCharacterComponent
        {
            RotationSharpness = 25f,
            GroundMaxSpeed = 10f,
            GroundedMovementSharpness = 15f,
            AirAcceleration = 50f,
            AirMaxSpeed = 10f,
            AirDrag = 0f,
            JumpSpeed = 10f,
            Gravity = math.up() * -30f,
            PreventAirAccelerationAgainstUngroundedHits = true,
            MaxJumpsInAir = 0,
            AlwaysFaceCameraDirection = false,
                
            StepAndSlopeHandling = BasicStepAndSlopeHandlingParameters.GetDefault(),
        };
    }
}

[Serializable]
public struct BasicCharacterControl : IComponentData
{
    public float3 MoveVector;
    public float3 RotationVector;
    public bool Jump;
}

public struct CharacterInitialized : IComponentData, IEnableableComponent
{
}

public struct FirstPersonCharacterView : IComponentData
{
    public Entity CharacterEntity;
}

public struct OwningPlayer : IComponentData
{
    public Entity Entity;
}