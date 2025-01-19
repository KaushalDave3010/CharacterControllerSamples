using Unity.Entities;
using UnityEngine;
using UnityEngine.VFX;

public class GameResourcesAuthoring : MonoBehaviour
{
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

public struct VfxHitResources : IBufferElementData
{
    public UnityObjectRef<GameObject> VfxPrefab;
}
