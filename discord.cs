using System;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Plantillabot.Handlers;
using Plantillabot.Services;

namespace Plantillabot
{
    /// <summary>
    /// Clase principal del bot de Discord. Configura la inyección de dependencias
    /// y gestiona el ciclo de vida de la conexión en el Heap ("memoria dinámica").
    /// </summary>
    public class Program
    {
        private DiscordSocketClient _client = null!;

        public static async Task Main(string[] args)
        {
            var program = new Program();
            await program.RunAsync();
        }

        public async Task RunAsync()
        {
            // 1. Configurar e instanciar los servicios requeridos de forma modular en el Heap.
            var services = ConfigureServices();

            // 2. Obtener y cargar el servicio de configuración.
            var config = services.GetRequiredService<IConfigService>();
            await config.LoadAsync();

            // 3. Obtener el cliente de Discord e inicializar el manejador de interacciones.
            _client = services.GetRequiredService<DiscordSocketClient>();
            
            // Configurar el log para depuración y monitoreo.
            _client.Log += LogAsync;

            // Inicializar el manejador de interacciones (Slash commands y Botones).
            var interactionHandler = services.GetRequiredService<IInteractionHandler>();
            await interactionHandler.InitializeAsync();

            // 4. Iniciar sesión y conectar el bot con la API de Discord.
            await _client.LoginAsync(TokenType.Bot, config.Token);
            await _client.StartAsync();

            // Mantener la aplicación en ejecución indefinidamente en un bucle asíncrono.
            await Task.Delay(Timeout.Infinite);
        }

        /// <summary>
        /// Configura el contenedor de inyección de dependencias ("Dependency Injection").
        /// </summary>
        /// <returns>El proveedor de servicios construido.</returns>
        private IServiceProvider ConfigureServices()
        {
            // Se activan los GatewayIntents necesarios para interactuar con canales, miembros y mensajes.
            var socketConfig = new DiscordSocketConfig
            {
                GatewayIntents = GatewayIntents.Guilds | 
                                 GatewayIntents.GuildMessages | 
                                 GatewayIntents.GuildMembers | 
                                 GatewayIntents.MessageContent,
                AlwaysDownloadUsers = true
            };

            return new ServiceCollection()
                .AddSingleton(new DiscordSocketClient(socketConfig))
                .AddSingleton<IConfigService, ConfigService>()
                .AddSingleton<IEmbedService, EmbedService>()
                .AddSingleton<ITicketService, TicketService>()
                .AddSingleton<IInteractionHandler, InteractionHandler>()
                .BuildServiceProvider();
        }

        /// <summary>
        /// Imprime los registros y logs generados por la biblioteca de Discord.Net en la consola.
        /// </summary>
        private Task LogAsync(LogMessage log)
        {
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] [{log.Severity}] {log.Source}: {log.Message} {log.Exception}");
            return Task.CompletedTask;
        }
    }
}
