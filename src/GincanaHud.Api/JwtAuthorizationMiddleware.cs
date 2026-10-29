using System.Collections.Concurrent;
using System.Reflection;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Middleware;

namespace GincanaHud.Api;

/// <summary>
/// Functions ignore <see cref="AuthorizeAttribute"/>: this middleware enforces it (D21).
/// Validates the panel's JWT with the scheme and policies from <see cref="JwtAuthExtensions"/>.
/// No token or invalid token → 401; valid token without the policy → 403.
/// </summary>
public sealed class JwtAuthorizationMiddleware : IFunctionsWorkerMiddleware
{
	private static readonly ConcurrentDictionary<string, AuthorizeAttribute?> Requirements = new();

	public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
	{
		var requirement = Requirements.GetOrAdd(context.FunctionDefinition.EntryPoint, FindRequirement);
		var http = context.GetHttpContext();
		if (requirement is null || http is null)
		{
			await next(context);
			return;
		}

		var auth = await http.AuthenticateAsync(JwtBearerDefaults.AuthenticationScheme);
		if (!auth.Succeeded)
		{
			http.Response.StatusCode = StatusCodes.Status401Unauthorized;
			return;
		}

		http.User = auth.Principal;
		if (requirement.Policy is not null)
		{
			var authorization = context.InstanceServices.GetRequiredService<IAuthorizationService>();
			var result = await authorization.AuthorizeAsync(auth.Principal, requirement.Policy);
			if (!result.Succeeded)
			{
				http.Response.StatusCode = StatusCodes.Status403Forbidden;
				return;
			}
		}

		await next(context);
	}

	// EntryPoint is "Namespace.Type.Method"; every function lives in this assembly.
	private static AuthorizeAttribute? FindRequirement(string entryPoint)
	{
		var lastDot = entryPoint.LastIndexOf('.');
		var type = typeof(JwtAuthorizationMiddleware).Assembly.GetType(entryPoint[..lastDot], throwOnError: true)!;
		var method = type.GetMethod(entryPoint[(lastDot + 1)..], BindingFlags.Public | BindingFlags.Instance)!;
		return method.GetCustomAttribute<AuthorizeAttribute>() ?? type.GetCustomAttribute<AuthorizeAttribute>();
	}
}
