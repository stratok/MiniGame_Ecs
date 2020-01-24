using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

namespace Minigames.Bubbles
{
    public class GetTargetJobSystem : JobComponentSystem
    {
        private struct EntityWithPosition
        {
            public Entity Entity;
            public float3 Position;
        }

        [ExcludeComponent(typeof(HasTarget))]
        [BurstCompile]
        private struct FindTargetBurstJob : IJobForEachWithEntity<Translation, FishComponent>
        {
            [ReadOnly]
            [DeallocateOnJobCompletion]
            public NativeArray<EntityWithPosition> targetArray;

            public NativeArray<Entity> closestTargetEntityArray;

            public void Execute(Entity entity, int index, [ReadOnly] ref Translation translation, [ReadOnly] ref FishComponent fish)
            {
                float3 fishPosition = translation.Value;
                float maxXDeviation = fish.MaxXDeviation;
                Entity closestTargetEntity = Entity.Null;
                float3 closestTargetPosition = float3.zero;

                for (int i = 0; i < targetArray.Length; i++)
                {
                    EntityWithPosition targetEntityWithPosition = targetArray[i];

                    if (closestTargetEntity == Entity.Null
                        && targetEntityWithPosition.Position.x < maxXDeviation && targetEntityWithPosition.Position.x > -maxXDeviation
                        )
                    {
                        closestTargetEntity = targetEntityWithPosition.Entity;
                        closestTargetPosition = targetEntityWithPosition.Position;
                    }
                    else
                    {
                        if (math.distance(fishPosition, targetEntityWithPosition.Position) < math.distance(fishPosition, closestTargetPosition)
                            && targetEntityWithPosition.Position.x < maxXDeviation && targetEntityWithPosition.Position.x > -maxXDeviation
                            )
                        {
                            closestTargetEntity = targetEntityWithPosition.Entity;
                            closestTargetPosition = targetEntityWithPosition.Position;
                        }
                    }

                };

                closestTargetEntityArray[index] = closestTargetEntity;
            }
        }

        [RequireComponentTag(typeof(FishComponent))]
        [ExcludeComponent(typeof(HasTarget))]
        private struct AddComponentJob : IJobForEachWithEntity<Translation>
        {
            [ReadOnly] [DeallocateOnJobCompletion] public NativeArray<Entity> closestTargetEntityArray;
            public EntityCommandBuffer.Concurrent entityCommandBuffer;

            public void Execute(Entity entity, int index, ref Translation translation)
            {
                if (closestTargetEntityArray[index] != Entity.Null)
                    entityCommandBuffer.AddComponent(index, entity, new HasTarget { Target = closestTargetEntityArray[index] });
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
            #region Bubbles
            EntityQuery targetQuery = GetEntityQuery(typeof(BubbleComponent), ComponentType.ReadOnly<Translation>());
            NativeArray<Entity> targetEntityArray = targetQuery.ToEntityArray(Allocator.TempJob);
            NativeArray<Translation> targetTranslationArray = targetQuery.ToComponentDataArray<Translation>(Allocator.TempJob);
            NativeArray<EntityWithPosition> targetArray = new NativeArray<EntityWithPosition>(targetEntityArray.Length, Allocator.TempJob);
            for (int i = 0; i < targetArray.Length; i++)
            {
                targetArray[i] = new EntityWithPosition
                {
                    Entity = targetEntityArray[i],
                    Position = targetTranslationArray[i].Value
                };
            }
            targetEntityArray.Dispose();
            targetTranslationArray.Dispose();
            #endregion

            EntityQuery fishQuery = GetEntityQuery(typeof(FishComponent), ComponentType.Exclude<HasTarget>());
            NativeArray<Entity> closestTargetEntityArray = new NativeArray<Entity>(fishQuery.CalculateEntityCount(), Allocator.TempJob);

            FindTargetBurstJob getTargetBurstJob = new FindTargetBurstJob
            {
                targetArray = targetArray,
                closestTargetEntityArray = closestTargetEntityArray
            };

            JobHandle jobHandle = getTargetBurstJob.Schedule(this, inputDeps);

            AddComponentJob addComponentJob = new AddComponentJob
            {
                closestTargetEntityArray = closestTargetEntityArray,
                entityCommandBuffer = endSimulationEntityCommandBufferSystem.CreateCommandBuffer().ToConcurrent()
            };
            jobHandle = addComponentJob.Schedule(this, jobHandle);

            endSimulationEntityCommandBufferSystem.AddJobHandleForProducer(jobHandle);

            return jobHandle;
        }
    }
}