using Spectre.Console;
using System.Text.Json;

namespace MyApp
{
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
                        .AddChoices(new[] { "Connect to server", "Exit" }));
                    if (choice == "Connect to server")
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
                    AnsiConsole.MarkupLine(currentDir);


                    if (files.Length > 0 || true)
                    {
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
                            if (Directory.GetParent(currentDir)?.FullName != null)
                            {
                                currentDir = Directory.GetParent(currentDir)?.FullName;
                            }


                        }
                        else if (fileSelection.ToLower().EndsWith(".json"))
                        {
                            Console.WriteLine("COHSEN JSON");
                            var json = File.ReadAllText(fileSelection);
                            var obj = JsonSerializer.Deserialize<object>(json);
                        }
                        else
                        {
                            currentDir += $"\\{fileSelection}";
                            Console.WriteLine("COHSEN DIRECTORY");
                        }

                        AnsiConsole.MarkupLine($"selected file extension {Path.GetExtension(fileSelection)}");
                    }
                    else
                    {
                        //what to do here ?
                    }


                }
            }
        }
    }
}