using System;
using ProjectM.Network;
using VampireCommandFramework;

namespace KindredCommands.Commands;
internal static class PingCommands
{
	[Command("ping", shortHand: "p", description: "Shows your latency.")]
	public static void PingCommand(ChatCommandContext ctx, string mode = "")
	{
		if (mode is null)
		{
			throw new ArgumentNullException(nameof(mode));
		}

		var ping = ctx.Event.SenderCharacterEntity.Read<Latency>().Value * 1000;
		ctx.Reply($"你向虛空的呼喚迴盪四周，在 <color=#FFFE0F>{ping:0}</color> 毫秒內回響於耳。");
	}
}
