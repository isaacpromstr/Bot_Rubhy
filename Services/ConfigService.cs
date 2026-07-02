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
            if (!File.Exists(ConfigPath))
            {
                throw new FileNotFoundException($"No se encontró el archivo de configuración en {ConfigPath}. Asegúrate de crearlo.");
            }

            try
            {
                string json = await File.ReadAllTextAsync(ConfigPath);
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
                Console.WriteLine($"Error al cargar la configuración: {ex.Message}");
                throw;
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
