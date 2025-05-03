namespace SCL.Types;

public struct List(ISclType[] value) : ISclType
{
    private readonly ISclType[] _value = value;

    public bool Equals(ISclType? other) => false;

    public object Literal() => _value;

    public SclTypeValue Type() => SclTypeValue.List;

    public override string ToString() => "[" + string.Join(", ", _value.Select(v => v.ToString())) + "]";
}