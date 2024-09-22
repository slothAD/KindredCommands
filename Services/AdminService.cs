using ProjectM.Network;
using Unity.Entities;

namespace KindredCommands.Services;
internal class AdminService
{
	public static void AdminUser(Entity userEntity)
	{
		var user = userEntity.Read<User>();
		var entity = Core.EntityManager.CreateEntity(
			ComponentType.ReadWrite<FromCharacter>(),
			ComponentType.ReadWrite<AdminAuthEvent>()
		);
		entity.Write(new FromCharacter()
		{
			Character = user.LocalCharacter.GetEntityOnServer(),
			User = userEntity
		});
	}
}
