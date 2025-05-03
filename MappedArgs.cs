namespace SCL;

public readonly struct MappedArgs(IReadOnlyDictionary<string, ISclType> mapped)
{
    public ISclType GetArg(string name, ISclType def, SclTypeValue? typeOverride = null) {
        if (mapped.TryGetValue(name, out ISclType? value))
        {
            
            if (!value.Type().Is(typeOverride ?? def.Type()))
                throw new SclException($"expected type {def.Type()} from mapped argument '{name}', but found type {value.Type()} instead");
                    
            return value;
        }

        return def;
    }
}