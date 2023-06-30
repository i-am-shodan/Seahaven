using System.CommandLine;

namespace Seahaven
{
    internal class CommandHandling
    {
        public static string[] CommandLineRaw { get; private set; } = new string[0];
        public static bool IsREPL { get; private set; } = false;

        private static async Task REPL(RootCommand rootCommand)
        {
            IsREPL = true;

            while (true)
            {
                Console.Write("> ");
                var line = Console.ReadLine();

                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                CommandLineRaw = line.Split(" ");
                await rootCommand.InvokeAsync(line.Split(" "));
            }
        }

        public static async Task Handle(RootCommand rootCommand, string[] cmdline = null)
        {
            if (cmdline == null)
            {
                cmdline = Environment.GetCommandLineArgs().Skip(1).ToArray();
            }

            if (cmdline.Any() && cmdline.First().StartsWith("#"))
            {
                return;
            }

            CommandLineRaw = cmdline;
            if (!cmdline.Any())
            {
                await REPL(rootCommand);
            }
            else
            {
                await rootCommand.InvokeAsync(cmdline);
            }
        }

        public static async Task Handle(RootCommand rootCommand, string line)
        {
            CommandLineRaw = line.Split(" ");

            await rootCommand.InvokeAsync(line);
        }
    }
}
