namespace SCL.Events;

public readonly struct Var(EventEnv env) : ISclEvent
{
    public void Invoke(string name, ISclType[] positional, MappedArgs mapped)
    {
        if (name == "")
            throw new SclException("variable name cannot be empty");
        
        if (env.vars.ContainsKey(name))
            throw new SclException($"variable '{name}' already exists");

        if (positional[0].Type() == SclTypeValue.String)
        {
            object[] parts = positional.Select(v => v.Literal()).ToArray();

            ISclType separator = mapped.GetArg("sep", new Types.String(""));

            env.vars.Add(name, new Types.String(string.Join(separator.Literal() as string, parts)));
        }
        else
        {
            env.vars.Add(name, positional[0]);
        }
    }

    public (SclTypeValue[], bool) Args() => ([SclTypeValue.Any, SclTypeValue.Any], true);
}