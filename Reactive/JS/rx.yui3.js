(function()
{
	var yuiUse = Rx.Observable.YUI3Use = function(what) 
	{
		return Rx.Observable.CreateWithDisposable(function(observer)
		{			
			var handler = function(y)
			{
				observer.OnNext(y);
			};
			var u = YUI().use(what, handler);
			return Rx.Disposable.Empty;
		});
	};
	var fromYUIEvent = Rx.Observable.FromYUI3Event = function(selector, eventType) 
	{
	    return yuiUse("node-base").SelectMany(function(y)
	    {
		    return Rx.Observable.Create(function(observer)
		    {
	        	var handler = function(eventObject) 
		        {
		            observer.OnNext(eventObject);
	        	};
		        y.on(eventType, handler, selector);
		        return function() 
	        	{
		            y.detach(eventType, handler, selector);
		        };
		    });
	    });	
	};
})();