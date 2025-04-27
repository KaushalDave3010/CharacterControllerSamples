using System;
using Unity.Burst;
using Unity.Entities;
using Unity.Template.CompetitiveActionMultiplayer;
using Unity.Transforms;
using UnityEngine;
using Random = Unity.Mathematics.Random;

[UpdateInGroup(typeof(InitializationSystemGroup))]
[BurstCompile]
public partial struct SceneInitializationSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<SceneInitialization>();
        state.RequireForUpdate<GameResources>();

        var randomSeed = (uint)DateTime.Now.Millisecond;
        Entity randomEntity = state.EntityManager.CreateEntity();
        state.EntityManager.AddComponentData(randomEntity, new FixedRandom
        {
            Random = Random.CreateFromIndex(randomSeed),
        });
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    { }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        // Game init
        if (SystemAPI.HasSingleton<SceneInitialization>())
        {
            if (!SystemAPI.TryGetSingleton(out GameResources gameResources))
                return;

            var weaponPrefabs = SystemAPI.GetSingletonBuffer<GameResourcesWeapon>();

            // Cursor
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            // Spawn player
            Entity playerEntity = state.EntityManager.Instantiate(gameResources.PlayerPrefabEntity);

            // Spawn character at spawn point
            Entity characterEntity = state.EntityManager.Instantiate(gameResources.CharacterPrefabEntity);
            SystemAPI.SetComponent(characterEntity, SystemAPI.GetComponent<LocalTransform>(gameResources.CharacterSpawnPointEntity));

            // Spawn camera
            Entity cameraEntity = state.EntityManager.Instantiate(gameResources.CameraPrefabEntity);

            // Assign camera & character to player
            BasicPlayer player = SystemAPI.GetComponent<BasicPlayer>(playerEntity);
            player.ControlledCharacter = characterEntity;
            player.ControlledCamera = cameraEntity;

            RefRW<BasicCharacterComponent> basicCharacterComponent = SystemAPI.GetComponentRW<BasicCharacterComponent>(characterEntity);
            basicCharacterComponent.ValueRW.ViewEntity = cameraEntity;

            state.EntityManager.SetName(playerEntity, "Player");
            SystemAPI.SetComponent(playerEntity, player);

            ref FixedRandom random = ref SystemAPI.GetSingletonRW<FixedRandom>().ValueRW;

            Entity randomWeaponPrefab;
            if (gameResources.ForceOnlyFirstWeapon)
            {
                randomWeaponPrefab = weaponPrefabs[0].WeaponPrefab;
            }
            else
            {
                randomWeaponPrefab = weaponPrefabs[random.Random.NextInt(0, weaponPrefabs.Length)].WeaponPrefab;
            }

            Entity weaponEntity = state.EntityManager.Instantiate(randomWeaponPrefab);
            
            state.EntityManager.SetComponentData(characterEntity, new ActiveWeapon { Entity = weaponEntity });
            state.EntityManager.SetComponentData(characterEntity, new OwningPlayer { Entity = playerEntity });

            state.EntityManager.SetName(characterEntity, "Character");
            state.EntityManager.SetName(cameraEntity, "Camera");
            state.EntityManager.SetName(weaponEntity, "Weapon");
            
            state.EntityManager.DestroyEntity(SystemAPI.GetSingletonEntity<SceneInitialization>());
        }
    }
}

public struct FixedRandom : IComponentData
{
    public Random Random;
}