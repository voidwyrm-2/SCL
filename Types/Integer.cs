namespace SCL.Types;

public struct Integer(int value) : ISclType
{
    private readonly int _value = value;

    public bool Equals(ISclType? other) => other is Integer i && _value == i._value;

    public object Literal() => _value;

    public SclTypeValue Type() => SclTypeValue.Integer;
    
    public override string ToString() => _value.ToString();
}