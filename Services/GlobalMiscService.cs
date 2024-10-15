using KindredCommands.Data;
using ProjectM.Gameplay.Scripting;

namespace KindredCommands.Services;
internal class GlobalMiscService
{
	public GlobalMiscService()
	{
		SetBatVisionState(Core.ConfigSettings.BatVision);
	}

	public bool ToggleBatVision()
	{
		Core.ConfigSettings.BatVision = !Core.ConfigSettings.BatVision;
		SetBatVisionState(Core.ConfigSettings.BatVision);
		return Core.ConfigSettings.BatVision;
	}

	void SetBatVisionState(bool enabled)
	{
		if (Core.PrefabCollectionSystem._PrefabGuidToEntityMap.TryGetValue(Prefabs.AB_Shapeshift_Bat_TakeFlight_Buff, out var prefabEntity))
		{
			var data = prefabEntity.Read<Script_SetFlyingHeightVision_Buff_DataShared>();
			data.BuffActive = false;
			data.Delay = float.MaxValue;
			prefabEntity.Write(data);
		}
	}
}
