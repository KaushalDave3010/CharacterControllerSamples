using System.Collections.Generic;
using Unity.Entities;
using Unity.Physics;
using UnityEngine;
using UnityEngine.VFX;

namespace Unity.Template.CompetitiveActionMultiplayer
{
    /// <summary>
    /// This class is used in the GameResources subscene.
    /// It contains setup information used by most of the Gameplay entity systems.
    /// </summary>
    public class GameResourcesAuthoring : MonoBehaviour
    {
        [Header("Network Parameters")]
        public uint DespawnTicks = 30;

        [Header("General Parameters")]
        public float RespawnTimeSeconds = 4f;

        [Header("Ghost Prefabs")]
        public GameObject PlayerPrefab;
        public GameObject CharacterPrefab;
        public GameObject CameraPrefab;
        public bool ForceOnlyFirstWeapon;
        public List<GameObject> WeaponGhosts;

        [Tooltip("Prevent player spawning if another player is within this radius!")]
        public GameObject playerSpawnPoint;

        [Header("VFX Graphs")]
        public VisualEffect MachineGunBulletHitVfx;
        public VisualEffect ShotgunBulletHitVfx;
        public VisualEffect LaserHitVfx;
        public VisualEffect PlasmaHitVfx;
        public VisualEffect RocketHitVfx;
        public VisualEffect DeathSparksVfx;

        public class Baker : Baker<GameResourcesAuthoring>
        {
            public override void Bake(GameResourcesAuthoring authoring)
            {
                Entity entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new GameResources
                {
                    DespawnTicks = authoring.DespawnTicks,
                    RespawnTime = authoring.RespawnTimeSeconds,
                    SpawnPointCollisionFilter = GameLayers.CollideWithPlayers,

                    PlayerPrefabEntity = GetEntity(authoring.PlayerPrefab, TransformUsageFlags.Dynamic),
                    CharacterPrefabEntity = GetEntity(authoring.CharacterPrefab, TransformUsageFlags.Dynamic),
                    CameraPrefabEntity = GetEntity(authoring.CameraPrefab, TransformUsageFlags.Dynamic),
                    CharacterSpawnPointEntity = GetEntity(authoring.playerSpawnPoint, TransformUsageFlags.Dynamic),

                    ForceOnlyFirstWeapon = authoring.ForceOnlyFirstWeapon,
                });

                DynamicBuffer<GameResourcesWeapon> weaponsBuffer = AddBuffer<GameResourcesWeapon>(entity);
                for (var i = 0; i < authoring.WeaponGhosts.Count; i++)
                {
                    weaponsBuffer.Add(new GameResourcesWeapon
                    {
                        WeaponPrefab = GetEntity(authoring.WeaponGhosts[i], TransformUsageFlags.Dynamic),
                    });
                }

                //The order in this array has to follow the VfxType enum order so the correct vfx is spawned
                var vfxArray = new GameObject[]
                {
                    authoring.MachineGunBulletHitVfx.gameObject,
                    authoring.ShotgunBulletHitVfx.gameObject,
                    authoring.LaserHitVfx.gameObject,
                    authoring.PlasmaHitVfx.gameObject,
                    authoring.RocketHitVfx.gameObject,
                    authoring.DeathSparksVfx.gameObject,
                };
                DynamicBuffer<VfxHitResources> vfxHitBuffer = AddBuffer<VfxHitResources>(entity);
                foreach (var vfx in vfxArray)
                {
                    vfxHitBuffer.Add(new VfxHitResources
                    {
                        VfxPrefab = vfx
                    });
                }
            }
        }
    }

    public struct GameResources : IComponentData
    {
        public uint DespawnTicks;
        public uint PolledEventsTicks;
        public float RespawnTime;

        public Entity PlayerPrefabEntity;
        public Entity CharacterPrefabEntity;
        public Entity CameraPrefabEntity;
        public Entity SpectatorPrefab;

        public bool ForceOnlyFirstWeapon;

        public Entity CharacterSpawnPointEntity;
        public CollisionFilter SpawnPointCollisionFilter;
    }

    public struct GameResourcesWeapon : IBufferElementData
    {
        public Entity WeaponPrefab;
    }

    public struct VfxHitResources : IBufferElementData
    {
        public UnityObjectRef<GameObject> VfxPrefab;
    }
}
