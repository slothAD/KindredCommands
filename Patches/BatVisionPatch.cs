using HarmonyLib;
using ProjectM;
using ProjectM.Gameplay.Scripting;
using ProjectM.Shared.Systems;
using Unity.Collections;
using Unity.Entities;

namespace KindredCommands.Patches;
[HarmonyPatch(typeof(ScriptSpawnServer), nameof(ScriptSpawnServer.OnUpdate))]
internal class BatVisionPatch
{
	static void Prefix(ScriptSpawnServer __instance)
	{

		NativeArray<Entity> entities = __instance.__query_1231292176_0.ToEntityArray(Allocator.Temp);
		try
		{
			foreach (Entity entity in entities)
			{
				if (!entity.Has<Script_SetFlyingHeightVision_Buff_DataShared>()) continue;
				if (!entity.Has<EntityOwner>()) continue;

				var entityOwner = entity.Read<EntityOwner>();
				var player = entityOwner.Owner;
				if (!Core.BoostedPlayerService.HasBatVision(player)) continue;
					
				entity.With((ref Script_SetFlyingHeightVision_Buff_DataShared flyHeightBuff) =>
				{
					flyHeightBuff.Delay = float.MaxValue;
				});
			}
		}
		finally
		{
			entities.Dispose();
		}
	}
}
