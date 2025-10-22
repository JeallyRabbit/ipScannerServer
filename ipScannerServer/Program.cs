using Dapper;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Spectre.Console;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MyApp
{



    class Database
    {
        //address, port, database name, postgre username, password
        [JsonInclude]
        private string address { get; set; }
        [JsonInclude]
        private string port { get; set; }
        [JsonInclude]
        private string databaseName { get; set; }
        [JsonInclude]
        private string username { get; set; }

        public Database(string address, string port, string databaseName, string username)
        {
            this.address = address;
            this.port = port;
            this.databaseName = databaseName;
            this.username = username;
        }

        public string getDatabaseName() => databaseName;
        public string getUsername() => username;
        public string getAddress() => address;
        public string getPort() => port;
    }




    public class IP
    {
        public string address { get; set; }
        public string hostname { get; set; }

        public string lastCheckedDate { get; set; }
    }

    public sealed class AppDbContext(DbContextOptions<AppDbContext> opts) : DbContext(opts)
    {
        public DbSet<IP> IPs => Set<IP>();
        protected override void OnModelCreating(ModelBuilder b)
            => b.Entity<IP>().HasIndex(x => x.lastCheckedDate);
    }

    internal class Program
    {
        const int MENU_EXIT = -1;
        const int MENU_SERVER_CLIENT = 0;
        const int MENU_DATABASE_CONNECTION_TYPE = 1;
        const int MENU_DATABASE_JSON = 2;
        const int MENU_DATABASE_INPUT = 3;
        const int MENU_PROCESS_SERVER_SIDE = 4;


        const string SELECTION_BACK = "back";


        public static string url = "";


        static void Main(string[] args)
        {
            String currentDir = Directory.GetCurrentDirectory();

            int menu = MENU_DATABASE_CONNECTION_TYPE;
            var choice = "";
            int height = AnsiConsole.Console.Profile.Height;
            int port = 60719;
            string connectionString = "";



            while (true)
            {
                if (menu == MENU_EXIT)
                {
                    break;
                }
                if (menu == MENU_SERVER_CLIENT)
                {
                    System.Console.Clear();
                    choice = AnsiConsole.Prompt(
                        new SelectionPrompt<string>()
                        .Title("Select an option:")
                        .PageSize(5)
                        .AddChoices(new[] { "Connect server to database", "Exit" }));
                    if (choice == "Connect server to database")
                    {
                        menu = MENU_DATABASE_CONNECTION_TYPE;
                    }
                    else
                    {
                        menu = MENU_EXIT;
                    }
                }
                if (menu == MENU_DATABASE_CONNECTION_TYPE)
                {
                    System.Console.Clear();
                    var serverConnectionChoice = AnsiConsole.Prompt(
                    new SelectionPrompt<string>()
                    .Title("Choose server connection configuration method:")
                    .PageSize(5)
                    .AddChoices(new[] { "Load from .JSON", "Input manually", "Return" }));




                    if (serverConnectionChoice == "Load from .JSON")
                    {
                        menu = MENU_DATABASE_JSON;
                    }
                    else if (serverConnectionChoice == "Input manually")
                    {
                        menu = MENU_DATABASE_INPUT;
                    }
                    else if (serverConnectionChoice == "Return")
                    {
                        menu = MENU_SERVER_CLIENT;

                    }


                }
                else if (menu == MENU_DATABASE_JSON)
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
                        var json = File.ReadAllText(fileSelection);
                        Database obj = null;
                        try
                        {
                            obj = JsonSerializer.Deserialize<Database>(json);
                        }
                        catch (System.Text.Json.JsonException ex)
                        {
                            AnsiConsole.MarkupLine($"[red]Failed to read:[/] {fileSelection}");
                            AnsiConsole.MarkupLine($"[grey]{ex.Message}[/]");
                        }

                        if (obj is not null)
                        {

                            var jsonPrint = new Spectre.Console.Json.JsonText(
                           $$"""
                            { 
                                "Database Host/address": "{{obj.getAddress()}}", 
                                "portNumber": "{{obj.getPort()}}",
                                "dataBaseName": "{{obj.getDatabaseName()}}", 
                                "username": "{{obj.getUsername()}}"
                            }
                        """);

                            AnsiConsole.Write(
                                new Panel(jsonPrint)
                                    .Header("Data loaded from JSON")
                                    .Collapse()
                                    .RoundedBorder()
                                    .BorderColor(Color.Yellow));

                            bool isJsonValid = AnsiConsole.Prompt(
                        new TextPrompt<bool>("Is this data correct?")
                        .AddChoice(true)
                        .AddChoice(false)
                        .DefaultValue(true)
                        .WithConverter(choice => choice ? "y" : "n"));


                            if (isJsonValid)
                            {
                                var passwordInput = AnsiConsole.Prompt(
                                                    new TextPrompt<string>($"[[postgres]] Enter user: {obj.getUsername()} password:").AllowEmpty().Secret());
                                string password = (passwordInput != "") ? passwordInput : "";

                                connectionString = $"Host={obj.getAddress()};Port={obj.getPort()};Database={obj.getDatabaseName()};User Id={obj.getUsername()};Password={password};Ssl Mode=Disable";
                                menu = MENU_PROCESS_SERVER_SIDE;
                            }
                            else
                            {
                                menu = MENU_DATABASE_CONNECTION_TYPE;
                            }


                        }
                        else
                        {
                            AnsiConsole.MarkupLine($"[yellow]File loaded but empty/null object:[/] {fileSelection}");
                        }

                    }
                    else if (fileSelection == SELECTION_BACK)
                    {
                        menu = MENU_DATABASE_CONNECTION_TYPE;
                    }
                    else
                    {
                        currentDir += $"\\{fileSelection}";
                        Console.WriteLine("Chosen DIRECTORY");
                    }

                    AnsiConsole.MarkupLine($"selected file extension {Path.GetExtension(fileSelection)}");



                }
                else if (menu == MENU_DATABASE_INPUT)
                {
                    var serverNameInput = AnsiConsole.Prompt(
                    new TextPrompt<string>("[[localhost]] Enter database hostname/ipAddress?:").AllowEmpty());
                    var serverName = (serverNameInput != "") ? serverNameInput : "localhost";

                    var portInput = AnsiConsole.Prompt(
                    new TextPrompt<string>("[[5432]] Enter port number:").AllowEmpty());
                    string portNumber = (portInput != "") ? portInput : "5432";

                    var databaseNameInput = AnsiConsole.Prompt(
                    new TextPrompt<string>("[[postgres]] Enter data base name:").AllowEmpty());
                    string dataBaseName = (databaseNameInput != "") ? databaseNameInput : "postgres";

                    var usernameInput = AnsiConsole.Prompt(
                    new TextPrompt<string>("[[postgres]] Enter username:").AllowEmpty());
                    string username = (usernameInput != "") ? usernameInput : "postgres";

                    var passwordInput = AnsiConsole.Prompt(
                    new TextPrompt<string>("[[postgres]] Enter user password:").AllowEmpty().Secret());
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
                        var database = new Database(serverName, portNumber, dataBaseName, username);
                        Database toSave = new Database(serverName, portNumber, dataBaseName, username);

                        string serializedToSave = JsonSerializer.Serialize(toSave);

                        string fileName = currentDir + Path.DirectorySeparatorChar + "serverConnection_" + dataBaseName + ".json";
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

                    connectionString = $"Host={serverNameInput};Port={portNumber};Database={dataBaseName};User Id={username};Password={password}";

                    menu = MENU_PROCESS_SERVER_SIDE;

                    //port, database name, postgre username, password
                }
                else if (menu == MENU_PROCESS_SERVER_SIDE)
                {
                    //waiting and operating connections from clients


                    var builder = WebApplication.CreateBuilder(args);
                    builder.WebHost.ConfigureKestrel(options =>
                    {
                        options.ListenAnyIP(60719);           // HTTP
                        options.ListenAnyIP(60718, listenOpts =>
                        {
                            listenOpts.UseHttps();            // HTTPS
                        });
                    });


                    var cs = "";
                    bool successConnectToDataBase = true;
                    try
                    {
                        cs = builder.Configuration.GetConnectionString("Postgres")
                             ?? Environment.GetEnvironmentVariable("POSTGRES_CS")
                             ?? connectionString
                             ?? throw new InvalidOperationException("DB connection string missing.");
                    }
                    catch (System.InvalidOperationException)
                    {
                        AnsiConsole.MarkupLine($"[red]Can't connect[/] using: {cs}");
                        successConnectToDataBase = false;
                        menu = 1;
                        AnsiConsole.Markup("[grey]Press [bold]<Enter>[/] to continue...[/]");
                        Console.ReadLine();


                    }


                    if (successConnectToDataBase || cs != "")
                    {
                        var app = builder.Build();

                        // GET /api/pcs/oldest  →  returns 10 PCs with oldest last_checked_date
                        app.MapGet("/api/pcs/oldest", async () =>
                        {
                            const string sql = """
                            SELECT
                                ip AS address,
                                hostname AS hostname,
                                last_checked_date as lastCheckedDate
                            FROM devices
                            ORDER BY last_checked_date ASC
                            LIMIT 10;
                            """;


                            //try
                            //{
                            await using var conn = new NpgsqlConnection(cs);
                            AnsiConsole.MarkupLine("cs: " + cs);
                            var rows = await conn.QueryAsync<IP>(sql);
                            Console.Write(rows.ToString());
                            foreach (var row in rows)
                            {
                                //add marking rows:
                                //lease_woner - ip of host requesting GET
                                AnsiConsole.MarkupLine($"{row.address} {row.hostname} {row.lastCheckedDate}");
                            }
                            //}
                            //catch (Exception ex)
                            //{
                            //    AnsiConsole.MarkupLine(ex.Message);
                            //}


                            return Results.Ok(rows);
                        });


                        var shutdown = new ManualResetEventSlim(false);
                        Console.CancelKeyPress += (_, e) =>
                        {
                            Console.WriteLine("Ctrl+C pressed, shutting down...");
                            e.Cancel = true; // prevent process termination; allow graceful shutdown
                            menu = MENU_DATABASE_CONNECTION_TYPE;
                            app.Lifetime.StopApplication();
                        };




                        app.Run();
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