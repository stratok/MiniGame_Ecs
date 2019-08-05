using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Minigames.Bubbles
{
    public class BubbleMovementJobSystem : JobComponentSystem
    {
        [ExcludeComponent(typeof(ShouldDestroy))]
        [BurstCompile]
        private struct MovementBurstJob : IJobForEachWithEntity<BubbleComponent, Translation, Rotation>
        {
            public float FixedDeltaTime;
            public float RealtimeSinceStartup;

            public void Execute(Entity entity, int index, [ReadOnly] ref BubbleComponent bubble, [ReadOnly] ref Translation translation, ref Rotation rotation)
            {
                float3 targetDir = math.normalize(bubble.DestinationPoint - translation.Value);
                translation.Value += targetDir * bubble.Speed * FixedDeltaTime;

                rotation.Value = quaternion.Euler(0, 0, math.PI * bubble.Speed * .2f * RealtimeSinceStartup);
            }
        }

        [ExcludeComponent(typeof(ShouldDestroy))]
        private struct AddComponentJob : IJobForEachWithEntity<BubbleComponent, Translation, Scale>
        {
            public EntityCommandBuffer.Concurrent entityCommandBuffer;
            [ReadOnly] [DeallocateOnJobCompletion] public NativeArray<Translation> fishTraslations;
            [ReadOnly] [DeallocateOnJobCompletion] public NativeArray<Translation> tapTraslations;

            public void Execute(Entity entity, int index, ref BubbleComponent bubble, ref Translation translation, ref Scale scale)
            {
                if (translation.Value.x <= bubble.DestinationPoint.x)
                {
                    entityCommandBuffer.AddComponent(index, entity, new ShouldDestroy { DestroyerType = DestroyerType.Screen });
                    return;
                }

                for (int i = 0; i < fishTraslations.Length; i++)
                {
                    if (math.distance(fishTraslations[i].Value, translation.Value) <= scale.Value * 0.5f)
                    {
                        entityCommandBuffer.AddComponent(index, entity, new ShouldDestroy { DestroyerType = DestroyerType.Fish });
                        return;
                    }
                }

                for (int i = 0; i < tapTraslations.Length; i++)
                {
                    if (math.distance(tapTraslations[i].Value, translation.Value) <= scale.Value)
                    {
                        entityCommandBuffer.AddComponent(index, entity, new ShouldDestroy { DestroyerType = DestroyerType.Tap });
                        return;
                    }
                }
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
            MovementBurstJob movementBurstBurstJob = new MovementBurstJob
            {
                FixedDeltaTime = Time.fixedDeltaTime,
                RealtimeSinceStartup = Time.realtimeSinceStartup
            };
            JobHandle jobHandle = movementBurstBurstJob.Schedule(this, inputDeps);

            EntityQuery fishQuery = GetEntityQuery(typeof(FishComponent), ComponentType.ReadOnly<Translation>());
            NativeArray<Translation> fishTranslationArray = fishQuery.ToComponentDataArray<Translation>(Allocator.TempJob);

            EntityQuery tapQuery = GetEntityQuery(typeof(ScreenTap), ComponentType.ReadOnly<Translation>());
            NativeArray<Translation> tapTranslationArray = tapQuery.ToComponentDataArray<Translation>(Allocator.TempJob);

            AddComponentJob addComponentJob = new AddComponentJob
            {
                entityCommandBuffer = endSimulationEntityCommandBufferSystem.CreateCommandBuffer().ToConcurrent(),
                fishTraslations = fishTranslationArray,
                tapTraslations = tapTranslationArray
            };

            jobHandle = addComponentJob.Schedule(this, jobHandle);

            endSimulationEntityCommandBufferSystem.AddJobHandleForProducer(jobHandle);

            return jobHandle;
        }
    }
}