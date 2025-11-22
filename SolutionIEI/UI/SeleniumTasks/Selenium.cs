using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using System.Threading.Tasks;
using System;
using System.Globalization;
using System.Threading.Tasks;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;

class Program
{
    private void InitializeComponent()
    {
        Console.WriteLine("Iniciando geocodificación...");

        string direccion = "Plaza del Ayuntamiento";
        string municipio = "Valencia";
        string provincia = "Valencia";

        var (lat, lon) = await GeocodeAsync(direccion, municipio, provincia);

        if (lat.HasValue && lon.HasValue)
        {
            Console.WriteLine($"Éxito! Coordenadas: {lat.Value.ToString(CultureInfo.InvariantCulture)}, {lon.Value.ToString(CultureInfo.InvariantCulture)}");
        }
        else
        {
            Console.WriteLine($"No se pudieron obtener las coordenadas para '{direccion}'.");
        }

        Console.WriteLine("Presiona Enter para salir.");
        Console.ReadLine();
    }

    private async Task<(double? lat, double? lon)> GeocodeAsync(string address, string municipality, string province)
    {
        if (string.IsNullOrEmpty(address) || address.Contains("I.T.V.") || address == "Variable según población")
        {
            return (null, null);
        }

        var query = $"{address}, {municipality}";

        var options = new ChromeOptions();
        //options.AddArgument("--headless");
        //options.AddArgument("--disable-gpu");
        //options.AddArgument("--no-sandbox");  // Opcional para entornos restringidos

        using (var driver = new ChromeDriver(options))
        {
            try
            {
                // Navegar al sitio
                driver.Navigate().GoToUrl("https://www.coordenadas-gps.com");

                driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(10);
                
                var submitButton = driver.FindElement(By.XPath("//*[@id="wrap"]/div[2]/div[3]/div[1]/form[1]/div[2]/div/button")); 
                submitButton.Click();

                var addressInput = driver.FindElement(By.Id("address")); 
                addressInput.Clear();
                addressInput.SendKeys(query);

                //Boton Obtener Coordenadas GPS
                var submitButton = driver.FindElement(By.XPath("//*[@id="wrap"]/div[2]/div[3]/div[1]/form[1]/div[2]/div/button")); 
                submitButton.Click();

                // Esperar resultados (usa explicit wait para elemento visible)
                var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));
                wait.Until(d => !string.IsNullOrEmpty(d.FindElement(By.Id("latitude")).GetAttribute("value")));  // Espera hasta que lat aparezca

                // Extraer lat y long
                string latText = driver.FindElement(By.Id("latitude")).GetAttribute("value");
                string lonText = driver.FindElement(By.Id("longitude")).GetAttribute("value");

                // Parsear a double
                if (double.TryParse(latText, NumberStyles.Any, CultureInfo.InvariantCulture, out double lat) &&
                    double.TryParse(lonText, NumberStyles.Any, CultureInfo.InvariantCulture, out double lon))
                {
                    return (lat, lon);
                }
                else{return (lat, lon);}
            }
            catch (Exception ex)
            {
                Log error (e.g., Console.WriteLine($"Error geocoding {query}: {ex.Message}"))
                Console.WriteLine("Error");
            }
            finally
            {
                Console.WriteLine("Finalizar");
                await Task.Delay(2000);  // Delay para evitar bans (2 seg)
            }
        }
        Console.WriteLine("null");
        return (null, null); 
    }
}