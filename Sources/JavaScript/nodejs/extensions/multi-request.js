var adapter = require('../adapter');
var types = require('../types');
var exceptions = require('../exceptions');
var woopsaUtils = require('../woopsa-utils');

/**
 * @class A service that allows clients to make multiple requests
 * at the same time.
 * @param {WoopsaObject} woopsaObject The WoopsaObject that this
 *                                    method will be added to.
 *                                    This should be your root 
 *                                    object.
 */
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
            if ( requests[i].Action !== 'meta' ){
                countAsync++;
            }
        }
        for ( var i in requests ){
            var request = requests[i];
            var element = woopsaUtils.getByPath(woopsaObject, request.Path);
            var newResult = {Id: request.id};
            results.push(newResult);
            if ( request.Action === 'meta' ){
                newResult.Result = adapter.generateMetaObject(element);
            }else if ( request.Action === 'read' ){
                adapter.readProperty(element, resultCallback.bind(this, results.length-1));
            }else if ( request.Action === 'write' ){
                adapter.writeProperty(element, request.Value, resultCallback.bind(this, results.length-1));
            }else if ( request.Action === 'invoke' ){
                adapter.invokeMethod(element, request.Arguments, resultCallback.bind(this, results.length-1))
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

        function resultCallback(resultIndex, value, error){
            doneAsync++;
            if ( typeof error !== 'undefined' )
                results[resultIndex].Result = error;
            else
                results[resultIndex].Result = value;
            if ( countAsync === doneAsync ){
                done(results);
            }
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