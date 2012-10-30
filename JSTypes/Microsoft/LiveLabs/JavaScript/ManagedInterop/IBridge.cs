using System;

namespace Microsoft.LiveLabs.JavaScript.ManagedInterop
{
    public interface IBridge
    {
        // Evaluate statement in JavaScript engine.
        // Caller is responsible for using InJavaScriptContext below to ensure this is in the correct thread context
        void EvalStatementString(string stmnt);

        // Evaluate expression in JavaScript engine and return result.
        // Caller is responsible for using InJavaScriptContext below to ensure this is in the correct thread context
        string EvalExpressionString(string expr);

        // Invoke f in JavaScript engine's execution context, taking account of any syncronization
        void InJavaScriptContext(Action f);

        // Expression to evaluate to yield the plugin object, which is then stored within the root structure
        string PluginExpression { get; }

        // Invoke managed method or delegate with given id and encoded arguments, and return encoded result.
        string CallManaged(int id, string args);

        // Log a message in the runtime
        void Log(string msg);

        // Increment nesting level of log
        void IndentLog();

        // Decrement nesting level of log
        void UnindentLog();
    }
}
