// <copyright file="Extensions.cs" company="Microsoft">
// Copyright © 2010 Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.Csa.SharedObjects
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using Microsoft.Csa.SharedObjects.Utilities;
    using Microsoft.LiveLabs.JavaScript;

    internal static class PropertyInfoExtensions
    {
        internal static bool IsIgnored(this PropertyInfo propertyInfo)
        {
            Object[] attributes = propertyInfo.GetCustomAttributes(typeof(Ignore), true);
            MethodInfo setInfo = propertyInfo.GetSetMethod();
            return (attributes != null && attributes.Length > 0) || (setInfo == null);
        }
    }

    internal static class ObjectExtensions
    {
        public static object GetPropertyValue(this object obj, string propertyName)
        {
            Type objectType = obj.GetType();
            PropertyInfo propertyInfo = objectType.GetProperty(propertyName);

            object propertyValue = propertyInfo.GetValue(obj, null);
            return propertyValue;
        }

        public static Type GetPropertyType(this object obj, string propertyName)
        {
            Type objectType = obj.GetType();
            PropertyInfo propertyInfo = objectType.GetProperty(propertyName);
            return propertyInfo.PropertyType;
        }

        #region Properties
        public static Dictionary<short, SharedProperty> GetSharedProperties(this object obj, Guid clientId)
        {
            Type objectType = obj.GetType();
            PropertyInfo[] allProps = objectType.GetProperties();

            Dictionary<short, SharedProperty> sharedProps = new Dictionary<short, SharedProperty>();
            short curIndex = 0;


            Console.Write("Inside GetSharedProperties");

            //if (obj is PrincipalObject)
            //{
            //    JSObject js = JSObject.From(obj);
            //    var prin = obj as PrincipalObject;

                
            //    //foreach(var propery in js)
            //    //{
            //    //    if (propery is JSProperty)
            //    //    {
            //    //        Console.WriteLine("BINGO Name:{0} Value:{1} Type:{2}", propery.Name, propery.Value, propery.GetType());
            //    //    }
            //    //    else
            //    //    {
            //    //        Console.WriteLine("Name:{0} Value:{1} Type:{2}", propery.Name, propery.Value, propery.GetType());
            //    //    }
            //    //}
            //    //return sharedProps;

            //    sharedProps.Add(curIndex, new SharedProperty()
            //    {                                        
            //        Name = "Id",
            //        Index = curIndex,
            //        Value = "Eli", // Json.WriteObject(prop.GetValue(obj, null)),
            //        ETag = new ETag(clientId),
            //        Attributes = new SharedAttributes(objectType, "Id")
            //    });
            //    ++curIndex;

            //    sharedProps.Add(curIndex, new SharedProperty()
            //    {
            //        Name = "Sid",
            //        Index = curIndex,
            //        Value = "sa", // Json.WriteObject(prop.GetValue(obj, null)),
            //        ETag = new ETag(clientId),
            //        Attributes = new SharedAttributes(objectType, "Sid")
            //    });

            //    return sharedProps;
            //}

            if (allProps.Count() == 0)
            {
                Console.WriteLine("NO PROPERTIES FOUND Extensions.cs line 52");
                return sharedProps;
            }

            
#if IL2JS
            foreach (PropertyInfo prop in allProps) //.OrderBy(p => p.Name))//, StringComparer.InvariantCulture))
#else
            foreach (PropertyInfo prop in allProps.Where(p => !p.IsIgnored()).OrderBy(p => p.Name))//, StringComparer.InvariantCulture))
#endif
            {
                sharedProps.Add(curIndex, new SharedProperty()
                {
                    Name = prop.Name,
                    Index = curIndex,
                    Value = Json.WriteObject(prop.GetValue(obj, null)),
                    ETag = new ETag(clientId),
                    Attributes = new SharedAttributes(objectType, prop.Name)
                });
                ++curIndex;
            }
            return sharedProps;
        }
    }

    internal static class SharedPropertyExtensions
    {
        public static bool TryReadString(this SharedProperty property, out string result)
        {
            if (property == null)
            {
                result = null;
                return false;
            }

            result = (string)Json.ReadObject(typeof(string), property.Value);
            return (result != null) ? true : false;
        }
    }

    internal static class IJsObjectExtensions
    {
        /// <summary>
        /// Extension method to serialize the key value pairs in a dictionary.
        /// </summary>
        /// <param name="obj">A Dictionary</param>
        /// <returns>Returns a Dictionary of serialized SharedProperty values</returns>
        public static Dictionary<short, SharedProperty> GetSharedProperties(this IJsObject obj, Guid clientId)
        {
            // TODO: Need to investigate adding attributes to the shared dictionary properties
            var sharedProps = new Dictionary<short, SharedProperty>();
            short curIndex = 0;

            // Explicitly casting the obj to IDictionary<string, object> because of a warning regarding the ambiguity between 
            // IDictionary.GetEnumerator() and IDictionary<string, object).GetEnumerator().  
            foreach (KeyValuePair<string, object> kv in (IDictionary<string, object>)obj)
            {
                sharedProps.Add(curIndex,
                                new SharedProperty()
                                {
                                    Name = kv.Key,
                                    Value = Json.WriteObject(kv.Value),
                                    Index = curIndex,
                                    ETag = new ETag(clientId),
                                    Attributes = new SharedAttributes(),
                                    PropertyType = DynamicTypeMapping.Instance.GetValueFromType(kv.Value.GetType())
                                });
                ++curIndex;
            }
            return sharedProps;
        }
        #endregion

    }
}
