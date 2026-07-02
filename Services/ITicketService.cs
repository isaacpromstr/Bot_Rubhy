using System.Threading.Tasks;
using Discord;

namespace Plantillabot.Services
{
    /// <summary>
    /// Interfaz para la gestión y ciclo de vida de los canales de consultorio (tickets).
    /// </summary>
    public interface ITicketService
    {
        /// <summary>
        /// Crea un canal de consultorio para un usuario específico, con permisos restringidos.
        /// </summary>
        /// <param name="guild">El gremio ("servidor de Discord") en el que se creará el canal.</param>
        /// <param name="creator">El usuario que solicita la creación del consultorio.</param>
        /// <returns>El canal de texto creado.</returns>
        Task<ITextChannel> CreateConsultorioAsync(IGuild guild, IUser creator);

        /// <summary>
        /// Cierra y elimina el canal de consultorio.
        /// </summary>
        /// <param name="channel">El canal de texto del consultorio a cerrar.</param>
        Task CloseConsultorioAsync(ITextChannel channel);
    }
}
