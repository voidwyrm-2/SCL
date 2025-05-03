namespace SCL.Events;

public struct Exit : ISclEvent
{
    public void Invoke(string name, ISclType[] positional, MappedArgs mapped)
    {
        throw new ExitCodeException((int)positional[0].Literal());
    }

    public (SclTypeValue[], bool) Args() => ([SclTypeValue.Integer], false);
}