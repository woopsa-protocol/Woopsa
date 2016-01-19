var types = require('./types');
var exceptions = require('./exceptions');
var woopsaUtils = require('./woopsa-utils');

exports.generateMetaObject = function (element){
    var metaObject = {
        Name: element.getName(),
        Properties: [],
        Methods: [],
        Items: []
    };
    var items = element.getItems();
    for ( var i in items ){
        metaObject.Items.push(items[i].getName());
    }
    var properties = element.getProperties();
    for ( var i in properties ){
        var property = properties[i];
        metaObject.Properties.push({
            Name: property.getName(),
            Type: property.getType(),
            ReadOnly: (typeof property.write === 'undefined')?true:false
        })
    }
    var methods = element.getMethods();
    for ( var i in methods ){
        var method = methods[i];
        var newMethod = {
            Name: method.getName(),
            ReturnType: method.getReturnType(),
            ArgumentInfos: []
        }
        var argumentInfos = method.getArgumentInfos();
        for ( var j in argumentInfos ){
            newMethod.ArgumentInfos.push({
                Name: argumentInfos[j].getName(),
                Type: argumentInfos[j].getType()
            })
        }
        metaObject.Methods.push(newMethod);
    }
    return metaObject;
}

exports.readProperty = function (property){
    var readResult = property.read();
    return {
        Value: readResult,
        Type: property.getType()
    };
}

exports.writeProperty = function (property, value){
    if ( typeof property.write === 'undefined' ){
        throw new exceptions.WoopsaInvalidOperationException("Cannot write to read-only property " + element.getName());
    }else{
        // TODO : basic type checking
        value = woopsaUtils.convertTo(value, property.getType())
        property.write(value);
        var readResult = property.read();
        return {
            Value: readResult,
            Type: property.getType()
        };
    }
}

exports.invokeMethod = function (method, arguments, done){
    var processedArguments = processArguments(method, arguments);
    if ( typeof method.invokeAsync !== 'undefined' ){
        method.invokeAsync(processedArguments, function (result, error){
            if ( typeof error === 'undefined' ){
                done({
                    Value: result,
                    Type: method.getReturnType()
                });
            }else{
                done(null, error);
            }
        });
        return undefined;
    }else{
        var invokeResult = method.invoke(processedArguments);
        done({
            Value: invokeResult,
            Type: method.getReturnType()
        });     
    }
}

function processArguments(method, arguments){
    // Convert the name-value pair arguments into a simple array in the right order
    var argumentInfos = method.getArgumentInfos();
    var args = [];
    for ( var i in argumentInfos ){
        var argInfo = argumentInfos[i];
        for ( var key in arguments ){
            if ( argInfo.getName() === key ){
                value = woopsaUtils.convertTo(arguments[key], argInfo.getType())
                args.push(value);
            }
        }
    }
    if ( args.length != argumentInfos.length ){
        throw new exceptions.WoopsaInvalidOperationException("Wrong parameters for WoopsaMethod " + method.getName());
    }
    return args;
}