using CommandLine;
using SCL.Events;
using SCL.Lexer;

namespace SCL;

internal static class Program
{
    private class Options
    {
        [Option('f', "file", Default = "run.scl", HelpText = "The SCL file to interpret")]
        public string? File { get; set; }
        
        [Option('e', "events", HelpText = "The event(s) in the SCL file to execute")]
        public IEnumerable<string>? Events { get; set; }
        
        [Option('l', "list", Default = false, HelpText = "Lists the events in the SCL file")]
        public bool ShowEvents { get; set; }
        
        [Option('v', "version", Default = false, HelpText = "Prints the interpreter version and exits")]
        public bool ShowVersion { get; set; }
        
        [Option('t', "tokens", Default = false, HelpText = "Prints the generated tokens")]
        public bool ShowTokens { get; set; }
    }
    
    public static int Main(string[] args)
    {
        try
        {
            Parser parser = new(settings =>
            {
                settings.AutoVersion = false;
            });

            parser.ParseArguments<Options>(args)
                .WithParsed(Program.HandleParsed);
        }
        catch (SclException e)
        {
            if (e is ExitCodeException ec)
                return ec.code;
            
            Console.Error.WriteLine(e.Message);
            return 1;
        }

        return 0;
    }

    private static void HandleParsed(Options opts)
    {
        if (opts.ShowVersion)
        {
            Console.WriteLine($"SCL interpreter version {Interpreter.VERSION}");
            return;
        }
        
        FileInfo file = new(opts.File!);

        if (opts.ShowTokens)
        {
            if (!file.Exists)
                throw new SclException($"'{file.Name}' does not exist");

            try
            {
                Lexer.Lexer lexer = new(File.ReadAllText(file.FullName));

                var tokens = lexer.Lex();

                foreach (Token token in tokens)
                    Console.WriteLine(token);
                
                Console.WriteLine("");

                return;
            }
            catch (IOException e)
            {
                throw new SclException($"could not read file '{file.Name}': {e.Message}");
            }
        }

        Interpreter interp = new();
        
        interp.InterpretFile(file);
        
        if (opts.ShowEvents)
        {
            (string[] named, int unnamed, int forced) = interp.Events();

            Console.WriteLine($"SCL file '{file.Name}':\n {unnamed} unnamed event{unnamed.Plural()} (of which {forced} are forced)\n {named.Length} named event{named.Length.Plural()}:\n  {string.Join("\n  ", named)}");
            
            return;
        }
        
        interp.RunUnnamed();
        interp.RunNamed(opts.Events ?? []);
    }
}