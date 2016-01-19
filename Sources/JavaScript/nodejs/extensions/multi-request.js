var adapter = require('../adapter');
var types = require('../types');
var exceptions = require('../exceptions');
var woopsaUtils = require('../woopsa-utils');

var MultiRequestHandler = function MultiRequestHandler(woopsaObject){
    this._multiRequest = new types.WoopsaMethodAsync("MultiRequest", "JsonData", multiRequest.bind(this), [
        {"Requests": "JsonData"}
    ]);
    woopsaObject.addMethod(this._multiRequest);

    function multiRequest(requests, done){
        if ( !isArray(requests) ){
            throw new exceptions.WoopsaInvalidOperationException("MultiRequest needs an array of requests.");
        }
        var results = [];
        var countAsync = 0;
        var doneAsync = 0;
        for ( var i in requests ){
            if ( requests[i].Action === 'invoke' ){
                countAsync++;
            }
        }
        for ( var i in requests ){
            var request = requests[i];
            var element = woopsaUtils.getByPath(woopsaObject, request.Path);
            if ( request.Action === 'read' ){
                results.push({
                    Id: request.Id,
                    Result: adapter.readProperty(element)
                });
            }else if ( request.Action === 'write' ){
                results.push({
                    Id: request.Id,
                    Result: adapter.writeProperty(element, request.Value)
                });
            }else if ( request.Action === 'meta' ){
                results.push({
                    Id: request.Id,
                    Result: adapter.generateMetaObject(element)
                })
            }else if ( request.Action === 'invoke' ){
                var newResult = {
                    Id: request.Id
                }
                results.push(newResult);
                adapter.invokeMethod(element, request.Arguments, function (resultIndex, value, error){
                    doneAsync++;
                    if ( typeof error !== 'undefined' )
                        results[resultIndex].Result = error;
                    else
                        results[resultIndex].Result = value;
                    if ( countAsync === doneAsync ){
                        done(results);
                    }
                }.bind(this, results.length-1))
            }else{
                throw new exceptions.WoopsaInvalidOperationException("Invalid MultiRequest action " + request.Action);
            }
        }        
        if ( countAsync === 0 ){
            // If there were no invokes, we can immediately
            // send the result.
            // Otherwise, the done callback will be called
            // when all invoke's are done.
            done(results);
        }
    }
}

function isArray(value){
    return value &&
        typeof value === 'object' &&
        typeof value.length === 'number' &&
        typeof value.splice === 'function' &&
        !(value.propertyIsEnumerable('length'));
}

exports.MultiRequestHandler = MultiRequestHandler;