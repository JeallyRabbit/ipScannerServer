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
        public DateTime lastCheckedDate { get; set; }
        public DateTime lastFoundDate { get; set; }

        public bool successFinding;

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

        const int PING_TIMEOUT = 2000;//5 sec




        public static string url = "";
        static void Main(string[] args)
        {
            string currentDir = Directory.GetCurrentDirectory();

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
                    Console.Clear();
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

                    Console.Clear();
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

                            List<ipResponse> ipResponses = new List<ipResponse>();


                            var cts = new CancellationTokenSource();
                            Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };

                            var table = new Spectre.Console.Table();

                            AnsiConsole.Live(table)
                            .AutoClear(false)
                            .StartAsync(async ctx =>
                            {

                                var refresh = TimeSpan.FromMilliseconds(100);
                                while (!cts.IsCancellationRequested)
                                {
                                    ctx.UpdateTarget(BuildTable(addresses, ipResponses));
                                    await Task.Delay(refresh, cts.Token).ContinueWith(_ => { });
                                }
                            });









                            foreach (var ip in addresses)
                            {
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



                                        try
                                        {

                                            var scope = new ManagementScope($@"\\{pingAddress}\root\cimv2");
                                            scope.Connect();

                                            var query = new ObjectQuery("SELECT UserName FROM Win32_ComputerSystem");
                                            using var searcher = new ManagementObjectSearcher(scope, query);

                                            foreach (ManagementObject mo in searcher.Get())
                                            {
                                                //TODO:
                                                // mo["UserName"].ToString(); can throw:
                                                //System.NullReferenceException: 'Object reference not set to an instance of an object.'
                                                //System.Management.ManagementBaseObject.this[string].get returned null.
                                                response.lastLoggedUser = mo["UserName"].ToString();
                                            }



                                        }
                                        catch (Exception ex)
                                        {
                                            //Console.WriteLine($"Error from: {ex.Source}");
                                            //Console.WriteLine(ex.GetType());
                                            //Console.WriteLine($"1. Error: {response.hostname}: {ex.Message}");
                                        }

                                        pingSuccess = true;
                                        response.successFinding = true;
                                        //AnsiConsole.Markup("[blue]Press [bold]<Enter>[/] to continue...[/]");
                                        //Console.ReadLine();
                                        break;
                                    }
                                    else
                                    {
                                        //AnsiConsole.Markup("[red]*[/]");
                                    }
                                    pingCounter++;




                                }

                                if (pingSuccess == false)
                                {

                                    response.successFinding = false;
                                    //AnsiConsole.MarkupLine($"[red] Failed to connect to:[/] {ip.address.ToString()}");
                                }

                                ipResponses.Add(response);




                            }

                            //sending response to server
                            // Send PUT request with JSON body
                            //AnsiConsole.Markup("[grey]Before sending response[/]");
                            sendResponseToServer(serverData.getAddress(), serverData.getPort(), client, ipResponses).Wait();

                            //AnsiConsole.Markup("[grey]Press [bold]<Enter>[/] to continue (after sending response)...[/]");
                            //Console.ReadLine();
                        }


                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[2 Error] {ex.GetType()} {ex.Message}");
                        Console.WriteLine($"Url: {url}");
                        AnsiConsole.Markup("[grey]Press [bold]<Enter>[/] to continue (errorrrrrr)...[/]");
                        Console.ReadLine();
                    }


                }
            }
        }


        static Spectre.Console.Table BuildTable(List<IP> addresses, List<ipResponse> ipResponses)
        {
            var tab = new Spectre.Console.Table();
            tab.Title("[bold]Live Ping[/]  (Ctrl+C to stop)");
            tab.AddColumn(new TableColumn(new Markup("[green] IpAddress [/]")));
            tab.AddColumn(new TableColumn("[blue] Hostname [/]"));
            tab.AddColumn(new TableColumn("[blue] lastLoggedUser [/]"));
            tab.AddColumn(new TableColumn("[blue] LastCheckedDate [/]"));
            tab.AddColumn(new TableColumn("[blue] lastFoundDate [/]"));

            tab.AddRow("a");
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

                        tab.AddRow($"[red]{re.address.ToString()}[/]", "[red] - not found -[/]", "[red] - not found -[/]", re.lastCheckedDate.ToString(), re.lastFoundDate.ToString());

                    }
                    else
                    {
                        tab.AddRow($"[green]{re.address.ToString()}[/]", re.hostname.ToString(), re.lastLoggedUser.ToString(), $"[green]{re.lastCheckedDate.ToString()}[/]", $"[green]{re.lastFoundDate.ToString()}[/]");

                    }
                }
            }

            var i = 0;
            if (addresses.Count() > 0)
            {
                foreach (var re in addresses)
                {
                    if (i >= responsesAmount)
                    {
                        tab.AddRow(re.address.ToString(), "---", "---", re.lastCheckedDate.ToString(), "---");
                    }

                    i++;
                }

            }


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