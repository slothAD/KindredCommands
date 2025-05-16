using System.Linq;
using ProjectM;
using ProjectM.Gameplay.Systems;
using ProjectM.Network;
using Unity.Entities;

namespace KindredCommands.Patches;

[HarmonyLib.HarmonyPatch(typeof(PlayerTeleportSystem), "OnUpdate")]
internal class PlayerTeleportSystemPatch
{
	public static void Prefix(PlayerTeleportSystem __instance)
	{
		var entities = __instance._Query.ToEntityArray(Unity.Collections.Allocator.Temp);

		foreach (var entity in entities)
		{
			var teleportEvent = entity.Read<PlayerTeleportDebugEvent>();
			var fromCharacter = entity.Read<FromCharacter>();
			var user = fromCharacter.User.Read<User>();

			if (user.IsAdmin)
			{
				Core.Log.LogInfo($"Admin {user.CharacterName} is teleporting {teleportEvent.Target} to {teleportEvent.Position}");
				Core.AuditService.LogMapTeleport(user, teleportEvent.Target, teleportEvent.Position);
			}
		}

		entities.Dispose();
	}
}

[HarmonyLib.HarmonyPatch(typeof(TeleportSystem), "OnUpdate")]
internal class TeleportSystemPatch
{
	public static void Prefix(TeleportSystem __instance)
	{
		var entities = __instance._Query.ToEntityArray(Unity.Collections.Allocator.Temp);

		foreach (var entity in entities)
		{
			var teleportEvent = entity.Read<TeleportDebugEvent>();
			var fromCharacter = entity.Read<FromCharacter>();
			var user = fromCharacter.User.Read<User>();

			if (user.IsAdmin)
			{
				Core.Log.LogInfo($"Admin {user.CharacterName} is teleporting {teleportEvent.Target} to {teleportEvent.Target} {teleportEvent.LocationPosition} {teleportEvent.MousePosition}");
				Core.AuditService.LogTeleport(user, teleportEvent.Target, teleportEvent.MousePosition, teleportEvent.Location, teleportEvent.LocationPosition);
			}
		}

		entities.Dispose();
	}
}

[HarmonyLib.HarmonyPatch(typeof(DebugEventsSystem), "OnUpdate")]
internal class DebugEventsSystemPatch
{
	public static void Prefix(DebugEventsSystem __instance)
	{
		var entities = __instance._Query.ToEntityArray(Unity.Collections.Allocator.Temp);

		foreach (var entity in entities)
		{
			var fromCharacter = entity.Read<FromCharacter>();
			var user = fromCharacter.User.Read<User>();

			// Log out the components
			//var components = Core.EntityManager.GetComponentTypes(entity).ToArray().Select(x => Core.EntityManager.Componx.TypeIndex);

			Core.Log.LogInfo($"Admin {user.CharacterName} is sending a debug event");

			if (entity.Has<DestroyDebugEvent>())
			{
				var destroyEvent = entity.Read<DestroyDebugEvent>();
				Core.AuditService.LogDestroy(user, destroyEvent.What.ToString(), destroyEvent.Where, destroyEvent.PrefabGuid, destroyEvent.Position, destroyEvent.Amount);
			}

			if (entity.Has<GiveDebugEvent>())
			{
				var giveEvent = entity.Read<GiveDebugEvent>();
				Core.AuditService.LogGive(user, giveEvent.PrefabGuid, giveEvent.Amount);
			}

			if (entity.Has<BecomeObserverEvent>())
			{
				var observerEvent = entity.Read<BecomeObserverEvent>();
				Core.AuditService.LogBecomeObserver(user, observerEvent.Mode);
			}

			if (entity.Has<CastleHeartAdminEvent>())
			{
				var castleHeartEvent = entity.Read<CastleHeartAdminEvent>();
				Core.AuditService.LogCastleHeartAdmin(user, castleHeartEvent.EventType, castleHeartEvent.CastleHeart, castleHeartEvent.UserIndex);
			}
		}


		entities.Dispose();
	}
}

[HarmonyLib.HarmonyPatch(typeof(ServerBootstrapSystem), "OnUpdate")]
internal class ServerBootstrapSystemPatch
{
	public static void Prefix(ServerBootstrapSystem __instance)
	{
		var entities = __instance.__query_677018907_5.ToEntityArray(Unity.Collections.Allocator.Temp);

		foreach (var entity in entities)
		{
			var fromCharacter = entity.Read<FromCharacter>();
			var user = fromCharacter.User.Read<User>();

			Core.Log.LogInfo($"Admin {user.CharacterName} is sending a debug event");

			if (entity.Has<DestroyDebugEvent>())
			{
				var destroyEvent = entity.Read<DestroyDebugEvent>();
				Core.AuditService.LogDestroy(user, destroyEvent.What.ToString(), destroyEvent.Where, destroyEvent.PrefabGuid, destroyEvent.Position, destroyEvent.Amount);
			}

			if (entity.Has<GiveDebugEvent>())
			{
				var giveEvent = entity.Read<GiveDebugEvent>();
				Core.AuditService.LogGive(user, giveEvent.PrefabGuid, giveEvent.Amount);
			}

			if (entity.Has<BecomeObserverEvent>())
			{
				var observerEvent = entity.Read<BecomeObserverEvent>();
				Core.AuditService.LogBecomeObserver(user, observerEvent.Mode);
			}

			if (entity.Has<CastleHeartAdminEvent>())
			{
				var castleHeartEvent = entity.Read<CastleHeartAdminEvent>();
				Core.AuditService.LogCastleHeartAdmin(user, castleHeartEvent.EventType, castleHeartEvent.CastleHeart, castleHeartEvent.UserIndex);
			}
		}

		entities.Dispose();
	}
}

