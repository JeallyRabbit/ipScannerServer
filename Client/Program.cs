using Spectre.Console;
using System.Management;
using System.Net;
using System.Net.Http.Json;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Client
{

    public class ipResponse
    {
        public string address { get; set; }
        public string hostname { get; set; }
        public string lastLoggedUser { get; set; }

        public string operatingSystem { get; set; }
        public DateTime lastCheckedDate { get; set; }
        public DateTime lastFoundDate { get; set; }

        public bool successFinding { get; set; }

        public ipResponse(string address, DateTime lastCheckedDate)
        {
            this.address = address;
            this.lastCheckedDate = lastCheckedDate;
            lastLoggedUser = "";
            hostname = "";
            lastFoundDate = new DateTime();
        }
    }
    public class IP
    {
        public string address { get; set; }
        public string hostname { get; set; }
        public string lastCheckedDate { get; set; }

        public bool isOperated { get; set; }

        public IP(string address, string hostname, string lastCheckedDate)
        {
            this.address = address;
            this.hostname = hostname;
            this.lastCheckedDate = lastCheckedDate;
        }
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

        public void setAddress(string address)
        {
            this.address = address;
        }

        public void setPort(string port)
        {
            this.port = port;
        }
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

        const int MAX_PING_COUNTER = 3;

        const string SELECTION_BACK = "back";
        const string NO_LOGGED_USER = "no_user";

        const int PING_TIMEOUT = 1000;

        const int FRAMES = 4;


        public static string url = "";
        static async Task Main(string[] args)
        {
            string currentDir = Directory.GetCurrentDirectory();

            int menu = MENU_CONNECT_TO_SERVER_TYPE;
            var choice = "";
            int height = AnsiConsole.Console.Profile.Height;
            int port = 60719;
            string connectionString = "";
            Server serverData = null;

            // Used to check if already started displaying data - to not start multiple workers
            bool startedDisplaying = false;

            while (true)
            {

                if (menu == MENU_EXIT)
                {
                    break;
                }
                if (menu == MENU_CONNECT_TO_SERVER_TYPE)
                {
                    //getting server connection data
                    Console.Clear();
                    var clientConnectionChoice = AnsiConsole.Prompt(
                    new SelectionPrompt<string>()
                    .Title("Choose client connection configuration method:")
                    .PageSize(5)
                    .AddChoices(new[] { "Load from .JSON", "Input manually", "Exit" }));

                    //resseting state of displaying live data of scanner
                    startedDisplaying = false;

                    if (clientConnectionChoice == "Load from .JSON")
                    {
                        menu = MENU_CLIENT_JSON;
                    }
                    else if (clientConnectionChoice == "Input manually")
                    {
                        menu = MENU_CLIENT_INPUT;
                    }
                    else if (clientConnectionChoice == "Exit")
                    {
                        menu = MENU_SERVER_CLIENT;

                    }
                }
                else if (menu == MENU_CLIENT_JSON)
                {
                    Console.Clear();
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
                        catch (JsonException ex)
                        {
                            menu = MENU_CLIENT_JSON;
                            AnsiConsole.MarkupLine($"[red]Failed to read:[/] {fileSelection}");
                            AnsiConsole.MarkupLine($"[grey]{ex.Message}[/]");
                        }

                        if (serverData is not null)
                        {
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
                    var serverName = serverNameInput != "" ? serverNameInput : "localhost";

                    var portInput = AnsiConsole.Prompt(
                    new TextPrompt<string>("[[60718]] Enter port number:").AllowEmpty());
                    string portNumber = portInput != "" ? portInput : "60718";



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



                    serverData = new Server(serverName, portNumber);

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
                        catch (UnauthorizedAccessException)
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

                    AnsiConsole.Clear();
                    AnsiConsole.MarkupLine("Processing client side");

                    url = $"https://{serverData.getAddress()}:{serverData.getPort()}/api/pcs/oldest";


                    try
                    {

                        using var client = new HttpClient();
                        client.DefaultRequestHeaders.Add("Client-Hostname", Dns.GetHostName().ToString());


                        List<IP> addresses = client.GetFromJsonAsync<List<IP>>(url).GetAwaiter().GetResult();


                        if (addresses is null)
                        {
                            AnsiConsole.MarkupLine($"No data acquired from server");
                            //Add sleeping here ?
                        }
                        else
                        {
                            addresses.Sort((a, b) => a.address.CompareTo(b.address));

                            foreach (var a in addresses)
                            {
                                a.isOperated = false;
                            }


                            // List into which put results of scanning
                            List<ipResponse> ipResponses = new List<ipResponse>();



                            //For stoping scanning
                            var cts = new CancellationTokenSource();

                            Console.CancelKeyPress += (_, e) =>
                            {
                                e.Cancel = true;
                                cts.Cancel();
                            };


                            var table = new Spectre.Console.Table();








                            //foreach (var ip in addresses)
                            var scanTask = Parallel.ForEachAsync(
                                addresses,
                                new ParallelOptions { CancellationToken = cts.Token },
                                async (ip, ct) =>
                            {
                                ip.isOperated = true;





                                /*
                                if (cts.IsCancellationRequested)
                                {
                                    menu = MENU_CONNECT_TO_SERVER_TYPE;
                                    AnsiConsole.MarkupLine("[yellow]Scan stopped[/]");
                                    AnsiConsole.Markup("[grey]Press [bold]<Enter>[/] to continue...[/]");
                                    Console.ReadLine();
                                    break;
                                }
                                */

                                Ping pingSender = new Ping();

                                int pingCounter = 0;
                                string data = "aa";
                                byte[] buffer = Encoding.ASCII.GetBytes(data);

                                ipResponse response = new ipResponse(ip.address.ToString(), DateTime.Now);
                                bool pingSuccess = false;

                                IPAddress pingAddress = IPAddress.Parse(ip.address.ToString());

                                while (pingCounter < MAX_PING_COUNTER)
                                {

                                    PingReply reply = pingSender.Send(pingAddress, PING_TIMEOUT);
                                    if (reply.Status == IPStatus.Success)
                                    {
                                        response.successFinding = true;
                                        response.lastFoundDate = DateTime.Now;

                                        try
                                        {
                                            IPHostEntry host = Dns.GetHostByAddress(pingAddress);

                                            response.hostname = host.HostName;

                                        }
                                        catch (SocketException ex)
                                        {
                                            response.hostname = "";

                                        }



                                        try// getting current logged user
                                        {
                                            var scope = new ManagementScope($@"\\{pingAddress}\root\cimv2");
                                            scope.Connect();

                                            var query = new ObjectQuery("SELECT UserName FROM Win32_ComputerSystem");
                                            using var searcher = new ManagementObjectSearcher(scope, query);

                                            foreach (ManagementObject mo in searcher.Get())
                                            {

                                                var aux = mo["UserName"];
                                                response.lastLoggedUser = aux?.ToString() ?? "-";

                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            //never catched that exception too
                                            response.lastLoggedUser = "-";
                                        }



                                        try//getting windows version
                                        {
                                            var scope = new ManagementScope($@"\\{pingAddress}\root\cimv2");
                                            scope.Connect();

                                            // Win32_OperatingSystem instead of Win32_ComputerSystem
                                            var query = new ObjectQuery(
                                                "SELECT Caption, Version, BuildNumber FROM Win32_OperatingSystem");

                                            using var searcher = new ManagementObjectSearcher(scope, query);
                                            using var results = searcher.Get();

                                            foreach (ManagementObject os in results)
                                            {
                                                var caption = (string?)os["Caption"];
                                                var version = (string?)os["Version"];
                                                var buildStr = (string?)os["BuildNumber"];

                                                int.TryParse(buildStr, out var build);

                                                // Windows 11 starts at build 22000
                                                var isWindows11 = build >= 22000;

                                                response.operatingSystem = isWindows11 ? "Windows 11" : "Windows 10";
                                            }


                                        }
                                        catch (Exception ex)
                                        {
                                            response.operatingSystem = "-";
                                        }

                                        pingSuccess = true;
                                        response.successFinding = true;
                                        break;
                                    }
                                    pingCounter++;




                                }

                                if (pingSuccess == false)
                                {
                                    response.successFinding = false;
                                }

                                ipResponses.Add(response);
                                ip.isOperated = false;



                            }
                            );//for parallel


                            //Printing progress
                            try
                            {

                                AnsiConsole.Live(table)
                               .AutoClear(true)
                               .StartAsync(async ctx =>
                               {
                                   startedDisplaying = true;
                                   var refresh = TimeSpan.FromMilliseconds(200);
                                   int frame = 0;
                                   while (!cts.IsCancellationRequested && ipResponses.Count() != addresses.Count())
                                   {
                                       //AnsiConsole.Clear();
                                       ctx.UpdateTarget(BuildTable(ipResponses, addresses, frame));


                                       frame++;
                                       if (frame >= FRAMES)
                                       {
                                           frame = 0;
                                       }
                                       await Task.Delay(refresh, cts.Token).ContinueWith(_ => { });
                                   }
                                   throw new OperationCanceledException("Cancel");
                               });

                            }
                            catch (OperationCanceledException ex)
                            {
                                AnsiConsole.MarkupLine("[yellow]Stopping live view...[/]");
                                AnsiConsole.MarkupLine("[yellow]Scan stopped[/]");
                                AnsiConsole.Markup("[grey]Press [bold]<Enter>[/] to continue...[/]");
                                Console.ReadLine();
                                AnsiConsole.Clear();
                                menu = MENU_CONNECT_TO_SERVER_TYPE;
                            }



                            await scanTask;

                            //sending response to server
                            sendResponseToServer(serverData.getAddress(), serverData.getPort(), client, ipResponses).Wait();



                            if (cts.IsCancellationRequested)
                            {
                                menu = MENU_CONNECT_TO_SERVER_TYPE;
                                AnsiConsole.MarkupLine("[yellow]Scan stopped[/]");
                                AnsiConsole.Markup("[grey]Press [bold]<Enter>[/] to continue...[/]");
                                Console.ReadLine();
                                break;
                            }


                            //stoping displaying table
                            cts.Cancel();
                        }


                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[2 Error] {ex.GetType()} {ex.Message}");
                        //Console.WriteLine($"Url: {url}");
                        AnsiConsole.Markup("[grey]Press [bold]<Enter>[/] to continue (errorrrrrr)...[/]");
                        Console.ReadLine();
                    }


                }
            }
        }



        static Spectre.Console.Table BuildTable(List<ipResponse> ipResponses, List<IP> addresses = null, int counter = 0)
        {
            addresses = addresses ?? new List<IP>();
            var tab = new Spectre.Console.Table();
            tab.Title("[bold]Live Ping[/]  (Ctrl+C to stop)");
            tab.AddColumn(new TableColumn(new Markup("[green] IpAddress [/]")));
            tab.AddColumn(new TableColumn("[blue] Hostname [/]"));
            tab.AddColumn(new TableColumn("[blue] lastLoggedUser [/]"));
            tab.AddColumn(new TableColumn("[blue] LastCheckedDate [/]"));
            tab.AddColumn(new TableColumn("[blue] OperatingSystem [/]"));
            tab.AddColumn(new TableColumn("[blue] lastFoundDate [/]"));

            int responsesAmount = ipResponses.Count();
            if (responsesAmount > 0)
            {
                foreach (var re in ipResponses)
                {
                    var addr = new Markup($"{re.address.ToString()}");
                    var hostname = new Markup($"{re.hostname.ToString()}");
                    var lastLoggedUser = new Markup($"{re.lastLoggedUser.ToString()}");
                    if (!re.successFinding)
                    {

                        tab.AddRow($"[red]{re.address.ToString()}[/]", "[red] - not found -[/]", "[red] - not found -[/]", re.lastCheckedDate.ToString(), "-", re.lastFoundDate.ToString());

                    }
                    else
                    {
                        tab.AddRow($"[green]{re.address.ToString()}[/]", re.hostname.ToString(), re.lastLoggedUser.ToString(), $"[green]{re.lastCheckedDate.ToString()}[/]", $"[green]{re.operatingSystem.ToString()}[/]", $"[green]{re.lastFoundDate.ToString()}[/]");

                    }
                }
            }

            if (addresses.Count() > 0)
            {
                foreach (var re in addresses)
                {
                    if (!(ipResponses.Any(x => x.address == re.address)))
                    {
                        if (re.isOperated)
                        {
                            List<string> myFrames = new List<string> { "-", "/", "|", "\\" };
                            tab.AddRow(re.address.ToString(), myFrames[counter], "---", re.lastCheckedDate.ToString(), "---");
                        }
                        else
                        {
                            tab.AddRow(re.address.ToString(), "---", "---", re.lastCheckedDate.ToString(), "---");
                        }

                    }

                }
            }

            tab.Collapse();
            return tab;
        }



        private static async Task sendResponseToServer(string serverIp, string serverPort, HttpClient client, List<ipResponse> ipResponses)
        {
            string url = $"https://{serverIp}:{serverPort}/api/pcs/response";
            HttpResponseMessage httpResponse = await client.PutAsJsonAsync(url, ipResponses);

            if (httpResponse.IsSuccessStatusCode)
            {
                //Console.WriteLine("✅ Update successful!");
                string result = await httpResponse.Content.ReadAsStringAsync();
                //Console.WriteLine($"Response: {result}");
            }
            else
            {
                //Console.WriteLine($"❌ Error: {httpResponse.StatusCode}");
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

            foreach (var file in files)
            {
                if (Path.GetExtension(file).ToLower() == ".json")
                {
                    fileNames.Add(Path.GetFileName(file));

                }
            }

            foreach (var directory in directories)
            {
                fileNames.Add(Path.GetFileName(directory));
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