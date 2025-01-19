using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

[Serializable]
public struct MainEntityCamera : IComponentData
{
    public MainEntityCamera(float fov)
    {
        BaseFov = fov;
        CurrentFov = fov;
    }

    public float BaseFov;
    public float CurrentFov;
}
