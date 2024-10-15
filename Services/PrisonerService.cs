using ProjectM;
using Stunlock.Core;
using UnityEngine;
using VampireCommandFramework;
using static KindredCommands.Commands.SpawnCommands;

namespace KindredCommands.Services;
internal class PrisonerService
{
	public PrisonerService()
	{
		GruelChange(Core.ConfigSettings.GruelMutantChance, Core.ConfigSettings.GruelBloodMin, Core.ConfigSettings.GruelBloodMax);
		GruelTransform(Core.ConfigSettings.GruelTransform);
	}

	public static void GruelChange(float chance, float min, float max)
    {
		max = Mathf.Clamp(max, min, 1);
		min = Mathf.Clamp(min, 0, max);
		if (max < min) max = min;
		var gruelChance = Helper.GetEntitiesByComponentType<AffectPrisonerWithToxic>(true);
        foreach (var entity in gruelChance)
        {
            var affect = entity.Read<AffectPrisonerWithToxic>();
			affect.ChanceToBecomeMutant = chance;			
			affect.IncreaseBloodQuality_Max = max;
            affect.IncreaseBloodQuality_Min = min;
            entity.Write(affect);
        }
        gruelChance.Dispose();
    }

	public static void GruelTransform(PrefabGUID prefab)
	{
		var gruelTransform = Helper.GetEntitiesByComponentType<AffectPrisonerWithToxic>(true);
		foreach (var entity in gruelTransform)
		{
			var affect = entity.Read<AffectPrisonerWithToxic>();
			affect.MutantType = prefab;
			entity.Write(affect);
		}
		gruelTransform.Dispose();
	}

}
