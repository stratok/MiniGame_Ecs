using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

namespace Minigames.Bubbles
{
	#region ComponentSystem
	public class CheckTapSystem : ComponentSystem
	{
		protected override void OnUpdate()
		{
			Entities.WithAll<ScreenTap>().ForEach((Entity tapEntity, ref Translation tapPosition) =>
			{
				//Позиция нажатия
				float3 position = tapPosition.Value;

				// Получаем все энтити с BubbleComponent и выбираем ближайший
				Entities.WithAll<BubbleComponent>().WithNone<ShouldDestroy>()
						.ForEach((Entity bubbleEntity, ref Translation bubbleTranslation, ref Scale scale) =>
				{
					if (math.distance(position, bubbleTranslation.Value) <= scale.Value * .5f)
					{
						PostUpdateCommands.AddComponent(bubbleEntity, new ShouldDestroy { DestroyerType = DestroyerType.Tap });
					}
				});

				PostUpdateCommands.DestroyEntity(tapEntity);
			});
		}
	}
	#endregion

	//#region JobSystem
	//public class TapJobSystem : JobComponentSystem
	//{
	//	[BurstCompile]
	//	private struct RemoveTapJob : IJobForEachWithEntity<ScreenTap>
	//	{
	//		public EntityCommandBuffer.Concurrent EntityCommandBuffer;

	//		public void Execute(Entity entity, int index, [ReadOnly] ref ScreenTap tap)
	//		{
	//			EntityCommandBuffer.DestroyEntity(index, entity);
	//		}
	//	}

	//	private EndSimulationEntityCommandBufferSystem endSimulationEntityCommandBufferSystem;

	//	protected override void OnCreate()
	//	{
	//		endSimulationEntityCommandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
	//		base.OnCreate();
	//	}

	//	protected override JobHandle OnUpdate(JobHandle inputDeps)
	//	{
	//		RemoveTapJob removeTapJob = new RemoveTapJob
	//		{
	//			EntityCommandBuffer = endSimulationEntityCommandBufferSystem.CreateCommandBuffer().ToConcurrent()
	//		};
	//		JobHandle jobHandle = removeTapJob.Schedule(this, inputDeps);

	//		endSimulationEntityCommandBufferSystem.AddJobHandleForProducer(jobHandle);

	//		return jobHandle;
	//	}
	//}
	//#endregion
}