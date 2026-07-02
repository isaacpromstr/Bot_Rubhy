using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace Plantillabot.Services
{
    /// <summary>
    /// Implementación del servicio de consultorios que gestiona la lógica de canales,
    /// permisos de privacidad y asignación en Discord.
    /// </summary>
    public class TicketService : ITicketService
    {
        private readonly IEmbedService _embedService;
        private readonly IConfigService _config;

        /// <summary>
        /// Constructor que inyecta el generador de embeds y el servicio de configuración aplicando SOLID.
        /// </summary>
        public TicketService(IEmbedService embedService, IConfigService config)
        {
            _embedService = embedService;
            _config = config;
        }

        public async Task<ITextChannel> CreateConsultorioAsync(IGuild guild, IUser creator)
        {
            // 1. Obtener la lista de canales de texto actuales para calcular el siguiente número incremental.
            var channels = await guild.GetTextChannelsAsync();
            int maxNum = 0;

            foreach (var ch in channels)
            {
                if (ch.Name.StartsWith("consultorio-", StringComparison.OrdinalIgnoreCase))
                {
                    string numStr = ch.Name.Substring("consultorio-".Length);
                    if (int.TryParse(numStr, out int num))
                    {
                        if (num > maxNum)
                        {
                            maxNum = num;
                        }
                    }
                }
            }

            int nextNum = maxNum + 1;
            string channelName = $"consultorio-{nextNum:D3}"; // Formato 001, 002, 003...

            // 2. Definir los permisos de sobrescritura para el canal privado.
            var permissionOverwrites = new List<Overwrite>();

            // Denegar ver el canal a @everyone por defecto.
            permissionOverwrites.Add(new Overwrite(guild.EveryoneRole.Id, PermissionTarget.Role, new OverwritePermissions(
                viewChannel: PermValue.Deny
            )));

            // Permitir ver y enviar mensajes al creador del ticket.
            permissionOverwrites.Add(new Overwrite(creator.Id, PermissionTarget.User, new OverwritePermissions(
                viewChannel: PermValue.Allow,
                sendMessages: PermValue.Allow,
                readMessageHistory: PermValue.Allow
            )));

            // Otorgar permisos de visualización al miembro del staff configurado específicamente.
            if (_config.StaffMemberToMentionId != 0)
            {
                permissionOverwrites.Add(new Overwrite(_config.StaffMemberToMentionId, PermissionTarget.User, new OverwritePermissions(
                    viewChannel: PermValue.Allow,
                    sendMessages: PermValue.Allow,
                    readMessageHistory: PermValue.Allow
                )));
            }

            // Buscar roles de administración o moderación del servidor para darles permisos explícitos de visualización.
            var staffRoles = guild.Roles.Where(r => 
                r.Permissions.Administrator || 
                r.Name.Contains("Admin", StringComparison.OrdinalIgnoreCase) || 
                r.Name.Contains("Mod", StringComparison.OrdinalIgnoreCase) ||
                r.Name.Contains("Staff", StringComparison.OrdinalIgnoreCase) ||
                r.Name.Contains("Ayudante", StringComparison.OrdinalIgnoreCase)
            ).ToList();

            foreach (var role in staffRoles)
            {
                if (role.Id != guild.EveryoneRole.Id)
                {
                    permissionOverwrites.Add(new Overwrite(role.Id, PermissionTarget.Role, new OverwritePermissions(
                        viewChannel: PermValue.Allow,
                        sendMessages: PermValue.Allow,
                        readMessageHistory: PermValue.Allow
                    )));
                }
            }

            // 3. Crear el canal en el gremio de Discord.
            var newChannel = await guild.CreateTextChannelAsync(channelName, tcp =>
            {
                tcp.PermissionOverwrites = permissionOverwrites;
                tcp.Topic = $"Consultorio privado para {creator.Username} (ID del usuario: {creator.Id})";
            });

            // 4. Construir el mensaje de bienvenida y menciones en el canal.
            // Generamos las menciones del creador y de la persona de soporte especificada.
            string mentions = creator.Mention;
            if (_config.StaffMemberToMentionId != 0)
            {
                mentions += $" <@{_config.StaffMemberToMentionId}>";
            }
            else if (staffRoles.Any())
            {
                mentions += " " + string.Join(" ", staffRoles.Select(r => r.Mention));
            }

            // Generamos el embed y los componentes (botones).
            var embed = _embedService.CreateConsultorioEmbed(creator.Mention);
            var components = _embedService.CreateConsultorioComponents();

            // Enviamos el mensaje inicial al nuevo canal.
            await newChannel.SendMessageAsync(
                text: mentions,
                embed: embed,
                components: components
            );

            return newChannel;
        }

        public async Task CloseConsultorioAsync(ITextChannel channel)
        {
            // El cierre elimina directamente el canal.
            await channel.DeleteAsync();
        }
    }
}
