using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;

namespace Unity.Template.CompetitiveActionMultiplayer
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial class ProjectilePredictionUpdateGroup : ComponentSystemGroup
    {
    }

    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateBefore(typeof(TransformSystemGroup))]
    public partial class ProjectileVisualsUpdateGroup : ComponentSystemGroup
    {
    }

    [BurstCompile]
    [UpdateInGroup(typeof(ProjectilePredictionUpdateGroup), OrderFirst = true)]
    public partial struct ProjectileSimulationSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<PhysicsWorldSingleton>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            ProjectileSimulationsJob job = new ProjectileSimulationsJob
            {
                DeltaTime = SystemAPI.Time.DeltaTime,
                PhysicsWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>().PhysicsWorld,
                DelayedDespawnLookup = SystemAPI.GetComponentLookup<DelayedDespawn>(),
            };
            state.Dependency = job.ScheduleParallel(state.Dependency);
        }

        [BurstCompile]
        [WithAll(typeof(Simulate))]
        [WithDisabled(typeof(DelayedDespawn))]
        public partial struct ProjectileSimulationsJob : IJobEntity, IJobEntityChunkBeginEnd
        {
            public float DeltaTime;
            [ReadOnly]
            public PhysicsWorld PhysicsWorld;
            [NativeDisableParallelForRestriction]
            public ComponentLookup<DelayedDespawn> DelayedDespawnLookup;

            [NativeDisableContainerSafetyRestriction]
            NativeList<RaycastHit> m_Hits;

            void Execute(Entity entity, ref PrefabProjectile projectile, ref LocalTransform localTransform, in DynamicBuffer<WeaponShotIgnoredEntity> ignoredEntities)
            {
                if (projectile.HasHit == 0)
                {
                    // Movement.
                    projectile.Velocity += math.up() * projectile.Gravity * DeltaTime;
                    float3 displacement = projectile.Velocity * DeltaTime;

                    // Hit detection.
                    m_Hits.Clear();
                    RaycastInput raycastInput = new RaycastInput
                    {
                        Start = localTransform.Position,
                        End = localTransform.Position + displacement,
                        Filter = CollisionFilter.Default,
                    };
                    PhysicsWorld.CastRay(raycastInput, ref m_Hits);
                    if (WeaponUtilities.GetClosestValidWeaponRaycastHit(in m_Hits, in ignoredEntities, out RaycastHit closestValidHit))
                    {
                        displacement *= closestValidHit.Fraction;
                        projectile.HitEntity = closestValidHit.Entity;
                        projectile.HasHit = 1;
                        projectile.HitNormal = closestValidHit.SurfaceNormal;
                    }

                    // Advance position.
                    localTransform.Position += displacement;
                }

                // Lifetime.
                projectile.LifetimeCounter += DeltaTime;
                if (projectile.LifetimeCounter >= projectile.MaxLifetime)
                {
                    DelayedDespawnLookup.SetComponentEnabled(entity, true);
                }
            }

            public bool OnChunkBegin(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                if (!m_Hits.IsCreated)
                {
                    m_Hits = new NativeList<RaycastHit>(128, Allocator.Temp);
                }

                return true;
            }

            public void OnChunkEnd(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask,
                bool chunkWasExecuted)
            {
            }
        }
    }

    [BurstCompile]
    [UpdateInGroup(typeof(WeaponVisualsUpdateGroup), OrderFirst = true)]
    public partial struct InitializeWeaponLastVisualTotalProjectilesCountSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var initializeWeaponLastVisualTotalProjectilesCountJob = new InitializeWeaponLastVisualTotalProjectilesCountJob();
            state.Dependency = initializeWeaponLastVisualTotalProjectilesCountJob.Schedule(state.Dependency);
        }

        [BurstCompile]
        public partial struct InitializeWeaponLastVisualTotalProjectilesCountJob : IJobEntity
        {
            void Execute(ref BaseWeapon baseWeapon)
            {
                // This prevents false visual feedbacks when a ghost is re-spawned due to relevancy.
                if (baseWeapon.LastVisualTotalProjectilesCountInitialized == 0)
                {
                    baseWeapon.LastVisualTotalProjectilesCount = baseWeapon.TotalProjectilesCount;
                    baseWeapon.LastVisualTotalProjectilesCountInitialized = 1;
                }
            }
        }
    }

    [BurstCompile]
    [UpdateInGroup(typeof(WeaponVisualsUpdateGroup), OrderLast = true)]
    public partial struct FinalizeWeaponLastVisualTotalProjectilesCountSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var finalizeWeaponLastVisualTotalProjectilesCountJob = new FinalizeWeaponLastVisualTotalProjectilesCountJob();
            state.Dependency = finalizeWeaponLastVisualTotalProjectilesCountJob.Schedule(state.Dependency);
        }

        [BurstCompile]
        public partial struct FinalizeWeaponLastVisualTotalProjectilesCountJob : IJobEntity
        {
            void Execute(ref BaseWeapon baseWeapon)
            {
                baseWeapon.LastVisualTotalProjectilesCount = baseWeapon.TotalProjectilesCount;
            }
        }
    }

    [BurstCompile]
    [UpdateInGroup(typeof(WeaponVisualsUpdateGroup))]
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    public partial struct RaycastWeaponProjectileVisualsSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<PhysicsWorldSingleton>();
            state.RequireForUpdate<BeginSimulationEntityCommandBufferSystem.Singleton>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            RaycastWeaponProjectileVisualsJob raycastWeaponProjectileVisualsJob = new RaycastWeaponProjectileVisualsJob
            {
                Ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged),
                PhysicsWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>().PhysicsWorld,
                RaycastProjectileLookup = SystemAPI.GetComponentLookup<RaycastProjectile>(true),
                LocalTransformLookup = SystemAPI.GetComponentLookup<LocalTransform>(true),
                ParentLookup = SystemAPI.GetComponentLookup<Parent>(true),
                PostTransformMatrixLookup = SystemAPI.GetComponentLookup<PostTransformMatrix>(true),
            };
            state.Dependency = raycastWeaponProjectileVisualsJob.Schedule(state.Dependency);
        }

        [BurstCompile]
        public partial struct RaycastWeaponProjectileVisualsJob : IJobEntity, IJobEntityChunkBeginEnd
        {
            public EntityCommandBuffer Ecb;
            [ReadOnly] public PhysicsWorld PhysicsWorld;
            [ReadOnly] public ComponentLookup<RaycastProjectile> RaycastProjectileLookup;
            [ReadOnly] public ComponentLookup<LocalTransform> LocalTransformLookup;
            [ReadOnly] public ComponentLookup<Parent> ParentLookup;
            [ReadOnly] public ComponentLookup<PostTransformMatrix> PostTransformMatrixLookup;

            [NativeDisableContainerSafetyRestriction]
            NativeList<RaycastHit> m_Hits;

            void Execute(
                ref BaseWeapon baseWeapon,
                ref RaycastWeapon raycastWeapon,
                ref DynamicBuffer<RaycastWeaponVisualProjectileEvent> visualProjectileEvents,
                in DynamicBuffer<WeaponShotIgnoredEntity> ignoredEntities,
                in WeaponShotSimulationOriginOverride shotSimulationOriginOverride)
            {
                // For efficient mode, create temporary projectile visual events reconstructed from: total shots count
                // and latest available data.
                if (raycastWeapon.VisualsSyncMode == RaycastWeaponVisualsSyncMode.BandwidthEfficient)
                {
                    if (baseWeapon.LastVisualTotalProjectilesCount < baseWeapon.TotalProjectilesCount)
                    {
                        if (RaycastProjectileLookup.TryGetComponent(raycastWeapon.ProjectilePrefab,
                                out RaycastProjectile raycastProjectile))
                        {
                            RigidTransform shotSimulationOrigin = WeaponUtilities.GetShotSimulationOrigin(
                                baseWeapon.ShotOrigin,
                                in shotSimulationOriginOverride,
                                ref LocalTransformLookup,
                                ref ParentLookup,
                                ref PostTransformMatrixLookup);

                            for (var i = baseWeapon.LastVisualTotalProjectilesCount; i < baseWeapon.TotalProjectilesCount; i++)
                            {
                                Random deterministicRandom = Random.CreateFromIndex(i);
                                quaternion shotRotationWithSpread =
                                    WeaponUtilities.CalculateSpreadRotation(shotSimulationOrigin.rot,
                                        baseWeapon.SpreadRadians,
                                        ref deterministicRandom);

                                WeaponUtilities.CalculateIndividualRaycastShot(
                                    shotSimulationOrigin.pos,
                                    math.mul(shotRotationWithSpread, math.forward()),
                                    raycastProjectile.Range,
                                    in PhysicsWorld.CollisionWorld,
                                    ref m_Hits,
                                    in ignoredEntities,
                                    out var hitFound,
                                    out float3 hitNormal,
                                    out _,
                                    out float3 shotSimulationEndPoint);

                                visualProjectileEvents.Add(new RaycastWeaponVisualProjectileEvent
                                {
                                    DidHit = hitFound ? (byte)1 : (byte)0,
                                    EndPoint = shotSimulationEndPoint,
                                    HitNormal = hitNormal,
                                });
                            }
                        }
                    }
                }

                // Processes visual projectile events (only of ticks that weren't already processed).
                if (visualProjectileEvents.Length > 0)
                {
                    TransformHelpers.ComputeWorldTransformMatrix(baseWeapon.ShotOrigin,
                        out float4x4 shotVisualsOrigin, ref LocalTransformLookup, ref ParentLookup,
                        ref PostTransformMatrixLookup);
                    float3 visualOrigin = shotVisualsOrigin.Translation();

                    for (var i = 0; i < visualProjectileEvents.Length; i++)
                    {
                        RaycastWeaponVisualProjectileEvent visualProjectileEvent = visualProjectileEvents[i];

                        float3 visualStartToEndDirection =
                            math.normalizesafe(visualProjectileEvent.EndPoint - visualOrigin);

                        Entity shotVisualsEntity = Ecb.Instantiate(raycastWeapon.ProjectilePrefab);
                        Ecb.SetComponent(shotVisualsEntity,
                            LocalTransform.FromPositionRotation(visualOrigin,
                                quaternion.LookRotationSafe(visualStartToEndDirection, math.up())));
                        Ecb.AddComponent(shotVisualsEntity, new RaycastVisualProjectile
                        {
                            DidHit = visualProjectileEvent.DidHit,
                            StartPoint = visualOrigin,
                            EndPoint = visualProjectileEvent.EndPoint,
                            HitNormal = visualProjectileEvent.HitNormal,
                        });
                    }
                }

                // Clear events for efficient mode, so they are not synced.
                if (raycastWeapon.VisualsSyncMode == RaycastWeaponVisualsSyncMode.BandwidthEfficient)
                {
                    visualProjectileEvents.Clear();
                }
            }

            public bool OnChunkBegin(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                if (!m_Hits.IsCreated)
                {
                    m_Hits = new NativeList<RaycastHit>(128, Allocator.Temp);
                }

                return true;
            }

            public void OnChunkEnd(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask,
                bool chunkWasExecuted)
            {
            }
        }
    }

    /// <summary>
    /// This system handles offsetting the projectile visual render meshes so that they look like they're coming out of the weapon
    /// barrel instead of the center of the camera. It merges the visual projectile position with the camera-centered trajectory
    /// over a certain period of time.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(TransformSystemGroup))]
    [UpdateBefore(typeof(LocalToWorldSystem))]
    public partial struct PrefabProjectileVisualsSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var job = new PrefabProjectileVisualsJob();
            state.Dependency = job.Schedule(state.Dependency);
        }

        [BurstCompile]
        public partial struct PrefabProjectileVisualsJob : IJobEntity
        {
            void Execute(ref LocalToWorld ltw, in LocalTransform transform, in PrefabProjectile projectile)
            {
                float3 visualOffset = math.lerp(projectile.VisualOffset, float3.zero, math.saturate(projectile.LifetimeCounter / projectile.VisualOffsetCorrectionDuration));
                float4x4 visualOffsetTransform = float4x4.Translate(visualOffset);
                ltw.Value = math.mul(visualOffsetTransform, float4x4.TRS(transform.Position, quaternion.LookRotationSafe(math.normalizesafe(projectile.Velocity), math.up()), transform.Scale));
            }
        }
    }
}
