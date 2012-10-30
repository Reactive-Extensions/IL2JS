namespace Microsoft.Csa.SharedObjects.Utilities
{
    using System;
    using System.Text;
    using System.Runtime.Serialization.Json;
    using System.Reflection;
    using System.IO;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;
    using System.Threading;

    public static class Json
    {
        //private static Lazy<JsonSerializer> serializer = new Lazy<JsonSerializer>(CreateSerializer, LazyThreadSafetyMode.PublicationOnly);
        private static JsonSerializer serializer = CreateSerializer();
        private static JsonSerializer CreateSerializer()
        {
            var serializer = new JsonSerializer();
#if !IL2JS
            serializer.Converters.Add(new JavaScriptDateTimeConverter());
            serializer.NullValueHandling = NullValueHandling.Ignore;
#endif
            return serializer;
        }

        private static JsonSerializer Serializer
        {
            get
            {
                return serializer;
            }
        }

        /// <summary>
        /// ReadObject - deserializes an object from json
        /// </summary>
        /// <param name="type">The type of the resulting object</param>
        /// <param name="json">The serialized json object</param>
        /// <param name="serializer">A serializer. If null a new serialized is created</param>
        /// <returns>A new instance of the object</returns>
        public static object ReadObject(Type type, string json)
        {
            var sw = new StreamWriter(new MemoryStream());
            sw.Write(json);
            sw.Flush();            
            return ReadObject(type, sw.BaseStream);
        }

        /// <summary>
        /// ReadObject - deserialize an object from a stream
        /// </summary>
        /// <param name="type">The type of the resulting object</param>
        /// <param name="stream">The stream that contains the object</param>
        /// <param name="serializer">A serializer. If null a new serialized is created</param>
        /// <returns></returns>
        public static object ReadObject(Type type, Stream stream)
        {
            return Serializer.Deserialize(new StreamReader(stream), type);
        }

        /// <summary>
        /// Reads a json object as a property of an object
        /// </summary>
        /// <param name="obj">The object to receive the new property value</param>
        /// <param name="propertyName">The name of the property</param>
        /// <param name="json">The serialized property value as json</param>
        /// <param name="setValue">If true, the property value is set. If false it is only retreived</param>
        /// <returns>The deserialized property value</returns>
        public static object ReadProperty(object obj, string propertyName, string json)
        {
            return ReadOrAssignProperty(obj, propertyName, json, false);
        }

        /// <summary>
        /// Reads a json object and assigns its value to a property of an object
        /// </summary>
        /// <param name="obj">The object to receive the new property value</param>
        /// <param name="propertyName">The name of the property</param>
        /// <param name="json">The serialized property value as json</param>
        /// <param name="setValue">If true, the property value is set. If false it is only retreived</param>
        /// <returns>The deserialized property value</returns>
        public static object AssignProperty(object obj, string propertyName, string json)
        {
            return ReadOrAssignProperty(obj, propertyName, json, true);
        }

        private static object ReadOrAssignProperty(object obj, string propertyName, string json, bool assignValue)
        {
            Type objectType = obj.GetType();
            PropertyInfo propertyInfo = objectType.GetProperty(propertyName);
            Type propertyType = propertyInfo.PropertyType;
            object propertyValue = ReadObject(propertyType, json);
            if (assignValue)
            {
                propertyInfo.SetValue(obj, propertyValue, null);
            }
            return propertyValue;
        }

        public static string WriteObject(object obj)
        {
            if (obj == null)
            {
                return string.Empty;
            }

            var stream = new MemoryStream();
            WriteObject(obj, stream);
            stream.Position = 0;
            StreamReader reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }

        public static void WriteObject(object obj, Stream stream)
        {
            WriteObject(obj, new StreamWriter(stream));
        }

        public static void WriteObject(object obj, StreamWriter sw)
        {
            if (obj != null)
            {
                JsonSerializer s = new JsonSerializer();
                s.Serialize(sw, obj);
            }
        }
    }

}
