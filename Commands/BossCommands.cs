using System.Collections.Generic;
using System.Linq;
using KindredCommands.Commands.Converters;
using ProjectM;
using ProjectM.Network;
using Stunlock.Core;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using VampireCommandFramework;

namespace KindredCommands.Commands;
[CommandGroup("boss")]
internal class BossCommands
{
	[Command("modify", "m", description: "Modify the level of the specified nearest boss.", adminOnly: true)]
	public static void ModifyBossCommand(ChatCommandContext ctx, FoundVBlood boss, int level)
	{
		var entityManager = Core.EntityManager;
		var playerEntity = ctx.Event.SenderCharacterEntity;
        var playerPos = playerEntity.Read<LocalToWorld>().Position;
        var closestVBlood = Entity.Null;
        var closestDistance = float.MaxValue;
		
		foreach (var entity in Helper.GetEntitiesByComponentType<VBloodUnit>(includeDisabled: true).ToArray().Where(x => Vector3.Distance(x.Read<Translation>().Value, Vector3.zero) > 1))
		{
			if (entity.Read<PrefabGUID>().GuidHash != boss.Value.GuidHash)
				continue;

			if (Vector3.Distance(entity.Read<Translation>().Value, playerPos) < closestDistance)
			{
				closestDistance = Vector3.Distance(entity.Read<Translation>().Value, playerPos);
				closestVBlood = entity;
			}
		}

		if (closestVBlood.Equals(Entity.Null))
        {
            ctx.Reply($"找不到要修改的首領「{boss.Name}」");
            return;
        }
		
		var unitLevel = closestVBlood.Read<UnitLevel>();
		var previousLevel = unitLevel.Level;
		unitLevel.Level._Value = level;
        closestVBlood.Write<UnitLevel>(unitLevel);

		ctx.Reply($"已將最近的 {boss.Name} 從等級 {previousLevel} 調整至 {level}");
	}

	[Command("modifyprimal", "mp", description: "Modify the level of the specified nearest primal boss.", adminOnly: true)]
	public static void ModifyPrimalBossCommand(ChatCommandContext ctx, FoundPrimal boss, int level)
	{
		var entityManager = Core.EntityManager;
		var playerEntity = ctx.Event.SenderCharacterEntity;
		var playerPos = playerEntity.Read<LocalToWorld>().Position;
		var closestVBlood = Entity.Null;
		var closestDistance = float.MaxValue;

		foreach (var entity in Helper.GetEntitiesByComponentType<VBloodUnit>(includeDisabled: true).ToArray().Where(x => Vector3.Distance(x.Read<Translation>().Value, Vector3.zero) > 1))
		{
			if (entity.Read<PrefabGUID>().GuidHash != boss.Value.GuidHash)
				continue;

			if (Vector3.Distance(entity.Read<Translation>().Value, playerPos) < closestDistance)
			{
				closestDistance = Vector3.Distance(entity.Read<Translation>().Value, playerPos);
				closestVBlood = entity;
			}
		}

		if (closestVBlood.Equals(Entity.Null))
		{
			ctx.Reply($"找不到要修改的首領「{boss.Name}」");
			return;
		}

		var unitLevel = closestVBlood.Read<UnitLevel>();
		var previousLevel = unitLevel.Level;
		unitLevel.Level._Value = level;
		closestVBlood.Write<UnitLevel>(unitLevel);

		ctx.Reply($"已將最近的 {boss.Name} 從等級 {previousLevel} 調整至 {level}");
	}

	[Command("teleportto", "tt", description: "Teleports you to the named boss. (If multiple specify the number of which one)", adminOnly: true)]
    public static void TeleportToBossCommand(ChatCommandContext ctx, FoundVBlood boss, int whichOne=0)
    {
		var foundBosses = new List<Entity>();

		static float3 GetBossPos(Entity entity)
		{
				var following = entity.Read<Follower>().Followed._Value;
				if (following == Entity.Null)
					return entity.Read<Translation>().Value;
				else
					return following.Read<Translation>().Value;
		}

        foreach (var entity in Helper.GetEntitiesByComponentType<VBloodUnit>(includeDisabled: true).ToArray()
			.Where(x => x.Read<PrefabGUID>().GuidHash == boss.Value.GuidHash)
			.Where(x => Vector3.Distance(GetBossPos(x), Vector3.zero)>1))
        {
			foundBosses.Add(entity);
		}

		if(!foundBosses.Any())
		{
			ctx.Reply($"找不到 {boss.Name}");
		}
		else if (foundBosses.Count > 1 && whichOne==0)
		{
			ctx.Reply($"找到 {foundBosses.Count} 個 {boss.Name}，請指定要傳送至哪一個編號。");
		}
		else
		{
			var index = whichOne == 0 ? 0 : Mathf.Clamp(whichOne, 1, foundBosses.Count) - 1;
			var bossEntity = foundBosses[index];
			var pos = GetBossPos(bossEntity);

			var archetype = Core.EntityManager.CreateArchetype(new ComponentType[] {
				ComponentType.ReadWrite<FromCharacter>(),
				ComponentType.ReadWrite<PlayerTeleportDebugEvent>()
			});

			var entity = Core.EntityManager.CreateEntity(archetype);
			Core.EntityManager.SetComponentData(entity, new FromCharacter()
			{
				User = ctx.Event.SenderUserEntity,
				Character = ctx.Event.SenderCharacterEntity
			});

			Core.EntityManager.SetComponentData(entity, new PlayerTeleportDebugEvent()
			{
				Position = new float3(pos.x, pos.y, pos.z),
				Target = PlayerTeleportDebugEvent.TeleportTarget.Self
			});

			ctx.Reply($"正在傳送至 {boss.Name}（位置：{pos}）");
		}
    }

	[Command("lock", "l", description: "Locks the specified boss from spawning.", adminOnly: true)]
	public static void LockBossCommand(ChatCommandContext ctx, FoundVBlood boss)
	{
		if (Core.Boss.LockBoss(boss))
			ctx.Reply($"已鎖定 {boss.Name}");
		else
			ctx.Reply($"{boss.Name} 已處於鎖定狀態");
	}


	[Command("unlock", "u", description: "Unlocks the specified boss allowing it to spawn.", adminOnly: true)]
	public static void UnlockBossCommand(ChatCommandContext ctx, FoundVBlood boss)
	{
		if(Core.Boss.UnlockBoss(boss))
			ctx.Reply($"已解鎖 {boss.Name}");
		else
			ctx.Reply($"{boss.Name} 已處於解鎖狀態");
	}

	[Command("lockprimal", "lp", description: "Locks the specified primal boss from spawning.", adminOnly: true)]
	public static void LockPrimalBossCommand(ChatCommandContext ctx, FoundPrimal primalBoss)
	{
		var boss = new FoundVBlood(primalBoss.Value, "Primal "+primalBoss.Name);
		if (Core.Boss.LockBoss(boss))
			ctx.Reply($"已鎖定 {boss.Name}");
		else
			ctx.Reply($"{boss.Name} 已處於鎖定狀態");
	}

	[Command("unlockprimal", "up", description: "Unlocks the specified primal boss allowing it to spawn.", adminOnly: true)]
	public static void UnlockPrimalBossCommand(ChatCommandContext ctx, FoundPrimal primalBoss)
	{
		var boss = new FoundVBlood(primalBoss.Value, "Primal " + primalBoss.Name);
		if (Core.Boss.UnlockBoss(boss))
			ctx.Reply($"已解鎖 {boss.Name}");
		else
			ctx.Reply($"{boss.Name} 已處於解鎖狀態");
	}

	[Command("list", "ls", description: "Lists all locked bosses.", adminOnly: false)]
    public static void ListLockedBossesCommand(ChatCommandContext ctx)
    {
        var lockedBosses = Core.Boss.LockedBossNames;
        if (lockedBosses.Any())
        {
            ctx.Reply($"已鎖定的首領：{string.Join(", ", lockedBosses)}");
        }
        else
        {
            ctx.Reply("目前沒有首領處於鎖定狀態。");
        }
    }
}
