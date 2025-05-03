using System.Diagnostics.CodeAnalysis;

namespace SCL.Lexer;

public enum TokenType
{
    None,
    Ident,
    Statement,
    Symbol,
    String,
    Integer,
    Boolean,
    Comma,
    Equals
}

public readonly struct Token(TokenType type = TokenType.None, string lit = "", int ln = -1, int col = -1) : IEquatable<Token>, IEquatable<TokenType>, IEquatable<string>
{
    public readonly TokenType type = type;
    public readonly string lit = lit;
    public readonly int ln = ln;

    public SclException Error(string msg) =>  new($"error on line {ln}, {col}: {msg}");

    public void Throw(string msg) => throw Error(msg);

    public override string ToString() => "{" + $"{type}, `{lit}`, {ln}, {col}" + "}";

    public override bool Equals([NotNullWhen(true)] object? obj) => obj switch
    {
        Token t => type == t.type && lit == t.lit,
        TokenType tt => type == tt,
        string s => lit == s,
        _ => false
    };

    public override int GetHashCode() => $"{type}{lit}{col}{ln}".GetHashCode();

    bool IEquatable<Token>.Equals(Token token) => token.type == type && token.lit == lit;
    
    bool IEquatable<TokenType>.Equals(TokenType tt) => type == tt;
    
    bool IEquatable<string>.Equals(string? str) => lit == str;

    public static bool operator ==(Token self, Token other) => self.Equals(other);
    public static bool operator !=(Token self, Token other) => !self.Equals(other);
    
    public static bool operator ==(Token self, TokenType other) => self.Equals(other);
    public static bool operator !=(Token self, TokenType other) => !self.Equals(other);
    
    public static bool operator ==(Token self, string other) => self.Equals(other);
    public static bool operator !=(Token self, string other) => !self.Equals(other);
}
