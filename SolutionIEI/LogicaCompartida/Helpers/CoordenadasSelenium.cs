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
            new DriverManager().SetUpDriver(new ChromeConfig());
            var driverService = ChromeDriverService.CreateDefaultService();
            driverService.HideCommandPromptWindow = true;
            driver = new ChromeDriver(driverService);
        }

        public (double Lat, double Lng) ObtenerCoordenadas(string direccion, string municipio)
        {
            try
            {
                if (!string.IsNullOrEmpty(direccion))
                {
                    if (direccion.Contains("Plá De Rascanya", StringComparison.OrdinalIgnoreCase))
                    {
                        direccion = "Calle Plá De Rascanya";
                    }
                    if (direccion.Contains("Azagador de Lliria", StringComparison.OrdinalIgnoreCase))
                    {
                        direccion = "ITV Massalfassar";
                    }
                    direccion = Regex.Replace(direccion, @"\s*[,]?\s*s/\s*nº?", "", RegexOptions.IgnoreCase);
                    direccion = Regex.Replace(direccion, @"\s*[,]?\s*km\.?\s*\d+([.,]\d+)?", "", RegexOptions.IgnoreCase);
                    direccion = direccion.Trim().TrimEnd(',');
                }

                driver.Navigate().GoToUrl("https://www.coordenadas-gps.com");
                Thread.Sleep(rnd.Next(2000, 4000));

                try
                {
                    string xpathCookies = "//*[contains(translate(text(), 'ABCDEFGHIJKLMNOPQRSTUVWXYZ', 'abcdefghijklmnopqrstuvwxyz'), 'consentir') or contains(translate(text(), 'ABCDEFGHIJKLMNOPQRSTUVWXYZ', 'abcdefghijklmnopqrstuvwxyz'), 'aceptar') or contains(text(), 'Agree')]";
                    bool ClickarBanner()
                    {
                        try
                        {
                            var btn = driver.FindElement(By.XPath(xpathCookies));
                            if (btn.Displayed && btn.Enabled)
                            {
                                btn.Click();
                                return true;
                            }
                        }
                        catch (NoSuchElementException) { }
                        return false;
                    }

                    Thread.Sleep(2000);
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
                            catch
                            {
                                driver.SwitchTo().DefaultContent();
                            }
                        }
                    }
                }
                catch (Exception) { }

                string direccionCompleta = $"{direccion}, {municipio}";
                var addressInput = driver.FindElement(By.Id("address"));
                addressInput.Clear();
                addressInput.SendKeys(direccionCompleta);

                var latInput = driver.FindElement(By.Id("latitude"));
                var lngInput = driver.FindElement(By.Id("longitude"));
                latInput.Clear();
                lngInput.Clear();

                var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));

                try
                {
                    var submitButton = driver.FindElement(By.XPath("//button[contains(text(), 'Obtener Coordenadas GPS')]"));
                    submitButton.Click();
                    wait.Until(d => !string.IsNullOrEmpty(d.FindElement(By.Id("latitude")).GetAttribute("value")));
                }
                catch (UnhandledAlertException)
                {
                    try { driver.SwitchTo().Alert().Accept(); } catch { }
                    return (0.0, 0.0);
                }
                catch (WebDriverTimeoutException)
                {
                    return (0.0, 0.0);
                }

                string latStr = latInput.GetAttribute("value");
                string lngStr = lngInput.GetAttribute("value");

                if (double.TryParse(latStr, NumberStyles.Any, CultureInfo.InvariantCulture, out double lat) &&
                    double.TryParse(lngStr, NumberStyles.Any, CultureInfo.InvariantCulture, out double lng))
                {
                    Thread.Sleep(rnd.Next(1000, 2000));
                    return (lat, lng);
                }

                return (0.0, 0.0);
            }
            catch (Exception)
            {
                return (0.0, 0.0);
            }
        }

        public void Dispose()
        {
            driver?.Quit();
        }
    }
}