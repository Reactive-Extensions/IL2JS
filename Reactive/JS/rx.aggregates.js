(function()
{
    var global = this;
    var root;
    if (typeof ProvideCustomRxRootObject == "undefined")
    {
        root = global.Rx;
    }
    else
    {
        root = ProvideCustomRxRootObject();
    }
    var _undefined = undefined;
    var defaultComparer = function(a, b) { return a === b };
    var identity = function(x) { return x; }
    var observable = root.Observable;
    var proto = observable.prototype;
    var sequenceContainsNoElements = "Sequence contains no elements.";
    var observableCreateWithDisposable = observable.CreateWithDisposable;

    proto.Aggregate = function(seed, accumulator)
    {
        return this.Scan(seed, accumulator).StartWith(seed).Final();
    }

    proto.Aggregate1 = function(accumulator)
    {
        return this.Scan1(accumulator).Final();
    }

    proto.IsEmpty = function()
    {
        var parent = this;
        return observableCreateWithDisposable(function(subscriber)
        {
            return parent.Subscribe(
                function()
                {
                    subscriber.OnNext(false);
                    subscriber.OnCompleted();
                },
                function(e)
                {
                    subscriber.OnError(e);
                },
                function()
                {
                    subscriber.OnNext(true);
                    subscriber.OnCompleted();
                });
        });
    }

    proto.Any = function(predicate)
    {
        if (predicate === _undefined)
            return this.IsEmpty().Select(function(b) { return !b; });
        return this.Where(predicate).Any();
    }

    proto.All = function(predicate)
    {
        if (predicate === _undefined)
            predicate = identity;
        return this.Where(function(v) { return !predicate(v); }).IsEmpty();
    }

    proto.Contains = function(value, comparer)
    {
        if (comparer === _undefined)
            comparer = defaultComparer;

        return this.Where(function(v) { comparer(v, value) }).Any();
    }

    proto.Count = function()
    {
        return this.Select(function(value, index) { return index + 1; }).Final();
    }

    proto.Sum = function()
    {
        return this.Scan(0, function(prev, cur) { return prev + cur }).StartWith(0).Final();
    }

    proto.Average = function()
    {
        return this.Scan({ sum: 0, count: 0 }, function(prev, cur) { return { sum: prev.sum + cur, count: prev.count + 1 }; })
            .Final()
            .Select(function(s) { return s.sum / s.count; });
    }

    proto.Final = function()
    {
        var parent = this;
        return observableCreateWithDisposable(function(subscriber)
        {
            var value;
            var hasValue = false;
            return parent.Subscribe(
                function(v)
                {
                    hasValue = true;
                    value = v;
                },
                function(e)
                {
                    subscriber.OnError(e);
                },
                function()
                {
                    if (!hasValue)
                        subscriber.OnError(sequenceContainsNoElements);
                    subscriber.OnNext(value);
                    subscriber.OnCompleted();
                });
        });
    }

    var extremaBy = function(source, keySelector, compare)
    {
        return observableCreateWithDisposable(function(subscriber)
        {
            var hasValue = false;
            var lastKey;
            var result;

            return source.Subscribe(
            function(v)
            {
                var key;
                try
                {
                    key = keySelector(v);
                }
                catch (e)
                {
                    subscriber.OnError(e);
                    return;
                }
                if (!hasValue)
                {
                    hasValue = true;
                    lastKey = key;
                    result = v;
                    return;
                }
                var replace;
                try
                {
                    replace = compare(lastKey, key);
                }
                catch (e)
                {
                }
                if (replace)
                {
                    lastKey = key;
                    result = v;
                }
            },
            function(e)
            {
                subscriber.OnError(e);
            },
            function()
            {
                if (!hasValue)
                    subscriber.OnError(sequenceContainsNoElements);

                subscriber.OnNext(result);
                subscriber.OnCompleted();
            }); 

        });
    };

    proto.Min = proto.MinBy = function(keySelector, comparer)
    {
        if (keySelector === _undefined)
            keySelector = identity;
        if (comparer === _undefined)
            comparer = function(current, key) { return key < current; };

        return extremaBy(this, keySelector, comparer);
    };

    proto.Max = proto.MaxBy = function(keySelector, comparer)
    {
        if (keySelector === _undefined)
            keySelector = identity;
        if (comparer === _undefined)
            comparer = function(current, key) { return key > current; };

        return extremaBy(this, keySelector, comparer);
    };
    
})();