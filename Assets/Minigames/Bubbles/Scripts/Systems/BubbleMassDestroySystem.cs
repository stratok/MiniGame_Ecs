using Unity.Entities;

namespace Minigames.Bubbles
{
    public class BubbleMassDestroySystem : ComponentSystem
    {
        protected override void OnUpdate()
        {
            Entities.WithAll<StopGame>().ForEach((Entity entity) =>
            {
                Entities.WithAll<BubbleComponent>().ForEach((Entity babbleEntity) =>
                {
                    PostUpdateCommands.DestroyEntity(babbleEntity);
                });
                PostUpdateCommands.DestroyEntity(entity);
            });
        }
    }
}