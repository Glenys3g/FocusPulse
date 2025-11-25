using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Runtime.InteropServices;
using iTextSharp.text;
using iTextSharp.text.pdf;

namespace InactivityDetector
{
    public class FocusPulse : IDisposable
    {
        // Eventos públicos
        public event Action<TimeSpan, TimeSpan>? OnTimeUpdated;
        public event Action<bool>? OnPauseChanged;
        public event Action? BloqueoNecesario;
        public event Action? BloqueoFinalizado;

        // Estado interno
        private TimeSpan activeTime = TimeSpan.Zero;
        private TimeSpan inactiveTime = TimeSpan.Zero;
        private bool isPaused = false;
        private bool isInactive = false;

        private DateTime lastActivityTime = DateTime.Now;
        private DateTime inicioMedicion = DateTime.Now;

        private TimeSpan tiempoMaximoActivo = TimeSpan.FromMinutes(50); // ejemplo: 50 min activos
        private TimeSpan tiempoActivo = TimeSpan.Zero;
        private System.Windows.Forms.Timer? uiTimer;

        public TimeSpan MargenInactividad { get; set; } = TimeSpan.FromSeconds(10);

        private List<(DateTime timestamp, string estado)> historialEstados = new();

        // Rx.NET subjects
        private readonly Subject<string> actividadSubject = new();
        private readonly IDisposable? subscripcionTimer;
        private readonly IDisposable? subscripcionActividad;

        // Hooks
        private IntPtr keyboardHookId = IntPtr.Zero;
        private IntPtr mouseHookId = IntPtr.Zero;
        private LowLevelProc? keyboardProc;
        private LowLevelProc? mouseProc;

        public FocusPulse()
        {
            // Inicializar delegados de hooks
            keyboardProc = HookCallback;
            mouseProc = HookCallback;

            // Observable de temporizador cada 100ms
            var timer = Observable.Interval(TimeSpan.FromMilliseconds(100));

            subscripcionTimer = timer.Subscribe(_ =>
            {
                if (isPaused) return;

                var incremento = TimeSpan.FromMilliseconds(100);

                if (isInactive)
                    inactiveTime += incremento;
                else
                    activeTime += incremento;

                OnTimeUpdated?.Invoke(activeTime, inactiveTime);

                if ((DateTime.Now - lastActivityTime) > MargenInactividad)
                {
                    if (!isInactive)
                    {
                        isInactive = true;
                        historialEstados.Add((DateTime.Now, "Inactivo"));
                        actividadSubject.OnNext("Inactivo");
                    }
                }
            });

            // Observable de actividad (teclado/ratón)
            subscripcionActividad = actividadSubject.Subscribe(estado =>
            {
                historialEstados.Add((DateTime.Now, estado));
            });
        }

        public void TogglePause()
        {
            isPaused = !isPaused;
            OnPauseChanged?.Invoke(isPaused);
        }

        public void SetupGlobalHooks()
        {
            keyboardHookId = SetHook(WH_KEYBOARD_LL, keyboardProc!);
            mouseHookId = SetHook(WH_MOUSE_LL, mouseProc!);
        }

        public void RegistrarActividad()
        {
            lastActivityTime = DateTime.Now;
            if (isInactive)
            {
                isInactive = false;
                actividadSubject.OnNext("Activo");
            }
        }

        public void ActualizarTiempoActivo(TimeSpan incremento)
        {
            tiempoActivo += incremento;
            if (tiempoActivo >= tiempoMaximoActivo)
            {
                BloqueoNecesario?.Invoke();
                IniciarDescanso();
            }
        }

        private async void IniciarDescanso()
        {
            await System.Threading.Tasks.Task.Delay(TimeSpan.FromMinutes(10)); // descanso obligatorio
            tiempoActivo = TimeSpan.Zero;
            BloqueoFinalizado?.Invoke();
        }

        public void ExportarPDF(string rutaArchivo)
        {
            Document doc = new Document(PageSize.A4);
            PdfWriter.GetInstance(doc, new System.IO.FileStream(rutaArchivo, System.IO.FileMode.Create));
            doc.Open();

            doc.Add(new Paragraph("Reporte de actividad ✦ FocusPulse"));
            doc.Add(new Paragraph($"Inicio de medición: {inicioMedicion}"));
            doc.Add(new Paragraph($"Fin de medición: {DateTime.Now}"));
            doc.Add(new Paragraph($"Tiempo activo total: {activeTime}"));
            doc.Add(new Paragraph($"Tiempo inactivo total: {inactiveTime}"));
            doc.Add(new Paragraph("\nHistorial de cambios de estado:\n"));
            doc.Add(new Paragraph(" "));

            PdfPTable tabla = new PdfPTable(2);
            tabla.AddCell("Hora");
            tabla.AddCell("Estado");

            DateTime? inicio = null;
            string? estadoActual = null;

            for (int i = 0; i < historialEstados.Count; i++)
            {
                var registro = historialEstados[i];
                if (inicio == null)
                {
                    inicio = registro.timestamp;
                    estadoActual = registro.estado;
                }

                bool cambioEstado = registro.estado != estadoActual;
                bool ultimo = i == historialEstados.Count - 1;

                if (cambioEstado || ultimo)
                {
                    DateTime fin = ultimo ? registro.timestamp : historialEstados[i - 1].timestamp;
                    tabla.AddCell($"{inicio:HH:mm:ss} - {fin:HH:mm:ss}");
                    tabla.AddCell(estadoActual ?? "");
                    inicio = registro.timestamp;
                    estadoActual = registro.estado;
                }
            }

            doc.Add(tabla);
            doc.Close();
        }

        public void Dispose()
        {
            subscripcionTimer?.Dispose();
            subscripcionActividad?.Dispose();
            actividadSubject?.Dispose();
        }

        public void Cleanup()
        {
            uiTimer?.Stop();
            uiTimer?.Dispose();
            if (keyboardHookId != IntPtr.Zero) UnhookWindowsHookEx(keyboardHookId);
            if (mouseHookId != IntPtr.Zero) UnhookWindowsHookEx(mouseHookId);
        }

        // -----------------------------
        // Hooks
        // -----------------------------
        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && !isPaused)
            {
                RegistrarActividad(); // notifica actividad y dispara cambio de estado
            }
            return CallNextHookEx(IntPtr.Zero, nCode, wParam, lParam);
        }

        private IntPtr SetHook(int idHook, LowLevelProc proc)
        {
            using var curProcess = Process.GetCurrentProcess();
            using var curModule = curProcess.MainModule!;
            IntPtr hModule = GetModuleHandle(curModule.ModuleName);
            return SetWindowsHookEx(idHook, proc, hModule, 0);
        }

        public delegate IntPtr LowLevelProc(int nCode, IntPtr wParam, IntPtr lParam);

        private const int WH_KEYBOARD_LL = 13;
        private const int WH_MOUSE_LL = 14;

        [DllImport("user32.dll")] private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelProc lpfn, IntPtr hMod, uint dwThreadId);
        [DllImport("user32.dll")] private static extern bool UnhookWindowsHookEx(IntPtr hhk);
        [DllImport("user32.dll")] private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);
        [DllImport("kernel32.dll")] private static extern IntPtr GetModuleHandle(string? lpModuleName);
    }
}
