using System;
using System.Collections.Generic;
using HarmonyLib;
using ProjectM;
using Stunlock.Core;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace KindredCommands.Services;
internal class UnitSpawnerService
{
	private static Entity empty_entity = new();

	internal const int DEFAULT_MINRANGE = 1;
	internal const int DEFAULT_MAXRANGE = 1;

	public UnitSpawnerService()
	{
		InitializeUnitLevels();
	}

	public static void Spawn(Entity user, PrefabGUID unit, int count, float2 position, float minRange = 1, float maxRange = 2, float duration = -1)
	{
		var translation = Core.EntityManager.GetComponentData<Translation>(user);
		var pos = new float3(position.x, translation.Value.y, position.y);
		var usus = Core.Server.GetExistingSystemManaged<UnitSpawnerUpdateSystem>();
		usus.SpawnUnit(empty_entity, unit, pos, count, minRange, maxRange, duration);
	}

	public void SpawnWithCallback(Entity user, PrefabGUID unit, float2 position, float duration, Action<Entity> postActions, float yPosition = -1)
	{
		if (yPosition == -1)
		{
			var translation = Core.EntityManager.GetComponentData<Translation>(user);
			yPosition = translation.Value.y;
		}
		var pos = new float3(position.x, yPosition, position.y);
		var usus = Core.Server.GetExistingSystemManaged<UnitSpawnerUpdateSystem>();

		UnitSpawnerReactSystem_Patch.Enabled = true;

		var durationKey = NextKey();
		usus.SpawnUnit(empty_entity, unit, pos, 1, DEFAULT_MINRANGE, DEFAULT_MAXRANGE, durationKey);
		PostActions.Add(durationKey, (duration, postActions));
	}

	internal long NextKey()
	{
		System.Random r = new();
		long key;
		int breaker = 5;
		do
		{
			key = r.NextInt64(10000) * 3;
			breaker--;
			if (breaker < 0)
			{
				throw new Exception($"Failed to generate a unique key for UnitSpawnerService");
			}
		} while (PostActions.ContainsKey(key));
		return key;
	}

	void InitializeUnitLevels()
	{
		foreach(var levelBuff in Helper.GetEntitiesByComponentTypes<ModifyUnitLevelBuff, Buff>())
		{
			var buff = levelBuff.Read<Buff>();
			var modifyUnitLevelBuff = levelBuff.Read<ModifyUnitLevelBuff>();

			if (buff.Target.Has<UnitLevel>())
			{
				var unitLevel = buff.Target.Read<UnitLevel>();
				unitLevel.Level._Value = modifyUnitLevelBuff.UnitLevel;
				buff.Target.Write(unitLevel);
			}
		}
	}

	internal Dictionary<long, (float actualDuration, Action<Entity> Actions)> PostActions = [];

	[HarmonyPatch(typeof(UnitSpawnerReactSystem), nameof(UnitSpawnerReactSystem.OnUpdate))]
	public static class UnitSpawnerReactSystem_Patch
	{
		public static bool Enabled { get; set; } = false;

		public static void Prefix(UnitSpawnerReactSystem __instance)
		{
			if (!Enabled) return;

			var entities = __instance._Query.ToEntityArray(Unity.Collections.Allocator.Temp);

			Core.Log.LogDebug($"Processing {entities.Length} in UnitSpawnerReactionSystem");

			foreach (var entity in entities)
			{
				Core.Log.LogDebug($"Checking if {entity.Index} has lifetime..");

				if (!Core.EntityManager.HasComponent<LifeTime>(entity)) return;

				var lifetimeComp = Core.EntityManager.GetComponentData<LifeTime>(entity);
				var durationKey = (long)Mathf.Round(lifetimeComp.Duration);
				Core.Log.LogDebug($"Found durationKey {durationKey} from LifeTime({lifetimeComp.Duration})");
				if (Core.UnitSpawner.PostActions.TryGetValue(durationKey, out var unitData))
				{
					var (actualDuration, actions) = unitData;
					Core.Log.LogDebug($"Found post actions for {durationKey} with {actualDuration} duration");
					Core.UnitSpawner.PostActions.Remove(durationKey);


					var endAction = actualDuration < 0 ? LifeTimeEndAction.None : LifeTimeEndAction.Destroy;

					var newLifeTime = new LifeTime()
					{
						Duration = actualDuration,
						EndAction = endAction
					};

					Core.EntityManager.SetComponentData(entity, newLifeTime);

					actions(entity);
				}
			}
			entities.Dispose();
		}
	}
}
