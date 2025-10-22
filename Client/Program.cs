using Spectre.Console;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;


namespace MyApp
{

    public class IP
    {
        public string address { get; set; }
        public string hostname { get; set; }

        public string lastCheckedDate { get; set; }
    }

    class Server
    {
        [JsonInclude]
        private string address { get; set; }
        [JsonInclude]
        private string port { get; set; }

        public Server(string address, string port)
        {
            this.address = address;
            this.port = port;
        }

        public string getAddress() => address;
        public string getPort() => port;
    }


    internal class Program
    {
        const int MENU_EXIT = -1;
        const int MENU_SERVER_CLIENT = 0;
        const int MENU_DATABYSE_CONNECTION_TYPE = 1;
        const int MENU_DATABASE_JSON = 2;
        const int MENU_DATABASE_INPUT = 3;
        const int MENU_PROCESS_SERVER_SIDE = 4;

        const int MENU_CONNECT_TO_SERVER_TYPE = 11;
        const int MENU_CLIENT_JSON = 12;
        const int MENU_CLIENT_INPUT = 13;
        const int MENU_PROCESS_CLIENT = 14;

        const string SELECTION_BACK = "back";


        public static string url = "";
        static void Main(string[] args)
        {
            String currentDir = Directory.GetCurrentDirectory();

            int menu = MENU_CONNECT_TO_SERVER_TYPE;
            var choice = "";
            int height = AnsiConsole.Console.Profile.Height;
            int port = 60719;
            string connectionString = "";
            Server serverData = null;
            while (true)
            {

                if (menu == MENU_EXIT)
                {
                    break;
                }
                if (menu == MENU_CONNECT_TO_SERVER_TYPE)
                {
                    //getting server connection data
                    System.Console.Clear();
                    var clientConnectionChoice = AnsiConsole.Prompt(
                    new SelectionPrompt<string>()
                    .Title("Choose client connection configuration method:")
                    .PageSize(5)
                    .AddChoices(new[] { "Load from .JSON", "Input manually", "Return" }));




                    if (clientConnectionChoice == "Load from .JSON")
                    {
                        menu = MENU_CLIENT_JSON;
                    }
                    else if (clientConnectionChoice == "Input manually")
                    {
                        menu = MENU_CLIENT_INPUT;
                    }
                    else if (clientConnectionChoice == "Return")
                    {
                        menu = MENU_SERVER_CLIENT;

                    }
                }
                else if (menu == MENU_CLIENT_JSON)
                {
                    System.Console.Clear();
                    string fileSelection = Program.fileSelection(currentDir, height);

                    if (fileSelection == "..")
                    {
                        if (Path.GetDirectoryName(currentDir) != null)
                        {
                            currentDir = Path.GetDirectoryName(currentDir);
                        }


                    }
                    else if (fileSelection.ToLower().EndsWith(".json"))
                    {

                        Console.WriteLine("Chosen JSON");
                        var json = File.ReadAllText(fileSelection);

                        try
                        {
                            serverData = JsonSerializer.Deserialize<Server>(json);
                        }
                        catch (System.Text.Json.JsonException ex)
                        {
                            menu = MENU_CLIENT_JSON;
                            AnsiConsole.MarkupLine($"[red]Failed to read:[/] {fileSelection}");
                            AnsiConsole.MarkupLine($"[grey]{ex.Message}[/]");
                        }

                        if (serverData is not null)
                        {
                            // create required objects to connect to server
                            //add pringint what was read
                            var jsonPrint = new Spectre.Console.Json.JsonText(
                           $$"""
                            { 
                                "serverAddress": "{{serverData.getAddress()}}", 
                                "portNumber": "{{serverData.getPort()}}"
                            }
                        """);

                            AnsiConsole.Write(
                                new Panel(jsonPrint)
                                    .Header("Data read from JSON")
                                    .Collapse()
                                    .RoundedBorder()
                                    .BorderColor(Color.Yellow));

                            if (serverData.getAddress() is null || serverData.getPort() is null)
                            {
                                AnsiConsole.MarkupLine("[red]Failed to read data[/]");

                                AnsiConsole.Markup("[grey]Press [bold]<Enter>[/] to continue...[/]");
                                Console.ReadLine();
                                menu = MENU_CLIENT_JSON;
                            }
                            else
                            {
                                menu = MENU_PROCESS_CLIENT;
                                AnsiConsole.Markup("[grey]Press [bold]<Enter>[/] to continue...[/]");
                                Console.ReadLine();
                            }



                        }
                        else
                        {
                            AnsiConsole.MarkupLine($"[yellow]File loaded but empty/null object:[/] {fileSelection}");
                        }



                    }
                    else if (fileSelection == SELECTION_BACK)
                    {
                        menu = MENU_CONNECT_TO_SERVER_TYPE;
                    }
                    else
                    {
                        currentDir += $"\\{fileSelection}";
                    }



                }
                else if (menu == MENU_CLIENT_INPUT)
                {
                    var serverNameInput = AnsiConsole.Prompt(
                    new TextPrompt<string>("[[localhost]] Enter server hostname/ipAddress?:").AllowEmpty());
                    var serverName = (serverNameInput != "") ? serverNameInput : "localhost";

                    var portInput = AnsiConsole.Prompt(
                    new TextPrompt<string>("[[60719]] Enter port number:").AllowEmpty());
                    string portNumber = (portInput != "") ? portInput : "60719";



                    var ifSave = AnsiConsole.Prompt(
                    new TextPrompt<bool>("Save to .json?")
                        .AddChoice(true)
                        .AddChoice(false)
                        .DefaultValue(true)
                        .WithConverter(choice => choice ? "y" : "n"));


                    var jsonPrint = new Spectre.Console.Json.JsonText(
                           $$"""
                            { 
                                "serverName": "{{serverName}}", 
                                "portNumber": "{{portNumber}}"
                            }
                        """);

                    AnsiConsole.Write(
                        new Panel(jsonPrint)
                            .Header("Data saved to JSON")
                            .Collapse()
                            .RoundedBorder()
                            .BorderColor(Color.Yellow));


                    if (ifSave == true)
                    {
                        Server toSave = new Server(serverName, portNumber);

                        string serializedToSave = JsonSerializer.Serialize(toSave);

                        string fileName = currentDir + Path.DirectorySeparatorChar + "clientConnection_" + toSave.getAddress() + ".json";
                        bool isSavingSucces = true;
                        try
                        {
                            File.WriteAllText(fileName, serializedToSave);
                        }
                        catch (System.UnauthorizedAccessException)
                        {
                            isSavingSucces = false;
                            AnsiConsole.MarkupLine($"[red]Acces denied[/] to: {fileName}");
                        }

                        if (isSavingSucces)
                        {
                            AnsiConsole.MarkupLine($"[green]Saved to:[/] {fileName}");
                        }


                        AnsiConsole.Markup("[grey]Press [bold]<Enter>[/] to continue...[/]");
                        Console.ReadLine();

                    }
                    menu = MENU_PROCESS_CLIENT;


                }
                else if (menu == MENU_PROCESS_CLIENT)
                {

                    System.Console.Clear();
                    AnsiConsole.MarkupLine("Processing client side");

                    url = $"https://{serverData.getAddress()}:{serverData.getPort()}/api/pcs/oldest";
                    AnsiConsole.MarkupLine($"url {url}");


                    try
                    {

                        using var client = new HttpClient();
                        //client.Timeout = TimeSpan.FromSeconds(5);


                        List<IP> addresses = client.GetFromJsonAsync<List<IP>>(url).GetAwaiter().GetResult();

                        if (addresses is null)
                        {
                            AnsiConsole.MarkupLine($"No data acquired from server");
                            //Add sleeping here ?
                        }
                        else
                        {
                            foreach (var address in addresses)
                            {
                                AnsiConsole.MarkupLine($"{address.address}, {address.hostname}, {address.lastCheckedDate}");



                            }
                            AnsiConsole.Markup("[grey]Press [bold]<Enter>[/] to continue...[/]");
                            Console.ReadLine();
                        }


                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[Error] {ex.Message}");
                        Console.WriteLine($"Url: {url}");
                        AnsiConsole.Markup("[grey]Press [bold]<Enter>[/] to continue...[/]");
                        Console.ReadLine();
                    }


                }
            }
        }

        private static string fileSelection(string currentDir, int height)
        {
            var files = Directory.GetFiles(currentDir);
            var directories = Directory.GetDirectories(currentDir);

            var path = new TextPath(currentDir).RootColor(Color.Red)
            .SeparatorColor(Color.Green)
            .StemColor(Color.Blue)
            .LeafColor(Color.Yellow);

            var panel = new Panel(path);
            panel.Border = BoxBorder.Square;

            AnsiConsole.Write(panel);


            List<string> fileNames = new List<string>();

            fileNames.Add("..");

            foreach (var directory in directories)
            {
                fileNames.Add(Path.GetFileName(directory));
            }

            foreach (var file in files)
            {
                if (Path.GetExtension(file).ToLower() == ".json")
                {
                    fileNames.Add(Path.GetFileName(file));

                }


            }
            fileNames.Add(SELECTION_BACK);


            var fileSelection = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
            .Title("Choose file:")
            .PageSize(height)
            .AddChoices(fileNames));

            return fileSelection;
        }


    }
}