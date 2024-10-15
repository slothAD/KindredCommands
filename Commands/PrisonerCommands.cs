using KindredCommands.Services;
using static KindredCommands.Commands.SpawnCommands;
using VampireCommandFramework;

namespace KindredCommands.Commands;

[CommandGroup("prisoner")]
internal class PrisonerCommands
{
	[Command("gruel", description: "adjust gruel details", adminOnly: true)]
	public static void AdjustGruel(ChatCommandContext ctx, float chance, float min, float max)
	{
		max /= 100;
		min /= 100;
		chance /= 100;
		PrisonerService.GruelChange(chance, min, max);
		Core.ConfigSettings.GruelMutantChance = chance;
		Core.ConfigSettings.GruelBloodMin = min;
		Core.ConfigSettings.GruelBloodMax = max;
		ctx.Reply($"Transform chance set to {chance * 100}%. Blood Quality increase range set to {min * 100}% - {max * 100}%");
	}

	[Command("grueltransform", description: "set the transform for gruel", adminOnly: true)]
	public static void GruelTransform(ChatCommandContext ctx, CharacterUnit prefab)
	{
		PrisonerService.GruelTransform(prefab.Prefab);
		Core.ConfigSettings.GruelTransform = prefab.Prefab;
		ctx.Reply($"Gruel transform set to {prefab.Name}");
	}
}
