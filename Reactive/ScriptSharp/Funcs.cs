using Rx;

namespace System
{

    [Imported]
    public delegate IDisposable FuncObserverIDisposable(Observer value);
   
    [Imported]
    public delegate Action FuncObserverAction(Observer value);

    [Imported]
    public delegate object FuncObjectObject(object value);

    [Imported]
    public delegate int FuncObjectInt32(object value);

    [Imported]
    public delegate object FuncObject();


    [Imported]
    public delegate bool FuncObjectBoolean(object value);

    [Imported]
    public delegate Observable FuncObservable();

    [Imported]
    public delegate IDisposable FuncIDisposable();

    [Imported]
    public delegate Observable FuncIDisposableObservable();       

    [Imported]
    public delegate Observable FuncObjectObservable(object value);

    [Imported]
    public delegate string FuncObjectString(object value);

    [Imported]
    public delegate Observable FuncObservableObservable(Observable value);

    [Imported]
    public delegate object FuncObjectInt32Object(object value, int count);

    [Imported]
    public delegate bool FuncObjectInt32Boolean(object value, int count);
    [Imported]
    public delegate ISubject FuncISubject();

    [Imported]
    public delegate object FuncObjectObjectObject(object o1, object o2);

    [Imported]
    public delegate object FuncObjectArrayObject(object[] values);

    [Imported]
    public delegate Observable FuncObjectArrayObservable(object[] values);

}


