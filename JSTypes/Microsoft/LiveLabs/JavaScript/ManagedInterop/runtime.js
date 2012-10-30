//
// The unmanaged half of the interop runtime
//

function LLDTNewInteropRuntime(root, plugin, executionContext) {

    // The plugin at the time of initialization is always available via this field for two reasons:
    //  - Some bridges emit code which reaches back into custom fields of the plugin via this field.
    //  - The expression from which the plugin is created may yield different objects over time.
    //    (eg if other browser helper objects are loaded)
    root.Plugin = plugin;

    // ----------------------------------------------------------------------
    // Helpers implemented by the plugin (since we can't effect this in pure JavaScript)
    // ----------------------------------------------------------------------

    // Given an arbitrary unmanged object, return the unmanaged id associated with it, or -1 if none such.
    // Used for both 'Proxied' objects and delegates.
    root.ObjectToId = function(obj) { return plugin.ObjectToId(obj); }

    // Associate given unmanaged object and id
    root.AddObjectToId = function(obj, id) { plugin.AddObjectToId(obj, id); }

    // Discard association between unmanaged id and unmanaged object
    root.RemoveObjectToId = function(obj) { plugin.RemoveObjectToId(obj); }

    // ----------------------------------------------------------------------
    // Wormhole to managed CLR engine via the plugin
    // ----------------------------------------------------------------------

    // Call managed delegate with given id, passing exported arguments as a string, and
    // returning string to evaluate to yield result.
    // Arguments follow the grammar:
    //    ()
    //    (<exported argument 0>)
    //    (<exported argument 0>,<exported argument 1> ... )
    // Resulting string will invoke the appropriate exporter, or may throw the appropriatly exported object.
    root.CallManaged = function(id, args) { return plugin.CallManaged(id, args); }

    // ----------------------------------------------------------------------
    // Caching
    // ----------------------------------------------------------------------

    // Map from type index (== order in which type was added to runtime) to type structures, of the form:
    // {
    //     QualifiedName : string                 (* Qualified name of type, which includes strong assembly name,
    //                                               namespace, any outer types, and type name *)
    //     AssemblyName : string                  (* Strong name of assembly which defines this type or the
    //                                               higher-kinded type it is an instance of *)
    //     ElementTypeIndex : <type index>        (* If type is Nullable<T> or T[], the type index for T.
    //                                               Otherwise undefined. *)
    //     ArgumentTypeIndexes : [<type index>]   (* If type is a delegate, the indexes for arguments types.
    //                                               Otherwise undefined. *)
    //     ResultTypeIndex : <type index>         (* If type is a delegate with return value, the index for the
    //                                               return type. Otherwise undefined. *)
    //     CaptureThis : bool                     (* If type is a delegate, true if 'this' should be captured
    //                                               as first argument when invoked from unmanaged. Otherwise false. *)
    //     InlineParamsArray : bool               (* If type is a delegate, true if last argument should be passed
    //                                               as an array containing remanining actual arguments *)
    //     UndefinedIsNotNull : bool              (* If type is 'Proxied', true if undefined should not be
    //                                               imported as null *)
    //     Default : string                       (* String representing the default value for this type *)
    //     Import : object -> string              (* Given a native JavaScript object, return a string which the
    //                                               managed side can interpret to reconstruct the corresonding
    //                                               managed object or value *)
    //     Export : object -> object              (* Given the result of eval-ing a string emitted from the managed
    //                                               side, return the corresponding native JavaScript object. *)
    //     GetKeyField : object -> object         (* If root 'Keyed' type, extract the key field from given unmanaged
    //                                               'Keyed' object. Otherwise undefined.
    //     SetKeyField : object -> object -> ()   (* If root 'Keyed' type, set the key field of given unmanaged
    //                                               'Keyed' object. Otherwise undefined. *)
    //     KeyToObject : string |-> object        (* If root 'Keyed' type, map from key string of unmanaged 'Keyed'
    //                                               objects to their managed wrappers. Otherwise undefined. *)
    //     RootIndex : <type index>               (* Index of root type for this type, which may be this type itself *)
    //     TypeClassifier : <root> -> object -> [string]
    //                                            (* Return the qualified type name matching given object *)
    // }
    //
    // Contains entries for:
    //  - The (possibly higher-kinded) 'Keyed' and 'Proxied' types (we don't care about any type arguments)
    //    of the application. These are loaded when the application first instantiates a JavaScript engine.
    //  - (Possibly instances of) delegate types which have crossed the managed/unmanaged boundary. These, and
    //    the following, are loaded before an interop call is made.
    //  - Primitive array types which have crossed the managed/unmanaged boundary
    //  - Instances of Nullable`1 which have crossed the managed/unmanaged boundary
    //  - The element types of primitive arrays, the argument and result types of delegates, and the instantiation
    //    types of Nullable`1.

    root.IndexToType = [];

    // Next free id for unmanaged objects
    root.NextObjectId = 0;

    root.IdToUnmanagedDelegate = {};

    // Map unmanaged id to 'Proxied' object or delegate associated with it
    root.IdToObject = {};

    // ----------------------------------------------------------------------
    // Exceptions
    // ----------------------------------------------------------------------

    root.InvalidOperationException = function(msg) {
        msg = "Invalid operation exception: " + msg;
        root.Plugin.Log("** " + msg); // DEBUGONLY
        return new Error(msg);
    }

    // ----------------------------------------------------------------------
    // Type management
    // ----------------------------------------------------------------------

    root.Load = function(type) {
        root.Plugin.Log("** Load(" + type.QualifiedName + ") into " + root.IndexToType.length); // DEBUGONLY
        root.IndexToType.push(type);
    }

    root.Find = function(index) {
        var type = root.IndexToType[index];
        if (type == null)
            throw root.InvalidOperationException("No such type at index: " + index.toString());
        return type;
    }

    // ----------------------------------------------------------------------
    // Imports and Exports (from point-of-view of managed side)
    // ----------------------------------------------------------------------

    // An import is from native JavaScript object to string from which managed side may reconstruct object
    // or associated wrapper. An export is from result of evaluating a string from managed side to
    // native JavaScript object.

    //
    // Nullable<T>
    //

    // XREF1223
    // In: null/undefined or value
    // Out:
    //   null
    //   <string representation of value>
    root.ImportNullable = function(typeIndex, u) {
        var type = root.Find(typeIndex);
        if (type.ElementTypeIndex == null)
            throw root.InvalidOperationException("Not a nullable type: " + type.QualifiedName);
        if (u == null)
            return "null";
        return root.Find(type.ElementTypeIndex).Import(u);
    }

    // XREF1229
    // In: null or value
    // Out: null or value
    root.ExportNullable = function(typeIndex, m) {
        var type = root.Find(typeIndex);
        if (type.ElementTypeIndex == null)
            throw root.InvalidOperationException("Not a nullable type: " + type.QualifiedName);
        if (m == null)
            return null;
        return root.Find(type.ElementTypeIndex).Export(m);
    }

    //
    // Delegates
    //

    // XREF1039
    // In: null/undefined or function
    // Out:
    //   null
    //   <unmanaged id of unmanaged delegate>
    root.ImportDelegate = function(typeIndex, u) {
        var type = root.Find(typeIndex);
        if (type.ArgumentTypeIndexes == null)
            throw root.InvalidOperationException("Not a delegate type: " + type.QualifiedName);
        if (u === null)
            return "null";
        if (typeof u != "function")
            return "~";
        var id = root.ObjectToId(u);
        if (id < 0) {
            id = root.NextObjectId++;
            root.IdToObject[id] = u;
            root.AddObjectToId(u, id);
        }
        return id.toString();
    }

    // XREF1049
    // In: null, or mananged id of managed delegate
    // Out: null, or function which effects call to managed delegate
    root.ExportDelegate = function(typeIndex, id) {
        var type = root.Find(typeIndex);
        if (type.ArgumentTypeIndexes == null)
            throw root.InvalidOperationException("Not a delegate type:  " + type.QualifiedName);
        if (id == null)
            return null;
        var u = root.IdToUnmanagedDelegate[id];
        if (u == null) {
            u = root.MakeExportRedirector(type.ArgumentTypeIndexes, id, type.CaptureThis, type.InlineParamsArray);
            root.IdToUnmanagedDelegate[id] = u;
        }
        return u;
    }

    // Invoke unmanaged delegate with given id, passing the given args, which is an array
    // of already exported objects. Caller must import result.
    root.CallUnmanaged = function(id, args, captureThis, inlineParamsArray) {
        if (id == null)
            throw root.InvalidOperationException("No id");
        var u = root.IdToObject[id];
        if (u == null)
            throw root.InvalidOperationException("No such unmanaged delegate");
        if (typeof u != "function")
            return "~";
        var instance = null;
        if (captureThis) {
            instance = args[0];
            args.splice(0, 1);
        }
        if (inlineParamsArray)
            args = args.slice(0, args.length - 1).concat(args[args.length - 1]);
        var res;
        root.Plugin.IndentLog();
        try {
            res = u.apply(instance, args);
            root.Plugin.UnindentLog();
        }
        catch (e) {
            root.Plugin.UnindentLog();
            throw e;
        }
        return res;
    }

    // Managed side promises never to invoke the previously imported delegate with this id (created by unmanaged side)
    root.DisconnectUnmanagedDelegate = function(id) {
        var u = root.IdToObject[id];
        if (u == null)
            return;
        delete root.IdToObject[id];
        root.RemoveObjectToId(obj);
    }

    // Managed side will no longer honour calls to exported delegate with this id (created by managed side)
    root.DisconnectManagedDelegate = function(id) {
        delete root.IdToUnmanagedDelegate[id];
    }

    //
    // Arrays
    //

    // XREF1451
    // In: null/undefined or array
    // Out:
    //   null
    //   [<imported element 0>,<...>]
    root.ImportArray = function(typeIndex, u) {
        var type = root.Find(typeIndex);
        if (type.ElementTypeIndex == null)
            throw root.InvalidOperationException("Not an array type: " + type.QualifiedName);
        var elemType = root.Find(type.ElementTypeIndex);
        if (u == null)
            return "null";
        if (u.length === undefined)
            return "~";
        var sb = [];
        sb.push("[");
        for (var i = 0; i < u.length; i++) {
            if (i > 0)
                sb.push(",");
            sb.push(elemType.Import(u[i]));
        }
        sb.push("]");
        return sb.join("");
    }

    // XREF1453
    // In: null or array
    // Out: null or array
    root.ExportArray = function(typeIndex, m) {
        var type = root.Find(typeIndex);
        if (type.ElementTypeIndex == null)
            throw root.InvalidOperationException("Not an array type: " + type.QualifiedName);
        // No need to export array elements - they've already been exported by virtue of eval of second argument
        return m;
    }

    //
    // 'Normal' or 'Primitive' value types
    //

    // XREF1319
    // In: null/undefined or value
    // Out: default representation if null, or string representation of value
    root.ImportNumber = function(typeIndex, u) {
        if (u == null) {
            var type = root.Find(typeIndex);
            return type.Default;
        }
        u = u.valueOf();
        if (typeof u == "number")
            return u.toString();
        else if (typeof u == "boolean")
            return u ? "1" : "0";
        else
            return "~";
    }

    // XREF1327
    // In: value
    // Out: value
    root.ExportNumber = function(typeIndex, m) {
        // typeIndex unused
        return m;
    }

    //
    // 'Normal' reference types
    //

    // In: null/undefined or string
    // Out:
    //   null
    //   "<escaped string>"
    root.ImportString = function(typeIndex, u) {
        // typeIndex unused
        if (u == null)
            return "null";
        // be generous with conversions since 'String' is often a synonym for variant
        return "\"" + escape(u.toString()) + "\"";
    }

    // In: null or string
    // Out: null or string
    root.ExportString = function(typeIndex, m) {
        // typeIndex unused
        return m;
    }

    // 'Normal' object proxies are of the form:
    // {
    //     Id : int               (* Id of managed counterpart *)
    // }

    // XREF1009
    // In: null/undefined, or normal object proxy structure
    // Out:
    //   null
    //   <managed id of object>
    root.ImportNormalReferenceType = function(typeIndex, u) {
        // typeIndex unused
        if (u == null)
            return "null";
        if (u.Id == null)
            return "~";
        return u.Id.toString();
    }

    // XREF1013
    // In: null, or managed id of object
    // Out: null, or object proxy structure
    root.ExportNormalReferenceType = function(typeIndex, id) {
        // typeIndex unused
        if (id == null)
            return null;
        return { Id: id };
    }

    //
    // 'Keyed' types
    //

    // XREF1093
    // In: null/undefined, or unmanaged 'Keyed' object
    // Out:
    //   null
    //   "<escaped key string of unmanaged object>" null
    //   "<escaped key string of unmanaged object>" "<escaped qualified name of type given by type classifier>"
    root.ImportKeyed = function(typeIndex, u) {
        var type = root.Find(typeIndex);
        var rootType = root.Find(type.RootIndex);
        if (rootType.KeyToObject == null || rootType.GetKeyField == null || rootType.SetKeyField == null)
            throw root.InvalidOperationException("Not a keyed type: " + type.QualifiedName);
        if (u == null)
            return "null";
        var key = rootType.GetKeyField(u);
        if (key == null) {
            key = root.NextObjectId++;
            rootType.SetKeyField(u, key);
        }
        key = key.toString();
        if (rootType.KeyToObject[key] == null)
            rootType.KeyToObject[key] = u;
        key = "\"" + escape(key) + "\"";
        var nm = null;
        if (rootType.TypeClassifier != null)
            nm = rootType.TypeClassifier(root, u);
        if (nm == null)
            nm = "null";
        else
            nm = "\"" + escape("[" + rootType.AssemblyName + "]" + nm[0]) + "\"";
        return key + " " + nm;
    }

    root.CreateKeyed = function(typeIndex) {
        var type = root.Find(typeIndex);
        var rootType = root.Find(type.RootIndex);
        var u = {};
        key = root.NextObjectId++;
        rootType.SetKeyField(u, key);
        key = key.toString();
        rootType.KeyToObject[key] = u;
        key = "\"" + escape(key) + "\"";
        return key;
    }

    // XREF1097
    // In: null, or key string for an unmanaged 'Keyed' object
    // Out: null, or unmanaged 'Keyed' object
    root.ExportKeyed = function(typeIndex, key) {
        var type = root.Find(typeIndex);
        var rootType = root.Find(type.RootIndex);
        if (key == null)
            return null;
        var u = rootType.KeyToObject[key];
        if (u == null)
            throw root.InvalidOperationException("No such keyed object with key");
        return u;
    }

    // Managed side is no longer interested in 'Keyed' object with root type qualified name and key
    root.DisconnectKeyedObject = function(typeIndex, key) {
        var type = root.Find(typeIndex);
        var rootType = root.Find(type.RootIndex);
        delete rootType.KeyToObject[key];
    }

    //
    // 'Proxied' types
    //

    // XREF1051
    // In: null, undefined, or unmanaged part of a 'Proxied' object
    // Out:
    //   null
    //   undefined
    //   <unmanaged id of unmanaged object> null
    //   <unmanaged id of unmanaged object> "<escaped qualified name of type given by type classifier>"
    root.ImportProxied = function(typeIndex, u) {
        var type = root.Find(typeIndex);
        var rootType = root.Find(type.RootIndex);
        if (u === null)
            return "null";
        if (u === undefined)
            return type.UndefinedIsNotNull ? "undefined" : "null";
        var id = root.ObjectToId(u);
        if (id < 0) {
            id = root.NextObjectId++;
            root.IdToObject[id] = u;
            root.AddObjectToId(u, id);
        }
        var nm = null;
        if (rootType.TypeClassifier != null)
            nm = rootType.TypeClassifier(root, u);
        if (nm == null)
            nm = "null"
        else
            nm = "\"" + escape("[" + rootType.AssemblyName + "]" + nm[0]) + "\"";
        return id.toString() + " " + nm;
    }

    root.CreateProxied = function(typeIndex) {
        // typeIndex unused
        var u = {};
        id = root.NextObjectId++;
        root.IdToObject[id] = u;
        root.AddObjectToId(u, id);
        return id.toString();
    }

    // XREF1061
    // In: null, undefined, or id representing unmanaged 'Proxied' object
    // Out: null, undefined, or unmanaged 'Proxied' object
    root.ExportProxied = function(typeIndex, id) {
        // typeIndex unused
        if (id == null)
            return id;
        var u = root.IdToObject[id];
        if (u == null)
            throw root.InvalidOperationException("No proxied object with id: " + id.toString());
        return u;
    }

    // Managed side is no longer interested in 'Proxied' object with this id (created by unmanaged side)
    root.DisconnectProxiedObject = function(id) {
        var u = root.IdToObject[id];
        if (u == null)
            return;
        delete root.IdToObject[id];
        root.RemoveObjectToId(u);
    }

    //
    // Exceptions
    //

    // In: Any JavaScript object representing an exception, or a proxy for a 'Normal' instance of System.Exception.
    // Out:
    //   null
    //   !<managed id of normal object>          (* if obj is instance of System.Exception *)
    //   #<unmanaged id of proxied object> null  (* otherwise *)
    root.ImportException = function(jsObjectTypeIndex, u) {
        root.Plugin.Log("** ImportException(" + jsObjectTypeIndex + ")"); // DEBUGONLY
        if (u == null)
            return "null";
        else if (u.Id != null)
            return "!" + u.Id.toString();
        else
            return "#" + root.ImportProxied(jsObjectTypeIndex, u);
    }

    // ----------------------------------------------------------------------
    // Imported and exported methods
    // ----------------------------------------------------------------------

    root.MakeExportRedirector = function(argTypeIndexes, id, captureThis, inlineParamsArray) {
        return function() {
            root.Plugin.Log("** Calling export redirector for id=" + id + " with " + argTypeIndexes.length + " parameters and " + arguments.length + " arguments..."); // DEBUGONLY
            var trueArgs = arguments;
            if (captureThis || inlineParamsArray) {
                trueArgs = [];
                var j = 0;
                for (var i = 0; i < argTypeIndexes.length; i++) {
                    if (i == 0 && captureThis) {
                        trueArgs.push(this);
                    }
                    else if (inlineParamsArray && i == argTypeIndexes.length - 1) {
                        var paramsArr = [];
                        while (j < arguments.length)
                            paramsArr.push(arguments[j++]);
                        trueArgs.push(paramsArr);
                    }
                    else {
                        if (j >= arguments.length)
                            throw root.InvalidOperationException("Too few arguments in call to managed");
                        trueArgs.push(arguments[j++]);
                    }
                }
                // NOTE: Ok if j < arguments.length here - we want to ignore extra args
            }

            var sb = [];
            sb.push("(");
            for (i = 0; i < argTypeIndexes.length; i++) {
                if (i > 0)
                    sb.push(",");
                sb.push(root.Find(argTypeIndexes[i]).Import(trueArgs[i]));
            }
            sb.push(")");
            var args = sb.join("");
            root.Plugin.Log("** calling managed with id = " + id.toString() + ", args = " + args); // DEBUGONLY
            var res = root.CallManaged(id, args);
            root.Plugin.Log("** eval result of call to managed: " + res); // DEBUGONLY
            // NOTE! No need to export result, since managed side will emit appropriate export in result
            return eval(res);
        }
    }

    root.Plugin.Log("** runtime initialized"); // DEBUGONLY
}
