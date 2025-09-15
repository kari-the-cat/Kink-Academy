using OpenQA.Selenium;
using OpenQA.Selenium.Firefox;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace BP_testing
{
    class CookieDto
    {
        public string Name { get; set; }
        public string Value { get; set; }
        public string Domain { get; set; }
        public string Path { get; set; }
        public DateTime? Expiry { get; set; }
        public bool Secure { get; set; }
        public bool HttpOnly { get; set; }
    }


    public class KAWebDriver
    {
        private FirefoxDriver driver;
        const string styleCssScript =
@"const style = document.createElement(""style"");
    style.textContent = `
      .yellow_top {
         border-top-width: 6px;
             border-top-style: solid;
             border-top-color: orange; 
      }
      .green_top {
         border-top-width: 6px;
             border-top-style: solid;
             border-top-color: green; 
      }
    .blue_top {
         border-top-width: 6px;
             border-top-style: solid;
             border-top-color: blue; 
      }
    `;
document.head.appendChild(style); ";

        private static string ConfigFolder =>
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".kinkacademy");

        void storeCookies()
        {
            Directory.CreateDirectory(ConfigFolder);

            var cookies = driver.Manage().Cookies.AllCookies
                .Select(c => new CookieDto
                {
                    Name = c.Name,
                    Value = c.Value,
                    Domain = c.Domain,
                    Path = c.Path,
                    Expiry = c.Expiry,
                    Secure = c.Secure,
                    HttpOnly = c.IsHttpOnly
                }).ToList();

            var json = JsonSerializer.Serialize(cookies);
            File.WriteAllText(Path.Combine(ConfigFolder, "cookies.json"), json);
        }


        void loadCookies()
        {
            string path = Path.Combine(ConfigFolder, "cookies.json");
            if (!File.Exists(path)) return;

            var json = File.ReadAllText(path);
            var dtos = JsonSerializer.Deserialize<List<CookieDto>>(json);

            foreach (var dto in dtos)
            {
                var seleniumCookie = new OpenQA.Selenium.Cookie(
                    dto.Name,
                    dto.Value,
                    dto.Domain,
                    dto.Path,
                    dto.Expiry
                );
                driver.Manage().Cookies.AddCookie(seleniumCookie);

            }
        }


        private void setYellowBorder()
        {
            driver.ExecuteScript("document.body.classList.remove(\"blue_top\"); document.body.classList.remove(\"green_top\"); \n document.body.classList.add(\"yellow_top\")");
        }

        private void setGreenBorder()
        {
            driver.ExecuteScript("document.body.classList.remove(\"blue_top\"); document.body.classList.remove(\"yellow_top\"); \n document.body.classList.add(\"green_top\")");
        }

        private void setBlueBorder()
        {
            driver.ExecuteScript("document.body.classList.remove(\"yellow_top\"); \n document.body.classList.add(\"blue_top\")");
        }


        public void SeleniumMain()
        {
            driver = new FirefoxDriver();
            driver.Navigate().GoToUrl("https://www.khanacademy.org");
            try
            {
                loadCookies();
            }
            catch (Exception e){
                Console.WriteLine("Could not read cookies: " + e.Message);
                Console.WriteLine("Re-starting...");
                driver.Close();
                driver = new FirefoxDriver(); // File corrupted, start fresh
            }
            string currentUrl = "";
            driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(2);
            //

            KhanEngine khanEngine = new KhanEngine();

            while (true)
            {

                if (driver.Url != currentUrl)
                {
                    currentUrl = driver.Url;

                    driver.ExecuteScript(styleCssScript);
                    storeCookies();
                }
                
                try
                {
                    switch (khanEngine.checkStatus(driver))
                    {
                        case QuizEngine.Status.INACTIVE:
                            setYellowBorder();
                            break;
                        case QuizEngine.Status.CORRECT:
                            setGreenBorder();
                            break;
                        case QuizEngine.Status.SOLVING:
                            setBlueBorder();
                            break;
                    }
                }
                catch (OpenQA.Selenium.NoSuchWindowException)
                {
                    storeCookies();
                    return;
                }
                catch (OpenQA.Selenium.WebDriverException ex)
                {
                    if (ex.Message.Contains("marionette"))
                    {
                        storeCookies();
                        return;
                    }
                    throw;
                }
                System.Threading.Thread.Sleep(100);
            }






        }



    }
}
