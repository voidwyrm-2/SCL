namespace SCL.Types;

public readonly struct String(string value = "") : ISclType
{
    private readonly string _value = value;

    public bool Equals(ISclType? other) => other is String s && _value == s._value;

    public object Literal() => _value;

    public SclTypeValue Type() => SclTypeValue.String;
    
    public override string ToString() => _value;
}