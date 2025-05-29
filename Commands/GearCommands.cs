using System.Text;
using KindredCommands.Commands.Converters;
using ProjectM;
using ProjectM.Shared;
using Stunlock.Core;
using Unity.Entities;
using VampireCommandFramework;
using System.Collections.Generic;
using System.Collections;
using Unity.Transforms;
using UnityEngine;

namespace KindredCommands.Commands;
internal static class DurabilityCommands
{
	[CommandGroup("gear")]
	internal class GearCommands
	{
		[Command("repair", "r", description: "Repairs all gear.", adminOnly: true)]
		public static void RepairCommand(ChatCommandContext ctx, FoundPlayer player = null)
		{
			var targetEntity = player?.Value.CharEntity ?? ctx.Event.SenderCharacterEntity;

			Helper.RepairGear(targetEntity);
			ctx.Reply($"{targetEntity.Read<PlayerCharacter>().Name} 的裝備已修復。");
		}

		[Command("break", "b", description: "Breaks all gear.", adminOnly: true)]
		public static void BreakGearCommand(ChatCommandContext ctx, FoundPlayer player = null)
		{
			var targetEntity = player?.Value.CharEntity ?? ctx.Event.SenderCharacterEntity;
			Helper.RepairGear(targetEntity, false);
			ctx.Reply($"{targetEntity.Read<PlayerCharacter>().Name} 的裝備已損壞。");
		}

		[Command("repairall", "ra", description: "Repairs all gear within a range.", adminOnly: true)]
		public static void RepairAllCommand(ChatCommandContext ctx, float range = 10)
		{
			var senderPos = ctx.Event.SenderCharacterEntity.Read<LocalToWorld>().Position;
			var playerEntities = Helper.GetEntitiesByComponentType<PlayerCharacter>();
			foreach (var playerEntity in playerEntities)
			{
				var pos = playerEntity.Read<LocalToWorld>().Position;
				if (Vector3.Distance(senderPos, pos) > range) continue;
				Helper.RepairGear(playerEntity);
			}

			ctx.Reply($"半徑 {range} 公尺內所有玩家的裝備已修復。");
		}

		[Command("breakall", "ba", description: "Breaks all gear within a range.", adminOnly: true)]
		public static void BreakAllCommand(ChatCommandContext ctx, float range = 10)
		{
			var senderPos = ctx.Event.SenderCharacterEntity.Read<LocalToWorld>().Position;
			var playerEntities = Helper.GetEntitiesByComponentType<PlayerCharacter>();
			foreach (var playerEntity in playerEntities)
			{
				var pos = playerEntity.Read<LocalToWorld>().Position;
				if (Vector3.Distance(senderPos, pos) > range) continue;
				Helper.RepairGear(playerEntity, false);
			}

			ctx.Reply($"半徑 {range} 公尺內所有玩家的裝備已損壞。");
		}

		[Command("headgear", "hg", description: "Toggles headgear loss on death.", adminOnly: true)]
		public static void HeadgearBloodBoundCommand(ChatCommandContext ctx)
		{
			if(Core.GearService.ToggleHeadgearBloodbound())
			{
				ctx.Reply("死亡時不會失去頭部裝備。");
			}
			else
			{
				ctx.Reply("死亡時會失去頭部裝備。");
			}
		}

		[Command("soulshardflight", "ssf", description: "Toggles soulshard flight restrictions.", adminOnly: true)]
		public static void SoulshardsFlightRestrictedCommand(ChatCommandContext ctx)
		{
			if (Core.GearService.ToggleShardsFlightRestricted())
			{
				if (Core.ServerGameSettingsSystem._Settings.BatBoundShards)
					ctx.Reply("靈魂碎片不再允許飛行。");
				else
					ctx.Reply("目前遊戲設定中 BatBoundShards 為關閉狀態，KindredCommands 無法強制啟用。");
			}
			else
			{
				ctx.Reply("靈魂碎片現在允許飛行。");
			}
		}

		[Command("togglesoulsharddropmanagement", "tssdm", description: "Toggles whether KindredCommands will do soulshard drop management.", adminOnly: true)]
		public static void ToggleSoulshardDropManagementCommand(ChatCommandContext ctx)
		{
			if (Core.SoulshardService.ToggleShardDropManagement())
			{
				ctx.Reply("KindredCommands 的靈魂碎片掉落管理已啟用。");
			}
			else
			{
				ctx.Reply("KindredCommands 的靈魂碎片掉落管理已停用。");
			}
		}

		[Command("soulshardlimit", "ssl", description: "How many soulshards can be dropped before a boss won't drop a new one if the relic Unique setting is active.", adminOnly: true)]
		public static void SoulshardLimitCommand(ChatCommandContext ctx, int limit, RelicType shardType=RelicType.None)
		{
			if (limit < 0)
			{
				throw ctx.Error("Limit must be zero or greater.");
			}
			Core.SoulshardService.SetShardDropLimit(limit, shardType);
			if (shardType == RelicType.None)
			{
				ctx.Reply($"所有靈魂碎片的上限設為 {limit}。");
			}
			else
			{
				ctx.Reply($"靈魂碎片類型 {shardType} 的上限設為 {limit}。");
			}
		}

		[Command("soulshardstatus", "sss", description: "Reports the current status of soulshards.", adminOnly: false)]
		public static void SoulshardStatusCommand(ChatCommandContext ctx)
		{
			var sb = new StringBuilder();
			var soulshardStatus = Core.SoulshardService.GetSoulshardStatus();
			sb.AppendLine("\nSoulshard Status");
			var soulshardFlightAllowed = !Core.ConfigSettings.SoulshardsFlightRestricted || !Core.ServerGameSettingsSystem._Settings.BatBoundShards;
			sb.AppendLine($"Can Fly: {(soulshardFlightAllowed ? "<color=green>Yes</color>" : "<color=red>No</color>")}");

			var notPlentiful = Core.ServerGameSettingsSystem._Settings.RelicSpawnType == RelicSpawnType.Unique;

			var managesDrops = notPlentiful && Core.ConfigSettings.ShardDropManagementEnabled;
			var theMonster = soulshardStatus[(int)RelicType.TheMonster];
			var solarus = soulshardStatus[(int)RelicType.Solarus];
			var wingedHorror = soulshardStatus[(int)RelicType.WingedHorror];
			var megara = soulshardStatus[(int)RelicType.Morgana];
			var dracula = soulshardStatus[(int)RelicType.Dracula];
			sb.Append($"The Monster: <color=white>{theMonster.droppedCount}</color>x");
			if (managesDrops) sb.Append($" out of <color=white>{Core.ConfigSettings.ShardMonsterDropLimit}</color>x");
			sb.AppendLine($" dropped <color=white>{theMonster.spawnedCount}</color>x spawned{(notPlentiful ? (theMonster.willDrop ? " <color=green>Will</color> drop" : " <color=red>Won't</color> drop") : "")}");

			sb.Append($"Solarus: <color=white>{solarus.droppedCount}</color>x ");
			if (managesDrops) sb.Append($"out of <color=white>{Core.ConfigSettings.ShardSolarusDropLimit}</color>x");
			sb.AppendLine($" dropped <color=white>{solarus.spawnedCount}</color>x spawned{(notPlentiful ? (solarus.willDrop ? " <color=green>Will</color> drop" : " <color=red>Won't</color> drop") : "")}");

			ctx.Reply(sb.ToString());
			sb.Clear();

			sb.Append($"Winged Horror: <color=white>{wingedHorror.droppedCount}</color>x");
			if (managesDrops) sb.Append($" out of <color=white>{Core.ConfigSettings.ShardWingedHorrorDropLimit}</color>x");
			sb.AppendLine($" dropped <color=white>{wingedHorror.spawnedCount}</color>x spawned{(notPlentiful ? (wingedHorror.willDrop ? " <color=green>Will</color> drop" : " <color=red>Won't</color> drop") : "")}");

			sb.Append($"Megara: <color=white>{megara.droppedCount}</color>x");
			if (managesDrops) sb.Append($" out of <color=white>{Core.ConfigSettings.ShardMonsterDropLimit}</color>x");
			sb.AppendLine($" dropped <color=white>{megara.spawnedCount}</color>x spawned{(notPlentiful ? (megara.willDrop ? " <color=green>Will</color> drop" : " <color=red>Won't</color> drop") : "")}");

			sb.Append($"Dracula: <color=white>{dracula.droppedCount}</color>x");
			if (managesDrops) sb.Append($" out of <color=white>{Core.ConfigSettings.ShardDraculaDropLimit}</color>x");
			sb.AppendLine($" dropped <color=white>{dracula.spawnedCount}</color>x spawned{(notPlentiful ? (dracula.willDrop ? " <color=green>Will</color> drop" : " <color=red>Won't</color> drop") : "")}");
			ctx.Reply(sb.ToString());
		}

		[Command("soulsharddurability", "ssd", description: "Sets the durability on all soulshards in the player's inventory to a specified amount", adminOnly: true)]
		public static void ShardDurabilityCommand(ChatCommandContext ctx, float durability, FoundPlayer player = null)
		{
			var charEntity = player?.Value.CharEntity ?? ctx.Event.SenderCharacterEntity;

			if (durability < 0)
			{
				throw ctx.Error("Durability must be zero or greater.");
			}

			var shardsChanged = 0;
			foreach(var shard in Helper.GetEntitiesByComponentType<Relic>())
			{
				var relic = shard.Read<Relic>();
				if (relic.RelicType == RelicType.None) continue;

				if (!shard.Has<InventoryItem>()) continue;

			    var itemContainer = shard.Read<InventoryItem>().ContainerEntity;
				if (itemContainer.Equals(Entity.Null)) continue;

				if (!itemContainer.Has<InventoryConnection>()) continue;
				
				var inventoryOwner = itemContainer.Read<InventoryConnection>().InventoryOwner;
				if (inventoryOwner != charEntity) continue;

				var durabilityData = shard.Read<Durability>();
				durabilityData.Value = durability;
				shard.Write(durabilityData);

				shardsChanged += 1;
			}

			var playerName = charEntity.Read<PlayerCharacter>().Name;
			if (shardsChanged > 0)
				ctx.Reply($"<color=white>{shardsChanged}</color> 個靈魂碎片的耐久度已為 <color=yellow>{playerName}</color> 設為 {durability}");
			else
				ctx.Reply($"<color=yellow>{playerName}</color> 的背包中未找到靈魂碎片。");
		}

		[Command("soulsharddurabilitytime", "ssdt", description: "How many seconds will soulshards last before they break", adminOnly: true)]
		public static void ShardDurabilityTimeCommand(ChatCommandContext ctx, int? seconds=null)
		{
			if (seconds.HasValue && seconds < 0)
			{
				throw ctx.Error("Time must be zero or greater.");
			}
			Core.SoulshardService.SetShardDurabilityTime(seconds);
			if (seconds == null)
			{
				ctx.Reply("靈魂碎片的耐久時間已還原為預設值。");
			}
			else
			{
				ctx.Reply($"靈魂碎片的耐久時間設為 {seconds} 秒。");
			}
		}


		[Command("destroyallshards", description: "Destroys all soulshards in the world, containers, and inventories", adminOnly: true)]
		public static void DestroyAllShards(ChatCommandContext ctx)
		{
			foreach (var shard in Helper.GetEntitiesByComponentType<Relic>())
			{
				if (!shard.Has<Durability>()) continue;

				var durability = shard.Read<Durability>();
				if (durability.Value == 0)
				{
					durability.Value = durability.MaxDurability;
					shard.Write(durability);
				}
			}
			Core.StartCoroutine(BreakShardsInAMoment(ctx));
		}

		static IEnumerator BreakShardsInAMoment(ChatCommandContext ctx)
		{
			yield return null;
			var destroyedPrefabs = new Dictionary<PrefabGUID, int>();
			foreach (var shard in Helper.GetEntitiesByComponentType<Relic>())
			{
				if (!shard.Has<Durability>()) continue;

				var durability = shard.Read<Durability>();
				if (durability.Value == 0)
					continue;

				durability.Value = 0;
				durability.DestroyItemWhenBroken = true;
				shard.Write(durability);

				if (shard.Has<PrefabGUID>())
				{
					var guid = shard.Read<PrefabGUID>();
					if (!destroyedPrefabs.TryGetValue(guid, out var count))
						count = 0;
					destroyedPrefabs[guid] = count + 1;
				}
			}

			foreach (var (guid, count) in destroyedPrefabs)
			{
				ctx.Reply($"已摧毀 <color=white>{count}</color> 個 <color=yellow>{guid.LookupName()}</color>");
				Core.Log.LogInfo($"Destroyed {count}x {guid.LookupName()}");
			}
		}
		/*
		   [Command("showhair", "sh", description: "Toggles hair visibility.")]
		   public static void ShowHairCommand(ChatCommandContext ctx)
		   {
			   var charEntity = ctx.Event.SenderCharacterEntity;
			   var equipment = charEntity.Read<Equipment>();

			   if (equipment.ArmorHeadgearSlot.Equals(Entity.Null))
			   {
				   ctx.Reply("未裝備頭部裝備。");
				   return;
			   }

			   var headgear = equipment.ArmorHeadgearSlot.GetType();
			   var equipmentToggleData = headgear.Read<EquipmentToggleData>();
			   equipmentToggleData.HideCharacterHairOnEquip = !equipmentToggleData.HideCharacterHairOnEquip;
			   headgear.Write(equipmentToggleData);

			   ctx.Reply("頭髮目前為「" + (equipmentToggleData.HideCharacterHairOnEquip ? "隱藏" : "顯示")") + " with current headgear");
		   }

	   */

	}
}



