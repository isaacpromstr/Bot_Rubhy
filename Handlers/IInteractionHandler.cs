using System.Threading.Tasks;
using Discord.WebSocket;

namespace Plantillabot.Handlers
{
    /// <summary>
    /// Interfaz para el manejo de interacciones (botones, comandos de barra inclinada) de Discord.
    /// </summary>
    public interface IInteractionHandler
    {
        /// <summary>
        /// Inicializa el manejador de interacciones y suscribe los eventos del cliente de Discord.
        /// </summary>
        Task InitializeAsync();
    }
}
