namespace SCL;

public struct EventEnv(Dictionary<string, ISclType> vars, Dictionary<string, Interpreter.EventEntry> events, List<Interpreter.EventEntry> unnamedEvents)
{
    internal readonly Dictionary<string, ISclType> vars = vars;
    
    internal readonly Dictionary<string, Interpreter.EventEntry> events = events;
    
    internal readonly List<Interpreter.EventEntry> unnamedEvents = unnamedEvents;
}