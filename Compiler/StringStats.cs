//
// Collect string usage statistics for a type so that we can decide whether to share string literals at the type
// scope.
//

using System;
using System.Collections.Generic;
using System.Linq;
using CST = Microsoft.LiveLabs.CST;

namespace Microsoft.LiveLabs.JavaScript.IL2JS
{
    public enum StringBindScope
    {
        Type,
        InPlace
    }

    public class StringStats : IEnumerable<KeyValuePair<string, StringBindScope>>
    {
        private readonly Dictionary<string, int> counts;

        public StringStats()
        {
            counts = new Dictionary<string, int>(StringComparer.Ordinal);
        }

        private void Seen(string str)
        {
            var n = default(int);
            if (counts.TryGetValue(str, out n))
                counts[str] = n + 1;
            else
                counts.Add(str, 1);
        }

        public void Collect(CST.Global global, CST.AssemblyDef assemblyDef, CST.TypeDef typeDef)
        {
            foreach (var methodDef in typeDef.Members.OfType<CST.MethodDef>().Where(m => m.IsUsed && m.Invalid == null))
                Collect(global, methodDef);
        }

        private void Collect(CST.Global global, CST.MethodDef methodDef)
        {
            var instructions = methodDef.Instructions(global);
            if (instructions != null)
            {
                foreach (var inst in instructions.Body)
                {
                    if (inst.Flavor == CST.InstructionFlavor.LdString)
                    {
                        var ldstri = (CST.LdStringInstruction)inst;
                        Seen(ldstri.Value);
                    }
                }
            }
        }

        public IEnumerator<KeyValuePair<string, StringBindScope>> GetEnumerator()
        {
            foreach (var kv in counts)
            {
                //                       Definition          Reference
                //                       ------------------  ------------------
                // In-place string    |  <nothing>           "..."
                // Type level string  |  a.sxy="...";        a.sxy
                var inplaceSize = kv.Value*(kv.Key.Length + 2);
                var typeSize = 9 + kv.Key.Length + kv.Value*5;
                yield return
                    new KeyValuePair<string, StringBindScope>
                        (kv.Key, typeSize < inplaceSize ? StringBindScope.Type : StringBindScope.InPlace);
            }
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}