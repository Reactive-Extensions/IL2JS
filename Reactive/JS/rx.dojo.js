(function()
{
	var fromDojoEvent = Rx.Observable.FromDojoEvent = function(dojoObject, eventType, context, dontFix) 
	{
	    return Rx.Observable.Create(function(observer)
	    {
	        var handler = function(eventObject) 
	        {
	            observer.OnNext(eventObject);
	        };
		var handle = dojo.connect(dojoObject, eventType, context, handler, dontFix);
	        return function() 
	        {
	            dojo.disconnect(handle);
	        };
	    });
	};	
})();


