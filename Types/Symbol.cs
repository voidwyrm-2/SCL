namespace SCL.Types;

public readonly struct Symbol(string value) : ISclType
{
    private readonly string _value = value;

    public bool Equals(ISclType? other) => other is Symbol s && _value == s._value;

    public object Literal() => _value;

    public SclTypeValue Type() => SclTypeValue.Symbol;
    
    public override string ToString() => _value;
}