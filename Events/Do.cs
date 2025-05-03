namespace SCL.Events;

public readonly struct Do(EventEnv env) : ISclEvent
{
    public void Invoke(string name, ISclType[] positional, MappedArgs mapped)
    {
        foreach (ISclType value in positional)
        {
            if (env.events.TryGetValue((string)value.Literal(), out Interpreter.EventEntry entry))
                entry.Invoke(env.vars);
            else
                throw new SclException($"event '{value.Literal()}' does not exist");
        }
    }

    public (SclTypeValue[], bool) Args() => ([SclTypeValue.Symbol], true);
}