using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;
using UnityEngine.VFX;

public class SceneInitializationAuthoring : MonoBehaviour
{
    public GameObject CharacterSpawnPointEntity;
    public GameObject CharacterPrefabEntity;
    public GameObject CameraPrefabEntity;
    public GameObject PlayerPrefabEntity;
    public List<GameObject> WeaponPrefabsEntity;

    public float DespawnTime;
    public class Baker : Baker<SceneInitializationAuthoring>
    {
        public override void Bake(SceneInitializationAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.None);
            AddComponent(entity, new SceneInitialization
            {
                CharacterSpawnPointEntity = GetEntity(authoring.CharacterSpawnPointEntity, TransformUsageFlags.Dynamic),
                CharacterPrefabEntity = GetEntity(authoring.CharacterPrefabEntity, TransformUsageFlags.Dynamic),
                CameraPrefabEntity = GetEntity(authoring.CameraPrefabEntity, TransformUsageFlags.Dynamic),
                PlayerPrefabEntity = GetEntity(authoring.PlayerPrefabEntity, TransformUsageFlags.None),
                DespawnTime = authoring.DespawnTime
            });
            
            DynamicBuffer<GameResourcesWeapon> weaponsBuffer = AddBuffer<GameResourcesWeapon>(entity);
            for (var i = 0; i < authoring.WeaponPrefabsEntity.Count; i++)
            {
                weaponsBuffer.Add(new GameResourcesWeapon
                {
                    WeaponPrefab = GetEntity(authoring.WeaponPrefabsEntity[i], TransformUsageFlags.Dynamic),
                });
            }
        }
    }
}

public struct GameResourcesWeapon : IBufferElementData
{
    public Entity WeaponPrefab;
}