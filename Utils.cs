using System.Diagnostics.CodeAnalysis;
using SCL.Lexer;
using SCL.Types;
using Boolean = SCL.Types.Boolean;
using String = SCL.Types.String;

namespace SCL;

internal static class Utils
{
    public static Token[][] SeparateTokens(this Token[] tokens)
    {
        if (tokens.Length == 0)
            return [];

        var tokenLists = new List<Token[]>();
        var interm = new List<Token>();
        
        int cln = tokens.First().ln;

        foreach (Token token in tokens)
        {
            if (token.ln != cln && interm.Count > 0)
            {
                tokenLists.Add(interm.ToArray());
                interm.Clear();
                cln = token.ln;
            }
            
            interm.Add(token);
        }

        if (interm.Count > 0)
            tokenLists.Add(interm.ToArray());
        
        return tokenLists.ToArray();
    }
    
    internal static string Plural(this int n) => n == 1 ? "" : "s";

    public static SclException? Expect(this Token[] tokens, TokenType[] types, int offset = 0, bool uncapped = true)
    {
        if (!uncapped && tokens.Length > types.Length)
            return tokens[types.Length].Error($"expected {types.Length} tokens, but found {tokens.Length} instead"); 
        
        if (tokens.Length < types.Length)
            return tokens.Last().Error($"expected {types.Length} tokens, but found {tokens.Length} instead");
        
        for (int i = offset; i < types.Length; i++)
        {
            if (tokens[i] != types[i])
                return tokens[i].Error($"expected {types[i]}, but found {tokens[i].type} instead");
        }

        return null;
    }

    public static void ThrowExpect(this Token[] tokens, TokenType[] types, int offset = 0, bool uncapped = true)
    {
        if (tokens.Expect(types, offset, uncapped) is { } e)
            throw e;
    }
    
    public static SclException? ExpectSeveral(this Token[] tokens, TokenType[][] types, int offset = 0, bool uncapped = true)
    {
        if (!uncapped && tokens.Length > types.Length)
            return tokens[types.Length].Error($"expected {types.Length} token{types.Length.Plural()}, but found {tokens.Length} instead"); 
        
        if (tokens.Length < types.Length)
            return tokens.Last().Error($"expected {types.Length} tokens{types.Length.Plural()}, but found {tokens.Length} instead");
        
        for (int i = offset; i < types.Length; i++)
        {
            if (!types[i].Contains(tokens[i].type))
                return tokens[i].Error($"expected {string.Join(" or ", types[i])}, but found {tokens[i].type} instead");
        }

        return null;
    }
    
    public static void ThrowExpectSeveral(this Token[] tokens, TokenType[][] types, int offset = 0, bool uncapped = true)
    {
        if (tokens.ExpectSeveral(types, offset, uncapped) is { } e)
            throw e;
    }
    
    public static bool Is(this SclTypeValue self, SclTypeValue other) => ((int)self & (int)other) == (int)self;

    public static (ISclType[], IReadOnlyDictionary<string, ISclType>) ParseEventArguments(this Token[] tokens, Dictionary<string, ISclType> vars, (SclTypeValue[], bool) args)
    {
        if (args.Item1.Length < 1 && args.Item2)
            throw new SclException("varargs arguments array must have at least one type");
        
        List<ISclType> positional = [];
        List<Token> positionalTokens = [];
        Dictionary<string, ISclType> mapped = [];
        
        var ttTypeMappings = new Dictionary<TokenType, Func<Token, ISclType>>
        {
            { TokenType.Ident, t =>
            {
                if (vars.TryGetValue(t.lit, out ISclType? value))
                    return value;
                
                t.Throw($"variable '{t.lit}' does not exist");
                return new String("");
            } },
            { TokenType.String, t => new String(t.lit) },
            { TokenType.Integer, t => new Integer(int.Parse(t.lit)) },
            { TokenType.Boolean, t => new Boolean(t.lit == "yes") },
            { TokenType.Symbol, t => new Symbol(t.lit) }
        };

        bool comma = false;

        for (int i = 0; i < tokens.Length; i++)
        {
            if (comma)
            {
                Utils.ThrowExpect([tokens[i]], [TokenType.Comma]);
                comma = false;
            }
            else
            {
                if (tokens.Skip(i).ToArray().Expect([TokenType.Ident, TokenType.Equals]) is null)
                {
                    tokens.ThrowExpectSeveral([[TokenType.Ident], [TokenType.Equals], ttTypeMappings.Keys.ToArray()], i);
                    
                    Token name = tokens[i];
                    Token value = tokens[i + 2];
                    
                    if (ttTypeMappings.TryGetValue(value.type, out var converter))
                        mapped.Add(name.lit, converter(value));
                    else
                        value.Throw($"no mapping found for token '{value.type}'");
                    
                    i += 3;
                }
                else
                {
                    tokens.ThrowExpectSeveral([ttTypeMappings.Keys.ToArray()], i);

                    if (ttTypeMappings.TryGetValue(tokens[i].type, out var converter))
                    {
                        positional.Add(converter.Invoke(tokens[i]));
                        positionalTokens.Add(tokens[i]);
                    }
                    else
                    {
                        tokens[i].Throw($"no mapping found for token '{tokens[i].type}'");
                    }
                    
                    comma = true;
                }
            }
        }
        
        if (args.Item1.Length == 1 && args.Item2)
            return (positional.ToArray(), mapped);

        int expectedAmount = args.Item1.Length - (args.Item2 ? 1 : 0);

        if (positional.Count != expectedAmount && !(positional.Count > expectedAmount && args.Item2)) 
        {
            (positional.Count > expectedAmount ? positionalTokens[expectedAmount] : positionalTokens.Last())
                .Throw($"expected {expectedAmount} argument{expectedAmount.Plural()}, but found {positional.Count} instead");
        }

        for (int i = 0; i < args.Item1.Length - (args.Item2 ? 1 : 0); i++)
        {
            if (!positional[i].Type().Is(args.Item1[i]))
                positionalTokens[i]
                    .Throw($"expected type {args.Item1[i]} for argument {i + 1}, but found {positional[i].Type()} instead");
        }

        if (!args.Item2)
            return (positional.ToArray(), mapped);
        
        for (int i = args.Item1.Length - 1; i < positional.Count; i++)
        {
            if (!positional[i].Type().Is(args.Item1.Last()))
                positionalTokens[i]
                    .Throw($"expected type {args.Item1.Last()} for argument {i + 1}, but found {positional[i].Type()} instead");
        }

        return (positional.ToArray(), mapped);
    }

    public static (string, int) ExecuteCommand(string command, string args, bool redirectOutput = true)
    {
        System.Diagnostics.Process process = new();
        if (OperatingSystem.IsWindows())
        {
            process.StartInfo.FileName = command + " " + args;
        }
        else
        {
            process.StartInfo.FileName = command;
            process.StartInfo.Arguments = args;
        }

        process.StartInfo.UseShellExecute = !OperatingSystem.IsWindows();
        process.StartInfo.RedirectStandardOutput = redirectOutput;
        process.StartInfo.RedirectStandardError = redirectOutput;
        process.StartInfo.RedirectStandardInput = redirectOutput;

        process.Start();

        process.WaitForExit();
        
        return (redirectOutput ? process.StandardOutput.ReadToEnd() : "", process.ExitCode);
    }
}