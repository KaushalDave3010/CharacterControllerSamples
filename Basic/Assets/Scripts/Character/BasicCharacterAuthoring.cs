using Unity.Entities;
using UnityEngine;
using Unity.CharacterController;
using Unity.Template.CompetitiveActionMultiplayer;

[DisallowMultipleComponent]
public class BasicCharacterAuthoring : MonoBehaviour
{
    public AuthoringKinematicCharacterProperties CharacterProperties = AuthoringKinematicCharacterProperties.GetDefault();
    public BasicCharacterComponent Character = BasicCharacterComponent.GetDefault();
    public GameObject ViewEntity;
    public GameObject WeaponSocket;
    public GameObject WeaponAnimationSocket;
    
    public class Baker : Baker<BasicCharacterAuthoring>
    {
        public override void Bake(BasicCharacterAuthoring authoring)
        {
            KinematicCharacterUtilities.BakeCharacter(this, authoring, authoring.CharacterProperties);

            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, authoring.Character);
            AddComponent(entity, new BasicCharacterControl());
            AddComponent(entity, new OwningPlayer());
            AddComponent(entity, new ActiveWeapon());
            AddComponent(entity, new DelayedDespawn());
            SetComponentEnabled<DelayedDespawn>(GetEntity(TransformUsageFlags.Dynamic), false);
            
            // Convert and assign references for ViewEntity, WeaponSocket, and WeaponAnimationSocket
            if (authoring.ViewEntity != null)
            {
                var viewEntity = GetEntity(authoring.ViewEntity, TransformUsageFlags.Dynamic);
                authoring.Character.ViewEntity = viewEntity;
            }

            if (authoring.WeaponSocket != null)
            {
                var weaponSocketEntity = GetEntity(authoring.WeaponSocket, TransformUsageFlags.Dynamic);
                authoring.Character.WeaponSocket = weaponSocketEntity;
            }

            if (authoring.WeaponAnimationSocket != null)
            {
                var weaponAnimationSocketEntity = GetEntity(authoring.WeaponAnimationSocket, TransformUsageFlags.Dynamic);
                authoring.Character.WeaponAnimationSocketEntity = weaponAnimationSocketEntity;
            }

            // Update the character component with the assigned entities
            SetComponent(entity, authoring.Character);
        }
    }
}