//
// A proxy for an arbitrary JavaScript object. Derived types:
//  - JSArray
//  - JSArray<_>
//  - JSDate
//  - JSNumber
//  - JSString
//  - JSFunction
//  - JSRegExp
//  - JSError
// NOTE: Appears in both IL2JS's mscorlib and CLR's JSTypes assemblies
//

using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.LiveLabs.JavaScript.IL2JS;
using Microsoft.LiveLabs.JavaScript.Interop;

namespace Microsoft.LiveLabs.JavaScript
{
    [UsedType(true)]
    [Interop(@"function(root, inst) {
                   var c = root.MSCORLIB;
                   if (c == null) {
                       c = root.MSCORLIB = {};
                       c.Table = {};
                       var p = ""Microsoft.LiveLabs.JavaScript."";
                       var f = function(ctor, n) { c.Table[ctor.toString()] = [p+n]; };
                       f(Number, ""JSNumber"");
                       f(String, ""JSString"");
                       f(Array, ""JSArray"");
                       f(Date, ""JSDate"");
                       f(RegExp, ""JSRegExp"");
                       f(Function, ""JSFunction"");
                       f(TypeError, ""JSTypeError"");
                       f(SyntaxError, ""JSSyntaxError"");
                       f(EvalError, ""JSEvalError"");
                       f(RangeError, ""JSRangeError"");
                       f(ReferenceError, ""JSReferenceError"");
                       // f(UriError, ""JSUriError"");
                       // NOTE: In IE, toString on all the errors yields the same string, thus we
                       //       won't be able to distinguish these types. Add Error last so we always
                       //       get a sound approximation of the true type.
                       f(Error, ""JSError"");
                   }
                   return c.Table[inst.constructor.toString()];
               }", State = InstanceState.JavaScriptOnly, UndefinedIsNotNull = true)]
    [Import]
    public class JSObject : IEnumerable<JSProperty>
    {
        [Import(Creation = Creation.Object)]
        extern public JSObject();

        public JSObject(JSContext ctxt)
        {
        }

        public void Add(string field, JSObject value)
        {
            SetField(field, value);
        }

        public void Add(string field, object value)
        {
            SetField(field, FromObject(value));
        }

        public static JSObject FromObject(object obj)
        {
            if (obj == null)
                return null;
            else
            {
                var jsov = obj as JSObject;
                if (jsov != null)
                    return jsov;
                else
                {
                    var iv = obj as int?;
                    if (iv != null)
                        return new JSNumber(iv.Value);
                    else
                    {
                        var dv = obj as double?;
                        if (dv != null)
                            return new JSNumber(dv.Value);
                        else
                        {
                            var sv = obj as string;
                            if (sv != null)
                                return new JSString(sv);
                            else
                            {
                                var av = obj as object[];
                                if (av != null)
                                    return JSArray.FromArray(av);
                                else
                                    throw new InvalidOperationException("unrecognised object type");
                            }
                        }
                    }
                }
            }
        }

        extern public bool IsUndefined
        {
            [Import(@"function(inst) { return inst === undefined; }", PassInstanceAsArgument = true)]
            get;
        }

        [Import(@"function(inst) { return inst; }", PassInstanceAsArgument = true)]
        extern public T To<T>();

        [Import(@"function(obj) { return obj; }")]
        extern public static JSObject From<T>(T obj);

        [Import("function(inst, name) { return inst[name] !== undefined; }", PassInstanceAsArgument = true)]
        extern public bool HasField(string name);

        [Import(@"function(inst, name) { return inst[name]; }", PassInstanceAsArgument = true)]
        extern public T GetField<T>(string name);

        [Import(@"function(inst, name, obj) { inst[name] = obj; }", PassInstanceAsArgument = true)]
        extern public void SetField<T>(string name, T obj);

        [Import(@"function(inst, name) { delete inst[name]; }", PassInstanceAsArgument = true)]
        extern public void RemoveField(string name);

        extern public override string ToString();
        extern public JSObject this[string name] { get; set; }
        extern public JSFunction Constructor { get; }
        extern public bool HasOwnProperty(string name);
        extern public bool IsPrototypeOf(JSObject obj);
        extern public bool PropertyIsEnumerable(string name);

        [Import(@"function(inst) { var names = []; for (var name in inst) names.push(name); return names; }", PassInstanceAsArgument = true)]
        extern public string[] FieldNames();

        public IEnumerator<JSProperty> GetEnumerator()
        {
            foreach (var name in FieldNames())
                yield return new JSProperty(name, this[name]);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
