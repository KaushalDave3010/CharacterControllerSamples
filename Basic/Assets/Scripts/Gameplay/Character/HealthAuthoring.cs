using Unity.Entities;
using UnityEngine;

namespace Unity.Template.CompetitiveActionMultiplayer
{
    public class HealthAuthoring : MonoBehaviour
    {
        public float MaxHealth = 100f;

        public class Baker : Baker<HealthAuthoring>
        {
            public override void Bake(HealthAuthoring authoring)
            {
                Entity entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new Health
                {
                    MaxHealth = authoring.MaxHealth,
                    CurrentHealth = authoring.MaxHealth,
                });
            }
        }
    }

    public struct Health : IComponentData
    {
        public float MaxHealth;
        public float CurrentHealth;

        public readonly bool IsDead()
        {
            return CurrentHealth <= 0f;
        }
    }
}
