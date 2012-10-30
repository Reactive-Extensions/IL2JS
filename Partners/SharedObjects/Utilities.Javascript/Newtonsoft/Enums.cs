
namespace Newtonsoft.Json
{
    public enum Formatting
    {
        None,
        Indented
    }

    public enum WriteState
    {
        Error,
        Closed,
        Object,
        Array,
        Constructor,
        Property,
        Start
    }

    public enum JTokenType
    {
        None,
        Object,
        Array,
        Constructor,
        Property,
        Comment,
        Integer,
        Float,
        String,
        Boolean,
        Null,
        Undefined,
        Date,
        Raw,
        Bytes
    }

    public enum JsonToken
    {
        None,
        StartObject,
        StartArray,
        StartConstructor,
        PropertyName,
        Comment,
        Raw,
        Integer,
        Float,
        String,
        Boolean,
        Null,
        Undefined,
        EndObject,
        EndArray,
        EndConstructor,
        Date,
        Bytes
    }
}
