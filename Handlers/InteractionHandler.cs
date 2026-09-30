using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Plantillabot.Services;

namespace Plantillabot.Handlers
{
    /// <summary>
    /// Implementación concreta de <see cref="IInteractionHandler"/> encargada de gestionar los comandos slash
    /// y las interacciones con botones del consultorio.
    /// </summary>
    public class InteractionHandler : IInteractionHandler
    {
        private readonly DiscordSocketClient _client;
        private readonly ITicketService _ticketService;
        private readonly IEmbedService _embedService;
        private readonly IConfigService _config;

        /// <summary>
        /// Constructor que inyecta el cliente de Discord y los servicios requeridos aplicando SOLID.
        /// </summary>
        public InteractionHandler(
            DiscordSocketClient client,
            ITicketService ticketService,
            IEmbedService embedService,
            IConfigService config)
        {
            _client = client;
            _ticketService = ticketService;
            _embedService = embedService;
            _config = config;
        }

        public Task InitializeAsync()
        {
            // Suscribir los eventos del cliente desacoplados en tareas secundarias (Task.Run)
            // para NUNCA bloquear el Gateway de Discord.Net ni causar timeouts.
            _client.Ready += () =>
            {
                _ = Task.Run(RegisterCommandsAsync);
                return Task.CompletedTask;
            };

            _client.SlashCommandExecuted += command =>
            {
                _ = Task.Run(() => HandleSlashCommandAsync(command));
                return Task.CompletedTask;
            };

            _client.ButtonExecuted += component =>
            {
                _ = Task.Run(() => HandleButtonExecutedAsync(component));
                return Task.CompletedTask;
            };

            _client.ModalSubmitted += modal =>
            {
                _ = Task.Run(() => HandleModalSubmittedAsync(modal));
                return Task.CompletedTask;
            };

            return Task.CompletedTask;
        }

        /// <summary>
        /// Registra el comando slash ("comando de barra inclinada") a nivel global.
        /// </summary>
        private async Task RegisterCommandsAsync()
        {
            try
            {
                var commandBuilder = new SlashCommandBuilder()
                    .WithName("configurar-panel")
                    .WithDescription("Abre un formulario interactivo para configurar el panel de la cita en este canal.");

                // Registramos globalmente
                await _client.CreateGlobalApplicationCommandAsync(commandBuilder.Build());
                Console.WriteLine("Comando slash /configurar-panel registrado exitosamente a nivel global.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al registrar comandos slash: {ex.Message}");
            }
        }

        /// <summary>
        /// Maneja la ejecución de comandos slash.
        /// </summary>
        private async Task HandleSlashCommandAsync(SocketSlashCommand command)
        {
            try
            {
                if (command.CommandName == "configurar-panel")
                {
                    // Verificar que el usuario tenga permisos de Administrador para configurar el panel.
                    var guildUser = command.User as SocketGuildUser;
                    if (guildUser == null || !guildUser.GuildPermissions.Administrator)
                    {
                        await command.RespondAsync("❌ Solo los administradores del servidor pueden configurar el panel de soporte.", ephemeral: true);
                        return;
                    }

                    // Construir el formulario interactivo (Modal) de 3 apartados: Título, Descripción y URL de la imagen.
                    var modalBuilder = new ModalBuilder()
                        .WithTitle("Configurar Panel de Citas")
                        .WithCustomId("config_panel_modal")
                        .AddTextInput("Título del Panel", "modal_titulo", placeholder: "Ej. Consultorio NiftyOk", required: true, value: _config.PanelTitle)
                        .AddTextInput("Descripción del Panel", "modal_descripcion", TextInputStyle.Paragraph, placeholder: "Instrucciones de la cita...", required: true, value: _config.PanelDescription)
                        .AddTextInput("URL de la Imagen", "modal_imagen", placeholder: "Ej. https://i.imgur.com/K3Z1mP6.png", required: false, value: _config.PanelImageUrl);

                    // Mostrar el modal al administrador de Discord.
                    await command.RespondWithModalAsync(modalBuilder.Build());
                }
            }
            catch (Discord.Net.HttpException ex) when ((int?)ex.DiscordCode == 40060)
            {
                Console.WriteLine($"[Aviso] La interacción del comando /{command.CommandName} ya fue respondida.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al procesar el comando slash /{command.CommandName}: {ex.Message}");
            }
        }

        /// <summary>
        /// Maneja las interacciones de los botones de Discord.
        /// </summary>
        private async Task HandleButtonExecutedAsync(SocketMessageComponent interaction)
        {
            try
            {
                string customId = interaction.Data.CustomId;

                if (customId == "crear_cita")
                {
                    // Responder diferido para procesar en segundo plano.
                    await interaction.DeferAsync(ephemeral: true);

                    var guild = (interaction.Channel as SocketGuildChannel)?.Guild;
                    if (guild == null)
                    {
                        await interaction.FollowupAsync("❌ Esta acción solo se puede realizar dentro de un servidor.", ephemeral: true);
                        return;
                    }

                    // Crear el consultorio privado usando el servicio de tickets.
                    var ticketChannel = await _ticketService.CreateConsultorioAsync(guild, interaction.User);

                    // Informar al usuario su canal creado.
                    await interaction.FollowupAsync($"✅ ¡Cita creada con éxito! Puedes acceder en {ticketChannel.Mention}", ephemeral: true);
                }
                else if (customId == "cerrar_ticket")
                {
                    // Responder para evitar timeout.
                    await interaction.RespondAsync("🔴 Cerrando el consultorio de la cita... Este canal se eliminará en 3 segundos.", ephemeral: false);

                    if (interaction.Channel is ITextChannel textChannel)
                    {
                        // Esperar 3 segundos antes de eliminar el canal para permitir leer el mensaje.
                        await Task.Delay(3000);
                        await _ticketService.CloseConsultorioAsync(textChannel);
                    }
                }
            }
            catch (Discord.Net.HttpException ex) when ((int?)ex.DiscordCode == 10062)
            {
                Console.WriteLine("[Aviso] La interacción del botón expiró o ya fue atendida por otra instancia (10062: Unknown interaction).");
            }
            catch (Discord.Net.HttpException ex) when ((int?)ex.DiscordCode == 40060)
            {
                Console.WriteLine("[Aviso] La interacción del botón ya fue respondida previamente (40060).");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al manejar el botón: {ex.Message}");
                // Si la interacción ya fue respondida, intentar enviar un mensaje de error por separado.
                try
                {
                    await interaction.FollowupAsync("❌ Ocurrió un error al procesar tu solicitud.", ephemeral: true);
                }
                catch
                {
                    // Evitar fallas si ya no se puede responder a la interacción.
                }
            }
        }

        /// <summary>
        /// Maneja el envío del formulario modal de configuración.
        /// </summary>
        private async Task HandleModalSubmittedAsync(SocketModal modal)
        {
            if (modal.Data.CustomId == "config_panel_modal")
            {
                try
                {
                    // Responder diferido de forma efímera inmediatamente para evitar timeout.
                    await modal.DeferAsync(ephemeral: true);

                    // Extraer los valores ingresados por el usuario administrador en el formulario.
                    var components = modal.Data.Components.ToList();
                    string titulo = components.FirstOrDefault(x => x.CustomId == "modal_titulo")?.Value ?? _config.PanelTitle;
                    string descripcion = components.FirstOrDefault(x => x.CustomId == "modal_descripcion")?.Value ?? _config.PanelDescription;
                    string? imagenUrl = components.FirstOrDefault(x => x.CustomId == "modal_imagen")?.Value;

                    // Construir el embed del panel a partir de la entrada del formulario.
                    var embedBuilder = new EmbedBuilder()
                        .WithTitle(titulo)
                        .WithDescription(descripcion)
                        .WithColor(new Color(0x7c, 0x1d, 0xd4)); // Violeta elegante

                    if (!string.IsNullOrEmpty(imagenUrl) && Uri.IsWellFormedUriString(imagenUrl, UriKind.Absolute))
                    {
                        embedBuilder.WithImageUrl(imagenUrl);
                    }

                    // Obtener los componentes del panel (Botón Crear Cita).
                    var panelComponents = _embedService.CreatePanelComponents();

                    // Enviar el panel de la cita en el canal actual.
                    if (modal.Channel is ISocketMessageChannel messageChannel)
                    {
                        await messageChannel.SendMessageAsync(embed: embedBuilder.Build(), components: panelComponents);
                        await modal.FollowupAsync("✅ ¡El panel de citas ha sido configurado con éxito!", ephemeral: true);
                    }
                    else
                    {
                        await modal.FollowupAsync("❌ No se pudo determinar el canal para enviar el panel.", ephemeral: true);
                    }
                }
                catch (Discord.Net.HttpException ex) when ((int?)ex.DiscordCode == 10062)
                {
                    Console.WriteLine("[Aviso] La interacción del formulario modal expiró o ya fue atendida por otra instancia (10062: Unknown interaction).");
                }
                catch (Discord.Net.HttpException ex) when ((int?)ex.DiscordCode == 40060)
                {
                    Console.WriteLine("[Aviso] La interacción del formulario modal ya fue respondida previamente (40060).");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error al procesar el modal: {ex.Message}");
                    try
                    {
                        await modal.FollowupAsync("❌ Ocurrió un error al procesar el formulario.", ephemeral: true);
                    }
                    catch { }
                }
            }
        }
    }
}
