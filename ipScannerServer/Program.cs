using Dapper;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Spectre.Console;
using System.Net;
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



    public class ipResponse
    {
        public string address { get; set; }
        public string hostname { get; set; }
        public string lastLoggedUser { get; set; }
        public string operatingSystem { get; set; }
        public string model { get; set; }
        public string serialNumber { get; set; }
        public int procGen { get; set; }
        public DateTime lastCheckedDate { get; set; }
        public DateTime lastFoundDate { get; set; }

        public bool successFinding { get; set; }

        public ipResponse(string address, DateTime lastCheckedDate)
        {
            this.address = address;
            this.lastCheckedDate = lastCheckedDate;
        }
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
        const int CLEAR_LEASE_OWNERS_INTERVAL = 600000;// 600 sec between clearing lease_owners in database
        const int REQUEST_PACKAGE_SIZE = 30;

        const string SELECTION_BACK = "back";


        public static string url = "";


        static async Task Main(string[] args)
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
                    .AddChoices(new[] { "Load from .JSON", "Input manually", "Exit" }));




                    if (serverConnectionChoice == "Load from .JSON")
                    {
                        menu = MENU_DATABASE_JSON;
                    }
                    else if (serverConnectionChoice == "Input manually")
                    {
                        menu = MENU_DATABASE_INPUT;
                    }
                    else if (serverConnectionChoice == "Exit")
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
                                                    new TextPrompt<string>($"[[postgres]] Enter user: [red]{obj.getUsername()}[/] password:").AllowEmpty().Secret());
                                string password = (passwordInput != "") ? passwordInput : "postgres";

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

                    connectionString = $"Host={serverName};Port={portNumber};Database={dataBaseName};User Id={username};Password={password}";



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

                        /*options.ListenAnyIP(60718, listenOpts =>
                        {
                            listenOpts.UseHttps();            // HTTPS
                        });*/
                    });
                    string hostname = Dns.GetHostName();
                    string ip = Dns.GetHostAddresses(Dns.GetHostName())?.ToString() ?? "localhost";
                    //builder.WebHost.UseUrls([$"https://{hostname}:60718", $"https://{ip}:60718"]);

                    builder.WebHost.UseUrls(["https://*:60718", "http://*:60718"]);

                    bool successConnectToDataBase = true;
                    try
                    {

                        //testing connection
                        using var conn = new NpgsqlConnection(connectionString);
                        await conn.OpenAsync();
                        conn.Close();

                    }
                    catch (System.InvalidOperationException)
                    {
                        successConnectToDataBase = false;
                        menu = MENU_DATABASE_CONNECTION_TYPE;
                        AnsiConsole.Markup("[grey]Press [bold]<Enter>[/] to continue...[/]");
                        Console.ReadLine();

                    }
                    catch (Npgsql.NpgsqlException ex)
                    {
                        successConnectToDataBase = false;
                        menu = MENU_DATABASE_CONNECTION_TYPE;
                        if (ex.SqlState == "28P01" || ex.SqlState == "28000")   // Invalid password for provided user
                        {
                            AnsiConsole.MarkupLine($"[red]{ex.Message}[/]");
                            AnsiConsole.Markup("[grey]Press [bold]<Enter>[/] to continue...[/]");
                            Console.ReadLine();
                        }
                    }
                    catch (Exception ex)
                    {
                        successConnectToDataBase = false;
                        menu = MENU_DATABASE_CONNECTION_TYPE;
                        AnsiConsole.MarkupLine("[red]faled to connect to database[/]");
                        AnsiConsole.MarkupLine(ex.Message);
                        AnsiConsole.Markup("[grey]Press [bold]<Enter>[/] to continue...[/]");
                        Console.ReadLine();
                    }

                    if (successConnectToDataBase)
                    {

                        var app = builder.Build();


                        clearLeaseOwners(connectionString);





                        ////////////////////////////



                        // GET /api/pcs/oldest  →  returns 10 PCs with oldest last_checked_date
                        app.MapGet("/api/pcs/oldest", async (HttpContext context) =>
                        {

                            var senderHostname = context.Request.Headers["Client-Hostname"].ToString();


                            var sql = @"
                            UPDATE devices AS d
                            SET lease_owner = @leaseOwner,
                            lease_end_date = NOW() + INTERVAL '5 minutes'
                            FROM (
                                SELECT ip AS address,
                                hostname AS hostname,
                                last_checked_date AS lastCheckedDate
                                FROM devices
                                WHERE lease_owner IS NULL
                                ORDER BY last_checked_date ASC
                                LIMIT @myLimit
                            ) AS oldest
                            WHERE d.ip = oldest.address
                            RETURNING d.ip AS address, d.hostname AS hostname, d.last_checked_date AS lastCheckedDate;
                            ";

                            try
                            {
                                await using var conn = new NpgsqlConnection(connectionString);

                                var rows = await conn.QueryAsync<IP>(sql, new { leaseOwner = senderHostname, myLimit = REQUEST_PACKAGE_SIZE });

                                Console.WriteLine($"Sending {rows.Count()} records");
                                /*
                                foreach (var r in rows)
                                {
                                    Console.WriteLine(r.address.ToString());
                                }
                                */
                                return Results.Ok(rows);
                            }
                            catch (System.ArgumentNullException ex)
                            {
                                Console.WriteLine(ex);
                                return Results.NotFound();
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine(ex.Message);
                                return Results.NotFound();
                            }

                        });

                        app.MapPut("/api/pcs/response", async (List<ipResponse> pcs) =>
                        {
                            //Console.WriteLine($"Received {pcs.Count} PCs");

                            await using var conn = new NpgsqlConnection(connectionString);

                            foreach (var pc in pcs)
                            {


                                if (pc.successFinding == true)
                                {

                                    string sqlResponse = @"
                                UPDATE devices
                                set lease_owner=NULL,
                                hostname = @Hostname,
                                last_logged_user = @LastLoggedUser, 
                                last_checked_date = @LastCheckedDate,
                                operating_system = @OperatingSystem,
                                last_found_date = @LastFoundDate,
                                model=@Model,
                                serial_number=@SN,
                                proc_gen=@ProcGen
                                WHERE ip= @Address
                                ";
                                    var rows = await conn.QueryAsync<IP>(sqlResponse, new
                                    {
                                        Hostname = pc.hostname,
                                        LastLoggedUser = pc.lastLoggedUser,
                                        LastFoundDate = pc.lastFoundDate,
                                        OperatingSystem = pc.operatingSystem,
                                        Address = pc.address,
                                        LastCheckedDate = pc.lastCheckedDate,
                                        Model = pc.model,
                                        SN = pc.serialNumber,
                                        ProcGen = pc.procGen
                                    });

                                }
                                else // successFinding==false
                                {
                                    string sqlResponse = @"
                                UPDATE devices
                                set lease_owner=NULL,
                                last_checked_date = @LastCheckedDate
                                WHERE ip= @Address
                                ";
                                    var rows = await conn.QueryAsync<IP>(sqlResponse, new { LastCheckedDate = pc.lastCheckedDate, Address = pc.address, ProcGen = pc.procGen });

                                }

                                //Console.WriteLine($"IP: {pc.address},successFinding: {pc.successFinding}, Hostname: {pc.hostname}, LastCheckedDate: {pc.lastCheckedDate}");


                            }



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




        private static async Task clearLeaseOwners(string connectionString = "")
        {
            var conn = new NpgsqlConnection(connectionString);
            while (true)
            {
                AnsiConsole.MarkupLine($"[red]CLEARING LEASE OWNERS !![/]");
                var sql = @"
                            UPDATE devices
                            SET lease_owner = NULL,
                            lease_end_date = NULL
                            WHERE lease_end_date < CURRENT_TIMESTAMP
                            ";
                try
                {

                    var rows = await conn.QueryAsync(sql);

                    AnsiConsole.MarkupLine($"[grey]Removed {rows.Count()} lease owners.[/]");
                    //return Results.Ok(rows);
                }
                catch (System.ArgumentNullException ex)
                {
                    Console.WriteLine(ex);
                    //return Results.NotFound();
                }


                await Task.Delay(CLEAR_LEASE_OWNERS_INTERVAL);
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