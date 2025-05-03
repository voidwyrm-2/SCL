namespace SCL;

public class ExitCodeException(int code) : SclException("")
{
    public readonly int code = code;
}