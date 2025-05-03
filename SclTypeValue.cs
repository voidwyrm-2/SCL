namespace SCL;

public enum SclTypeValue
{
    Any = SclTypeValue.String | SclTypeValue.Integer | SclTypeValue.Boolean | SclTypeValue.Symbol,
    String = 0b1,
    Integer = 0b10,
    Boolean = 0b100,
    Symbol = 0b1000,
    List = 0b10000,
}