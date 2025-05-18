using System;
using System.Collections.Concurrent;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HarmonyLib;
using ProjectM.Network;
using Stunlock.Core;
using Unity.Mathematics;
using VampireCommandFramework;

namespace KindredCommands.Services;

record CommandMetadata(CommandAttribute Attribute, Assembly Assembly, MethodInfo Method, ConstructorInfo Constructor, ParameterInfo[] Parameters, Type ContextType, Type ConstructorType, CommandGroupAttribute GroupAttribute);

class AuditMiddleware : CommandMiddleware
{
	public override void BeforeExecute(ICommandContext ctx, CommandAttribute attribute, MethodInfo method)
	{
		var chatCommandContext = (ChatCommandContext)ctx;

		var commandName = method.DeclaringType.Assembly.GetName().Name;

		if (method.DeclaringType.IsDefined(typeof(CommandGroupAttribute)))
		{
			var groupAttribute = (CommandGroupAttribute)Attribute.GetCustomAttribute(method.DeclaringType, typeof(CommandGroupAttribute), false);
			commandName += "." + groupAttribute.Name;
		}

		commandName += "." + attribute.Name;

		Core.AuditService.LogCommandUsage(chatCommandContext.Event.User, commandName);
	}
}

internal class AuditService : IDisposable
{
	static readonly string CONFIG_PATH = Path.Combine(BepInEx.Paths.ConfigPath, MyPluginInfo.PLUGIN_NAME);
	static readonly string AUDIT_PATH = Path.Combine(CONFIG_PATH, $"audit-{DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss")}.csv");

	// Define the datetime format with milliseconds once for consistency
	private const string DATE_TIME_FORMAT = "yyyy-MM-dd HH:mm:ss.fff";

	private readonly ConcurrentQueue<string> _auditQueue = new();
	private readonly AutoResetEvent _signal = new(false);
	private readonly Task _processingTask;
	private bool _isDisposed = false;
	private readonly CancellationTokenSource _cancellationTokenSource = new();

	public AuditService()
	{
		var canCommandExecuteMethod = AccessTools.Method(typeof(CommandRegistry), "CanCommandExecute");
		var postfix = new HarmonyMethod(typeof(AuditService), nameof(CanCommandExecutePostfix));
		Plugin.Harmony.Patch(canCommandExecuteMethod, postfix: postfix);

		var auditMiddleware = new AuditMiddleware();
		CommandRegistry.Middlewares.Add(auditMiddleware);

		// Make sure directory exists
		Directory.CreateDirectory(CONFIG_PATH);

		// Start the background processing task
		_processingTask = Task.Run(ProcessQueueAsync, _cancellationTokenSource.Token);
	}

	private async Task ProcessQueueAsync()
	{
		while (!_cancellationTokenSource.Token.IsCancellationRequested)
		{
			// Wait for signal or timeout (checking periodically helps ensure we eventually exit)
			await Task.Run(() => _signal.WaitOne(TimeSpan.FromSeconds(1)), _cancellationTokenSource.Token);

			// Process all current items in the queue
			while (_auditQueue.TryDequeue(out var message) && !_cancellationTokenSource.Token.IsCancellationRequested)
			{
				try
				{
					// Write to file
					File.AppendAllText(AUDIT_PATH, message);
				}
				catch (Exception ex)
				{
					// Log error but continue processing
					Console.WriteLine($"Error writing to audit log: {ex.Message}");
				}
			}
		}
	}

	static void CanCommandExecutePostfix(ICommandContext ctx, CommandMetadata command, ref bool __result)
	{
		if (!__result)
		{
			// Check if we're being called from ExecuteCommandWithArgs
			bool isFromExecuteCommandWithArgs = false;
			var stackTrace = new System.Diagnostics.StackTrace();

			// Iterate through the stack frames to find if ExecuteCommandWithArgs is in the call chain
			for (int i = 0; i < Math.Min(stackTrace.FrameCount, 7); i++)
			{
				var frame = stackTrace.GetFrame(i);
				var method = frame.GetMethod();

				if (method != null &&
					method.Name == "ExecuteCommandWithArgs" &&
					method.DeclaringType != null &&
					method.DeclaringType.Name == "CommandRegistry")
				{
					isFromExecuteCommandWithArgs = true;
					break;
				}
			}

			// Only log rejected commands when they come from ExecuteCommandWithArgs
			if (isFromExecuteCommandWithArgs)
			{
				var chatCommandContext = (ChatCommandContext)ctx;

				var commandName = command.Assembly.GetName().Name;
				if (command.GroupAttribute != null)
				{
					commandName += "." + command.GroupAttribute.Name;
				}
				commandName += "." + command.Attribute.Name;

				Core.AuditService.LogRejectCommand(chatCommandContext.User, commandName);
			}
		}
	}

	public void LogChatMessage(User fromUser, User toUser, ChatMessageType type, string message)
	{
		var sb = new StringBuilder();
		sb.Append(DateTime.Now.ToString(DATE_TIME_FORMAT));
		sb.Append(",");
		sb.Append("chat");
		sb.Append(",");
		sb.Append(fromUser.PlatformId);
		sb.Append(",");
		sb.Append(fromUser.CharacterName);
		sb.Append(",");
		sb.Append(type);
		sb.Append(",");
		sb.Append(toUser.PlatformId);
		sb.Append(",");
		sb.Append(toUser.CharacterName);
		sb.Append(",");
		sb.Append(message);
		sb.Append("\n");
		AddAuditString(sb);
	}

	public void LogCommandUsage(User user, string command)
	{
		var sb = new StringBuilder();
		sb.Append(DateTime.Now.ToString(DATE_TIME_FORMAT));
		sb.Append(",");
		sb.Append("command");
		sb.Append(",");
		sb.Append(user.PlatformId);
		sb.Append(",");
		sb.Append(user.CharacterName);
		sb.Append(",");
		sb.Append(command);
		sb.Append("\n");
		AddAuditString(sb);
	}

	public void LogRejectCommand(User user, string commandName)
	{
		var sb = new StringBuilder();
		sb.Append(DateTime.Now.ToString(DATE_TIME_FORMAT));
		sb.Append(",");
		sb.Append("rejectCommand");
		sb.Append(",");
		sb.Append(user.PlatformId);
		sb.Append(",");
		sb.Append(user.CharacterName);
		sb.Append(",");
		sb.Append(commandName);
		sb.Append("\n");
		AddAuditString(sb);
	}

	public void LogMapTeleport(User user, PlayerTeleportDebugEvent.TeleportTarget teleportTarget, float3 destination)
	{
		var sb = new StringBuilder();
		sb.Append(DateTime.Now.ToString(DATE_TIME_FORMAT));
		sb.Append(",");
		sb.Append("teleportMap");
		sb.Append(",");
		sb.Append(user.PlatformId);
		sb.Append(",");
		sb.Append(user.CharacterName);
		sb.Append(",");
		sb.Append(teleportTarget);
		sb.Append(",");
		sb.Append(destination);
		sb.Append("\n");
		AddAuditString(sb);
	}

	public void LogTeleport(User user, TeleportDebugEvent.TeleportTarget teleportTarget, float3 mousePosition,
		TeleportDebugEvent.TeleportLocation location, float3 locationPosition)
	{
		var sb = new StringBuilder();
		sb.Append(DateTime.Now.ToString(DATE_TIME_FORMAT));
		sb.Append(",");
		sb.Append("teleport");
		sb.Append(",");
		sb.Append(user.PlatformId);
		sb.Append(",");
		sb.Append(user.CharacterName);
		sb.Append(",");
		sb.Append(teleportTarget);
		sb.Append(",");
		sb.Append(mousePosition);
		sb.Append(",");
		sb.Append(location);
		sb.Append(",");
		sb.Append(locationPosition);
		sb.Append("\n");
		AddAuditString(sb);
	}

	public void LogDestroy(User user, string what, DestroyDebugEvent.DestroyWhere where, PrefabGUID prefabGuid, float3 position, int amount)
	{
		var sb = new StringBuilder();
		sb.Append(DateTime.Now.ToString(DATE_TIME_FORMAT));
		sb.Append(",");
		sb.Append("destroy");
		sb.Append(",");
		sb.Append(user.PlatformId);
		sb.Append(",");
		sb.Append(user.CharacterName);
		sb.Append(",");
		sb.Append(what);
		sb.Append(",");
		sb.Append(where);
		sb.Append(",");
		sb.Append(prefabGuid.LookupName());
		sb.Append(",");
		sb.Append(position);
		sb.Append(",");
		sb.Append(amount);
		sb.Append("\n");
		AddAuditString(sb);
	}

	public void LogGive(User user, PrefabGUID prefabGuid, int amount)
	{
		var sb = new StringBuilder();
		sb.Append(DateTime.Now.ToString(DATE_TIME_FORMAT));
		sb.Append(",");
		sb.Append("give");
		sb.Append(",");
		sb.Append(user.PlatformId);
		sb.Append(",");
		sb.Append(user.CharacterName);
		sb.Append(",");
		sb.Append(prefabGuid.LookupName());
		sb.Append(",");
		sb.Append(amount);
		sb.Append("\n");
		AddAuditString(sb);
	}

	public void LogBecomeObserver(User user, int mode)
	{
		var sb = new StringBuilder();
		sb.Append(DateTime.Now.ToString(DATE_TIME_FORMAT));
		sb.Append(",");
		sb.Append("becomeObserver");
		sb.Append(",");
		sb.Append(user.PlatformId);
		sb.Append(",");
		sb.Append(user.CharacterName);
		sb.Append(",");
		sb.Append(mode);
		sb.Append("\n");
		AddAuditString(sb);
	}

	public void LogCastleHeartAdmin(User user, CastleHeartInteractEventType eventType, NetworkId castleHeart, int userIndex)
	{
		var sb = new StringBuilder();
		sb.Append(DateTime.Now.ToString(DATE_TIME_FORMAT));
		sb.Append(",");
		sb.Append("castleHeartAdmin");
		sb.Append(",");
		sb.Append(user.PlatformId);
		sb.Append(",");
		sb.Append(user.CharacterName);
		sb.Append(",");
		sb.Append(eventType);
		sb.Append(",");
		sb.Append(castleHeart);
		sb.Append(",");
		sb.Append(userIndex);
		sb.Append("\n");
		AddAuditString(sb);
	}

	void AddAuditString(StringBuilder sb)
	{
		if (_isDisposed) return;

		// Add the message to the queue
		_auditQueue.Enqueue(sb.ToString());

		// Signal that there's work to do
		_signal.Set();
	}

	public void Dispose()
	{
		if (_isDisposed) return;
		_isDisposed = true;

		// Cancel the processing task
		_cancellationTokenSource.Cancel();

		// Wait for processing to complete
		try
		{
			_processingTask.Wait(TimeSpan.FromSeconds(5));
		}
		catch (TaskCanceledException)
		{
			// This is expected
		}

		// Dispose managed resources
		_cancellationTokenSource.Dispose();
		_signal.Dispose();
	}
}
