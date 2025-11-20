using iTextSharp.text.pdf;
using iTextSharp.text;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace InactivityDetector
{
    public class FocusPulse
    {
        public event Action<TimeSpan, TimeSpan>? OnTimeUpdated;
        public event Action<bool>? OnPauseChanged;

        private TimeSpan activeTime = TimeSpan.Zero;
        private TimeSpan inactiveTime = TimeSpan.Zero;

        private System.Windows.Forms.Timer? uiTimer;
        private bool isInactive = false;
        private bool isPaused = false;

        private IntPtr keyboardHookId = IntPtr.Zero;
        private IntPtr mouseHookId = IntPtr.Zero;
        private LowLevelProc? keyboardProc;
        private LowLevelProc? mouseProc;

        private DateTime lastActivityTime = DateTime.Now;

        public event Action BloqueoNecesario;
        public event Action BloqueoFinalizado;

        private TimeSpan tiempoMaximoActivo = TimeSpan.FromSeconds(1);
        private TimeSpan tiempoActivo = TimeSpan.Zero;

        private List<(DateTime timestamp, string estado)> historialEstados = new();

        private DateTime inicioMedicion = DateTime.Now;

        public TimeSpan MargenInactividad { get; set; } = TimeSpan.FromSeconds(10);

        public FocusPulse()
        {
            keyboardProc = HookCallback;
            mouseProc = HookCallback;
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

        public void SetupUITimer()
        {
            uiTimer = new System.Windows.Forms.Timer { Interval = 100 };
            uiTimer.Tick += (s, e) =>
            {
                if (isPaused) return;

                var increment = TimeSpan.FromMilliseconds(uiTimer.Interval);

                if (isInactive)
                    inactiveTime = inactiveTime.Add(increment);
                else
                    activeTime = activeTime.Add(increment);

                OnTimeUpdated?.Invoke(activeTime, inactiveTime);

                if ((DateTime.Now - lastActivityTime) > MargenInactividad)
                    isInactive = true;
            };
            uiTimer.Start();
        }

        public void Cleanup()
        {
            uiTimer?.Stop();
            uiTimer?.Dispose();
            if (keyboardHookId != IntPtr.Zero) UnhookWindowsHookEx(keyboardHookId);
            if (mouseHookId != IntPtr.Zero) UnhookWindowsHookEx(mouseHookId);
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
            await Task.Delay(TimeSpan.FromMinutes(1));
            tiempoActivo = TimeSpan.Zero;
            BloqueoFinalizado?.Invoke();
        }

        public void ExportarPDF(string rutaArchivo)
        {
            Document doc = new Document(PageSize.A4);
            PdfWriter.GetInstance(doc, new System.IO.FileStream(rutaArchivo, System.IO.FileMode.Create));
            doc.Open();

            // Título
            doc.Add(new Paragraph("Reporte de actividad ✦ FocusPulse"));
            doc.Add(new Paragraph($"Inicio de medición: {inicioMedicion}"));
            doc.Add(new Paragraph($"Fin de medición: {DateTime.Now}"));
            doc.Add(new Paragraph($"Tiempo activo total: {activeTime}"));
            doc.Add(new Paragraph($"Tiempo inactivo total: {inactiveTime}"));
            doc.Add(new Paragraph("\nHistorial de cambios de estado:\n"));
            doc.Add(new Paragraph(" "));

            // Tabla con los cambios
            PdfPTable tabla = new PdfPTable(2);
            tabla.AddCell("Hora");
            tabla.AddCell("Estado");

            //foreach (var registro in historialEstados)
            //{
            //    tabla.AddCell(registro.timestamp.ToString("HH:mm:ss"));
            //    tabla.AddCell(registro.estado);
            //}

            DateTime? inicio = null;
            string estadoActual = null;

            for (int i = 0; i < historialEstados.Count; i++)
            {
                var registro = historialEstados[i];

                // Si es el primer registro, inicializamos
                if (inicio == null)
                {
                    inicio = registro.timestamp;
                    estadoActual = registro.estado;
                }

                // Si cambia el estado o llegamos al último registro
                bool cambioEstado = registro.estado != estadoActual;
                bool ultimo = i == historialEstados.Count - 1;

                if (cambioEstado || ultimo)
                {
                    // Fin del rango: usamos el timestamp anterior como cierre
                    DateTime fin = ultimo ? registro.timestamp : historialEstados[i - 1].timestamp;

                    tabla.AddCell($"{inicio:HH:mm:ss} - {fin:HH:mm:ss}");
                    tabla.AddCell(estadoActual);

                    // Reiniciamos para el nuevo estado
                    inicio = registro.timestamp;
                    estadoActual = registro.estado;
                }
            }

            doc.Add(tabla);
            doc.Close();
        }

        // -----------------------------
        // Hooks
        // -----------------------------
        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && !isPaused)
            {
                lastActivityTime = DateTime.Now;
                if (isInactive)
                {
                    isInactive = false;
                    historialEstados.Add((DateTime.Now, "Inactivo"));
                }
                else
                {
                    historialEstados.Add((DateTime.Now, "Activo"));
                }
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