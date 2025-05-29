using VampireCommandFramework;

namespace KindredCommands.Commands.BloodBound;

/// <summary>
/// Commands adding/removing entities from blood-bound category.
/// </summary>
[CommandGroup("bloodbound", "blb")]
public static class BloodBoundCommands
{
	/// <summary>
	/// Adds an entity to blood-bound category.
	/// </summary>
	/// <param name="ctx">Command context.</param>
	/// <param name="descriptor">Command parameter. See <see cref="BloodBoundItemParameterConverter"/>.</param>
	[Command("add", "a", "<Prefab GUID or name>", description: "Adds Blood-Bound attribute to items", adminOnly: true)]
	public static void AddBloodBound(ChatCommandContext ctx, BloodBoundItemParameter descriptor)
	{
		if (Core.BloodBoundService.SetBloodBound(descriptor.Prefab, descriptor.Entity, true))
		{
			Core.ConfigSettings.SetBloodBound(descriptor.Name, true);
			ctx.Reply($"已為 {descriptor.Name} 添加血契屬性");
		}
		else
		{
			ctx.Reply($"{descriptor.Name} 已擁有血契屬性。");
		}
	}

	/// <summary>
	/// Removes an entity from blood-bound category.
	/// </summary>
	/// <param name="ctx">Command context.</param>
	/// <param name="descriptor">Command parameter. See <see cref="BloodBoundItemParameterConverter"/>.</param>
	[Command("remove", "r", "<Prefab GUID or name>", description: "Removes Blood-Bound attribute from items", adminOnly: true)]
	public static void RemoveBloodBound(ChatCommandContext ctx, BloodBoundItemParameter descriptor)
	{
		if (Core.BloodBoundService.SetBloodBound(descriptor.Prefab, false))
		{
			Core.ConfigSettings.SetBloodBound(descriptor.Name, false);
			ctx.Reply($"已移除 {descriptor.Name} 的血契屬性");
		}
		else
		{
			ctx.Reply($"{descriptor.Name} 尚未擁有血契屬性。");
		}
	}
}
