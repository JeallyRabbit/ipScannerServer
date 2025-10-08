using Spectre.Console;
using System.Text.Json;

namespace MyApp
{

    class Database
    {
        //address, port, database name, postgre username, password
        public string address { get; set; }
        public string port { get; set; }
        public string databaseName { get; set; }
        public string username { get; set; }

        public Database(string address, string port, string databaseName, string username)
        {
            this.address = address;
            this.port = port;
            this.databaseName = databaseName;
            this.username = username;
        }
    }
    internal class Program
    {
        static void Main(string[] args)
        {
            String currentDir = Directory.GetCurrentDirectory();

            int menu = 0;
            var choice = "";
            int height = AnsiConsole.Console.Profile.Height;
            while (true)
            {
                if (menu == -1)
                {
                    break;
                }
                if (menu == 0)
                {
                    System.Console.Clear();
                    choice = AnsiConsole.Prompt(
                        new SelectionPrompt<string>()
                        .Title("Select an option:")
                        .PageSize(5)
                        .AddChoices(new[] { "Connect server to database", "Exit" }));
                    if (choice == "Connect server to database")
                    {
                        menu = 1;
                    }
                    else
                    {
                        menu = -1;
                    }
                }
                if (menu == 1)
                {
                    System.Console.Clear();
                    var serverConnectionChoice = AnsiConsole.Prompt(
                    new SelectionPrompt<string>()
                    .Title("Choose server connection configuration method:")
                    .PageSize(5)
                    .AddChoices(new[] { "Load from .JSON", "Input manually", "Return" }));




                    if (serverConnectionChoice == "Load from .JSON")
                    {
                        menu = 2;
                    }
                    else if (serverConnectionChoice == "Input manually")
                    {
                        menu = 3;
                    }
                    else if (serverConnectionChoice == "Return")
                    {
                        menu = 0;

                    }


                }
                if (menu == 2)
                {
                    System.Console.Clear();


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


                    var fileSelection = AnsiConsole.Prompt(
                    new SelectionPrompt<string>()
                    .Title("Choose file:")
                    .PageSize(height)
                    .AddChoices(fileNames));

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
                        var obj = JsonSerializer.Deserialize<object>(json);
                    }
                    else
                    {
                        currentDir += $"\\{fileSelection}";
                        Console.WriteLine("Chosen DIRECTORY");
                    }

                    AnsiConsole.MarkupLine($"selected file extension {Path.GetExtension(fileSelection)}");
                }
                if (menu == 3)
                {
                    var serverNameInput = AnsiConsole.Prompt(
                    new TextPrompt<string>("[[localhost]] Enter erver hostname/ipAddress?:").AllowEmpty());
                    var serverName = (serverNameInput != "") ? serverNameInput : "localhost";

                    var portInput = AnsiConsole.Prompt(
                    new TextPrompt<string>("[[5432]] Enter port number:").AllowEmpty());
                    string portNumber = (portInput != "") ? portInput : "5432";

                    var databaseNameInput = AnsiConsole.Prompt(
                    new TextPrompt<string>("[[postgress]] Enter data base name:").AllowEmpty());
                    string dataBaseName = (databaseNameInput != "") ? databaseNameInput : "postgress";

                    var usernameInput = AnsiConsole.Prompt(
                    new TextPrompt<string>("[[postgress]] Enter username:").AllowEmpty());
                    string username = (usernameInput != "") ? usernameInput : "postgress";

                    var passwordInput = AnsiConsole.Prompt(
                    new TextPrompt<string>("[[postgress]] Enter user password:").AllowEmpty().Secret());
                    string password = (passwordInput != "") ? passwordInput : "";

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
                                "portNumber": "{{portNumber}}",
                                "dataBaseName": "{{dataBaseName}}", 
                                "username": "{{username}}"
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
                        Database toSave = new Database(serverName, portNumber, dataBaseName, username);

                        string serializedToSave = JsonSerializer.Serialize(toSave);

                        string fileName = currentDir + Path.DirectorySeparatorChar + dataBaseName + ".json";
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
                    menu = 4;

                    //port, database name, postgre username, password
                }
                if (menu == 4)
                {
                    //waiting and operating connections from clients
                }
            }
        }
    }
}