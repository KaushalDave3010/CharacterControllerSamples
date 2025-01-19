using System;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Unity.Template.CompetitiveActionMultiplayer
{
    [WriteGroup(typeof(LocalToWorld))]
    public struct PrefabProjectile : IComponentData
    {
        public float Speed;
        public float Gravity;
        public float MaxLifetime;
        public float VisualOffsetCorrectionDuration;

        public float3 Velocity;
        public float LifetimeCounter;
        public byte HasHit;
        public Entity HitEntity;
        public float3 VisualOffset;
        public float3 HitNormal;
    }

    public struct RaycastProjectile : IComponentData
    {
        public float Range;
        public float Damage;
    }

    [Serializable]
    public struct RaycastVisualProjectile : IComponentData
    {
        public byte DidHit;
        public float3 StartPoint;
        public float3 EndPoint;
        public float3 HitNormal;

        public readonly float GetLengthOfTrajectory()
        {
            return math.length(EndPoint - StartPoint);
        }

        public readonly float3 GetDirection()
        {
            return math.normalizesafe(EndPoint - StartPoint);
        }
    }

    public struct ProjectileSpawnId : IComponentData
    {
        public Entity WeaponEntity;
        public uint SpawnId;

        public bool IsSame(ProjectileSpawnId other)
        {
            return WeaponEntity == other.WeaponEntity && SpawnId == other.SpawnId;
        }
    }
}
