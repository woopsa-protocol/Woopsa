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

exports.readProperty = function (property, done){
    property.read(function (readResult){
        done({
            Value: readResult,
            Type: property.getType()
        });
    });
}

exports.writeProperty = function (property, value, done){
    if ( typeof property.write === 'undefined' ){
        throw new exceptions.WoopsaInvalidOperationException("Cannot write to read-only property " + property.getName());
    }else{
        value = woopsaUtils.convertTo(value, property.getType())
        property.write(value, function (writeResult){
            done({
                Value: writeResult,
                Type: property.getType()
            });
        });
        
    }
}

exports.invokeMethod = function (method, args, done){
    var processedArguments = processArguments(method, args);
    method.invoke(processedArguments, function (result, error){
        if ( typeof error === 'undefined' ){
            done({
                Value: result,
                Type: method.getReturnType()
            });
        }else{
            done(null, error);
        }
    });
}

function processArguments(method, methodArguments){
    // Convert the name-value pair methodArguments into a simple array in the right order
    var argumentInfos = method.getArgumentInfos();
    var args = [];
    for ( var i in argumentInfos ){
        var argInfo = argumentInfos[i];
        for ( var key in methodArguments ){
            if ( argInfo.getName() === key ){
                value = woopsaUtils.convertTo(methodArguments[key], argInfo.getType())
                args.push(value);
            }
        }
    }
    if ( args.length != argumentInfos.length ){
        throw new exceptions.WoopsaInvalidOperationException("Wrong parameters for WoopsaMethod " + method.getName());
    }
    return args;
}
