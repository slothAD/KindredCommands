using KindredCommands.Data;
using ProjectM;
using ProjectM.Shared;
using VampireCommandFramework;

namespace KindredCommands.Commands;
internal class AFKCommands
{
	[Command("afk", description: "AFK animation, locking WASD movement. Use again to remove.", adminOnly: false)]
	public static void AFKCommand(ChatCommandContext ctx)
	{
		//"AB_Bear_FallAsleep_SleepingIdle_Buff": -883762685 - gives the ZZzzz animation and locks player in place, spells still work
		if (!BuffUtility.TryGetBuff(Core.EntityManager, ctx.Event.SenderCharacterEntity, Prefabs.AB_Bear_FallAsleep_SleepingIdle_Buff, out var buffEntity))
		{
			Buffs.AddBuff(ctx.Event.SenderUserEntity, ctx.Event.SenderCharacterEntity, Prefabs.AB_Bear_FallAsleep_SleepingIdle_Buff, -1, false);
			ctx.Reply("你現在進入掛機狀態！");
		}
		else
		{
			DestroyUtility.Destroy(Core.EntityManager, buffEntity, DestroyDebugReason.TryRemoveBuff);
			ctx.Reply("你已解除掛機狀態！");
		}
	}
}
