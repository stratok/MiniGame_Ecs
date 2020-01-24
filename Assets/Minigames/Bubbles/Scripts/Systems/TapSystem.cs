using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace Minigames.Bubbles
{
    public class TapJobSystem : JobComponentSystem
    {
        [BurstCompile]
        private struct RemoveTapJob : IJobForEachWithEntity<ScreenTap>
        {
            public EntityCommandBuffer.Concurrent EntityCommandBuffer;

            public void Execute(Entity entity, int index, [ReadOnly] ref ScreenTap tap)
            {
                EntityCommandBuffer.DestroyEntity(index, entity);
            }
        }

        private EndSimulationEntityCommandBufferSystem endSimulationEntityCommandBufferSystem;

        protected override void OnCreate()
        {
            endSimulationEntityCommandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
            base.OnCreate();
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            RemoveTapJob removeTapJob = new RemoveTapJob
            {
                EntityCommandBuffer = endSimulationEntityCommandBufferSystem.CreateCommandBuffer().ToConcurrent()
            };
            JobHandle jobHandle = removeTapJob.Schedule(this, inputDeps);

            endSimulationEntityCommandBufferSystem.AddJobHandleForProducer(jobHandle);

            return jobHandle;
        }
    }
}