using System.Reflection;

namespace GincanaHud.Api.Common.Messaging;

public static class MessagingServiceCollectionExtensions
{
	public static IServiceCollection AddApiMediator(this IServiceCollection services, params Assembly[] assemblies)
	{
		services.AddScoped<ISender, Sender>();

		var targets = assemblies.Length > 0
			? assemblies
			: [typeof(MessagingServiceCollectionExtensions).Assembly];

		var handlerOpen = typeof(IRequestHandler<,>);

		foreach (var assembly in targets)
		{
			foreach (var type in assembly.GetTypes())
			{
				if (type is not { IsClass: true, IsAbstract: false })
					continue;

				foreach (var iface in type.GetInterfaces())
				{
					if (!iface.IsGenericType || iface.GetGenericTypeDefinition() != handlerOpen)
						continue;

					services.AddScoped(iface, type);
				}
			}
		}

		return services;
	}
}
