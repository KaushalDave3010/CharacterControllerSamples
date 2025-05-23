using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Physics;
using Unity.Transforms;
using Collider = Unity.Physics.Collider;

namespace Unity.Template.CompetitiveActionMultiplayer
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [BurstCompile]
    public partial struct DelayedDespawnSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GameResources>();
            state.RequireForUpdate(SystemAPI.QueryBuilder().WithAll<DelayedDespawn>().Build());
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            DelayedDespawnJob job = new DelayedDespawnJob
            {
                DeltaTime = SystemAPI.Time.DeltaTime,
                DespawnTicks = SystemAPI.GetSingleton<GameResources>().DespawnTicks,
                Ecb = SystemAPI.GetSingletonRW<BeginSimulationEntityCommandBufferSystem.Singleton>().ValueRW.CreateCommandBuffer(state.WorldUnmanaged),
                ChildBufferLookup = SystemAPI.GetBufferLookup<Child>(true),
                PhysicsColliderLookup = SystemAPI.GetComponentLookup<PhysicsCollider>(),
            };
            state.Dependency = job.Schedule(state.Dependency);
        }

        [BurstCompile]
        public unsafe partial struct DelayedDespawnJob : IJobEntity
        {
            public float DeltaTime;
            public uint DespawnTicks;
            public EntityCommandBuffer Ecb;
            [ReadOnly] public BufferLookup<Child> ChildBufferLookup;
            public ComponentLookup<PhysicsCollider> PhysicsColliderLookup;

            void Execute(Entity entity, ref DelayedDespawn delayedDespawn)
            {
                delayedDespawn.Ticks++;
                if (delayedDespawn.Ticks > DespawnTicks)
                {
                    Ecb.DestroyEntity(entity);
                }

                // Handle pre-despawn logic (disable rendering and collisions)
                if (delayedDespawn.HasHandledPreDespawn == 0)
                {
                    // Disable rendering
                    MiscUtilities.DisableRenderingInHierarchy(Ecb, entity, ref ChildBufferLookup);

                    // Disable collisions
                    if (PhysicsColliderLookup.TryGetComponent(entity, out PhysicsCollider physicsCollider))
                    {
                        ref Collider collider = ref *physicsCollider.ColliderPtr;
                        collider.SetCollisionResponse(CollisionResponsePolicy.None);
                    }

                    delayedDespawn.HasHandledPreDespawn = 1;
                }
            }
        }
    }
}
