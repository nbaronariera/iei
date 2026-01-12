using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;

namespace UI.Helpers
{
    // Clase Helper que utiliza Selenium WebDriver para obtener coordenadas geográficas (Latitud/Longitud)
    // a partir de una dirección postal, haciendo scraping de una web de terceros.
    internal class CoordenadasSelenium : IDisposable
    {
        // Singleton thread-safe para reutilizar la instancia del navegador
        private static CoordenadasSelenium instance;
        private static readonly object _lock = new();
        public bool Disponible { get; private set; }
        private IWebDriver driver;
        private Random rnd = new Random();

        // Constructor privado: Inicializa el driver de Chrome con las opciones necesarias
        public CoordenadasSelenium()
        {
            try
            {
                var options = new ChromeOptions();
                // Opciones para entorno servidor/headless (sin interfaz gráfica) y compatibilidad Docker
                options.AddArgument("--no-sandbox");
                options.AddArgument("--disable-dev-shm-usage");
                options.AddArgument("--window-size=1920,1080");
                options.AddArgument("--ignore-certificate-errors");

                // Configuración específica para entornos CI/CD o contenedores donde las rutas son fijas
                string chromeBinEnv = Environment.GetEnvironmentVariable("CHROME_BIN");
                string driverPathEnv = Environment.GetEnvironmentVariable("CHROMEDRIVER_PATH");

                ChromeDriverService service;
                if (!string.IsNullOrEmpty(chromeBinEnv) && !string.IsNullOrEmpty(driverPathEnv))
                {
                    options.BinaryLocation = chromeBinEnv;
                    string driverDir = Path.GetDirectoryName(driverPathEnv);
                    string driverName = Path.GetFileName(driverPathEnv);
                    service = ChromeDriverService.CreateDefaultService(driverDir, driverName);
                }
                else
                {
                    Debug.WriteLine($"[INFO] Modo Windows/Estándar detectado.");
                    service = ChromeDriverService.CreateDefaultService();
                }

                // Ocultar ventana de consola del driver para limpieza visual
                service.HideCommandPromptWindow = true;
                service.SuppressInitialDiagnosticInformation = true;

                driver = new ChromeDriver(service, options);

                // Timeout implícito para búsqueda de elementos
                driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(3);
                Disponible = true;
            }
            catch (Exception ex)
            {
                Disponible = false;
                Debug.WriteLine($"[SELENIUM] ERROR inicializando: {ex.Message}");
            }
        }

        // Acceso a la instancia única (Patrón Singleton)
        public static CoordenadasSelenium Instance
        {
            get
            {
                lock (_lock)
                {
                    instance ??= new CoordenadasSelenium();
                    return instance;
                }
            }
        }

        // Método principal: Obtiene Lat/Lon scrapeando 'coordenadas-gps.com'
        public (double Lat, double Lng) ObtenerCoordenadas(string direccion, string municipio)
        {
            if (!Disponible)
                return (0.0, 0.0);

            Debug.WriteLine($"[SELENIUM] Iniciando búsqueda para: '{direccion}', {municipio}");

            try
            {
                // Construir dirección completa
                string direccionCompleta = $"{direccion}, {municipio}, España";
                Debug.WriteLine($"[SELENIUM] Dirección para búsqueda: '{direccionCompleta}'");

                // 1. Navegar a la página objetivo
                driver.Navigate().GoToUrl("https://www.coordenadas-gps.com");

                // Espera para carga inicial
                Thread.Sleep(rnd.Next(2000, 3000));

                // 2. GESTIÓN DE CONSENTIMIENTO DE COOKIES (CRÍTICO)
                // Muchos sitios bloquean la interacción hasta aceptar cookies.
                bool cookiesManejadas = false;
                for (int intento = 0; intento < 3; intento++)
                {
                    try
                    {
                        // Buscar botones de cookies de diferentes formas
                        var cookieButtons = driver.FindElements(By.XPath(@"
                            //button[contains(translate(text(), 'ABCDEFGHIJKLMNOPQRSTUVWXYZ', 'abcdefghijklmnopqrstuvwxyz'), 'aceptar')] |
                            //button[contains(translate(text(), 'ABCDEFGHIJKLMNOPQRSTUVWXYZ', 'abcdefghijklmnopqrstuvwxyz'), 'consentir')] |
                            //button[contains(translate(text(), 'ABCDEFGHIJKLMNOPQRSTUVWXYZ', 'abcdefghijklmnopqrstuvwxyz'), 'allow')] |
                            //button[contains(translate(text(), 'ABCDEFGHIJKLMNOPQRSTUVWXYZ', 'abcdefghijklmnopqrstuvwxyz'), 'agree')] |
                            //a[contains(translate(text(), 'ABCDEFGHIJKLMNOPQRSTUVWXYZ', 'abcdefghijklmnopqrstuvwxyz'), 'aceptar')]
                        "));

                        foreach (var btn in cookieButtons)
                        {
                            try
                            {
                                if (btn.Displayed && btn.Enabled)
                                {
                                    Debug.WriteLine($"[SELENIUM] Encontrado botón cookies: '{btn.Text}'");
                                    // Click mediante JS para saltar superposiciones visuales
                                    IJavaScriptExecutor js = (IJavaScriptExecutor)driver;
                                    js.ExecuteScript("arguments[0].click();", btn);
                                    Thread.Sleep(1000);
                                    cookiesManejadas = true;
                                    Debug.WriteLine("[SELENIUM] Cookies aceptadas");
                                    break;
                                }
                            }
                            catch { }
                        }

                        if (cookiesManejadas) break;
                    }
                    catch { }

                    Thread.Sleep(1000);
                }

                if (!cookiesManejadas)
                {
                    Debug.WriteLine("[SELENIUM] No se pudo manejar cookies, intentando continuar...");
                }

                // Esperar un poco más después de manejar cookies
                Thread.Sleep(1000);

                // 3. INTERACCIÓN CON EL FORMULARIO
                var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));

                // Encontrar campo de dirección
                IWebElement addressInput;
                try
                {
                    addressInput = wait.Until(d => d.FindElement(By.Id("address")));
                }
                catch
                {
                    // Si no lo encuentra por ID, intentar por otros selectores
                    addressInput = driver.FindElement(By.CssSelector("input[type='text'][name*='address']"));
                }

                // Usar JavaScript para interactuar - más confiable
                IJavaScriptExecutor jsExecutor = (IJavaScriptExecutor)driver;

                // Limpiar y establecer valor con JavaScript
                jsExecutor.ExecuteScript(@"
                    arguments[0].value = '';
                    arguments[0].focus();
                ", addressInput);

                Thread.Sleep(500);

                // Escribir dirección
                jsExecutor.ExecuteScript("arguments[0].value = arguments[1];", addressInput, direccionCompleta);
                Thread.Sleep(500);

                // Disparar evento 'change' para que scripts de la página detecten el cambio
                jsExecutor.ExecuteScript("arguments[0].dispatchEvent(new Event('change', { bubbles: true }));", addressInput);

                // Buscar botón de búsqueda
                IWebElement submitButton = null;
                try
                {
                    submitButton = driver.FindElement(By.XPath("//button[contains(text(), 'Obtener Coordenadas GPS')]"));
                }
                catch
                {
                    // Buscar cualquier botón que contenga "Obtener" o "Buscar"
                    var buttons = driver.FindElements(By.TagName("button"));
                    foreach (var btn in buttons)
                    {
                        if (btn.Text.Contains("Obtener", StringComparison.OrdinalIgnoreCase) ||
                            btn.Text.Contains("Buscar", StringComparison.OrdinalIgnoreCase))
                        {
                            submitButton = btn;
                            break;
                        }
                    }
                }

                if (submitButton == null)
                {
                    Debug.WriteLine("[SELENIUM] No se encontró el botón de búsqueda");
                    return (0.0, 0.0);
                }

                // Click en botón de búsqueda
                jsExecutor.ExecuteScript("arguments[0].click();", submitButton);

                // Esperar a que la página procese y actualice los campos de lat/lon
                Thread.Sleep(rnd.Next(3000, 4000));

                // 4. EXTRACCIÓN DE RESULTADOS
                // Los resultados suelen aparecer en inputs con IDs 'latitude' y 'longitude'
                var latInput = driver.FindElement(By.Id("latitude"));
                var lngInput = driver.FindElement(By.Id("longitude"));

                string latStr = latInput.GetAttribute("value");
                string lngStr = lngInput.GetAttribute("value");

                Debug.WriteLine($"[SELENIUM] Coordenadas obtenidas: Lat={latStr}, Lng={lngStr}");

                // Parseo de strings a double (Culture Invariant para punto decimal)
                if (double.TryParse(latStr, NumberStyles.Any, CultureInfo.InvariantCulture, out double lat) &&
                    double.TryParse(lngStr, NumberStyles.Any, CultureInfo.InvariantCulture, out double lng))
                {
                    Debug.WriteLine($"[SELENIUM] ÉXITO → ({lat}, {lng})");
                    return (lat, lng);
                }

                Debug.WriteLine("[SELENIUM] No se pudieron parsear las coordenadas");
                return (0.0, 0.0);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SELENIUM] Error: {ex.Message}");
                return (0.0, 0.0);
            }
        }


        // Limpieza de recursos (cerrar navegador)
        public void Dispose()
        {
            try
            {
                driver?.Quit();
                driver?.Dispose();
            }
            catch { }
        }
    }
}