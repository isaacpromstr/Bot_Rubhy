# Guía para Desplegar el Bot de Rubhy en Render (24/7)

Esta carpeta contiene la versión preparada y contenerizada con Docker para funcionar en [Render](https://render.com/) sin necesidad de tener tu terminal ni tu computadora encendida.

---

## 🛠️ ¿Qué mejoras incluye esta versión?

1. **`Dockerfile` optimizado para .NET 10**: Compila y empaqueta el bot en un contenedor ligero de Linux.
2. **Servidor HTTP integrado**: Incluye un servidor web ligero que escucha en el puerto `$PORT` (10000 por defecto en Render). Esto permite:
   - Que Render apruebe el despliegue del servicio (Health Check OK).
   - Recibir "pings" para que el bot nunca se duerma en el plan gratuito.
3. **Soporte de Variables de Entorno**: Tu token de Discord no necesita subirse a GitHub; se configura directamente de forma segura en Render.

---

## 🚀 Pasos para Subir y Desplegar en Render

### Paso 1: Subir tus cambios a GitHub
Desde esta carpeta (`bot de rubhy - render`), ejecuta:
```bash
git add .
git commit -m "Preparar bot para despliegue en Render con Docker"
git push origin main
```
*(O súbelo a un nuevo repositorio en GitHub si prefieres separarlo).*

---

### Paso 2: Crear el servicio en Render
1. Ve a [Render Dashboard](https://dashboard.render.com/) e inicia sesión con tu cuenta de GitHub.
2. Haz clic en **New +** y selecciona **Web Service**.
3. Elige tu repositorio (`bot_rubhy` o el que hayas creado).
4. Configura los siguientes campos:
   - **Name**: `bot-de-rubhy` (o el nombre que gustes)
   - **Region**: La más cercana (ej. *Oregon (US West)* u *Ohio (US East)*)
   - **Language / Runtime**: **Docker** (Render detectará el `Dockerfile` automáticamente)
   - **Instance Type**: **Free**

---

### Paso 3: Configurar las Variables de Entorno
En la misma pantalla de configuración (o en la sección **Environment** del servicio):
1. Añade la variable:
   - **Key**: `DISCORD_TOKEN`
   - **Value**: *(Pega aquí el token de tu bot de Discord)*
2. *(Opcional)* Si quieres configurar los textos del panel sin tocar el código:
   - `PANEL_TITLE`: `NiftyOk`
   - `PANEL_HEADING`: `Ayuda y Apoyo`
   - `STAFF_MEMBER_ID`: `918693619253248000`

---

### Paso 4: Desplegar
1. Haz clic en **Deploy Web Service** (o **Create Web Service**).
2. Render comenzará a construir la imagen Docker y verás los logs de compilación.
3. Cuando termine, verás el estado en verde **Live** y en los logs:
   ```text
   [Render HealthCheck] Servidor HTTP activo en el puerto 10000.
   ```
   ¡Tu bot ya estará conectado a Discord!

---

### Paso 5: Mantener el Bot Activo 24/7 Gratis (Evitar que se duerma)
Los servicios gratuitos de Render entran en suspensión tras 15 minutos sin tráfico web. Para evitarlo:
1. Copia la URL pública que te dio Render (por ejemplo: `https://bot-de-rubhy.onrender.com`).
2. Ve a un servicio gratuito de monitoreo como [UptimeRobot](https://uptimerobot.com/) o [Cron-job.org](https://cron-job.org/).
3. Crea un nuevo monitor HTTP que haga un "Ping" / GET a tu URL cada **5 o 10 minutos**.
4. Cada vez que reciba un ping, el bot responderá `200 OK` y Render lo mantendrá despierto permanentemente.
