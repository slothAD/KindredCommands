using System;
using System.Linq;
using System.Text.RegularExpressions;
using KindredCommands.Commands.Converters;
using ProjectM;
using ProjectM.Network;
using Stunlock.Core;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using VampireCommandFramework;

namespace KindredCommands.Commands;

public static class PlayerCommands
{
	[Command("rename", description: "Rename another player.", adminOnly: true)]
	public static void RenameOther(ChatCommandContext ctx, FoundPlayer player, NewName newName)
	{
		Core.Players.RenamePlayer(player.Value.UserEntity, player.Value.CharEntity, newName.Name);
		ctx.Reply($"將名稱 {Format.B(player.Value.CharacterName.ToString())} 改為 {Format.B(newName.Name.ToString())}");
	}

	[Command("rename", description: "Rename yourself.", adminOnly: true)]
	public static void RenameMe(ChatCommandContext ctx, NewName newName)
	{
		Core.Players.RenamePlayer(ctx.Event.SenderUserEntity, ctx.Event.SenderCharacterEntity, newName.Name);
		ctx.Reply($"你的名稱已更新為：{Format.B(newName.Name.ToString())}");
	}

	public record struct NewName(FixedString64Bytes Name);

	public class NewNameConverter : CommandArgumentConverter<NewName>
	{
		public override NewName Parse(ICommandContext ctx, string input)
		{
			if (!IsAlphaNumeric(input))
			{
				throw ctx.Error("Name must be alphanumeric.");
			}
			var newName = new NewName(input);
			if (newName.Name.utf8LengthInBytes > 20)
			{
				throw ctx.Error("Name too long.");
			}

			var userEntities = Helper.GetEntitiesByComponentType<User>();
			var lowerName = input.ToLowerInvariant();
			foreach (var userEntity in userEntities)
			{
				var user = userEntity.Read<User>();
				if (user.CharacterName.ToString().ToLowerInvariant().Equals(lowerName))
				{
					throw ctx.Error("Name already in use.");
				}
			}
			userEntities.Dispose();

			return newName;
		}
		public static bool IsAlphaNumeric(string input)
		{
			return Regex.IsMatch(input, @"^[a-zA-Z0-9\[\]]+$");
		}
	}

	[Command("unbindplayer", description: "Unbinds a SteamID from a player's save.", adminOnly: true)]
	public static void UnbindPlayer(ChatCommandContext ctx, FoundPlayer player)
	{
		var userEntity = player.Value.UserEntity;
		var user = userEntity.Read<User>();
		ctx.Reply($"已解除玩家 {user.CharacterName} 的綁定");

		Helper.KickPlayer(userEntity);

		user = userEntity.Read<User>();
		user.PlatformId = 0;
		userEntity.Write(user);
	}

	[Command("swapplayers", description: "Switches the steamIDs of two players.", adminOnly: true)]
	public static void SwapPlayers(ChatCommandContext ctx, FoundPlayer player1, FoundPlayer player2)
	{
		var userEntity1 = player1.Value.UserEntity;
		var userEntity2 = player2.Value.UserEntity;
		var user1 = userEntity1.Read<User>();
		var user2 = userEntity2.Read<User>();

		Helper.KickPlayer(userEntity1);
		Helper.KickPlayer(userEntity2);

		ctx.Reply($"{user1.CharacterName} 與 {user2.CharacterName} 已互換位置");

		user1 = userEntity1.Read<User>();
		user2 = userEntity2.Read<User>();
		(user1.PlatformId, user2.PlatformId) = (user2.PlatformId, user1.PlatformId);
		userEntity1.Write(user1);
		userEntity2.Write(user2);
	}

	[Command("unlock", description: "Unlocks a player's skills, journal, etc.", adminOnly: true)]
	public static void UnlockPlayer(ChatCommandContext ctx, FoundPlayer player)
	{
		var User = player?.Value.UserEntity ?? ctx.Event.SenderUserEntity;
		var Character = player?.Value.CharEntity ?? ctx.Event.SenderCharacterEntity;

		try
		{
			var debugEventsSystem = Core.Server.GetExistingSystem<DebugEventsSystem>();
			var fromCharacter = new FromCharacter()
			{
				User = User,
				Character = Character
			};

			UnlockPlayer(fromCharacter);
			ctx.Reply($"已為 {player?.Value.CharacterName ?? "你"} 解鎖所有項目。");
		}
		catch (Exception e)
		{
			throw ctx.Error(e.ToString());
		}
	}

	public static DebugEventsSystem debugEventsSystem = Core.Server.GetExistingSystemManaged<DebugEventsSystem>();
	//public static SpellSchoolProgressionEventSystem spellSchoolProgressionEventSystem = Core.Server.GetExistingSystemManaged<SpellSchoolProgressionEventSystem>();

	public static void UnlockPlayer(FromCharacter fromCharacter)
	{
		debugEventsSystem.UnlockAllResearch(fromCharacter);
		debugEventsSystem.UnlockAllVBloods(fromCharacter);
		//debugEventsSystem.EnsureVBloodAbilitiesConverted(fromCharacter);
		debugEventsSystem.CompleteAllAchievements(fromCharacter);
		

		Helper.UnlockWaypoints(fromCharacter.User);
		Helper.RevealMapForPlayer(fromCharacter.User);

		var progressionEntity = fromCharacter.User.Read<ProgressionMapper>().ProgressionEntity.GetEntityOnServer();
		//UnlockAllSpellSchoolPassives(fromCharacter.User, fromCharacter.Character);
		//UnlockMusic(progressionEntity);
	}

	//[Command("unlockpassives", description: "Unlocks all spell school passives for a player.", adminOnly: true)]
	public static void UnlockPassives(ChatCommandContext ctx, FoundPlayer player=null)
	{
		var userEntity = player?.Value.UserEntity ?? ctx.Event.SenderUserEntity;
		var charEntity = player?.Value.CharEntity ?? ctx.Event.SenderCharacterEntity;
		var progressionEntity = userEntity.Read<ProgressionMapper>().ProgressionEntity.GetEntityOnServer();
		UnlockAllSpellSchoolPassives(userEntity, charEntity);
	}

	static void UnlockAllSpellSchoolPassives(Entity userEntity, Entity charEntity)
	{
		var passiveBuffer = Core.EntityManager.GetBuffer<UnlockedPassivesBuffer>(charEntity);
		var progressionEntity = userEntity.Read<ProgressionMapper>().ProgressionEntity.GetEntityOnServer();
		var progressionBuffer = Core.EntityManager.GetBuffer<UnlockedProgressionElement>(progressionEntity);
		var progressionArray = progressionBuffer.ToNativeArray(Allocator.Temp).ToArray();
		foreach (var item in Core.PrefabCollectionSystem._PrefabGuidToEntityMap)
		{
			if (!item.Key.LookupName().StartsWith("SpellPassive_"))
				continue;

			// Verify it's not already unlocked
			if (progressionArray.Where(x => x.UnlockedPrefab == item.Key).Any())
				continue;

			progressionBuffer.Add(new UnlockedProgressionElement()
			{
				UnlockedPrefab = item.Key
			});

			//Buffs.AddBuff(userEntity, charEntity, item.Key);
			/*if (!BuffUtility.TryGetBuff(Core.Server.EntityManager, charEntity, item.Key, out Entity buffEntity))
			{
				passiveBuffer.Add(new PassiveBuffer()
				{
					Entity = buffEntity,
					PrefabGuid = item.Key
				});
			}*/


		}

	}

	/*[Command("unlockmusic", description: "Unlocks all music tracks for a player.", adminOnly: true)]
	public static void UnlockMusic(ChatCommandContext ctx, FoundPlayer player=null)
	{
		var userEntity = player?.Value.UserEntity ?? ctx.Event.SenderUserEntity;
		var progressionEntity = userEntity.Read<ProgressionMapper>().ProgressionEntity.GetEntityOnServer();
		UnlockMusic(progressionEntity);
	}

	static void UnlockMusic(Entity progressionEntity)
	{
		var progressionBuffer = Core.EntityManager.GetBuffer<UnlockedProgressionElement>(progressionEntity);
		var progressionArray = progressionBuffer.ToNativeArray(Allocator.Temp).ToArray();
		foreach (var item in Core.PrefabCollectionSystem._PrefabGuidToEntityMap)
		{
			if (item.Key == Prefabs.MusicPlayerStationTrack_Base)
				continue;
			if (!item.Key.LookupName().StartsWith("MusicPlayerStationTrack_"))
				continue;

			// Verify it's not already unlocked
			if (progressionArray.Where(x => x.UnlockedPrefab == item.Key).Any())
				continue;

			progressionBuffer.Add(new UnlockedProgressionElement()
			{
				UnlockedPrefab = item.Key
			});
		}
	}*/

	[Command("revealmap", description: "Reveal the map for a player.", adminOnly: true)]
	public static void RevealMap(ChatCommandContext ctx, FoundPlayer player = null)
	{
		var userEntity = player?.Value.UserEntity ?? ctx.Event.SenderUserEntity;
		Helper.RevealMapForPlayer(userEntity);
		if (player != null)
		{
			FixedString512Bytes message = $"Your map has been revealed, you must relog to see.";
			ServerChatUtils.SendSystemMessageToClient(Core.EntityManager, userEntity.Read<User>(), ref message);
		}
		ctx.Reply($"地圖已揭示，{player?.Value.CharacterName ?? "你"} 需重新登入以顯示變更。");
	}

	[Command("revealmapforallplayers", description: "Reveal the map for all players.", adminOnly: true)]
	public static void RevealMapForAllPlayers(ChatCommandContext ctx)
	{
		if(Core.ConfigSettings.RevealMapToAll)
		{
			ctx.Reply("地圖已對所有玩家揭示。");
			return;
		}

		ctx.Reply("已向所有玩家揭示地圖，目前線上玩家需重新登入以查看更新。");
		Core.ConfigSettings.RevealMapToAll = true;
		var userEntities = Helper.GetEntitiesByComponentType<User>();
		foreach (var userEntity in userEntities)
		{
			Helper.RevealMapForPlayer(userEntity);
		}
	}

	[Command("teleport", description: "Teleport a player to a specified coordinate.", adminOnly: true)]
	public static void Teleport(ChatCommandContext ctx, float x, float y, float z, FoundPlayer player=null)
	{
		var charEntity = player!=null ? player.Value.CharEntity : ctx.Event.SenderCharacterEntity;
		var pos = new float3(x, y, z);
		charEntity.Write(new Translation { Value = pos });
		charEntity.Write(new LastTranslation { Value = pos });
		ctx.Reply($"已將 {charEntity.Read<PlayerCharacter>().Name} 傳送至 {pos}");
	}

	[Command ("fly" , description: "Toggle fly mode for a player.", adminOnly: true)]
	public static void Fly(ChatCommandContext ctx, FoundPlayer player = null)
	{
		var charEntity = player?.Value.CharEntity ?? ctx.Event.SenderCharacterEntity;
		var name = player?.Value.UserEntity.Read<User>().CharacterName ?? ctx.Event.User.CharacterName;

		if (Core.BoostedPlayerService.ToggleFlying(charEntity))
		{
			ctx.Reply($"{name} 獲得飛行能力");
		}
		else
		{
			ctx.Reply($"{name} 的飛行能力已移除");
		}
		Core.BoostedPlayerService.UpdateBoostedPlayer(charEntity);
	}

	[Command("flyup", "f^", description: "Set fly height up a floor", adminOnly: true)]
    public static void Up(ChatCommandContext ctx, FoundPlayer player = null)
    {
        var charEntity = player?.Value.CharEntity ?? ctx.Event.SenderCharacterEntity;
        var canFly = charEntity.Read<CanFly>();
        var currentHeight = canFly.FlyingHeight._Value;
        var newHeight = currentHeight + 5;
        canFly.FlyingHeight._Value = newHeight;
		canFly.HeightAboveObstacle._Value = 0;
		charEntity.Write(canFly);

        var floorLevel = newHeight / 5;
        var name = player?.Value.UserEntity.Read<User>().CharacterName ?? ctx.Event.User.CharacterName;

        ctx.Reply($"已將 {name} 移至樓層 {floorLevel}");
    }

	[Command("flydown", "fv", description: "Set fly height down a floor", adminOnly: true)]
	public static void Down(ChatCommandContext ctx, FoundPlayer player = null)
	{
		var charEntity = player?.Value.CharEntity ?? ctx.Event.SenderCharacterEntity;
		var canFly = charEntity.Read<CanFly>();
		var currentHeight = canFly.FlyingHeight._Value;
		var newHeight = currentHeight - 5;
		canFly.FlyingHeight._Value = newHeight;
		canFly.HeightAboveObstacle._Value = 0;
		charEntity.Write(canFly);

		var floorLevel = newHeight / 5;
		var name = player?.Value.UserEntity.Read<User>().CharacterName ?? ctx.Event.User.CharacterName;

		ctx.Reply($"已將 {name} 移至樓層 {floorLevel}");
	}

	[Command("flylevel", description: "Set fly height to a specific level", adminOnly: true)]
	public static void Floor(ChatCommandContext ctx, int floor, FoundPlayer player = null)
	{
		var charEntity = player?.Value.CharEntity ?? ctx.Event.SenderCharacterEntity;
		var canFly = charEntity.Read<CanFly>();
		var newHeight = floor * 5;
		canFly.FlyingHeight._Value = newHeight;
		canFly.HeightAboveObstacle._Value = 0;
		charEntity.Write(canFly);

		var name = player?.Value.UserEntity.Read<User>().CharacterName ?? ctx.Event.User.CharacterName;

		ctx.Reply($"已將 {name} 的飛行高度調整至第 {floor} 層");
	}

	[Command("flyheight", description: "Sets the fly height for the user", adminOnly: true)]
	public static void SetFlyHeight(ChatCommandContext ctx, float height = 30)
	{
		var charEntity = ctx.Event.SenderCharacterEntity;
		var canFly = charEntity.Read<CanFly>();
		canFly.FlyingHeight._Value = height;
		charEntity.Write(canFly);
		ctx.Reply($"設置飛行高度為 {height}");
	}

	[Command("flyobstacleheight", description: "Set the height to fly above any obstacles", adminOnly: true)]
	public static void SetFlyObstacleHeight(ChatCommandContext ctx, float height = 7)
	{
		var charEntity = ctx.Event.SenderCharacterEntity;
		var canFly = charEntity.Read<CanFly>();
		canFly.HeightAboveObstacle._Value = height;
		charEntity.Write(canFly);
		ctx.Reply($"設置飛行障礙高度為 {height}");
	}


	static bool initializedMoveSpeedQuery = false;
	static EntityQuery npcMoveSpeedQuery;
	[Command("pace", description: "Pace at the closest NPC near you")]
	public static void TogglePace(ChatCommandContext ctx)
	{
		var charEntity = ctx.Event.SenderCharacterEntity;

		if (!initializedMoveSpeedQuery)
		{
			npcMoveSpeedQuery = Core.EntityManager.CreateEntityQuery(new []{
				ComponentType.ReadOnly<AiMoveSpeeds>(),
				ComponentType.ReadOnly<Movement>(),
				ComponentType.ReadOnly<Translation>()
			});
		}

		var charPos = charEntity.Read<Translation>().Value;

		var closestNPC = Entity.Null;
		var closestDistance = 30f;
		var npcs = npcMoveSpeedQuery.ToEntityArray(Allocator.TempJob);
		foreach(var npc in npcs)
		{
			var translation = npc.Read<Translation>();
			var distance = math.distance(translation.Value, charPos);
			if (distance < closestDistance)
			{
				closestDistance = distance;
				closestNPC = npc;
			}
		}
		npcs.Dispose();

		if(closestNPC.Equals(Entity.Null))
		{
			
			if (Core.BoostedPlayerService.RemoveSpeedBoost(charEntity))
			{
				ctx.Reply("附近沒有發現任何 NPC，將恢復你正常的移動速度。");
				Core.BoostedPlayerService.UpdateBoostedPlayer(charEntity);
			}
			else
			{
				ctx.Reply("附近未發現任何 NPC。");
			}
			return;
		}

		var moveSpeed = closestNPC.Read<Movement>().Speed;

		if (moveSpeed > 4.0)
		{
			ctx.Reply($"{closestNPC.EntityName()} 移動速度太快，你無法跟上。");
			if(Core.BoostedPlayerService.RemoveSpeedBoost(charEntity))
				Core.BoostedPlayerService.UpdateBoostedPlayer(charEntity);
			return;
		}

		var isPacing = Core.BoostedPlayerService.GetSpeedBoost(charEntity, out var curSpeed) && Mathf.Abs(curSpeed - moveSpeed) < 0.01f;

		if (isPacing)
		{
			Core.BoostedPlayerService.RemoveSpeedBoost(charEntity);
			Core.BoostedPlayerService.UpdateBoostedPlayer(charEntity);
			ctx.Reply("你已恢復正常速度。");
		}
		else
		{
			Core.BoostedPlayerService.SetSpeedBoost(charEntity, moveSpeed);
			Core.BoostedPlayerService.UpdateBoostedPlayer(charEntity);
			ctx.Reply($"你已減緩移動速度，現在正以 {closestNPC.EntityName()} 的速度移動。");
		}
	}

	[Command("killplayer", description: "Kills the target player", adminOnly: true)]
	public static void KillPlayer(ChatCommandContext ctx, FoundPlayer player)
	{
		StatChangeUtility.KillEntity(Core.EntityManager, player.Value.CharEntity, ctx.Event.SenderCharacterEntity, Core.ServerTime, StatChangeReason.Default, true);
		ctx.Reply($"已擊殺 {player.Value.CharacterName}");
	}


	[Command("staydown", description: "Downs the target player until they get revived- respawns will not get them up.", adminOnly: true)]
	public static void PermDownPlayer(ChatCommandContext ctx, FoundPlayer player)
	{
		Buffs.AddBuff(player.Value.UserEntity, player.Value.CharEntity, new PrefabGUID(-1992158531), -1, true);
		ctx.Reply($"{player.Value.CharacterName} 已被擊倒");
	}

	[Command("playerheartcount", description: "Set the number of hearts a player has", adminOnly: true)]
	public static void PlayerHeartCount(ChatCommandContext ctx, int amount, FoundPlayer player)
	{
		var charEntity = player.Value.UserEntity;
		var heartCount = charEntity.Read<UserHeartCount>();
		heartCount.HeartCount = amount;
		charEntity.Write(heartCount);
		ctx.Reply($"將 {player.Value.CharacterName} 的心臟數量設為 {amount}");

	}

	/* // This was made for a specific server who wanted to wipe players and castles but keep certain ones to clone the map.
	static Entity UserDoingWipe;
	static Entity[] castleHeartsToNotWipe;
	static Entity[] usersToNotWipe;

	[Command("wipe", description: "Wipe's a server except excluded territoryIds and their owners.", adminOnly: true)]
	public static void WipeServer(ChatCommandContext ctx, IntArray territoryIds=null)
	{
		// Check if they are allowed to wipe
		if (!Database.CanWipe(ctx.Event.SenderUserEntity))
		{
			ctx.Reply("你沒有權限清除伺服器資料。");
			return;
		}

		if(UserDoingWipe != Entity.Null)
		{
			ctx.Reply($"{UserDoingWipe.Read<User>().CharacterName} 已經啟動清除作業");
			return;
		}

		var castleHeartList = new List<Entity>();
		var userList = new List<Entity>();
		var castleHearts = Helper.GetEntitiesByComponentType<CastleHeart>();
		foreach (var heartEntity in castleHearts)
		{
			var castleHeart = heartEntity.Read<CastleHeart>();
			if (castleHeart.CastleTerritoryEntity.Equals(Entity.Null))
				continue;

			var castleTerritoryIndex = castleHeart.CastleTerritoryEntity.Read<CastleTerritory>().CastleTerritoryIndex;
			if (territoryIds!=null && territoryIds.Value.Contains(castleTerritoryIndex))
			{
				castleHeartList.Add(heartEntity);

				var userOwner = heartEntity.Read<UserOwner>();
				var userEntity = userOwner.Owner.GetEntityOnServer();
				userList.Add(userEntity);
				ctx.Reply($"{userEntity.Read<User>().CharacterName} 因位於領地 {castleTerritoryIndex} 而排除於清除作業之外");
			}
		}
		castleHearts.Dispose();

		UserDoingWipe = ctx.Event.SenderUserEntity;
		castleHeartsToNotWipe = castleHeartList.ToArray();
		usersToNotWipe = userList.ToArray();
		ctx.Reply("若要繼續清除，請使用指令 .commencewipe；若要取消，請使用 .cancelwipe。");
		ServerChatUtils.SendSystemMessageToAllClients(Core.EntityManager, $"{ctx.User.CharacterName} has started to initiate a wipe");
	}

	[Command("commencewipe", description: "Actually performs the wipe", adminOnly: true)]
	public static void CommenceWipe(ChatCommandContext ctx)
	{
		if(ctx.Event.SenderUserEntity != UserDoingWipe)
		{
			if(UserDoingWipe == Entity.Null)
			{
				ctx.Reply("目前未進行任何清除作業。");
				return;
			}
			ctx.Reply($"你不是啟動清除作業的使用者。該操作由 {UserDoingWipe.Read<User>().CharacterName} 發起。");
			return;
		}

		/*var heartConnections = Helper.GetEntitiesByComponentType<CastleHeartConnection>();
		foreach (var connectionEntity in heartConnections)
		{
			if(connectionEntity.Has<CastleHeart>())
				continue;

			var heartConnection = connectionEntity.Read<CastleHeartConnection>();
			var heart = heartConnection.CastleHeartEntity.GetEntityOnServer();
			if (heart.Equals(Entity.Null))
				continue;
			if(castleHeartsToNotWipe.Where(x => x.Equals(heart)).Any())
				continue;

			if (connectionEntity.Has<SpawnChainChild>())
				connectionEntity.Remove<SpawnChainChild>();

			if (connectionEntity.Has<DropTableBuffer>())
				connectionEntity.Remove<DropTableBuffer>();

			if (connectionEntity.Has<InventoryBuffer>())
				Core.EntityManager.GetBuffer<InventoryBuffer>(connectionEntity).Clear();

			DestroyUtility.Destroy(Core.EntityManager, connectionEntity);
		}
		heartConnections.Dispose();*/
	/*
		var pss = Core.Server.GetExistingSystem<PylonstationSystem>();
		var serverTime = Core.CastleBuffsTickSystem._ServerTime.GetSingleton();
		var bufferSystem = Core.Server.GetExistingSystem<EntityCommandBufferSystem>();
		var castleHearts = Helper.GetEntitiesByComponentType<CastleHeart>();
		foreach (var heartEntity in castleHearts)
		{
			if(castleHeartsToNotWipe.Where(x => x.Equals(heartEntity)).Any())
				continue;

			var userEntity = heartEntity.Read<UserOwner>().Owner.GetEntityOnServer();
			var fromCharacter = new FromCharacter()
			{
				User = userEntity,
				Character = userEntity.Read<User>().LocalCharacter.GetEntityOnServer()
			};

			var pylonstation = heartEntity.Read<Pylonstation>();
			var buffer = bufferSystem.CreateCommandBuffer();
			pss.DestroyCastle(heartEntity, ref pylonstation, ref fromCharacter, ref serverTime, ref buffer);
		}
		castleHearts.Dispose();

		List<Entity> clansToIgnore = new();
		var userEntities = Helper.GetEntitiesByComponentType<User>();
		foreach(var userEntity in userEntities)
		{
			if (usersToNotWipe.Where(x => x.Equals(userEntity)).Any())
			{
				clansToIgnore.Add(userEntity.Read<User>().ClanEntity.GetEntityOnServer());
				continue;
			}
			Helper.KickPlayer(userEntity);
			var user = userEntity.Read<User>();
			user.PlatformId = 0;
			userEntity.Write(user);

			var charEntity = user.LocalCharacter.GetEntityOnServer();
			if(charEntity.Equals(Entity.Null))
				continue;

			Core.Players.RenamePlayer(userEntity, charEntity, "");

			charEntity.Write(new Translation() { Value = new float3(-818f, 10f, -1989f) });
			charEntity.Write(new LastTranslation() { Value = new float3(-818f, 10f, -1989f) });

			StatChangeUtility.KillEntity(Core.EntityManager, charEntity, ctx.Event.SenderCharacterEntity, 0, true);
		}
		userEntities.Dispose();

		var playerDeathContainers = Helper.GetEntitiesByComponentType<PlayerDeathContainer>();
		foreach (var deathContainer in playerDeathContainers)
		{
			Core.EntityManager.GetBuffer<InventoryBuffer>(deathContainer).Clear();
			DestroyUtility.Destroy(Core.EntityManager, deathContainer);
		}
		playerDeathContainers.Dispose();

		var clanTeams = Helper.GetEntitiesByComponentType<ClanTeam>();
		foreach (var clanEntity in clanTeams)
		{
			if (clansToIgnore.Where(x => x.Equals(clanEntity)).Any())
				continue;

			var clanTeam = clanEntity.Read<ClanTeam>();
			clanTeam.Name = "";
			clanTeam.Motto = "";
			clanEntity.Write(clanTeam);
		}
		clanTeams.Dispose();

		var st = Core.EntityManager.CreateEntity(new ComponentType[1] { ComponentType.ReadOnly<SetTimeOfDayEvent>() });
		st.Write(new SetTimeOfDayEvent()
		{
			Day = 10,
			Hour = 0,
			Type = SetTimeOfDayEvent.SetTimeType.Add
		});


		UserDoingWipe = Entity.Null;
		ServerChatUtils.SendSystemMessageToAllClients(Core.EntityManager, $"{ctx.User.CharacterName} has wiped the server");
		Core.Log.LogInfo("Server has been wiped.");
	}

	[Command("cancelwipe", description: "Cancels the wipe", adminOnly: true)]
	public static void CancelWipe(ChatCommandContext ctx)
	{
		if (UserDoingWipe == Entity.Null)
		{
			ctx.Reply("目前未進行任何清除作業。");
			return;
		}

		UserDoingWipe = Entity.Null;
		castleHeartsToNotWipe = null;
		usersToNotWipe = null;
		ctx.Reply("清除作業已取消。");
		ServerChatUtils.SendSystemMessageToAllClients(Core.EntityManager, $"{ctx.User.CharacterName} has canceled the wipe");
	}
	*/
	/*
	[Command("unbindall", description: "First renames all players to OLD##### and unbinds them from their SteamIDs.", adminOnly: true)]	
	public static void UnbindAll(ChatCommandContext ctx)
	{
		var userEntities = Helper.GetEntitiesByComponentType<User>();
		foreach (var userEntity in userEntities)
		{
			var user = userEntity.Read<User>();

			var charEntity = user.LocalCharacter.GetEntityOnServer();
			if (charEntity.Equals(Entity.Null))
				continue;

			Core.Players.RenamePlayer(userEntity, charEntity, $"OLD{user.PlatformId}");

			if (user.PlatformId == 0)
				continue;

			Helper.KickPlayer(userEntity);
			user.PlatformId = 0;
			userEntity.Write(user);

		}
		userEntities.Dispose();
	}


	*/
}
