using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using Unity.CharacterController;
using Unity.Physics.Systems;
using Unity.Template.CompetitiveActionMultiplayer;

[UpdateInGroup(typeof(InitializationSystemGroup))]
public partial class BasicPlayerInputsSystem : SystemBase
{
    private BasicInputActions InputActions;

    protected override void OnCreate()
    {
        base.OnCreate();
        
        RequireForUpdate<FixedTickSystem.Singleton>();
        RequireForUpdate(SystemAPI.QueryBuilder().WithAll<BasicPlayer, BasicPlayerInputs>().Build());

        InputActions = new BasicInputActions();
        InputActions.Enable();
        InputActions.DefaultMap.Enable();
    }

    protected override void OnUpdate()
    {
        BasicInputActions.DefaultMapActions defaultMapActions = InputActions.DefaultMap;
        uint tick = SystemAPI.GetSingleton<FixedTickSystem.Singleton>().Tick;
        
        foreach (var (playerInputs, player) in SystemAPI.Query<RefRW<BasicPlayerInputs>, BasicPlayer>())
        {
            playerInputs.ValueRW.MoveInput = Vector2.ClampMagnitude(defaultMapActions.Move.ReadValue<Vector2>(), 1f);
            playerInputs.ValueRW.CameraLookInput = default;
            if(math.lengthsq(defaultMapActions.LookConst.ReadValue<Vector2>()) > math.lengthsq(defaultMapActions.LookDelta.ReadValue<Vector2>()))
            {
                playerInputs.ValueRW.CameraLookInput = defaultMapActions.LookConst.ReadValue<Vector2>() * SystemAPI.Time.DeltaTime;
            }
            else
            {
                playerInputs.ValueRW.CameraLookInput = defaultMapActions.LookDelta.ReadValue<Vector2>();
            }
            playerInputs.ValueRW.CameraZoomInput = defaultMapActions.Scroll.ReadValue<float>();
            
            if (defaultMapActions.Jump.WasPressedThisFrame())
            {
                playerInputs.ValueRW.JumpPressed.Set(tick);
            }
            
            if (defaultMapActions.Fire.WasPressedThisFrame())
            {
                playerInputs.ValueRW.ShootPressed.Set(tick);
            }
            
            if (defaultMapActions.Fire.WasReleasedThisFrame())
            {
                playerInputs.ValueRW.ShootReleased.Set(tick);
            }

            playerInputs.ValueRW.AimHeld = defaultMapActions.Aim.IsPressed();
        }
    }
}

[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(FixedStepSimulationSystemGroup))]
[BurstCompile]
public partial struct BasicPlayerVariableStepControlSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate(SystemAPI.QueryBuilder().WithAll<BasicPlayer, BasicPlayerInputs>().Build());
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        uint tick = SystemAPI.GetSingleton<FixedTickSystem.Singleton>().Tick;

        foreach (var (playerInputs, player) in SystemAPI.Query<BasicPlayerInputs, BasicPlayer>().WithAll<Simulate>())
        {
            if (SystemAPI.HasComponent<OrbitCameraControl>(player.ControlledCamera))
            {
                OrbitCameraControl cameraControl = SystemAPI.GetComponent<OrbitCameraControl>(player.ControlledCamera);
                
                cameraControl.FollowedCharacterEntity = player.ControlledCharacter;
                cameraControl.LookDegreesDelta = playerInputs.CameraLookInput;
                cameraControl.ZoomDelta = playerInputs.CameraZoomInput;

                SystemAPI.SetComponent(player.ControlledCamera, cameraControl);
            }
            
            // Weapon
            if (SystemAPI.HasComponent<ActiveWeapon>(player.ControlledCharacter))
            {
                ActiveWeapon activeWeapon = SystemAPI.GetComponent<ActiveWeapon>(player.ControlledCharacter);
                if (SystemAPI.HasComponent<WeaponControl>(activeWeapon.Entity))
                {
                    RefRW<WeaponControl>  weaponControl = SystemAPI.GetComponentRW<WeaponControl>(activeWeapon.Entity);
                    // Shoot
                    weaponControl.ValueRW.ShootPressed = playerInputs.ShootPressed.IsSet(tick);
                    weaponControl.ValueRW.ShootReleased = playerInputs.ShootReleased.IsSet(tick);
                    Debug.Log($"ShootPressed: {weaponControl.ValueRW.ShootPressed}");
                    Debug.Log($"ShootReleased: {weaponControl.ValueRW.ShootReleased}");

                    // Aim
                    weaponControl.ValueRW.AimHeld = playerInputs.AimHeld;
                    Debug.Log($"AimHeld: {weaponControl.ValueRW.AimHeld}");

                    //SystemAPI.SetComponent(activeWeapon.Entity, weaponControl);
                }
            }

        }
    }
}

[UpdateInGroup(typeof(FixedStepSimulationSystemGroup), OrderFirst = true)]
[BurstCompile]
public partial struct BasicFixedStepPlayerControlSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<FixedTickSystem.Singleton>();
        state.RequireForUpdate(SystemAPI.QueryBuilder().WithAll<BasicPlayer, BasicPlayerInputs>().Build());
    }

    public void OnUpdate(ref SystemState state)
    {
        uint tick = SystemAPI.GetSingleton<FixedTickSystem.Singleton>().Tick;
        
        foreach (var (playerInputs, player) in SystemAPI.Query<RefRW<BasicPlayerInputs>, BasicPlayer>().WithAll<Simulate>())
        {
            if (SystemAPI.HasComponent<BasicCharacterControl>(player.ControlledCharacter))
            {
                BasicCharacterControl characterControl = SystemAPI.GetComponent<BasicCharacterControl>(player.ControlledCharacter);

                float3 characterUp = MathUtilities.GetUpFromRotation(SystemAPI.GetComponent<LocalTransform>(player.ControlledCharacter).Rotation);
                
                // Get camera rotation, since our movement is relative to it.
                quaternion cameraRotation = quaternion.identity;
                if (SystemAPI.HasComponent<OrbitCamera>(player.ControlledCamera))
                {
                    // Camera rotation is calculated rather than gotten from transform, because this allows us to 
                    // reduce the size of the camera ghost state in a netcode prediction context.
                    // If not using netcode prediction, we could simply get rotation from transform here instead.
                    OrbitCamera orbitCamera = SystemAPI.GetComponent<OrbitCamera>(player.ControlledCamera);
                    cameraRotation = OrbitCameraUtilities.CalculateCameraRotation(characterUp, orbitCamera.PlanarForward, orbitCamera.PitchAngle);
                }
                float3 cameraForwardOnUpPlane = math.normalizesafe(MathUtilities.ProjectOnPlane(MathUtilities.GetForwardFromRotation(cameraRotation), characterUp));
                float3 cameraRight = MathUtilities.GetRightFromRotation(cameraRotation);

                // Move
                characterControl.MoveVector = (playerInputs.ValueRW.MoveInput.y * cameraForwardOnUpPlane) + (playerInputs.ValueRW.MoveInput.x * cameraRight);
                characterControl.MoveVector = MathUtilities.ClampToMaxLength(characterControl.MoveVector, 1f);

                //RotationVector
                characterControl.RotationVector = MathUtilities.ClampToMaxLength(cameraForwardOnUpPlane, 1f);
                
                // Jump
                // We detect a jump event if the jump counter has changed since the last fixed update.
                // This is part of a strategy for proper handling of button press events that are consumed during the fixed update group
                characterControl.Jump = playerInputs.ValueRW.JumpPressed.IsSet(tick);

                SystemAPI.SetComponent(player.ControlledCharacter, characterControl);
            }
        }
    }
}