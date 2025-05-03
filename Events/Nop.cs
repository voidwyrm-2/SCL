namespace SCL.Events;

public struct Nop : ISclEvent
{
    public void Invoke(string name, ISclType[] positional, MappedArgs mapped) { }

    public (SclTypeValue[], bool) Args() => ([SclTypeValue.Any], true);
}