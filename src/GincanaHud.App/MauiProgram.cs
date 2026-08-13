using CommunityToolkit.Maui;
using GincanaHud.App.Services;
using GincanaHud.App.ViewModels;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Logging;
using ZXing.Net.Maui.Controls;

namespace GincanaHud.App;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.UseMauiCommunityToolkit()
			.UseMauiCommunityToolkitCamera()
			.UseBarcodeReader()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
				fonts.AddFont("ChakraPetch-Regular.ttf", "ChakraPetch");
				fonts.AddFont("ChakraPetch-Bold.ttf", "ChakraPetchBold");
			});

		var apiOptions = new ApiOptions
		{
			// Apunta a tu Api: Dev Tunnel, IP de LAN o URL publicada.
			// Emulador Android → host: http://10.0.2.2:<puerto>/
			BaseUrl = "https://localhost:7202/",
			PlayerName = "jugador"
		};
		builder.Services.AddSingleton(apiOptions);
		builder.Services.AddSingleton<IJoinSessionStore, PreferencesJoinSessionStore>();
		builder.Services.AddSingleton<IPlayerSettings, PreferencesPlayerSettings>();
		builder.Services.AddSingleton<IAppNavigator, AppNavigator>();
		builder.Services.AddSingleton<IGameplayLauncher, GameplayLauncher>();

		builder.Services
			.AddHttpClient<IGincanaApiClient, GincanaApiClient>(client =>
			{
				client.BaseAddress = new Uri(apiOptions.BaseUrl);
				client.Timeout = TimeSpan.FromSeconds(100);
			})
			.AddStandardResilienceHandler();

		// Cliente ligero para el indicador (sin reintentos largos del resilience handler).
		builder.Services.AddHttpClient("api-health", client =>
		{
			client.BaseAddress = new Uri(apiOptions.BaseUrl);
			client.Timeout = TimeSpan.FromSeconds(5);
		});
		builder.Services.AddSingleton<IApiHealthMonitor>(sp =>
			new ApiHealthMonitor(
				sp.GetRequiredService<IHttpClientFactory>().CreateClient("api-health"),
				sp.GetService<ILogger<ApiHealthMonitor>>()));

		builder.Services.AddSingleton<ILocationService, MauiLocationService>();
		builder.Services.AddSingleton<ICompassService, MauiCompassService>();
		builder.Services.AddSingleton<IOrientationService, MauiOrientationService>();
		builder.Services.AddSingleton<ICaptureFxService, CaptureFxService>();
		builder.Services.AddSingleton<ICaptureQueue, PreferencesCaptureQueue>();
		builder.Services.AddSingleton<ICaptureSyncService, CaptureSyncService>();
		builder.Services.AddSingleton<JoinViewModel>();
		builder.Services.AddSingleton<HudViewModel>();
		builder.Services.AddTransient<FinishViewModel>();
		builder.Services.AddSingleton<HudPage>();
		builder.Services.AddSingleton<JoinPage>();
		builder.Services.AddTransient<ScanPage>();
		builder.Services.AddTransient<FinishPage>();
#if DEBUG
		builder.Services.AddTransient<SensorDebugViewModel>();
		builder.Services.AddTransient<MainPage>();
#endif
		builder.Services.AddSingleton<AppRoot>();

#if DEBUG
		builder.Logging.AddDebug();
#endif

		return builder.Build();
	}
}
