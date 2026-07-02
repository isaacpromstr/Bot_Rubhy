using Discord;
using Plantillabot.Services;

namespace Plantillabot.Services
{
    /// <summary>
    /// Implementación concreta de <see cref="IEmbedService"/> que genera embeds con diseño estético premium.
    /// </summary>
    public class EmbedService : IEmbedService
    {
        private readonly IConfigService _config;

        /// <summary>
        /// Constructor que recibe el servicio de configuración mediante inyección de dependencias.
        /// </summary>
        public EmbedService(IConfigService config)
        {
            _config = config;
        }

        public Embed CreatePanelEmbed()
        {
            var embedBuilder = new EmbedBuilder()
                .WithTitle(_config.PanelTitle)
                .WithDescription($"**_*{_config.PanelHeading}*_**\n\n{_config.PanelDescription}")
                .WithColor(new Color(0x7c, 0x1d, 0xd4)); // Color morado vibrante y estético

            if (!string.IsNullOrEmpty(_config.PanelImageUrl))
            {
                embedBuilder.WithImageUrl(_config.PanelImageUrl);
            }

            return embedBuilder.Build();
        }

        public MessageComponent CreatePanelComponents()
        {
            var builder = new ComponentBuilder()
                .WithButton(
                    label: "CREAR CITA", 
                    customId: "crear_cita", 
                    style: ButtonStyle.Primary
                );

            return builder.Build();
        }

        public Embed CreateConsultorioEmbed(string creatorMention)
        {
            return new EmbedBuilder()
                .WithTitle("Cita Abierta")
                .WithDescription($"{creatorMention} ha creado una nueva cita.\n\nla psicóloga Rubhy le atenderá en la brevedad de su disponibilidad.")
                .WithColor(new Color(0x2f, 0x31, 0x36)) // Color premium gris oscuro
                .WithFooter(new EmbedFooterBuilder().WithText("Consultorio | Cierra con el botón"))
                .Build();
        }

        public MessageComponent CreateConsultorioComponents()
        {
            return new ComponentBuilder()
                .WithButton(
                    label: "Cerrar Cita", 
                    customId: "cerrar_ticket", // Mantener el customId cerrar_ticket para la lógica de borrado
                    style: ButtonStyle.Danger,
                    emote: new Emoji("🔴")
                )
                .Build();
        }
    }
}
