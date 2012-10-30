(function()
{
	var fromMooToolsEvent = Rx.Observable.FromMooToolsEvent = function(mooToolsObject, eventType) 
	{
	    return Rx.Observable.Create(function(observer)
	    {
	        var handler = function(eventObject) 
	        {
	            observer.OnNext(eventObject);
	        };
	        var handle = mooToolsObject.addEvent(eventType, handler);
	        return function() 
	        {
	            mooToolsObject.removeEvent(eventType, handler);
	        };
	    });
	};
})();