using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace Minigames.Bubbles
{
    public class DebugPathToTargetSystem : ComponentSystem
    {
        protected override void OnUpdate()
        {
            Entities.ForEach((Entity entity, ref Translation translation, ref HasTarget hasTarget) =>
            {
                if (World.Active.EntityManager.Exists(hasTarget.Target))
                {
                    Translation targetTranslation = World.Active.EntityManager.GetComponentData<Translation>(hasTarget.Target);
                    Debug.DrawLine(translation.Value, targetTranslation.Value);
                }
            });
        }
    }
}