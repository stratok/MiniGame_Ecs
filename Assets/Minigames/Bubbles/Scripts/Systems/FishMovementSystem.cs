using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Minigames.Bubbles
{
    public class FishMovementSystem : ComponentSystem
    {
        protected override void OnUpdate()
        {
            Entities.ForEach((Entity targetEntity, ref HasTarget hasTarget, ref Translation translation,
                                ref Rotation rotation, ref FishComponent fish) =>
            {
                if (World.Active.EntityManager.Exists(hasTarget.Target))
                {
                    Translation targetTranslation = World.Active.EntityManager.GetComponentData<Translation>(hasTarget.Target);
                    BubbleComponent bubble = World.Active.EntityManager.GetComponentData<BubbleComponent>(hasTarget.Target);
                    float3 targetDir = math.normalize(targetTranslation.Value - translation.Value);

                    float distance = math.distance(translation.Value, targetTranslation.Value);

                    if (distance > .2f)
                    {
                        if ((targetTranslation.Value.x - translation.Value.x) > 0)
                        {
                            rotation.Value.value.y = 0;
                        }
                        else
                        {
                            rotation.Value.value.y = 1;
                        }
						translation.Value += targetDir * fish.Speed * Time.fixedDeltaTime;
					}

                    if (targetTranslation.Value.x < -fish.MaxXDeviation)
                        PostUpdateCommands.RemoveComponent(targetEntity, typeof(HasTarget));
                }
                else
                {
                    PostUpdateCommands.RemoveComponent(targetEntity, typeof(HasTarget));
                }
            });
        }
    }
}