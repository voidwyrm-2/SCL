using String = SCL.Types.String;

namespace SCL.Events;

public struct Require(EventEnv env) : ISclEvent
{
    public void Invoke(string name, ISclType[] positional, MappedArgs mapped)
    {
        throw new NotImplementedException();
        
        ISclType path = mapped.GetArg("path", new String(""));

        foreach (ISclType value in positional)
        {
            Dictionary<string, ISclType> vars = [];
            Dictionary<string, Interpreter.EventEntry> named = [];
            
            Interpreter interp = new(vars, named);
            
            interp.InterpretFile((string)path.Literal() + (string)value.Literal());
            
            /*
            if (label.lit == "" || forceUnnamed)
                   _unnamedEvents.Add(new EventEntry(ev, label.lit, line[2..]));
               else if (_namedEvents.ContainsKey(label.lit))
                   label.Throw($"event '{label.lit}' already exists'");
               else
                   _namedEvents.Add(label.lit, new EventEntry(ev, label.lit, line[2..]));
            */
        }
    }

    public (SclTypeValue[], bool) Args() => ([SclTypeValue.String], true);
}