using KindredCommands.Services;
using static KindredCommands.Commands.SpawnCommands;
using VampireCommandFramework;
using Stunlock.Core;
using KindredCommands.Data;
using KindredCommands.Commands.Converters;

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
		ctx.Reply($"轉化機率設為 {chance * 100}%｜血液品質提升範圍為 {min * 100}% - {max * 100}%");
	}

	[Command("grueltransform", description: "set the transform for gruel", adminOnly: true)]
	public static void GruelTransform(ChatCommandContext ctx, CharacterUnit prefab)
	{
		PrisonerService.GruelTransform(prefab.Prefab);
		Core.ConfigSettings.GruelTransform = prefab.Prefab;
		ctx.Reply($"變異食物轉化設為 {prefab.Name}");
	}
	[Command("gruelsettings", description: "show current gruel settings", adminOnly: true)]
    public static void ShowGruelSettings(ChatCommandContext ctx)
    {
        var prefabInt = Core.ConfigSettings.GruelTransform;
		var prefabInt2 = Character.NameFromPrefab[prefabInt.GuidHash];
		var prefabName = Character.NameFromPrefab.TryGetValue(prefabInt.GuidHash, out var name) ? name : prefabInt2;

		ctx.Reply($"變異機率：{Core.ConfigSettings.GruelMutantChance * 100}%｜血液品質提升範圍：{Core.ConfigSettings.GruelBloodMin * 100}% - {Core.ConfigSettings.GruelBloodMax * 100}%｜轉化為：{prefabName}");
    }

	[Command("feed", description: "Change one of the feed items that isn't gruel", adminOnly: true)]
	public static void ChangeFeed(ChatCommandContext ctx, FoundPrisonerFeed feed,
		                          float healthChangeMin, float healthChangeMax,
								  float miseryChangeMin, float miseryChangeMax,
								  float bloodQualityChangeMin, float bloodQualityChangeMax)
	{
		Core.Prisoners.ChangeFeed(feed.Value, healthChangeMin, healthChangeMax, miseryChangeMin, miseryChangeMax, bloodQualityChangeMin, bloodQualityChangeMax);
		ctx.Reply($"已修改 {feed.Name} 的設定");
	}

	[Command("feeddefault", description: "Restores a feed prisoner to default settings", adminOnly: true)]
	public static void DefaultFeed(ChatCommandContext ctx, FoundPrisonerFeed feed)
	{
		Core.Prisoners.ResetToDefault(feed.Value);
		ctx.Reply($"{feed.Name} 的餵食設定已重置為預設值");
	}

	[Command("feedsettings", description: "Show settings of a feed prisoner", adminOnly: true)]
	public static void FeedSettings(ChatCommandContext ctx, FoundPrisonerFeed feed)
	{
		if (Core.ConfigSettings.PrisonerFeeds.TryGetValue(feed.Value._Value, out var prisonerFeed))
		{
			ctx.Reply($"{feed.Name} 的囚犯餵食設定：
生命變化：{prisonerFeed.HealthChangeMin} - {prisonerFeed.HealthChangeMax}
痛苦值變化：{prisonerFeed.MiseryChangeMin} - {prisonerFeed.MiseryChangeMax}
血液品質變化：{prisonerFeed.BloodQualityChangeMin} - {prisonerFeed.BloodQualityChangeMax}");
		}
		else if (Core.Prisoners.defaultPrisonerFeeds.TryGetValue(feed.Value, out prisonerFeed))
		{

			ctx.Reply($"{feed.Name} 的囚犯餵食設定為預設值：
生命變化：{prisonerFeed.HealthChangeMin} - {prisonerFeed.HealthChangeMax}
痛苦值變化：{prisonerFeed.MiseryChangeMin} - {prisonerFeed.MiseryChangeMax}
血液品質變化：{prisonerFeed.BloodQualityChangeMin} - {prisonerFeed.BloodQualityChangeMax}");
		}
		else
		{
			ctx.Reply($"{feed.Name} 的囚犯餵食設定未知");
		}
	}
}
