using System.Collections.Generic;
using System.Text;
using KindredCommands.Commands.Converters;
using KindredCommands.Models;
using KindredCommands.Services;
using ProjectM;
using ProjectM.Network;
using Unity.Collections;
using Unity.Entities;
using VampireCommandFramework;

namespace KindredCommands.Commands;

internal class StaffCommands
{
	[Command("staff", description: "Shows online Staff members.", adminOnly: false)]
	public static void WhoIsOnline(ChatCommandContext ctx)
	{
		var users = PlayerService.GetUsersOnline();
		var staff = Database.GetStaff();

		StringBuilder builder = new();
		foreach (var user in users)
		{
			Player player = new(user);
			foreach (KeyValuePair<string, string> _kvp in staff)
			{
				if (player.SteamID.ToString() == _kvp.Key)
				{
					string _role = _kvp.Value.Replace("</color>", "");
					builder
						.Append(_role)
						.Append(player.Name)
						.Append("</color>");
					builder.Append(' ');
				}
			}
		}
		if (builder.Length == 0)
		{
			ctx.Reply("目前沒有任何工作人員在線上。");
			return;
		}
		ctx.Reply($"線上工作人員：{builder}");
	}
	[Command("reloadstaff", description: "Reloads the staff config.", adminOnly: true)]
	public static void ReloadStaff(ChatCommandContext ctx)
	{
		Database.InitConfig();
		ctx.Reply("工作人員設定已重新載入！");
	}

	[Command("setstaff", description: "Sets someones staff rank.", adminOnly: true)]
	public static void AddStaff(ChatCommandContext ctx, FoundPlayer player, string rank)
	{
		var userEntity = player.Value.UserEntity;
		var rankname = "[" + rank + "]";

		Database.SetStaff(userEntity, rankname);
		ctx.Reply("已設置工作人員！");
	}

	[Command("removestaff", description: "Removes someones staff rank.", adminOnly: true)]
	public static void RemoveStaff(ChatCommandContext ctx, FoundPlayer player)
	{
		var userEntity = player.Value.UserEntity;

		if (Database.RemoveStaff(userEntity))
			ctx.Reply("已移除該工作人員！");
		else
			ctx.Reply("找不到該工作人員！");
	}

	public static AdminAuthSystem adminAuthSystem = Core.Server.GetExistingSystemManaged<AdminAuthSystem>();
	[Command("reloadadmin", description: "Reloads the admin list.", adminOnly: true)]
	public static void ReloadCommand(ChatCommandContext ctx)
	{
		adminAuthSystem._LocalAdminList.Save();
		adminAuthSystem._LocalAdminList.Refresh();
		ctx.Reply("管理員清單已重新載入！");
	}

	[Command("toggleadmin", description: "Adds/Removes a player to the admin list, authing and deauthing.", adminOnly: true)]
	public static void ToggleAdminCommand(ChatCommandContext ctx, FoundPlayer player)
	{
		var userEntity = player.Value.UserEntity;
		var user = userEntity.Read<User>();
		var platformId = user.PlatformId;

		if (adminAuthSystem._LocalAdminList.Contains(platformId))
		{
			ctx.Reply($"已撤銷 {player.Value.CharacterName} 的管理員權限");
			adminAuthSystem._LocalAdminList.Remove(platformId);

			if (userEntity.Has<AdminUser>())
			{
				userEntity.Remove<AdminUser>();
			}

			user.IsAdmin = false;
			userEntity.Write(user);

			var archetype = Core.EntityManager.CreateArchetype(new ComponentType[]
			{
					ComponentType.ReadWrite<FromCharacter>(),
					ComponentType.ReadWrite<DeauthAdminEvent>()
			});

			var entity = Core.EntityManager.CreateEntity(archetype);
			entity.Write(new FromCharacter()
			{
				Character = player.Value.CharEntity,
				User = userEntity
			});

			FixedString512Bytes message = "You were removed as admin and deauthed";
			ServerChatUtils.SendSystemMessageToClient(Core.EntityManager, user, ref message);

		}
		else
		{
			ctx.Reply($"已授權管理員權限給 {player.Value.CharacterName}");
			adminAuthSystem._LocalAdminList.Add(platformId);

			AdminService.AdminUser(userEntity);

			FixedString512Bytes message = "You were added as admin and authed";
			ServerChatUtils.SendSystemMessageToClient(Core.EntityManager, user, ref message);
		}

		adminAuthSystem._LocalAdminList.Save();
	}

	[Command("autoadminauth", description: "Adds/Removes yourself from the auto AdminAuth list", adminOnly: true)]
	public static void AddAutoAuthAdmin(ChatCommandContext ctx)
	{
		if (Database.GetAutoAdmin().Contains(ctx.Event.SenderUserEntity.Read<User>().PlatformId.ToString()))
		{
			Database.RemoveAutoAdmin(ctx.Event.SenderUserEntity);
			ctx.Reply("你登入後將不再自動獲得管理員授權。");
			return;
		}
		else
		{
			Database.SetAutoAdmin(ctx.Event.SenderUserEntity);
			ctx.Reply("你登入後將自動獲得管理員授權。");
		}
	}
}
