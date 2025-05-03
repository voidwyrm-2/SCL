namespace SCL.Lexer;


public class Lexer(string text)
{
    private int _idx;
    private int _col = 1;
    private int _ln = 1;
    private char? Cur => _idx < text.Length ? text[_idx] : null;
    private char? Next => _idx + 1 < text.Length ? text[_idx + 1] : null;

    private static readonly Dictionary<char, TokenType> CharTokens = new()
    {
        {',', TokenType.Comma},
        {'=',  TokenType.Equals},
    };

    private void Advance(int amount = 1)
    {
        _idx += amount;
        _col += amount;

        if (Cur != '\n')
            return; 
        
        _ln++; 
        _col = 0;
    }

    private Token CollectNumber(bool neg = false)
    {
        int start = _col;
        int startLn = _ln;
        string str = "";

        if (neg)
        {
            str += "-";
            Advance();
        }

        while (Cur is { } c && char.IsAsciiDigit(c))
        {
            str += c;
            Advance();
        }

        return new Token(TokenType.Integer, str, startLn, start); 
    }

    private Token CollectIdent(TokenType tt = TokenType.Ident)
    {
        int start = _col;
        int startLn = _ln;
        string str = "";
        
        if (tt != TokenType.Ident)
            Advance();

        while (Cur is { } c && (char.IsAsciiLetterOrDigit(c) || c == '_'))
        {
            str += c;
            Advance();
        }

        return new Token(tt != TokenType.Ident ? tt : str is "yes" or "no" ? TokenType.Boolean : TokenType.Ident, str, startLn, start);
    }

    private Token CollectString(bool raw = false, bool filepath = false)
    {
        int start = _col;
        int startLn = _ln;
        string str = "";
        bool escaped = false;

        char delimiter = raw ? '\'' : '"';
        
        Advance();
        
        if (filepath)
            Advance();

        while (Cur is { } c)
        {
            if (escaped)
            {
                char? ec = c switch
                {
                    '\\' or '\'' or '"' or '$' => c,
                    'n' => '\n',
                    'r' => '\r',
                    't' => '\t',
                    'v' => '\v',
                    'b' => '\b',
                    '0' => '\0',
                    _ => null
                };

                if (ec is null)
                    new Token(ln: _ln, col: _col).Throw($"invalid escape character '{c}'");

                str += ec;
                escaped = false;
            }
            else if (c == delimiter)
                break;
            else if (c == '\\' && !raw)
                escaped = true;
            else if (c == '$' && filepath)
                str += Environment.OSVersion.Platform == PlatformID.Unix ? '/' : '\\';
            else
                str += c;
            Advance();
        }

        if (Cur != delimiter)
            new Token(ln: startLn, col: start).Throw("unterminated string literal");
        
        Advance();

        return new Token(TokenType.String, str, startLn, start);
    }

    public Token[] Lex()
    {
        var tokens = new List<Token>();

        while (Cur is { } c)
        {
            if (char.IsWhiteSpace(c))
            {
                while (Cur is {} w && char.IsWhiteSpace(w))
                    Advance();
            }
            else if (c == '/' && Next == '*')
            {
                if (_col != 1)
                    new Token(ln: _ln, col: _col).Throw($"'{c}' '{Next}' comments must be on the first column"); 
                
                Advance(2);
                
                while (Cur != '\n')
                    Advance();
            }
            else if (Lexer.CharTokens.TryGetValue(c, out TokenType tt))
            {
                tokens.Add(new Token(tt, c.ToString(), _ln, _col));
                Advance();
            }
            else if (c is '"' or '\'')
            {
                tokens.Add(CollectString(c == '\''));
            }
            else if (c == '$' && Next is '"' or '\'')
            {
                tokens.Add(CollectString(Next == '\'', true));
            }
            else if (char.IsAsciiDigit(c) || c == '-')
            {
                tokens.Add(CollectNumber(c == '-'));
            }
            else if (char.IsAsciiLetterOrDigit(c) || c == '_' || c == '/' || c == '@')
            {
                if (_col != 1 && c == '/')
                    new Token(ln: _ln, col: _col).Throw("statements must be on the first column"); 
                
                tokens.Add(CollectIdent(c switch
                {
                    '/' => TokenType.Statement,
                    '@' => TokenType.Symbol,
                    _ => TokenType.Ident
                }));
            }
            else
                new Token(ln: _ln, col: _col).Throw($"illegal character '{c}'");
        }

        return tokens.ToArray();
    }
}