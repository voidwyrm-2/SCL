namespace SCL;

public interface ISclEvent
{
    public void Invoke(string name, ISclType[] positional, MappedArgs mapped);

    /// <summary>
    /// Returns the arguments this event takes.
    /// <br/>
    /// If the event takes varargs, the last item of the array is the vararg and the array of argument types is expected to have at least one value;
    /// an exception will be thrown if this is not the case.
    /// </summary>
    /// <returns>The types of the arguments and if the event takes varargs or not.</returns>
    public (SclTypeValue[], bool) Args();
}