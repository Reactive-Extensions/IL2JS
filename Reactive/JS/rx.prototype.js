(function()
{
	var fromPrototypeEvent = Rx.Observable.FromPrototypeEvent = function(prototypeElement, eventType) 
	{
	    return Rx.Observable.Create(function(observer)
	    {
	        var handler = function(eventObject) 
	        {
	            observer.OnNext(eventObject);
	        };
		    Element.observe(prototypeElement, eventType, handler);
	        return function() 
	        {
	            Element.stopObserving(prototypeElement, eventType, handler);
	        };
	    });
	};
	
	Element.addMethods( { ToObservable : function(element, eventType) { return fromPrototypeEvent(element, eventType); } });
})();


