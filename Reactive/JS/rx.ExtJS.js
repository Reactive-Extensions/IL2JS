(function()
{
	var fromExtJSEvent = Rx.Observable.FromExtJSEvent = function(extJSObject, eventName, scope, options) 
	{
	    return Rx.Observable.Create(function(observer)
	    {
	        var handler = function(eventObject) 
        	{
	            observer.OnNext(eventObject);
	        };
		Ext.EventManager.on(extJSObject, eventName, handler, scope, options);
	        return function() 
	        {
	            Ext.EventManager.un(extJSObject, eventName, handler, scope);
	        };
	    });
	};

	Ext.Element.prototype.ToObservable = function(eventName, scope, options)
	{
		return fromExtJSEvent(this, eventName, scope, options);
	}
})();
