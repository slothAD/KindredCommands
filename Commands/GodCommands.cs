using System.Collections.Generic;
using System.Text;
using KindredCommands.Commands.Converters;
using KindredCommands.Data;
using ProjectM;
using ProjectM.Network;
using Unity.Transforms;
using UnityEngine;
using VampireCommandFramework;

namespace KindredCommands.Commands;
internal class GodCommands
{
	const int DEFAULT_FAST_SPEED = 15;

	[Command("god", adminOnly: true)]
	public static void GodCommand(ChatCommandContext ctx, OnlinePlayer player = null)
	{
		var userEntity = player?.Value.UserEntity ?? ctx.Event.SenderUserEntity;
		var charEntity = player?.Value.CharEntity ?? ctx.Event.SenderCharacterEntity;

		Core.BoostedPlayerService.RemoveBoostedPlayer(charEntity);
		Core.BoostedPlayerService.SetAttackSpeedMultiplier(charEntity, 10f);
		Core.BoostedPlayerService.SetDamageBoost(charEntity, 10000f);
		Core.BoostedPlayerService.SetHealthBoost(charEntity, 100000);
		Core.BoostedPlayerService.SetSpeedBoost(charEntity, DEFAULT_FAST_SPEED);
		Core.BoostedPlayerService.SetYieldMultiplier(charEntity, 10f);
		Core.BoostedPlayerService.ToggleBatVision(charEntity);
		Core.BoostedPlayerService.ToggleNoAggro(charEntity);
		Core.BoostedPlayerService.ToggleNoBlooddrain(charEntity);
		Core.BoostedPlayerService.ToggleNoCooldown(charEntity);
		Core.BoostedPlayerService.ToggleNoDurability(charEntity);
		Core.BoostedPlayerService.TogglePlayerImmaterial(charEntity);
		Core.BoostedPlayerService.TogglePlayerInvincible(charEntity);
		Core.BoostedPlayerService.TogglePlayerShrouded(charEntity);
		Core.BoostedPlayerService.UpdateBoostedPlayer(charEntity);

		var name = player?.Value.UserEntity.Read<User>().CharacterName ?? ctx.Event.User.CharacterName;
		ctx.Reply($"<color=white>{name}</color> 已啟用無敵模式");
	}

	[Command("mortal", adminOnly: true)]
	public static void MortalCommand(ChatCommandContext ctx, OnlinePlayer player = null)
	{
		var charEntity = (player?.Value.CharEntity ?? ctx.Event.SenderCharacterEntity);

		if (!Core.BoostedPlayerService.IsBoostedPlayer(charEntity) && !BuffUtility.HasBuff(Core.EntityManager, charEntity, Prefabs.BoostedBuff1)) return;

		Core.BoostedPlayerService.RemoveBoostedPlayer(charEntity);

		var name = player?.Value.UserEntity.Read<User>().CharacterName ?? ctx.Event.User.CharacterName;
		ctx.Reply($"<color=white>{name}</color> 的無敵模式與所有增益效果已移除");
	}

	static Dictionary<string, Vector3> positionBeforeSpectate = [];

	[Command("spectate", adminOnly: true, description:"Toggles spectate on the target player")]
	public static void SpectateCommand(ChatCommandContext ctx, OnlinePlayer player = null, bool returnToStart=true)
	{
		var charEntity = player?.Value.CharEntity ?? ctx.Event.SenderCharacterEntity;
		var userEntity = player?.Value.UserEntity ?? ctx.Event.SenderUserEntity;
		var name = userEntity.Read<User>().CharacterName;

		if (BuffUtility.HasBuff(Core.EntityManager, charEntity, Prefabs.Admin_Observe_Invisible_Buff))
		{
			if (returnToStart)
			{
				if (!positionBeforeSpectate.TryGetValue(name.ToString(), out var returnPos))
					returnPos = ctx.Event.SenderCharacterEntity.Read<Translation>().Value;
				charEntity.Write<Translation>(new Translation { Value = returnPos });
				charEntity.Write<LastTranslation>(new LastTranslation { Value = returnPos });
			}
			positionBeforeSpectate.Remove(name.ToString());
			Buffs.RemoveBuff(charEntity, Prefabs.Admin_Observe_Invisible_Buff);
			ctx.Reply($"<color=white>{name}</color> 已移除 <color=yellow>觀戰模式</color>");
		}
		else
		{

			Buffs.AddBuff(userEntity, charEntity, Prefabs.Admin_Observe_Invisible_Buff, -1);
			positionBeforeSpectate.Add(name.ToString(), charEntity.Read<Translation>().Value);
			ctx.Reply($"<color=white>{name}</color> 已加入 <color=yellow>觀戰模式</color>");
		}
	}


	[CommandGroup("boost", "bst")]
	internal class BoostedCommands
	{
		[Command("state", adminOnly: true)]
		public static void BoostState(ChatCommandContext ctx, OnlinePlayer player = null)
		{
			var charEntity = player?.Value.CharEntity ?? ctx.Event.SenderCharacterEntity;
			var name = player?.Value.UserEntity.Read<User>().CharacterName ?? ctx.Event.User.CharacterName;

			if (Core.BoostedPlayerService.IsBoostedPlayer(charEntity))
			{
				var sb = new StringBuilder();
				sb.AppendLine($"<color=white>{name}</color> is boosted");
				var attackSpeedSet = Core.BoostedPlayerService.GetAttackSpeedMultiplier(charEntity, out var attackSpeed);
				var damageSet = Core.BoostedPlayerService.GetDamageBoost(charEntity, out var damage);
				var healthSet = Core.BoostedPlayerService.GetHealthBoost(charEntity, out var health);
				var speedSet = Core.BoostedPlayerService.GetSpeedBoost(charEntity, out var speed);
				var yieldSet = Core.BoostedPlayerService.GetYieldMultiplier(charEntity, out var yield);
				var batVision = Core.BoostedPlayerService.HasBatVision(charEntity);
				var noAggro = Core.BoostedPlayerService.HasNoAggro(charEntity);
				var noBlooddrain = Core.BoostedPlayerService.HasNoBlooddrain(charEntity);
				var noCooldown = Core.BoostedPlayerService.HasNoCooldown(charEntity);
				var noDurability = Core.BoostedPlayerService.HasNoDurability(charEntity);
				var immaterial = Core.BoostedPlayerService.IsPlayerImmaterial(charEntity);
				var invincible = Core.BoostedPlayerService.IsPlayerInvincible(charEntity);
				var shrouded = Core.BoostedPlayerService.IsPlayerShrouded(charEntity);
				var sunInvulnerable = Core.BoostedPlayerService.IsSunInvulnerable(charEntity);

				if(attackSpeedSet)
					sb.AppendLine($"Attack Speed: <color=white>{attackSpeed}</color>");
				if(damageSet)
					sb.AppendLine($"Damage: <color=white>{damage}</color>");
				if(healthSet)
					sb.AppendLine($"Health: <color=white>{health}</color>");
				if(speedSet)
					sb.AppendLine($"Speed: <color=white>{speed}</color>");
				if(yieldSet)
					sb.AppendLine($"Yield: <color=white>{yield}</color>");

				var flags = new List<string>();
				if (batVision)
					flags.Add("<color=white>Bat Vision</color>");
				if (noAggro)
					flags.Add("<color=white>No Aggro</color>");
				if(noBlooddrain)
					flags.Add("<color=white>No Blooddrain</color>");
				if(noCooldown)
					flags.Add("<color=white>No Cooldown</color>");
				if(noDurability)
					flags.Add("<color=white>No Durability Loss</color>");
				if(immaterial)
					flags.Add("<color=white>Immaterial</color>");
				if(invincible)
					flags.Add("<color=white>Invincible</color>");
				if(shrouded)
					flags.Add("<color=white>Shrouded</color>");
				if (sunInvulnerable)
					flags.Add("<color=white>Sun Invulnerable</color>");
				if (flags.Count > 0)
					sb.AppendLine($"Has: {string.Join(", ", flags)}");


				ctx.Reply(sb.ToString());
			}
			else
			{
				ctx.Reply($"<color=white>{name}</color> 未獲得任何增益效果");
			}
		}

		[Command("attackspeed", "as", adminOnly: true)]
		public static void AttackSpeed(ChatCommandContext ctx, float speed = 10, OnlinePlayer player = null)
		{
			var name = player?.Value.UserEntity.Read<User>().CharacterName ?? ctx.Event.User.CharacterName;
			var charEntity = player?.Value.CharEntity ?? ctx.Event.SenderCharacterEntity;

			Core.BoostedPlayerService.SetAttackSpeedMultiplier(charEntity, speed);
			Core.BoostedPlayerService.UpdateBoostedPlayer(charEntity);
			ctx.Reply($"已將 <color=white>{name}</color> 的攻擊速度加成設為 {speed}");
		}

		[Command("removeattackspeed", "ras", adminOnly: true)]
		public static void RemoveAttackSpeed(ChatCommandContext ctx, OnlinePlayer player = null)
		{
			var name = player?.Value.UserEntity.Read<User>().CharacterName ?? ctx.Event.User.CharacterName;
			var charEntity = player?.Value.CharEntity ?? ctx.Event.SenderCharacterEntity;

			if(!Core.BoostedPlayerService.RemoveAttackSpeedMultiplier(charEntity))
			{
				ctx.Reply($"<color=white>{name}</color> 未獲得攻擊速度加成");
				return;
			}
			Core.BoostedPlayerService.UpdateBoostedPlayer(charEntity);
			ctx.Reply($"已移除 <color=white>{name}</color> 的攻擊速度加成");
		}

		[Command("damage", "d", adminOnly: true)]
		public static void Damage(ChatCommandContext ctx, float damage = 10000, OnlinePlayer player = null)
		{
			var name = player?.Value.UserEntity.Read<User>().CharacterName ?? ctx.Event.User.CharacterName;
			var charEntity = player?.Value.CharEntity ?? ctx.Event.SenderCharacterEntity;

			Core.BoostedPlayerService.SetDamageBoost(charEntity, damage);
			Core.BoostedPlayerService.UpdateBoostedPlayer(charEntity);
			ctx.Reply($"已將 <color=white>{name}</color> 的傷害加成設為 {damage}");
		}

		[Command("removedamage", "rd", adminOnly: true)]
		public static void RemoveDamage(ChatCommandContext ctx, OnlinePlayer player = null)
		{
			var name = player?.Value.UserEntity.Read<User>().CharacterName ?? ctx.Event.User.CharacterName;
			var charEntity = player?.Value.CharEntity ?? ctx.Event.SenderCharacterEntity;

			if (!Core.BoostedPlayerService.RemoveDamageBoost(charEntity))
			{
				ctx.Reply($"<color=white>{name}</color> 未獲得傷害加成");
				return;
			}
			Core.BoostedPlayerService.UpdateBoostedPlayer(charEntity);
			ctx.Reply($"已移除 <color=white>{name}</color> 的傷害加成");
		}

		[Command("health", "h", adminOnly: true)]
		public static void Health(ChatCommandContext ctx, int health = 100000, OnlinePlayer player = null)
		{
			var name = player?.Value.UserEntity.Read<User>().CharacterName ?? ctx.Event.User.CharacterName;
			var charEntity = player?.Value.CharEntity ?? ctx.Event.SenderCharacterEntity;

			Core.BoostedPlayerService.SetHealthBoost(charEntity, health);
			Core.BoostedPlayerService.UpdateBoostedPlayer(charEntity);
			ctx.Reply($"<color=white>{name}</color> 的生命加成設為 {health}");
		}

		[Command("removehealth", "rh", adminOnly: true)]
		public static void RemoveHealth(ChatCommandContext ctx, OnlinePlayer player = null)
		{
			var name = player?.Value.UserEntity.Read<User>().CharacterName ?? ctx.Event.User.CharacterName;
			var charEntity = player?.Value.CharEntity ?? ctx.Event.SenderCharacterEntity;

			if (!Core.BoostedPlayerService.RemoveHealthBoost(charEntity))
			{
				ctx.Reply($"<color=white>{name}</color> 未獲得生命加成");
				return;
			}
			Core.BoostedPlayerService.UpdateBoostedPlayer(charEntity);
			ctx.Reply($"<color=white>{name}</color> 的生命加成已移除");
		}

		[Command("speed", "s", adminOnly: true)]
		public static void Speed(ChatCommandContext ctx, float speed = DEFAULT_FAST_SPEED, OnlinePlayer player = null)
		{
			var name = player?.Value.UserEntity.Read<User>().CharacterName ?? ctx.Event.User.CharacterName;
			var charEntity = player?.Value.CharEntity ?? ctx.Event.SenderCharacterEntity;

			Core.BoostedPlayerService.SetSpeedBoost(charEntity, speed);
			Core.BoostedPlayerService.UpdateBoostedPlayer(charEntity);
			ctx.Reply($"已設定 <color=white>{name}</color> 的移動速度為 {speed}");
		}

		[Command("removespeed", "rs", adminOnly: true)]
		public static void RemoveSpeed(ChatCommandContext ctx, OnlinePlayer player = null)
		{
			var name = player?.Value.UserEntity.Read<User>().CharacterName ?? ctx.Event.User.CharacterName;
			var charEntity = player?.Value.CharEntity ?? ctx.Event.SenderCharacterEntity;

			if (!Core.BoostedPlayerService.RemoveSpeedBoost(charEntity))
			{
				ctx.Reply($"<color=white>{name}</color> 未獲得移動速度加成");
				return;
			}
			Core.BoostedPlayerService.UpdateBoostedPlayer(charEntity);
			ctx.Reply($"已移除 <color=white>{name}</color> 的移動速度加成");
		}

		[Command("yield", "y", adminOnly: true)]
		public static void Yield(ChatCommandContext ctx, float yield = 10, OnlinePlayer player = null)
		{
			var name = player?.Value.UserEntity.Read<User>().CharacterName ?? ctx.Event.User.CharacterName;
			var charEntity = player?.Value.CharEntity ?? ctx.Event.SenderCharacterEntity;

			Core.BoostedPlayerService.SetYieldMultiplier(charEntity, yield);
			Core.BoostedPlayerService.UpdateBoostedPlayer(charEntity);
			ctx.Reply($"<color=white>{name}</color> 的資源產量加成設為 {yield}");
		}

		[Command("removeyield", "ry", adminOnly: true)]
		public static void RemoveYield(ChatCommandContext ctx, OnlinePlayer player = null)
		{
			var name = player?.Value.UserEntity.Read<User>().CharacterName ?? ctx.Event.User.CharacterName;
			var charEntity = player?.Value.CharEntity ?? ctx.Event.SenderCharacterEntity;

			if (!Core.BoostedPlayerService.RemoveYieldMultiplier(charEntity))
			{
				ctx.Reply($"<color=white>{name}</color> 未獲得資源產量加成");
				return;
			}
			Core.BoostedPlayerService.UpdateBoostedPlayer(charEntity);
			ctx.Reply($"已移除 <color=white>{name}</color> 的資源產量加成");
		}

		[Command("batvision", "bv", adminOnly: true)]
		public static void BatVision(ChatCommandContext ctx, OnlinePlayer player = null)
		{
			var name = player?.Value.UserEntity.Read<User>().CharacterName ?? ctx.Event.User.CharacterName;
			var charEntity = player?.Value.CharEntity ?? ctx.Event.SenderCharacterEntity;
			if (Core.BoostedPlayerService.ToggleBatVision(charEntity))
			{
				ctx.Reply($"<color=white>{name}</color> 獲得了蝙蝠視覺");
			}
			else
			{
				ctx.Reply($"<color=white>{name}</color> 失去了蝙蝠視覺");
			}
			Core.BoostedPlayerService.UpdateBoostedPlayer(charEntity);
		}

		[Command("fly", "f", adminOnly: true)]
		public static void Fly(ChatCommandContext ctx, OnlinePlayer player = null)
		{
			var charEntity = player?.Value.CharEntity ?? ctx.Event.SenderCharacterEntity;
			var name = player?.Value.UserEntity.Read<User>().CharacterName ?? ctx.Event.User.CharacterName;

			if(Core.BoostedPlayerService.ToggleFlying(charEntity))
			{
				ctx.Reply($"<color=white>{name}</color> 獲得飛行能力");
			}
			else
			{
				ctx.Reply($"<color=white>{name}</color> 的飛行能力已移除");
			}

			Core.BoostedPlayerService.UpdateBoostedPlayer(charEntity);
		}

		[Command("noaggro", "na", adminOnly: true)]
		public static void NoAggro(ChatCommandContext ctx, OnlinePlayer player = null)
		{
			var name = player?.Value.UserEntity.Read<User>().CharacterName ?? ctx.Event.User.CharacterName;
			var charEntity = player?.Value.CharEntity ?? ctx.Event.SenderCharacterEntity;

			if (Core.BoostedPlayerService.ToggleNoAggro(charEntity))
			{
				ctx.Reply($"<color=white>{name}</color> 沒有新增仇恨");
			}
			else
			{
				ctx.Reply($"<color=white>{name}</color> 沒有移除仇恨");
			}
			Core.BoostedPlayerService.UpdateBoostedPlayer(charEntity);
		}

		[Command("noblooddrain", "nb", adminOnly: true)]
		public static void NoBlooddrain(ChatCommandContext ctx, OnlinePlayer player = null)
		{
			var name = player?.Value.UserEntity.Read<User>().CharacterName ?? ctx.Event.User.CharacterName;
			var charEntity = player?.Value.CharEntity ?? ctx.Event.SenderCharacterEntity;

			if (Core.BoostedPlayerService.ToggleNoBlooddrain(charEntity))
			{
				ctx.Reply($"<color=white>{name}</color> 未獲得吸血效果");
			}
			else
			{
				ctx.Reply($"<color=white>{name}</color> 未移除吸血效果");
			}
			Core.BoostedPlayerService.UpdateBoostedPlayer(charEntity);
		}

		[Command("nocooldown", "nc", adminOnly: true)]
		public static void NoCooldown(ChatCommandContext ctx, OnlinePlayer player = null)
		{
			var name = player?.Value.UserEntity.Read<User>().CharacterName ?? ctx.Event.User.CharacterName;
			var charEntity = player?.Value.CharEntity ?? ctx.Event.SenderCharacterEntity;

			if (Core.BoostedPlayerService.ToggleNoCooldown(charEntity))
			{
				ctx.Reply($"<color=white>{name}</color> 未新增冷卻時間");
			}
			else
			{
				ctx.Reply($"<color=white>{name}</color> 未移除冷卻時間");
			}
			Core.BoostedPlayerService.UpdateBoostedPlayer(charEntity);
		}

		[Command("nodurability", "nd", adminOnly: true)]
		public static void NoDurability(ChatCommandContext ctx, OnlinePlayer player = null)
		{
			var name = player?.Value.UserEntity.Read<User>().CharacterName ?? ctx.Event.User.CharacterName;
			var charEntity = player?.Value.CharEntity ?? ctx.Event.SenderCharacterEntity;

			if (Core.BoostedPlayerService.ToggleNoDurability(charEntity))
			{
				ctx.Reply($"<color=white>{name}</color> 未新增耐久耗損");
			}
			else
			{
				ctx.Reply($"<color=white>{name}</color> 未移除耐久耗損");
			}
			Core.BoostedPlayerService.UpdateBoostedPlayer(charEntity);
		}

		[Command("immaterial", "i", adminOnly: true)]
		public static void Immaterial(ChatCommandContext ctx, OnlinePlayer player = null)
		{
			var name = player?.Value.UserEntity.Read<User>().CharacterName ?? ctx.Event.User.CharacterName;
			var charEntity = player?.Value.CharEntity ?? ctx.Event.SenderCharacterEntity;

			if(Core.BoostedPlayerService.TogglePlayerImmaterial(charEntity))
			{
				ctx.Reply($"<color=white>{name}</color> 獲得靈體狀態");
			}
			else
			{
				ctx.Reply($"<color=white>{name}</color> 的靈體狀態已移除");
			}
			Core.BoostedPlayerService.UpdateBoostedPlayer(charEntity);
		}

		[Command("invincible", "inv", adminOnly: true)]
		public static void Invincible(ChatCommandContext ctx, OnlinePlayer player = null)
		{
			var name = player?.Value.UserEntity.Read<User>().CharacterName ?? ctx.Event.User.CharacterName;
			var charEntity = player?.Value.CharEntity ?? ctx.Event.SenderCharacterEntity;

			if (Core.BoostedPlayerService.TogglePlayerInvincible(charEntity))
			{
				ctx.Reply($"<color=white>{name}</color> 獲得無敵狀態");
			}
			else
			{
				ctx.Reply($"<color=white>{name}</color> 的無敵狀態已移除");
			}
			Core.BoostedPlayerService.UpdateBoostedPlayer(charEntity);
		}

		[Command("shrouded", "sh", adminOnly: true)]
		public static void Shrouded(ChatCommandContext ctx, OnlinePlayer player = null)
		{
			var name = player?.Value.UserEntity.Read<User>().CharacterName ?? ctx.Event.User.CharacterName;
			var charEntity = player?.Value.CharEntity ?? ctx.Event.SenderCharacterEntity;

			if (Core.BoostedPlayerService.TogglePlayerShrouded(charEntity))
			{
				ctx.Reply($"<color=white>{name}</color> 進入隱蔽狀態");
			}
			else
			{
				ctx.Reply($"<color=white>{name}</color> 退出隱蔽狀態");
			}
			Core.BoostedPlayerService.UpdateBoostedPlayer(charEntity);
		}

		[Command("suninvulnerable", "suninv", adminOnly: true)]
		public static void SunInvulnerable(ChatCommandContext ctx, OnlinePlayer player = null)
		{
			var name = player?.Value.UserEntity.Read<User>().CharacterName ?? ctx.Event.User.CharacterName;
			var charEntity = player?.Value.CharEntity ?? ctx.Event.SenderCharacterEntity;
			if (Core.BoostedPlayerService.ToggleSunInvulnerable(charEntity))
			{
				ctx.Reply($"<color=white>{name}</color> 獲得太陽免疫");
			}
			else if (Core.BoostedPlayerService.IsPlayerInvincible(charEntity))
			{
				ctx.Reply($"<color=white>{name}</color> 的太陽免疫已移除，但仍因無敵效果保有該能力");
				return;
			}
			else
			{
				ctx.Reply($"<color=white>{name}</color> 失去太陽免疫");
			}
			Core.BoostedPlayerService.UpdateBoostedPlayer(charEntity);
		}

		[Command("players", description: "provides a list of all boosted players", adminOnly: true)]
		public static void ListBoostedPlayers(ChatCommandContext ctx)
		{
			var boostedPlayers = Core.BoostedPlayerService.GetBoostedPlayers();
			if (boostedPlayers == null)
			{
				ctx.Reply("目前無任何玩家處於增益狀態");
				return;
			}

			var sb = new StringBuilder();
			sb.Append("Boosted players: ");
			var first = true;
			foreach (var player in boostedPlayers)
			{
				var playerCharacter = player.Read<PlayerCharacter>();
				var name = $"<color=white>{playerCharacter.Name}</color>";
				if (sb.Length + name.Length + 2 > Core.MAX_REPLY_LENGTH)
				{
					ctx.Reply(sb.ToString());
					sb.Clear();
					first = true;
				}
				if (first)
					sb.Append(name);
				else
					sb.Append($", {name}");
				first = false;
			}
			ctx.Reply(sb.ToString());

		}




		}
}
