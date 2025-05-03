namespace SCL;

public readonly struct MappedArgs(IReadOnlyDictionary<string, ISclType> mapped)
{
    /// <summary>
    /// Attempts to get a mapped argument by name.
    /// </summary>
    /// <param name="name">The name of the argument to get.</param>
    /// <param name="def">The default value to use if the </param>
    /// <param name="required">Set to true if an exception should be thrown if the specified argument does not exist.</param>
    /// <param name="typeOverride">Overrides the type inferred from <paramref name="def"/>.</param>
    /// <returns>The value of the mapped argument.</returns>
    /// <exception cref="SclException">The specified argument is not the expected type, or the specified argument does not exist if <paramref name="required"/> is true.</exception>
    public ISclType GetArg(string name, ISclType def, bool required = false, SclTypeValue? typeOverride = null) {
        if (!mapped.TryGetValue(name, out ISclType? value))
        {
            if (required)
                throw new SclException($"mapped argument '{name}' is required");
            
            return def;
        }

        if (!value.Type().Is(typeOverride ?? def.Type()))
            throw new SclException($"expected type {def.Type()} from mapped argument '{name}', but found type {value.Type()} instead");
                    
        return value;
    }
}