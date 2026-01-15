using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

class Program
{
    // Lista para rastrear todos los procesos hijos y poder cerrarlos al salir
    private static readonly List<Process> procesosLanzados = new();

    static void Main(string[] args)
    {
        // --- CONFIGURACIÓN DE RUTAS ---
        // Se calcula la ruta raíz de la solución subiendo 4 niveles desde el ejecutable (bin/Debug/net...)
        string launcherDirectory = AppDomain.CurrentDomain.BaseDirectory;
        string solutionRoot = Path.GetFullPath(Path.Combine(launcherDirectory, "..", "..", "..", ".."));

        // Ubicación de los archivos de proyecto (.csproj)
        string apisProjectPath = Path.Combine(solutionRoot, "APIs", "APIs.csproj");
        string uiProjectPath = Path.Combine(solutionRoot, "UI", "UI.csproj");

        Console.WriteLine($"Solution Root: {solutionRoot}");

        Console.WriteLine($"APIs Project: {apisProjectPath}");
        Console.WriteLine($"UI Project: {uiProjectPath}");

        // --- LANZAMIENTO DE MICROSERVICIOS (APIs) ---
        // Se lanzan en segundo plano (sin ventana) usando diferentes perfiles de lanzamiento 
        LanzarAPI(apisProjectPath, "Carga8081");
        LanzarAPI(apisProjectPath, "WrapperCV8082");
        LanzarAPI(apisProjectPath, "WrapperCAT8083");
        LanzarAPI(apisProjectPath, "WrapperGAL8084");





        // --- LANZAMIENTO DE INTERFAZ (UI) ---
        // La UI se lanza con ventana visible para que el usuario interactúe
        LanzarUI(uiProjectPath);

        Console.WriteLine("\n=== TODAS LAS 5 APIs Y LA INTERFAZ HAN SIDO LANZADAS CORRECTAMENTE ===");
        Console.WriteLine("Pulsa cualquier tecla para cerrar TODO limpiamente.\n");
        Console.ReadKey(); // Espera hasta que pulses tecla

        // --- CIERRE DEL SISTEMA ---
        Console.WriteLine("Cerrando procesos lanzados por el launcher...");
        CerrarProcesosLanzados();

        Console.WriteLine("Todo cerrado. Pulsa cualquier tecla para salir.");
        Console.ReadKey();
    }

    /// <summary>
    /// Ejecuta un perfil específico del proyecto de APIs de forma oculta.
    /// </summary>
    static void LanzarAPI(string projectPath, string profileName)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"run --project \"{projectPath}\" --launch-profile {profileName}",
            UseShellExecute = true,
            CreateNoWindow = true,  // Evita que se abra una consola por cada API
            WindowStyle = ProcessWindowStyle.Hidden,
            WorkingDirectory = Path.GetDirectoryName(projectPath)!
        };

        var proc = Process.Start(startInfo);
        if (proc != null)
        {
            procesosLanzados.Add(proc);
            Console.WriteLine($" Lanzada API: {profileName}");
        }
        else
        {
            Console.WriteLine($" Fallo al lanzar API {profileName}");
        }
    }

    /// <summary>
    /// Ejecuta el proyecto de Interfaz de Usuario de forma visible.
    /// </summary>
    static void LanzarUI(string projectPath)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"run --project \"{projectPath}\"",
            UseShellExecute = true,
            CreateNoWindow = false,  // La UI sí debe ser visible
            WorkingDirectory = Path.GetDirectoryName(projectPath)!
        };

        var proc = Process.Start(startInfo);
        if (proc != null)
        {
            procesosLanzados.Add(proc);
            Console.WriteLine("✓ Lanzada UI");
        }
        else
        {
            Console.WriteLine(" Fallo al lanzar UI");
        }
    }

    /// <summary>
    /// Recorre la lista de procesos activos y los finaliza forzosamente.
    /// </summary>
    static void CerrarProcesosLanzados()
    {
        foreach (var proc in procesosLanzados)
        {
            try
            {
                if (!proc.HasExited)
                {
                    proc.Kill(); // Detiene el proceso inmediatamente
                    proc.WaitForExit(3000); // Espera hasta 3 segundos para confirmar el cierre
                }
            }
            catch
            {
                // Si el proceso ya se cerró o no hay permisos, ignoramos el error
            }
        }
    }
}