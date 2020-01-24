using System;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace Minigames.Bubbles
{
    public class BubbleDestroySystem : ComponentSystem
    {
        public static Action FishDestroyBubble;
        public static Action TapDestroyBubble;
        public static Action ScreenDestroyBubble;

        protected override void OnUpdate()
        {
            Entities.ForEach((Entity entity, ref ShouldDestroy shouldDestroy, ref Translation translation, ref Scale scale, ref BubbleComponent bubble) =>
            {
                if (bubble.TimeToDestroy > 0)
                {
                    bubble.TimeToDestroy -= Time.deltaTime;
                    scale.Value += 0.2f;
                }
                else
                {
                    switch (shouldDestroy.DestroyerType)
                    {
                        case DestroyerType.Fish:
                            FishDestroyBubble?.Invoke();
                            break;
                        case DestroyerType.Tap:
                            TapDestroyBubble?.Invoke();
                            break;
                        case DestroyerType.Screen:
                            ScreenDestroyBubble?.Invoke();
                            break;
                    }
                    PostUpdateCommands.DestroyEntity(entity);
                }
            });
        }
    }
}