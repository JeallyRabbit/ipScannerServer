using Spectre.Console;

namespace MyApp
{
    internal class Program
    {
        static void Main(string[] args)
        {

            int menu = 0;
            var choice = "";
            while (true)
            {

                if (menu == 0)
                {
                    choice = AnsiConsole.Prompt(
                        new SelectionPrompt<string>()
                        .Title("Select an option:")
                        .PageSize(5)
                        .AddChoices(new[] { "Connect to server", "Exit" }));
                    menu = 1;
                }
                else if (menu == 1)
                {
                    if (choice == "Connect to server")
                    {
                        var serverConnectionChoice = AnsiConsole.Prompt(
                        new SelectionPrompt<string>()
                        .Title("Choose server connection configuration method:")
                        .PageSize(5)
                        .AddChoices(new[] { "Load from .JSON", "Input manually", "Return" }));


                        if (serverConnectionChoice == "Load from .JSON")
                        {
                            System.Console.Clear();

                            var currentDir = Directory.GetCurrentDirectory();
                            var files = Directory.GetFiles(currentDir);
                            AnsiConsole.MarkupLine(currentDir);


                            if (files.Length > 0)
                            {
                                List<string> fileNames = new List<string>();


                                foreach (var file in files)
                                {
                                    fileNames.Add(Path.GetFileName(file));
                                }


                                var fileSelection = AnsiConsole.Prompt(
                                new SelectionPrompt<string>()
                                .Title("Choose file:")
                                .PageSize(5)
                                .AddChoices(fileNames));

                                AnsiConsole.MarkupLine($"selected file extension {Path.GetExtension(fileSelection)}");
                            }
                            else
                            {
                                //what to do here ?
                            }


                        }
                        else if (serverConnectionChoice == "Input manually")
                        {

                        }
                        else if (serverConnectionChoice == "Return")
                        {
                            menu = 0;

                        }

                    }
                    else
                    {
                        break;
                    }
                }
            }
        }
    }
}