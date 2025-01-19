using Unity.Entities;

namespace Unity.Template.CompetitiveActionMultiplayer
{
    public struct DelayedDespawn : IComponentData, IEnableableComponent
    {
        public float Lifetime;                // Time in seconds the entity has existed
        public byte HasHandledPreDespawn;     // Flag to track pre-despawn logic
    }
}
