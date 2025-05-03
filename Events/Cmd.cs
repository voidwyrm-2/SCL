namespace SCL.Events;

public struct Cmd : ISclEvent
{
    public void Invoke(string name, ISclType[] positional, MappedArgs mapped)
    {
        object[] parts = positional.Skip(1).Select(v => v.Literal()).ToArray();
        
        ISclType separator = mapped.GetArg("sep", new Types.String(" "));
        
        ISclType redirectOutput = mapped.GetArg("redirectOutput", new Types.Boolean(false));

        string command = (string)positional[0].Literal();
        
        string args = string.Join(separator.Literal() as string, parts);
        
        Utils.ExecuteCommand(command, args, (bool)redirectOutput.Literal());
    }

    public (SclTypeValue[], bool) Args() => ([SclTypeValue.String, SclTypeValue.Any], true);
}