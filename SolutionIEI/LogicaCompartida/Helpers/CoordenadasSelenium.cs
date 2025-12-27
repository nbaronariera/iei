using System;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;

namespace UI.Helpers
{
    internal class CoordenadasSelenium : IDisposable
    {
        private IWebDriver driver;
        private Random rnd = new Random();

        public CoordenadasSelenium()
        {
            // 1. Configuración de Opciones
            var options = new ChromeOptions();
            
            options.AddArgument("--no-sandbox"); 
            options.AddArgument("--disable-dev-shm-usage");
            options.AddArgument("--window-size=1920,1080"); 
            options.AddArgument("--ignore-certificate-errors");

            string chromeBinEnv = Environment.GetEnvironmentVariable("CHROME_BIN");
            string driverPathEnv = Environment.GetEnvironmentVariable("CHROMEDRIVER_PATH");

            ChromeDriverService service;

            if (!string.IsNullOrEmpty(chromeBinEnv) && !string.IsNullOrEmpty(driverPathEnv))
            {
                // --- Lógica para NixOS / Linux Configurado ---
                Console.WriteLine($"[INFO] Modo NixOS detectado.");
                
                options.BinaryLocation = chromeBinEnv;
                string driverDir = Path.GetDirectoryName(driverPathEnv);
                string driverName = Path.GetFileName(driverPathEnv);
                service = ChromeDriverService.CreateDefaultService(driverDir, driverName);
            }
            else
            {
                // --- Lógica para Windows / Entorno Estándar ---
                Console.WriteLine($"[INFO] Modo Windows/Estándar detectado.");
                service = ChromeDriverService.CreateDefaultService(AppDomain.CurrentDomain.BaseDirectory);
            }

            service.HideCommandPromptWindow = true;
            service.SuppressInitialDiagnosticInformation = true;
            driver = new ChromeDriver(service, options);
            
            // Timeout implícito para encontrar elementos
            driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(5);
        }

        public (double Lat, double Lng) ObtenerCoordenadas(string direccion, string municipio)
        {
            try
            {
                // --- Limpieza de dirección (Tu lógica original) ---
                if (!string.IsNullOrEmpty(direccion))
                {
                    if (direccion.Contains("Plá De Rascanya", StringComparison.OrdinalIgnoreCase))
                        direccion = "Calle Plá De Rascanya";
                    
                    if (direccion.Contains("Azagador de Lliria", StringComparison.OrdinalIgnoreCase))
                        direccion = "ITV Massalfassar";
                    
                    direccion = Regex.Replace(direccion, @"\s*[,]?\s*s/\s*nº?", "", RegexOptions.IgnoreCase);
                    direccion = Regex.Replace(direccion, @"\s*[,]?\s*km\.?\s*\d+([.,]\d+)?", "", RegexOptions.IgnoreCase);
                    direccion = direccion.Trim().TrimEnd(',');
                }

                driver.Navigate().GoToUrl("https://www.coordenadas-gps.com");
                // Pequeña espera aleatoria para parecer humano
                Thread.Sleep(rnd.Next(2000, 3000));

                // --- Gestión de Cookies ---
                try
                {
                    string xpathCookies = "//*[contains(translate(text(), 'ABCDEFGHIJKLMNOPQRSTUVWXYZ', 'abcdefghijklmnopqrstuvwxyz'), 'consentir') or contains(translate(text(), 'ABCDEFGHIJKLMNOPQRSTUVWXYZ', 'abcdefghijklmnopqrstuvwxyz'), 'aceptar') or contains(text(), 'Agree')]";
                    
                    bool ClickarBanner()
                    {
                        try
                        {
                            var elements = driver.FindElements(By.XPath(xpathCookies));
                            foreach(var btn in elements) {
                                if (btn.Displayed && btn.Enabled) {
                                    btn.Click();
                                    return true;
                                }
                            }
                        }
                        catch { }
                        return false;
                    }

                    if (!ClickarBanner())
                    {
                        var iframes = driver.FindElements(By.TagName("iframe"));
                        foreach (var frame in iframes)
                        {
                            try
                            {
                                driver.SwitchTo().Frame(frame);
                                if (ClickarBanner())
                                {
                                    driver.SwitchTo().DefaultContent();
                                    break;
                                }
                                driver.SwitchTo().DefaultContent();
                            }
                            catch { driver.SwitchTo().DefaultContent(); }
                        }
                    }
                }
                catch (Exception) { }

                // --- Introducir Datos ---
                string direccionCompleta = $"{direccion}, {municipio}";
                
                var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));
                var addressInput = wait.Until(d => d.FindElement(By.Id("address"))); // Espera explícita
                
                addressInput.Clear();
                addressInput.SendKeys(direccionCompleta);

                var latInput = driver.FindElement(By.Id("latitude"));
                var lngInput = driver.FindElement(By.Id("longitude"));
                
                // Limpiar valores previos vía JS para asegurar
                IJavaScriptExecutor js = (IJavaScriptExecutor)driver;
                js.ExecuteScript("arguments[0].value = '';", latInput);
                js.ExecuteScript("arguments[0].value = '';", lngInput);

                // --- Click en Obtener Coordenadas ---
                try
                {
                    var submitButton = driver.FindElement(By.XPath("//button[contains(text(), 'Obtener Coordenadas GPS')]"));
                    
                    try { submitButton.Click(); }
                    catch 
                    { 
                        js.ExecuteScript("arguments[0].click();", submitButton); 
                    }

                    // Esperar a que la latitud tenga valor
                    wait.Until(d => !string.IsNullOrEmpty(d.FindElement(By.Id("latitude")).GetAttribute("value")));
                }
                catch (Exception)
                {
                    // Si falla el wait o salta alerta
                    try { driver.SwitchTo().Alert().Accept(); } catch { }
                    return (0.0, 0.0);
                }

                // --- Parsear Resultados ---
                string latStr = latInput.GetAttribute("value");
                string lngStr = lngInput.GetAttribute("value");

                if (double.TryParse(latStr, NumberStyles.Any, CultureInfo.InvariantCulture, out double lat) &&
                    double.TryParse(lngStr, NumberStyles.Any, CultureInfo.InvariantCulture, out double lng))
                {
                    return (lat, lng);
                }

                return (0.0, 0.0);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Error Selenium] {ex.Message}");
                return (0.0, 0.0);
            }
        }

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