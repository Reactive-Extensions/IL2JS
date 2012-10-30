//
// Representation of a binding of a field name to a value within a JavaScript object
// NOTE: Appears in both IL2JS's mscorlib and CLR's JSTypes assemblies
//

namespace Microsoft.LiveLabs.JavaScript
{
    public class JSProperty
    {
        public string Name { get; private set; }
        public JSObject Value { get; private set; }

        public JSProperty(string name, JSObject value)
        {
            Name = name;
            Value = value;
        }
    }
}
