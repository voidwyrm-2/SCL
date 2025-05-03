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
    
    public const string VERSION = "1.0";

    private readonly Dictionary<string, ISclType> _vars = [];

    private readonly Dictionary<string, EventEntry> _namedEvents = [];

    private readonly List<EventEntry> _unnamedEvents = [];

    private readonly Dictionary<string, (ISclEvent, bool)> _registeredEvents = [];

    public Interpreter()
    {
            RegisterEvent("ECHO", new Echo());
            RegisterEvent("VAR", new Var(GenerateEnv()), true);
            RegisterEvent("DO", new Do(GenerateEnv()));
            RegisterEvent("NOP", new Nop());
            RegisterEvent("CMD", new Cmd());
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
                else
                    _namedEvents.Add(label.lit, new EventEntry(ev, label.lit, line[2..]));
            }
            else
            {
                eventName.Throw($"event '{eventName}' does not exist");
            }
        }
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
    /// Returns a list of named events and a count of unnamed events.
    /// </summary>
    /// <returns>A list of named events and the amount of unnamed events.</returns>
    public (string[], int) Events() => (_namedEvents.Keys.Where(s => !s.StartsWith('_')).ToArray(), _unnamedEvents.Count);
    
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