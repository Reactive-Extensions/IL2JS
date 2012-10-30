using System;
using System.Collections.Generic;
using System.Reflection;

namespace Microsoft.LiveLabs.JavaScript.ManagedInterop
{
    public class InteropDatabase
    {
        private Dictionary<string, Type> delegateShims;
        private Dictionary<Type, TypeInfo> types;
        private List<ExportInfo> staticExports;
        private string root;

        public InteropDatabase()
        {
            delegateShims = new Dictionary<string, Type>();
            types = new Dictionary<Type, TypeInfo>();
            staticExports = new List<ExportInfo>();
            root = "IL2JSR";
        }

        public TypeInfo FindTypeInfo(Type type)
        {
            var ti = default(TypeInfo);
            if (types.TryGetValue(type, out ti))
                return ti;
            else
                return null;
        }

        public void RegisterRootExpression(string root)
        {
            this.root = root;
        }

        // The global variable to hold the 'root' interop runtime structure
        public string RootExpression { get { return root; } }

        public void RegisterType(Type type, int style, string keyField, string typeClassifier, int rootTypeSteps, bool captureThis, bool inlineParamsArray, bool undefinedIsNotNull)
        {
            var typeInfo = new TypeInfo
                           {
                               Style = (InteropStyle)style,
                               KeyField = keyField,
                               TypeClassifier = typeClassifier,
                               RootTypeSteps = rootTypeSteps,
                               CaptureThis = captureThis,
                               InlineParamsArray = inlineParamsArray,
                               InstanceExports = null,
                               UndefinedIsNotNull = undefinedIsNotNull
                           };
            if (!types.ContainsKey(type))
                types.Add(type, typeInfo);
        }

        public Type FindDelegateShim(Type delegateType)
        {
            var hkDelegateType = delegateType;
            var typeArgs = delegateType.GetGenericArguments();
            if (typeArgs.Length > 0)
                hkDelegateType = delegateType.GetGenericTypeDefinition();
            var shimType = default(Type);
            var nm = TypeInfo.ShimFullName(hkDelegateType.FullName);
            if (!delegateShims.TryGetValue(nm, out shimType))
                throw new InvalidOperationException("no shim has been registered for delegate: " + hkDelegateType.FullName);
            if (typeArgs.Length > 0)
                return shimType.MakeGenericType(typeArgs);
            else
                return shimType;
        }

        public void RegisterDelegateShim(Type shimType)
        {
            if (shimType.IsGenericType && !shimType.IsGenericTypeDefinition)
                throw new InvalidOperationException("delegate shims should be registered as definitions, not instances");
            var mi = shimType.GetMethod("Invoke");
            if (mi == null)
                throw new InvalidOperationException("not a shim type");
            if (!delegateShims.ContainsKey(shimType.FullName))
                delegateShims.Add(shimType.FullName, shimType);
        }

        public void RegisterExport(MethodBase methodBase, bool bindToInstance, string script)
        {
            var ei = new ExportInfo { MethodBase = methodBase, Script = script };
            if (bindToInstance)
            {
                var ti = FindTypeInfo(methodBase.DeclaringType);
                if (ti == null)
                    throw new InvalidOperationException("no type info for exported instance's type");
                if (ti.InstanceExports == null)
                    ti.InstanceExports = new List<ExportInfo>();
                ti.InstanceExports.Add(ei);
            }
            else
            {
                staticExports.Add(ei);
                InteropContextManager.WithAllRuntimes
                    (runtime => runtime.BindExportedMethod(methodBase, null, script));
            }
        }

        public void PrepareNewRuntime(Runtime runtime)
        {
            foreach (var sei in staticExports)
                runtime.BindExportedMethod(sei.MethodBase, null, sei.Script);
        }
    }
}
