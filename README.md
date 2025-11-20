# ✦ FocusPulse

FocusPulse es una aplicación de escritorio en **C# / Windows Forms** diseñada para **detectar la actividad e inactividad del usuario**, gestionar pausas y promover descansos saludables durante sesiones de trabajo.  

## 🚀 Características principales
- **Detección global de actividad**: Monitorea teclado y ratón incluso fuera de la ventana principal.
- **Tiempo activo e inactivo**: Muestra en tiempo real cuánto tiempo has estado trabajando o inactivo.
- **Control de pausa**: Permite pausar y reanudar el conteo manualmente.
- **Bloqueo suave automático**: Cuando se supera el tiempo máximo activo, se fuerza un descanso de 1 minuto.
- **Configuración personalizada**: El usuario puede definir el margen de inactividad antes de que empiece a contar.
- **Exportación a PDF**: Genera un reporte con los tiempos medidos y los intervalos de actividad/inactividad.
- **Interfaz minimalista**: Ventana flotante sin bordes, con diseño circular o cuadrado, estilo oscuro y botones intuitivos.
- **Tooltips informativos**: Cada botón incluye una descripción rápida de su función.

## 🖼️ Interfaz
- Panel principal con:
  - Título ✦ FocusPulse
  - Tiempo activo 🟢
  - Tiempo inactivo 💤
  - Botón de pausa ⏸️ / ▶️
  - Botón de cerrar ✖
  - Botón de exportar 📄
  - Botón de configuración ⚙️ para mostrar opciones avanzadas (margen de inactividad)

## 📐 Arquitectura
- **Separación de responsabilidades**:
  - *Hooks*: detección de actividad global.
  - *Timer*: medición de tiempo activo/inactivo.
  - *Eventos*: control de flujo y comunicación con la UI.
- **Orientado a eventos**: la lógica funciona independientemente de la interfaz gráfica.
- **Extensible**: fácil de ajustar intervalos, tiempos máximos y reglas de descanso.

## 📊 Ejemplo de flujo
1. Se inicializan hooks y timer.
2. El usuario interactúa → se actualiza `lastActivityTime`.
3. El timer incrementa tiempo activo o inactivo.
4. Si pasan más de *N* segundos sin actividad → estado inactivo.
5. Si tiempo activo ≥ tiempo máximo → se dispara bloqueo y descanso.
6. Tras el descanso → se reinicia el contador.

## 📄 Exportación a PDF
El reporte incluye:
- Hora de inicio y fin de la medición.
- Tiempo total activo e inactivo.
- Historial de cambios de estado con rangos horarios.

## 🛠️ Requisitos
- .NET Framework 4.8 o superior / .NET 6+
- Windows Forms
- [iTextSharp](https://www.nuget.org/packages/iTextSharp/) para exportación a PDF

## ▶️ Instalación y ejecución
```bash
git clone https://github.com/glenys3g/FocusPulse.git
cd FocusPulse
# Abrir el proyecto en Visual Studio
# Compilar y ejecutar
