using SCL.Events;
using SCL.Lexer;

namespace SCL;

public class Interpreter
{
    public readonly struct EventEntry(ISclEvent ev, string name, Token[] args)
    {
        public void Invoke(Dictionary<string, ISclType> vars)
        {
            var (positional, mapped) = args.ParseEventArguments(vars, ev.Args());
            ev.Invoke(name, positional, new MappedArgs(mapped));
        }
    }
    
    public const string VERSION = "1.2";

    private readonly Dictionary<string, ISclType> _vars;

    private readonly Dictionary<string, EventEntry> _namedEvents;

    private readonly List<EventEntry> _unnamedEvents;

    private int _forcedUnamedCount = 0;

    private readonly Dictionary<string, (ISclEvent, bool)> _registeredEvents = [];

    public Interpreter(Dictionary<string, ISclType>? vars = null, Dictionary<string, EventEntry>? namedEvents = null, List<EventEntry>? unnamedEvents = null)
    {
        _vars = vars ?? [];
        _namedEvents = namedEvents ?? [];
        _unnamedEvents = unnamedEvents ?? [];
        
        RegisterEvent("ECHO", new Echo());
        RegisterEvent("VAR", new Var(GenerateEnv()), true);
        RegisterEvent("DO", new Do(GenerateEnv()));
        RegisterEvent("NOP", new Nop());
        RegisterEvent("CMD", new Cmd());
        RegisterEvent("EXIT", new Exit());
    }

    public void Interpret(Token[] source)
    {
        var tokens = source.SeparateTokens();

        for (int i = 0; i < tokens.Length; i++)
        {
            var line = tokens[i];
            
            line.ThrowExpect([TokenType.Statement, TokenType.Ident]);

            Token label = line[0];
            Token eventName = line[1];

            if (_registeredEvents.TryGetValue(eventName.lit, out (ISclEvent, bool) value))
            {
                (ISclEvent ev, bool forceUnnamed) = value;
                
                if (label.lit == "" || forceUnnamed)
                    _unnamedEvents.Add(new EventEntry(ev, label.lit, line[2..]));
                else if (_namedEvents.ContainsKey(label.lit))
                    label.Throw($"event '{label.lit}' already exists'");
                else
                    _namedEvents.Add(label.lit, new EventEntry(ev, label.lit, line[2..]));
            }
            else
            {
                eventName.Throw($"event '{eventName}' does not exist");
            }
        }
    }

    public void Interpret(string source)
    {
        Interpret(new Lexer.Lexer(source).Lex());
    }

    public void InterpretFile(FileInfo file)
    {
        if (!file.Exists)
            throw new SclException($"'{file.Name}' does not exist");

        try
        {
            Interpret(File.ReadAllText(file.FullName));
        }
        catch (IOException e)
        {
            throw new SclException($"could not read file '{file.Name}': {e.Message}");
        }
    }

    public void InterpretFile(string path)
    {
        InterpretFile(new FileInfo(path));
    }

    /// <summary>
    /// Executes all unnamed events.
    /// </summary>
    public void RunUnnamed()
    {
        foreach (EventEntry entry in _unnamedEvents)
            entry.Invoke(_vars);
    }

    /// <summary>
    /// Executes all named events.
    /// </summary>
    /// <param name="names">The list of events to execute.</param>
    public void RunNamed(IEnumerable<string> names)
    {
        foreach (string name in names)
        {
            if (_namedEvents.TryGetValue(name, out EventEntry entry))
                entry.Invoke(_vars);
            else
                throw new SclException($"event '{name}' does not exist");
        }
    }

    /// <summary>
    /// Returns a list of named events, a count of unnamed events, and a count of how many unnamed events are forced.
    /// </summary>
    /// <returns>A list of named events, the amount of unnamed events, and the amount of forced unnamed events.</returns>
    public (string[], int, int) Events() => (_namedEvents.Keys.Where(s => !s.StartsWith('_')).ToArray(), _unnamedEvents.Count, _forcedUnamedCount);
    
    /// <summary>
    /// Registers an event, allowing it to be used in SCL programs.
    /// </summary>
    /// <param name="name">The name to register the event under.</param>
    /// <param name="ev">The event to register.</param>
    /// <param name="forceUnnamed">Force the registered event to be unnamed, even if it isn't.</param>
    /// <returns>True if the event was registered successfully; otherwise, false.</returns>
    public bool RegisterEvent(string name, ISclEvent ev, bool forceUnnamed = false) => _registeredEvents.TryAdd(name, (ev, forceUnnamed));
    
    public EventEnv GenerateEnv() => new(_vars, _namedEvents, _unnamedEvents);
}