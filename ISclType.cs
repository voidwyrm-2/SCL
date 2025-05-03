namespace SCL;

public interface ISclType : IEquatable<ISclType>
{
    public SclTypeValue Type();
    public string ToString();
    public object Literal();
}