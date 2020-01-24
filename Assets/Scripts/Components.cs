using System;
using Unity.Entities;
using Unity.Mathematics;

namespace Minigames.Bubbles
{
    [Serializable]
    public struct BubbleComponent : IComponentData
    {
        public float3 DestinationPoint;
        public Entity Entity;
        public float Speed;
        public float TimeToDestroy;
    }

    public struct FishComponent : IComponentData
    {
        public float MaxXDeviation;
        public float Speed;
    }

    public struct HasTarget : IComponentData { public Entity Target; }

    public struct ShouldDestroy : IComponentData
    {
        public DestroyerType DestroyerType;
    }

    public struct StopGame : IComponentData { }

    public struct ScreenTap : IComponentData { }

    public enum DestroyerType
    {
        Fish,
        Tap,
        Screen
    }
}