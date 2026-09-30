using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace Plantillabot.Services
{
    /// <summary>
    /// Implementación concreta de <see cref="IConfigService"/> que lee la configuración desde un archivo JSON.
    /// </summary>
    public class ConfigService : IConfigService
    {
        private const string ConfigPath = "config.json";

        public string Token { get; private set; } = null!;
        public string PanelTitle { get; private set; } = null!;
        public string PanelHeading { get; private set; } = null!;
        public string PanelDescription { get; private set; } = null!;
        public string PanelImageUrl { get; private set; } = null!;
        public string PanelFooter { get; private set; } = null!;
        public ulong StaffMemberToMentionId { get; private set; }

        public async Task LoadAsync()
        {
            string configFilePath = ConfigPath;
            if (!File.Exists(configFilePath))
            {
                string baseDirConfig = Path.Combine(AppContext.BaseDirectory, ConfigPath);
                if (File.Exists(baseDirConfig))
                {
                    configFilePath = baseDirConfig;
                }
                else
                {
                    string subDirConfig = Path.Combine("Plantillabot", ConfigPath);
                    if (File.Exists(subDirConfig))
                    {
                        configFilePath = subDirConfig;
                    }
                }
            }

            // 1. Cargar valores por defecto o del archivo JSON si existe.
            if (File.Exists(configFilePath))
            {
                try
                {
                    string json = await File.ReadAllTextAsync(configFilePath);
                    using JsonDocument doc = JsonDocument.Parse(json);
                    JsonElement root = doc.RootElement;

                    Token = GetPropertyString(root, "Token", "");
                    PanelTitle = GetPropertyString(root, "PanelTitle", "NiftyOk");
                    PanelHeading = GetPropertyString(root, "PanelHeading", "Ayuda y Apoyo");
                    PanelDescription = GetPropertyString(root, "PanelDescription", "Presiona el botón de abajo para abrir un ticket.");
                    PanelImageUrl = GetPropertyString(root, "PanelImageUrl", "");
                    PanelFooter = GetPropertyString(root, "PanelFooter", "By NiftyOk");
                    
                    if (root.TryGetProperty("StaffMemberToMentionId", out JsonElement staffProp))
                    {
                        if (staffProp.ValueKind == JsonValueKind.Number)
                        {
                            StaffMemberToMentionId = staffProp.GetUInt64();
                        }
                        else if (staffProp.ValueKind == JsonValueKind.String && ulong.TryParse(staffProp.GetString(), out ulong parsedId))
                        {
                            StaffMemberToMentionId = parsedId;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Advertencia] Error al cargar config.json: {ex.Message}. Se intentará usar variables de entorno.");
                }
            }

            // 2. Sobreescribir o inicializar con variables de entorno ("Environment Variables") para compatibilidad en la nube.
            string? envToken = Environment.GetEnvironmentVariable("DISCORD_TOKEN");
            if (!string.IsNullOrEmpty(envToken)) Token = envToken;

            string? envTitle = Environment.GetEnvironmentVariable("PANEL_TITLE");
            if (!string.IsNullOrEmpty(envTitle)) PanelTitle = envTitle;

            string? envHeading = Environment.GetEnvironmentVariable("PANEL_HEADING");
            if (!string.IsNullOrEmpty(envHeading)) PanelHeading = envHeading;

            string? envDesc = Environment.GetEnvironmentVariable("PANEL_DESCRIPTION");
            if (!string.IsNullOrEmpty(envDesc)) PanelDescription = envDesc;

            string? envImg = Environment.GetEnvironmentVariable("PANEL_IMAGE_URL");
            if (!string.IsNullOrEmpty(envImg)) PanelImageUrl = envImg;

            string? envFooter = Environment.GetEnvironmentVariable("PANEL_FOOTER");
            if (!string.IsNullOrEmpty(envFooter)) PanelFooter = envFooter;

            string? envStaffId = Environment.GetEnvironmentVariable("STAFF_MEMBER_ID");
            if (!string.IsNullOrEmpty(envStaffId) && ulong.TryParse(envStaffId, out ulong parsedEnvId))
            {
                StaffMemberToMentionId = parsedEnvId;
            }

            // 3. Validar que al menos tengamos el token del bot para iniciar.
            if (string.IsNullOrEmpty(Token))
            {
                throw new InvalidOperationException("Error: No se ha configurado el Token del bot de Discord. Configúralo en config.json o como variable de entorno 'DISCORD_TOKEN'.");
            }
        }

        private string GetPropertyString(JsonElement element, string propertyName, string defaultValue)
        {
            if (element.TryGetProperty(propertyName, out JsonElement prop))
            {
                return prop.GetString() ?? defaultValue;
            }
            return defaultValue;
        }
    }
}
