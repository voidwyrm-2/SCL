namespace SCL.Types;

public struct Boolean(bool value) : ISclType
{
    private readonly bool _value = value;

    public bool Equals(ISclType? other) => other is Boolean b && _value == b._value;

    public object Literal() => _value;

    public SclTypeValue Type() => SclTypeValue.Boolean;
    
    public override string ToString() => _value.ToString();
}