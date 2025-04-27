using Unity.Entities;
using UnityEngine;

public class SceneInitializationAuthoring : MonoBehaviour
{
    public class Baker : Baker<SceneInitializationAuthoring>
    {
        public override void Bake(SceneInitializationAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.None);
            AddComponent(entity, new SceneInitialization());
        }
    }
}