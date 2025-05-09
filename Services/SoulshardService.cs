using System.Collections.Generic;
using System.Linq;
using Il2CppInterop.Runtime;
using KindredCommands.Data;
using ProjectM;
using ProjectM.Scripting;
using ProjectM.Shared;
using Stunlock.Core;
using Unity.Collections;
using Unity.Entities;

namespace KindredCommands.Services;
internal class SoulshardService
{
	readonly List<Entity> droppedSoulshards = [];
	readonly List<Entity> spawnedSoulshards = []; // Tracked with the ScriptSpawn tag

	EntityQuery relicDroppedQuery;
	EntityQuery soulshardAndPrefabsQuery;

	public bool IsPlentiful => Core.ServerGameSettingsSystem._Settings.RelicSpawnType == RelicSpawnType.Plentiful;

	public SoulshardService()
	{
		var relicDroppedQueryBuilder = new EntityQueryBuilder(Allocator.Temp)
			.AddAll(new(Il2CppType.Of<RelicDropped>(), ComponentType.AccessMode.ReadOnly))
			.WithOptions(EntityQueryOptions.IncludeSystems);

		relicDroppedQuery = Core.EntityManager.CreateEntityQuery(ref relicDroppedQueryBuilder);
		relicDroppedQueryBuilder.Dispose();

		var soulshardAndPrefabsQueryBuilder = new EntityQueryBuilder(Allocator.Temp)
			.AddAll(new(Il2CppType.Of<ItemData>(), ComponentType.AccessMode.ReadOnly))
			.AddAll(new(Il2CppType.Of<Relic>(), ComponentType.AccessMode.ReadOnly))
			.WithOptions(EntityQueryOptions.IncludePrefab);
		soulshardAndPrefabsQuery = Core.EntityManager.CreateEntityQuery(ref soulshardAndPrefabsQueryBuilder);
		soulshardAndPrefabsQueryBuilder.Dispose();

		foreach (var entity in Helper.GetEntitiesByComponentTypes<ItemData, Relic>())
		{
			if (entity.Has<ScriptSpawn>())
				spawnedSoulshards.Add(entity);
			else
				droppedSoulshards.Add(entity);
		}
		RefreshWillDrop();

		if (Core.ConfigSettings.ShardDurabilityTime.HasValue)
			SetShardDurabilityNoSave(Core.ConfigSettings.ShardDurabilityTime);
	}

	int ShardDropLimit(RelicType relicType) => relicType switch
	{
		RelicType.TheMonster => Core.ConfigSettings.ShardMonsterDropLimit,
		RelicType.Solarus => Core.ConfigSettings.ShardSolarusDropLimit,
		RelicType.WingedHorror => Core.ConfigSettings.ShardWingedHorrorDropLimit,
		RelicType.Dracula => Core.ConfigSettings.ShardDraculaDropLimit,
		RelicType.Morgana => Core.ConfigSettings.ShardMorganaDropLimit,
		_ => 1
	};

	public void RefreshWillDrop()
	{
		if (IsPlentiful || !Core.ConfigSettings.ShardDropManagementEnabled) return;
		var relicDropped = GetRelicDropped();
		for (var relicType = RelicType.TheMonster; relicType <= RelicType.Morgana; relicType++)
		{
			var droppedCount = droppedSoulshards.Where(e => e.Read<Relic>().RelicType == relicType).Count();
			var shouldDrop = droppedCount < ShardDropLimit(relicType);
			var isDropped = relicDropped[(int)relicType].Value;

			if (isDropped == shouldDrop)
				relicDropped[(int)relicType] = new RelicDropped() { Value = !shouldDrop };
		}
	}

	public void SetShardDropLimit(int limit, RelicType relicType)
	{
		switch (relicType)
		{
			case RelicType.TheMonster:
				Core.ConfigSettings.ShardMonsterDropLimit = limit;
				break;
			case RelicType.Solarus:
				Core.ConfigSettings.ShardSolarusDropLimit = limit;
				break;
			case RelicType.WingedHorror:
				Core.ConfigSettings.ShardWingedHorrorDropLimit = limit;
				break;
			case RelicType.Dracula:
				Core.ConfigSettings.ShardDraculaDropLimit = limit;
				break;
			case RelicType.Morgana:
				Core.ConfigSettings.ShardMorganaDropLimit = limit;
				break;
			case RelicType.None:
				Core.ConfigSettings.ShardMonsterDropLimit = limit;
				Core.ConfigSettings.ShardSolarusDropLimit = limit;
				Core.ConfigSettings.ShardWingedHorrorDropLimit = limit;
				Core.ConfigSettings.ShardDraculaDropLimit = limit;
				Core.ConfigSettings.ShardMorganaDropLimit = limit;
				break;
		}
		RefreshWillDrop();
	}

	DynamicBuffer<RelicDropped> GetRelicDropped()
	{
		var entities = relicDroppedQuery.ToEntityArray(Allocator.Temp);

		var buffer = Core.EntityManager.GetBuffer<RelicDropped>(entities[0]);
		entities.Dispose();
		return buffer;
	}

	public (bool willDrop, int droppedCount, int spawnedCount)[] GetSoulshardStatus()
	{
		var returning = new (bool willDrop, int droppedCount, int spawnedCount)[6];

		var relicDropped = GetRelicDropped();
		
		for(var relicType = RelicType.None; relicType <= RelicType.Morgana; relicType++)
		{
			var droppedCount = droppedSoulshards.Where(e => e.Read<Relic>().RelicType == relicType).Count();
			var spawnedCount = spawnedSoulshards.Where(e => e.Read<Relic>().RelicType == relicType).Count();
			var willDrop = !relicDropped[(int)relicType].Value;
			returning[(int)relicType] = (willDrop, droppedCount, spawnedCount);
		}
		return returning;
	}

	public void HandleSoulshardSpawn(Entity soulshardItemEntity)
	{
		if (!soulshardItemEntity.Has<InventoryItem>()) return;

		var invItem = soulshardItemEntity.Read<InventoryItem>();
		var isSpawned = invItem.ContainerEntity == Entity.Null || invItem.ContainerEntity.Read<PrefabGUID>() == Prefabs.External_Inventory;

		if (isSpawned)
		{
			spawnedSoulshards.Add(soulshardItemEntity);
			soulshardItemEntity.Add<ScriptSpawn>();
		}
		else
		{
			droppedSoulshards.Add(soulshardItemEntity);
		}
		RefreshWillDrop();
	}

	public void HandleSoulshardDestroy(Entity soulshardItemEntity)
	{
		if (!droppedSoulshards.Remove(soulshardItemEntity))
			spawnedSoulshards.Remove(soulshardItemEntity);
	}

	public void SetShardDurabilityTime(int? durabilityTime)
	{
		Core.ConfigSettings.ShardDurabilityTime = durabilityTime;

		SetShardDurabilityNoSave(durabilityTime);
	}

	void SetShardDurabilityNoSave(int? durabilityTime)
	{
		var entities = soulshardAndPrefabsQuery.ToEntityArray(Allocator.Temp);
		foreach (var entity in entities)
		{
			if (!entity.Has<LoseDurabilityOverTime>())
				continue;

			var ldot = entity.Read<LoseDurabilityOverTime>();
			ldot.TimeUntilBroken = durabilityTime ?? 129600;
			entity.Write(ldot);
		}
		entities.Dispose();
	}

	public bool ToggleShardDropManagement()
	{
		Core.ConfigSettings.ShardDropManagementEnabled = !Core.ConfigSettings.ShardDropManagementEnabled;
		return Core.ConfigSettings.ShardDropManagementEnabled;
	}
}
