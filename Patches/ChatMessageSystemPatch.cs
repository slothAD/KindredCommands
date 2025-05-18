using HarmonyLib;
using ProjectM;
using ProjectM.Network;
using Unity.Collections;

namespace KindredCommands.Patches;

[HarmonyBefore("gg.deca.VampireCommandFramework")]
[HarmonyPatch(typeof(ChatMessageSystem), nameof(ChatMessageSystem.OnUpdate))]
internal class ChatMessageSystemPatch
{
	public static void Prefix(ChatMessageSystem __instance)
	{
		var entities = __instance.__query_661171423_0.ToEntityArray(Allocator.Temp);
		foreach (var entity in entities)
		{
			var fromData = entity.Read<FromCharacter>();
			var userData = fromData.User.Read<User>();
			var chatEventData = entity.Read<ChatMessageEvent>();
			var messageText = chatEventData.MessageText.ToString();

			User toUser = default;
			if (Core.Players.TryFindUserFromNetworkId(chatEventData.ReceiverEntity, out var toUserEntity))
			{
				toUser = toUserEntity.Read<User>();
			}

			Core.AuditService.LogChatMessage(userData, toUser, chatEventData.MessageType, messageText);
		}
		entities.Dispose();
	}
}
