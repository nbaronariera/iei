using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Firefox;
using OpenQA.Selenium.Support.UI;
using System.Globalization;
using System.Text.RegularExpressions;
using WebDriverManager;
using WebDriverManager.DriverConfigs.Impl;

namespace UI.Helpers
{
    internal class CoordenadasSelenium : IDisposable
    {
        private IWebDriver driver;
        private Random rnd = new Random();

        public CoordenadasSelenium()
        {
            // Inicializar el driver UNA SOLA VEZ en el constructor
            new DriverManager().SetUpDriver(new ChromeConfig());
            var driverService = ChromeDriverService.CreateDefaultService();
            driverService.HideCommandPromptWindow = true;
            driver = new ChromeDriver(driverService);
        }

        public (double Lat, double Lng) ObtenerCoordenadas(string direccion, string municipio)
        {
            try
            {
                // FILTROS DE LIMPIEZA DE DIRECCIÓN
                if (!string.IsNullOrEmpty(direccion))
                {
                    // CORRECCIONES MANUALES
                    if (direccion.Contains("Plá De Rascanya", StringComparison.OrdinalIgnoreCase))
                    {
                        direccion = "Calle Plá De Rascanya";
                    }

                    if (direccion.Contains("Azagador de Lliria", StringComparison.OrdinalIgnoreCase))
                    {
                        direccion = "ITV Massalfassar";
                    }

                    // Quitar "s/n", "s/ nº"
                    direccion = Regex.Replace(direccion, @"\s*[,]?\s*s/\s*nº?", "", RegexOptions.IgnoreCase);

                    // Quitar puntos kilométricos (Ej: "Km. 8,3", "km 55")
                    direccion = Regex.Replace(direccion, @"\s*[,]?\s*km\.?\s*\d+([.,]\d+)?", "", RegexOptions.IgnoreCase);

                    // Limpieza final de espacios o comas sueltas
                    direccion = direccion.Trim().TrimEnd(',');
                }

                driver.Navigate().GoToUrl("https://www.coordenadas-gps.com");

                // Espera aleatoria para parecer humano (2 a 3.5 segundos)
                Thread.Sleep(rnd.Next(2000, 4000));

                try
                {
                    // 1. Definimos qué buscamos (Texto flexible para "Consentir", "Aceptar", "Agree")
                    // Usamos un XPath que busca texto independientemente de mayúsculas/minúsculas o espacios
                    string xpathCookies = "//*[contains(translate(text(), 'ABCDEFGHIJKLMNOPQRSTUVWXYZ', 'abcdefghijklmnopqrstuvwxyz'), 'consentir') or contains(translate(text(), 'ABCDEFGHIJKLMNOPQRSTUVWXYZ', 'abcdefghijklmnopqrstuvwxyz'), 'aceptar') or contains(text(), 'Agree')]";

                    // 2. Función local para intentar clicar
                    bool ClickarBanner()
                    {
                        try
                        {
                            var btn = driver.FindElement(By.XPath(xpathCookies));
                            if (btn.Displayed && btn.Enabled)
                            {
                                Console.WriteLine("Botón de cookies encontrado. Intentando pulsar...");
                                btn.Click();
                                return true;
                            }
                        }
                        catch (NoSuchElementException) { }
                        return false;
                    }

                    // 3. INTENTO 1: Buscar en la página principal
                    // Esperamos un poco a que cargue el banner
                    Thread.Sleep(2000);
                    if (!ClickarBanner())
                    {
                        // 4. INTENTO 2: Si no está en el principal, buscamos dentro de los IFRAMES
                        var iframes = driver.FindElements(By.TagName("iframe"));
                        Console.WriteLine($"Buscando cookies en {iframes.Count} iframes...");

                        foreach (var frame in iframes)
                        {
                            try
                            {
                                driver.SwitchTo().Frame(frame);
                                if (ClickarBanner())
                                {
                                    Console.WriteLine("¡Cookies aceptadas dentro de un iframe!");
                                    driver.SwitchTo().DefaultContent(); // Volver a la página principal
                                    break;
                                }
                                driver.SwitchTo().DefaultContent();
                            }
                            catch
                            {
                                driver.SwitchTo().DefaultContent(); // Asegurar que volvemos si falla
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Aviso: No se pudo gestionar el banner de cookies: {ex.Message}");
                }

                // 4. Combinar dirección y rellenar el formulario
                string direccionCompleta = $"{direccion}, {municipio}";
                var addressInput = driver.FindElement(By.Id("address"));
                addressInput.Clear(); // Limpiamos el valor por defecto ("New York, NY")
                addressInput.SendKeys(direccionCompleta);

                // 5. Hacer clic en el botón de búsqueda
                var submitButton = driver.FindElement(By.XPath("//button[contains(text(), 'Obtener Coordenadas GPS')]"));
                submitButton.Click();

                // 6. Esperar a que aparezcan los resultados (¡Importante!)
                var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));

                // Esperamos hasta que el campo de latitud NO esté vacío
                wait.Until(d => !string.IsNullOrEmpty(d.FindElement(By.Id("latitude")).GetAttribute("value")));

                // 7. Leer los valores de los campos
                var latInput = driver.FindElement(By.Id("latitude"));
                var lngInput = driver.FindElement(By.Id("longitude"));

                string latStr = latInput.GetAttribute("value");
                string lngStr = lngInput.GetAttribute("value");

                // 8. Convertir y devolver los valores
                double lat = double.Parse(latStr, CultureInfo.InvariantCulture);
                double lng = double.Parse(lngStr, CultureInfo.InvariantCulture);

                // Espera antes de devolver el resultado (simula tiempo de lectura)
                Thread.Sleep(rnd.Next(1000, 2000));

                return (lat, lng);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                return (0.0, 0.0);
            }
        }

        public void Dispose()
        {
            // Método para cerrar el navegador al terminar todo el proceso
            driver?.Quit();
        }
    }
}
