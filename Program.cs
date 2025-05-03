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

        if (!file.Exists)
        {
            Console.Error.WriteLine($"'{file.Name}' does not exist");
            return;
        }
        
        Lexer.Lexer lexer = new(File.ReadAllText(file.FullName));

        var tokens = lexer.Lex();

        if (opts.ShowTokens)
        {
            foreach (Token token in tokens)
                Console.WriteLine(token);
            Console.WriteLine("");

            return;
        }

        Interpreter interp = new();
        
        interp.Interpret(tokens);
        
        if (opts.ShowEvents)
        {
            (string[] named, int unnamed) = interp.Events();

            Console.WriteLine($"SCL file '{file.Name}':\n {unnamed} unnamed event{unnamed.Plural()}\n {named.Length} named event{named.Length.Plural()}:\n  {string.Join("\n  ", named)}");
            
            return;
        }
        
        interp.RunUnnamed();
        interp.RunNamed(opts.Events ?? []);
    }
}