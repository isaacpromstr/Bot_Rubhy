using Discord;

namespace Plantillabot.Services
{
    /// <summary>
    /// Interfaz para la generación de embeds ("mensajes con formato especial enriquecido").
    /// </summary>
    public interface IEmbedService
    {
        /// <summary>
        /// Crea el embed ("mensaje enriquecido") para el panel principal de soporte.
        /// </summary>
        Embed CreatePanelEmbed();

        /// <summary>
        /// Crea el componente de mensaje (como botones) para el panel principal.
        /// </summary>
        MessageComponent CreatePanelComponents();

        /// <summary>
        /// Crea el embed ("mensaje enriquecido") para el canal del consultorio cuando se abre un ticket.
        /// </summary>
        /// <param name="creatorName">Nombre del creador del consultorio.</param>
        Embed CreateConsultorioEmbed(string creatorName);

        /// <summary>
        /// Crea los botones de interacción para el canal de consultorio (Cerrar, Reclamar).
        /// </summary>
        MessageComponent CreateConsultorioComponents();
    }
}
