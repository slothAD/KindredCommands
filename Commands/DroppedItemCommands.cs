using Unity.Transforms;
using VampireCommandFramework;

namespace KindredCommands.Commands;

[CommandGroup("dropitems", "Commands for managing dropped items.")]
internal class DroppedItemCommands
{
	//reduce the time a dropped item stays on the ground
	[Command("lifetime", "lt", description: "Sets the lifetime of dropped items in seconds.", adminOnly: true)]
	public static void SetDroppedItemLifetime(ChatCommandContext ctx, int seconds)
	{
		if (seconds < 0)
		{
			throw ctx.Error("Lifetime must be a positive number.");
		}
		Core.DropItem.SetDroppedItemLifetime(seconds);
		ctx.Reply($"掉落物存續時間設為 {seconds} 秒。");
	}

	[Command("removelifetime", "rlt", description:"Removes the lifetime of dropped items.", adminOnly: true)]
	public static void RemoveDroppedItemLifetime(ChatCommandContext ctx)
	{
		Core.DropItem.RemoveDroppedItemLifetime();
		ctx.Reply("掉落物的存續時間已取消。");
	}

	[Command("lifetimewhendisabled", "ltwd", description:"Sets the lifetime of dropped items when disabled in seconds.", adminOnly: true)]
	public static void SetDroppedItemLifetimeWhenDisabled(ChatCommandContext ctx, int seconds=300)
	{
		if (seconds < 0)
		{
			throw ctx.Error("Lifetime must be a positive number.");
		}
		Core.DropItem.SetDroppedItemLifetimeWhenDisabled(seconds);
		ctx.Reply($"掉落物禁用狀態下的存續時間設為 {seconds} 秒。");
	}

	[Command("shardlifetime", "slt", description: "Sets the lifetime of dropped shards when disabled in seconds.", adminOnly: true)]
	public static void SetDroppedShardLifetimeWhenDisabled(ChatCommandContext ctx, int seconds=3600)
	{
		if (seconds < 0)
		{
			throw ctx.Error("Lifetime must be a positive number.");
		}
		Core.DropItem.SetDroppedShardLifetime(seconds);
		ctx.Reply($"靈魂碎片存續時間設為 {seconds} 秒。");
	}

	//remove dropped items around the player in a radius
	[Command("clear", "c", description: "Clears all dropped items within a radius of the player.", adminOnly: true)]
	public static void ClearDroppedItems(ChatCommandContext ctx, float radius)
	{
		var pos = ctx.Event.SenderCharacterEntity.Read<Translation>().Value;
		var cleared = Core.DropItem.ClearDropItemsInRadius(pos, radius);
		ctx.Reply($"已清除半徑 {radius} 內的 {cleared} 個掉落物。");
	}

	[Command("clearall", "ca", description: "Clears all dropped items in the world.", adminOnly: true)]
	public static void ClearAllDroppedItems(ChatCommandContext ctx)
	{
		var cleared = Core.DropItem.ClearDropItems();
		ctx.Reply($"已清除世界中所有 {cleared} 個掉落物。");
	}

	[Command("clearshards", "cs", description: "Clears all dropped shards within a radius of the player.", adminOnly: true)]
	public static void ClearDroppedShards(ChatCommandContext ctx, float radius)
	{
		var pos = ctx.Event.SenderCharacterEntity.Read<Translation>().Value;
		var cleared = Core.DropItem.ClearDropShardsInRadius(pos, radius);
		ctx.Reply($"已清除半徑 {radius} 內的 {cleared} 個靈魂碎片。");
	}

	[Command("clearallshards", "cas", description: "Clears all dropped shards in the world.", adminOnly: true)]
	public static void ClearAllDroppedShards(ChatCommandContext ctx)
	{
		var cleared = Core.DropItem.ClearDropShards();
		ctx.Reply($"已清除世界中所有 {cleared} 個掉落的靈魂碎片。");
	}
}
