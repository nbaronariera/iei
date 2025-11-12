using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using OpenQA.Selenium;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WebDriverManager.DriverConfigs.Impl;
using WebDriverManager;

namespace UI.Helpers
{
    internal class CoordenadasSelenium
    {
        public (double Lat, double Lng) ObtenerCoordenadas(string direccion, string municipio)
        {
            IWebDriver driver = null;

            try
            {
                // 1. Configura ChromeDriver automáticamente
                new DriverManager().SetUpDriver(new ChromeConfig());

                // Ocultar la ventana de la consola de ChromeDriver
                var driverService = ChromeDriverService.CreateDefaultService();
                driverService.HideCommandPromptWindow = true;

                driver = new ChromeDriver(driverService);

                // 2. Ir a la página
                driver.Navigate().GoToUrl("https://www.coordenadas-gps.com");

                // 3. Esperar y manejar el banner de Cookies
                // Le damos 2 segundos para que aparezca el banner
                Thread.Sleep(2000);
                try
                {
                    // El script de cookies muestra que el texto es "OK!"
                    var cookieButton = driver.FindElement(By.LinkText("OK!"));
                    if (cookieButton != null)
                    {
                        cookieButton.Click();
                        Thread.Sleep(500); // Pequeña pausa para que se cierre
                    }
                }
                catch (NoSuchElementException)
                {
                    // No se encontró el banner de cookies, no hacemos nada
                }

                // 4. Combinar dirección y rellenar el formulario
                // Confirmado por HTML: <input id="address" ...>
                string direccionCompleta = $"{direccion}, {municipio}";
                var addressInput = driver.FindElement(By.Id("address"));
                addressInput.Clear(); // Limpiamos el valor por defecto ("New York, NY")
                addressInput.SendKeys(direccionCompleta);

                // 5. Hacer clic en el botón de búsqueda
                // Confirmado por HTML: <button ...>Obtener Coordenadas GPS</button>
                var submitButton = driver.FindElement(By.XPath("//button[contains(text(), 'Obtener Coordenadas GPS')]"));
                submitButton.Click();

                // 6. Esperar a que aparezcan los resultados (¡Importante!)
                var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));

                // Esperamos hasta que el campo de latitud NO esté vacío
                // Confirmado por HTML: <input id="latitude" ...>
                wait.Until(d => !string.IsNullOrEmpty(d.FindElement(By.Id("latitude")).GetAttribute("value")));

                // 7. Leer los valores de los campos
                // Confirmado por HTML: <input id="latitude" ...>
                var latInput = driver.FindElement(By.Id("latitude"));
                // Confirmado por HTML: <input id="longitude" ...>
                var lngInput = driver.FindElement(By.Id("longitude"));

                string latStr = latInput.GetAttribute("value");
                string lngStr = lngInput.GetAttribute("value");

                // 8. Convertir y devolver los valores
                double lat = double.Parse(latStr, CultureInfo.InvariantCulture);
                double lng = double.Parse(lngStr, CultureInfo.InvariantCulture);

                return (lat, lng);
            }
            catch (Exception ex)
            {
                // Manejar cualquier error (ej. no se encontró la dirección)
                Console.WriteLine($"Error en Selenium: {ex.Message}");
                return (0.0, 0.0); // Devolver 0,0 en caso de error
            }
            finally
            {
                // 9. Asegurarse de cerrar el navegador siempre
                driver?.Quit();
            }
        }
    }
}
