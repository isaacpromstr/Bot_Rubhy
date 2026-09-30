using System;
using System.Collections.Generic;
using System.IO;
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
        private const string CounterFilePath = "ticket_counter.txt";
        private static readonly object _counterLock = new object();

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

        /// <summary>
        /// Obtiene y actualiza de forma persistente el número del siguiente consultorio.
        /// Garantiza que siempre vaya aumentando de 1 en 1 sin reiniciarse.
        /// </summary>
        private int GetNextConsultorioNumber(int channelMax)
        {
            lock (_counterLock)
            {
                int savedCounter = 0;
                if (File.Exists(CounterFilePath))
                {
                    if (int.TryParse(File.ReadAllText(CounterFilePath).Trim(), out int val))
                    {
                        savedCounter = val;
                    }
                }

                int next = Math.Max(savedCounter, channelMax) + 1;
                try
                {
                    File.WriteAllText(CounterFilePath, next.ToString());
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[TicketService] Error al guardar contador en archivo: {ex.Message}");
                }
                return next;
            }
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
                    string numStr = ch.Name.Substring("consultorio-".Length).Trim();
                    if (int.TryParse(numStr, out int num))
                    {
                        if (num > maxNum)
                        {
                            maxNum = num;
                        }
                    }
                }
            }

            int nextNum = GetNextConsultorioNumber(maxNum);
            string channelName = $"consultorio-{nextNum:D3}"; // Formato 001, 002, 003...

            // Evitar cualquier colisión si un canal con ese nombre ya existiera activo
            while (channels.Any(c => c.Name.Equals(channelName, StringComparison.OrdinalIgnoreCase)))
            {
                nextNum++;
                channelName = $"consultorio-{nextNum:D3}";
            }

            lock (_counterLock)
            {
                try { File.WriteAllText(CounterFilePath, nextNum.ToString()); } catch { }
            }

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

            // IDs autorizados únicos: 1554661710466515036 y 918693619253248000 (Psicóloga Rubhy / Staff)
            var targetIds = new List<ulong> { 1554661710466515036, 918693619253248000 };
            if (_config.StaffMemberToMentionId != 0 && !targetIds.Contains(_config.StaffMemberToMentionId))
            {
                targetIds.Add(_config.StaffMemberToMentionId);
            }

            foreach (var id in targetIds)
            {
                var role = guild.GetRole(id);
                if (role != null)
                {
                    permissionOverwrites.Add(new Overwrite(id, PermissionTarget.Role, new OverwritePermissions(
                        viewChannel: PermValue.Allow,
                        sendMessages: PermValue.Allow,
                        readMessageHistory: PermValue.Allow
                    )));
                }
                else
                {
                    permissionOverwrites.Add(new Overwrite(id, PermissionTarget.User, new OverwritePermissions(
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
            // SOLO debe hacer ping a la persona que realizó el consultorio y a los IDs 1554661710466515036 y 918693619253248000.
            var pingList = new List<string> { creator.Mention };

            foreach (var id in targetIds)
            {
                var role = guild.GetRole(id);
                if (role != null)
                {
                    pingList.Add($"<@&{id}>");
                }
                else
                {
                    pingList.Add($"<@{id}>");
                }
            }

            string mentions = string.Join(" ", pingList.Distinct());

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
