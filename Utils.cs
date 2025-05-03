using SCL.Lexer;
using SCL.Types;
using Boolean = SCL.Types.Boolean;
using String = SCL.Types.String;

namespace SCL;

internal static class Utils
{
    /// <summary>
    /// A mapping of <see cref="TokenType"/> values to converter functions for <see cref="Token"/> conversion, mainly meant for the <see cref="ParseEventArguments"/> method.
    /// </summary>
    public static readonly Dictionary<TokenType, Func<Token, IReadOnlyDictionary<string, ISclType>, ISclType>> TtTypeMappings = new()
    {
        { TokenType.Ident, (t, vars) =>
            {
                if (vars.TryGetValue(t.lit, out ISclType? value))
                    return value!;
                
                t.Throw($"variable '{t.lit}' does not exist");
                return new String("");
            }
        },
        { TokenType.String, (t, vars) => new String(t.lit) },
        { TokenType.Integer, (t, vars) =>
            {
                try
                {
                    return new Integer(int.Parse(t.lit));
                }
                catch (FormatException)
                {
                    t.Throw($"cannot convert '{t.lit}' to integer");
                }

                return new Integer(0);
            }
        },
        { TokenType.Boolean, (t, vars) =>
            {
                if (t.lit != "yes" && t.lit != "no")
                    t.Throw($"invalid boolean value '{t.lit}', booleans can only be 'yes' or 'no'");
                
                return new Boolean(t.lit == "yes");
            }
        },
        { TokenType.Symbol, (t, vars) => new Symbol(t.lit) }
    };
    
    /// <summary>
    /// Separates the given array of tokens into subarrays by line number.
    /// </summary>
    /// <param name="tokens">An array of tokens to separate.</param>
    /// <returns>An array of <see cref="Token"/> arrays, separated by line number.</returns>
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
    
    /// <summary>
    /// Returns "s" or an empty string depending on if the input number is 1 or not.
    /// </summary>
    /// <param name="n">The number to check.</param>
    /// <returns>An empty string if <paramref name="n"/> is 1; otherwise "s".</returns>
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
    
    /// <summary>
    /// Checks the equality of two <see cref="SclTypeValue"/>s via bitwise.
    /// </summary>
    /// <param name="self">The SclTypeValue to compare to.</param>
    /// <param name="other">The SclTypeValue to compare against.</param>
    /// <returns>True if the two SclTypeValues are equal; otherwise false.</returns>
    public static bool Is(this SclTypeValue self, SclTypeValue other) => ((int)self & (int)other) == (int)self;

    /// <summary>
    /// Parses an array of tokens as arguments to an <see cref="ISclEvent"/>.
    /// </summary>
    /// <param name="tokens">The tokens to parse.</param>
    /// <param name="vars">The variable dictionary.</param>
    /// <param name="args">The result of <see cref="ISclEvent.Args"/>, used to type check the arguments.</param>
    /// <returns>The positional arguments, and the mapped arguments.</returns>
    /// <exception cref="SclException">Not enough, too many, or the wrong type of arguments.</exception>
    public static (ISclType[], IReadOnlyDictionary<string, ISclType>) ParseEventArguments(this Token[] tokens, IReadOnlyDictionary<string, ISclType> vars, (SclTypeValue[], bool) args)
    {
        if (args.Item1.Length < 1 && args.Item2)
            throw new SclException("varargs arguments array must have at least one type");
        
        List<ISclType> positional = [];
        List<Token> positionalTokens = [];
        Dictionary<string, ISclType> mapped = [];

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
                    tokens.ThrowExpectSeveral([[TokenType.Ident], [TokenType.Equals], Utils.TtTypeMappings.Keys.ToArray()], i);
                    
                    Token name = tokens[i];
                    Token value = tokens[i + 2];
                    
                    if (Utils.TtTypeMappings.TryGetValue(value.type, out var converter))
                        mapped.Add(name.lit, converter.Invoke(value, vars));
                    else
                        value.Throw($"no mapping found for token '{value.type}'");
                    
                    i += 3;
                }
                else
                {
                    tokens.ThrowExpectSeveral([Utils.TtTypeMappings.Keys.ToArray()], i);

                    if (Utils.TtTypeMappings.TryGetValue(tokens[i].type, out var converter))
                    {
                        positional.Add(converter.Invoke(tokens[i], vars));
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

    /// <summary>
    /// Executes a terminal command; should be cross-platform, make an issue if testing finds it's not.
    /// </summary>
    /// <param name="command">The command to call, e.g. "mv", "mkdir", etc.</param>
    /// <param name="args">The arguments of the command.</param>
    /// <param name="redirectOutput">Set to true if the output should be redirected to stdout.</param>
    /// <returns>An empty string if <paramref name="redirectOutput"/> is true, otherwise the contents of stdout, and the exitcode of the process</returns>
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