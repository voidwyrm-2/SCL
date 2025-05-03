namespace SCL.Events;

public struct Echo : ISclEvent
{
    public void Invoke(string name, ISclType[] positional, MappedArgs mapped)
    {
        string[] str = positional.Select(value => value.ToString()).ToArray();

        ISclType separator = mapped.GetArg("sep", new Types.String(" "));
        
        Console.WriteLine(string.Join(separator.Literal() as string, str));
    }

    public (SclTypeValue[], bool) Args() => ([SclTypeValue.Any], true);
}