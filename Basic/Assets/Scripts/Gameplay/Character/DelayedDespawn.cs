using Unity.Entities;

namespace Unity.Template.CompetitiveActionMultiplayer
{
    public struct DelayedDespawn : IComponentData, IEnableableComponent
    {
        public uint Ticks;
        public byte HasHandledPreDespawn;
    }
}
