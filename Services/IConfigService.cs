using System.Threading.Tasks;

namespace Plantillabot.Services
{
    /// <summary>
    /// Interfaz para el servicio de configuración.
    /// Define los métodos para obtener los parámetros de configuración del bot.
    /// </summary>
    public interface IConfigService
    {
        /// <summary>
        /// Obtiene el token ("clave de acceso única") del bot de Discord.
        /// </summary>
        string Token { get; }

        /// <summary>
        /// Obtiene el título del panel de tickets.
        /// </summary>
        string PanelTitle { get; }

        /// <summary>
        /// Obtiene el encabezado principal del panel de tickets.
        /// </summary>
        string PanelHeading { get; }

        /// <summary>
        /// Obtiene la descripción del panel de tickets.
        /// </summary>
        string PanelDescription { get; }

        /// <summary>
        /// Obtiene la URL de la imagen del panel.
        /// </summary>
        string PanelImageUrl { get; }

        /// <summary>
        /// Obtiene el texto del pie de página del panel.
        /// </summary>
        string PanelFooter { get; }

        /// <summary>
        /// Obtiene el ID del miembro de staff ("personal de soporte o administración") al que se debe mencionar.
        /// </summary>
        ulong StaffMemberToMentionId { get; }

        /// <summary>
        /// Carga las configuraciones de forma asíncrona desde el almacenamiento.
        /// </summary>
        Task LoadAsync();
    }
}
