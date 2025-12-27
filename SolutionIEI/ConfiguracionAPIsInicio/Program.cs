using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

class Program
{
    private static readonly List<Process> procesosLanzados = new();

    static void Main(string[] args)
    {
        
        string launcherDirectory = AppDomain.CurrentDomain.BaseDirectory;
        string solutionRoot = Path.GetFullPath(Path.Combine(launcherDirectory, "..", "..", "..", ".."));
        string apisProjectPath = Path.Combine(solutionRoot, "APIs", "APIs.csproj");
        string uiProjectPath = Path.Combine(solutionRoot, "UI", "UI.csproj");

        Debug.WriteLine($"Solution Root: {solutionRoot}");
        Debug.WriteLine($"APIs Project: {apisProjectPath}");
        Debug.WriteLine($"UI Project: {uiProjectPath}");

        // Lanzar las 5 APIs (sin ventana de consola)
        LanzarAPI(apisProjectPath, "Busqueda8080");
        LanzarAPI(apisProjectPath, "Carga8081");
        LanzarAPI(apisProjectPath, "WrapperCV8082");
        LanzarAPI(apisProjectPath, "WrapperCAT8083");
        LanzarAPI(apisProjectPath, "WrapperGAL8084");

        

        
      

        // Lanzar la UI (con ventana)
        LanzarUI(uiProjectPath);

        Console.WriteLine("\n=== TODAS LAS 5 APIs Y LA INTERFAZ HAN SIDO LANZADAS CORRECTAMENTE ===");
        Console.WriteLine("Pulsa cualquier tecla para cerrar TODO limpiamente.\n");
        Console.ReadKey(); // Espera hasta que pulses tecla


        Console.WriteLine("Cerrando procesos lanzados por el launcher...");
        CerrarProcesosLanzados();

        Console.WriteLine("Todo cerrado. Pulsa cualquier tecla para salir.");
        Console.ReadKey();
    }

    static void LanzarAPI(string projectPath, string profileName)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"run --project \"{projectPath}\" --launch-profile {profileName}",
            UseShellExecute = true,
            CreateNoWindow = true,  // Sin consola visible
            WindowStyle = ProcessWindowStyle.Hidden,
            WorkingDirectory = Path.GetDirectoryName(projectPath)!
        };

        var proc = Process.Start(startInfo);
        if (proc != null)
        {
            procesosLanzados.Add(proc);
            Debug.WriteLine($" Lanzada API: {profileName}");
        }
        else
        {
            Debug.WriteLine($" Fallo al lanzar API {profileName}");
        }
    }

    static void LanzarUI(string projectPath)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"run --project \"{projectPath}\"",
            UseShellExecute = true,
            CreateNoWindow = false,  // UI con ventana
            WorkingDirectory = Path.GetDirectoryName(projectPath)!
        };

        var proc = Process.Start(startInfo);
        if (proc != null)
        {
            procesosLanzados.Add(proc);
            Debug.WriteLine("✓ Lanzada UI");
        }
        else
        {
            Debug.WriteLine(" Fallo al lanzar UI");
        }
    }

    static void CerrarProcesosLanzados()
    {
        foreach (var proc in procesosLanzados)
        {
            try
            {
                if (!proc.HasExited)
                {
                    proc.Kill();
                    proc.WaitForExit(3000);
                }
            }
            catch
            {
                // Ignorar errores de cierre
            }
        }
    }
}