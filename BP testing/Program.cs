using BP_testing;
using Buttplug.Client;
using Buttplug.Core;
using Lib.ConsoleHelper;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Firefox;
using System;
using System.Runtime;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace AsyncExample
{

    class KinkAcademy
    {

        
        KASettings settings = new KASettings();
        bool currentState;
        static void OnDeviceAdded(object o, DeviceAddedEventArgs args)
        {
            Console.WriteLine($"Device ${args.Device.Name} connected");
        }

        async Task AwaitExample()
        {
            // In CSharp, anything that will block is awaited. For instance, if
            // we're going to connect to a remote server, that might take some
            // time due to the network connection quality, or other issues. To
            // deal with that, we use async/await.
            //
            // For now, you can ignore the API calls here, since we're just
            // talking about how our API works in general. Setting up a
            // connection is discussed more in the Connecting section of this
            // document.
            var connector =
                new ButtplugWebsocketConnector(
                    new Uri(settings.hostname));
            var client =
                new ButtplugClient("Kink Academy");

            // As an example of events, we'll assume the server might send the
            // client notifications about new devices that it has found. The
            // client will let us know about this via events.
            client.DeviceAdded += OnDeviceAdded;

            // As an example response/reply messages, we'll use our Connect API.
            // Connecting to a server requires the client and server to send
            // information back and forth, so we'll await that while those
            // transfers happen. It is possible for these to be slow, depending
            // on if network is being used and other factors)
            //
            // If something goes wrong, we throw, which breaks out of the await.
            bool connected = false;
            while (!connected)
            {
                try
                {
                    await client.ConnectAsync(connector);
                    connected = true;
                }
                catch (ButtplugClientConnectorException ex)
                {
                    Console.WriteLine(
                        "Can't connect to Buttplug Server, retrying... " +
                        $"Message: {(ex.InnerException ?? ex).Message}");

                    connector = new ButtplugWebsocketConnector(new Uri(settings.hostname));
                    await Task.Delay(5000);
                    
                }
                catch (ButtplugHandshakeException ex)
                {
                    Console.WriteLine(
                        "Handshake with Buttplug Server, exiting!" +
                        $"Message: { (ex.InnerException ?? ex).Message}");
                    return;

                }
            }
            // There's also no requirement that the tasks returned from these
            // methods be run immediately. Each method returns a task which will
            // not run until awaited, so we can store it off and run it later,
            // run it on the scheduler, etc...
            //
            // As a rule, if you don't want to worry about all of the async task
            // scheduling and what not, you can just use "await" on methods when
            // you call them and they'll block until return. This is the easiest
            // way to work sometimes.
            var startScanningTask = client.StartScanningAsync();
            try
            {
                await startScanningTask;
            }
            catch (ButtplugException ex)
            {
                Console.WriteLine(
                    $"Scanning failed: {ex.InnerException.Message}");
            }
        }



        public static KAWebDriver kaWebDriver = new KAWebDriver();
       

        void MainMenu()
        {
            while (true) { 
                ChoiceMenu menu = new ChoiceMenu();
                menu.Options.Add(new MenuItem
                {
                    Title = "Run",
                    Value = "run"
                });
                menu.Options.Add(new MenuItem
                {
                    Title = "Settings",
                    Value = "settings"
                });
                menu.Options.Add(new MenuItem
                {
                    Title = "Exit",
                    Value = "exit"
                });
                var selectedLanguage = menu.ReadChoice(false);

                switch (selectedLanguage.Value)
                {
                    case "run":
                        Console.WriteLine("Loading browser, please wait...");
                        try
                        {
                            kaWebDriver.SeleniumMain();
                        }
                        catch(Exception e)
                        {
                            Console.WriteLine("Encoutered an error : " + e.Message);
                        }
                        Console.WriteLine("Press any key to continue...");
                        Console.ReadKey();

                        break;
                    case "settings":
                        SettingsTUI stui = new SettingsTUI
                        {
                            settings = settings
                        };
                        stui.Show();
                        settings = stui.settings;
                        break;
                    case "exit":
                        Console.WriteLine("\0");
                        return;
                }
            }
        }

        
        public void Run()
        {
            Console.WriteLine("Starting Kink Academy...");
            settings = KASettings.Load();
            MainMenu();
        }
    }
    class Program
    {
        static void Main(string[] args)
        {
            KinkAcademy kinkAcademy = new KinkAcademy();
            kinkAcademy.Run();
        }
    }
}