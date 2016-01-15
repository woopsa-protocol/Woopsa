var exceptions = require('./exceptions');

exports.defaults = function (options, defaultOptions){
  var result = typeof options !== 'undefined' ? options : defaultOptions;
  for(var key in defaultOptions){
    result[key] = typeof result[key] !== 'undefined' ? result[key] : defaultOptions[key];
  }
  return result;
}

exports.removeExtraSlashes = function (value){
    return value.replace(/\/+/g,"/");
}

// This functions uses JavaScript's various
// features to try and find out the type of a 
// value. 
// Returns a string representation of the Woopsa type
// or undefined if it seems like a regular object
// (and should thus probably be a WoopsaObject)
exports.inferWoopsaType = function (value){
    var type = typeof value;
    if ( type === 'number' )
        return 'Real'; // There are no integers in JS
    else if ( type === 'boolean' )
        return 'Logical';
    else if ( type === 'string' )
        return 'Text';
    else
        if ( value instanceof Date )
            return 'DateTime';
        else
            return undefined;
}

exports.convertTo = function (value, type){
    if ( type === 'Integer' ){
        if ( isNaN(parseInt(value)) )
            throw new exceptions.WoopsaInvalidOperationException("Cannot convert value " + value + " to type " + type);
        else
            return parseInt(value);
    }else if ( type === 'Real' || type === 'TimeSpan' ){
        if ( isNaN(parseFloat(value)) )
            throw new exceptions.WoopsaInvalidOperationException("Cannot convert value " + value + " to type " + type);
        else
            return parseFloat(value);
    }else if ( type === 'Logical' ){
        if ( value.toLowerCase() === 'true' )
            return true;
        else if ( value.toLowerCase() === 'false' )
            return false;
        else
            throw new exceptions.WoopsaInvalidOperationException("Cannot convert value " + value + " to type " + type);
    }else if ( type === 'JsonData' ){
        return JSON.parse(value);
    }else if ( type === 'DateTime' ){
        var parsedDate = Date.parse(value);
        if ( isNaN(parsedDate) ) // Warning: might not be consistent across implementations
            throw new exceptions.WoopsaInvalidOperationException("Cannot convert value " + value + " to type " + type);
        else
            return parsedDate;
    }else{
        return value;
    }
}

exports.getByPath = function (element, path){
    var pathParts = path.split("/");
    if ( pathParts[0] == "" )
        pathParts.splice(0, 1);
    if ( pathParts[pathParts.length-1] == "" )
        pathParts.splice(pathParts.length-1, 1);

    var returnElement = element;

    if ( pathParts.length == 0 ){
        return element;
    }else{
        return getByPathArray(returnElement, pathParts);
    }
}

function getByPathArray(element, pathArray){
    var key = pathArray[0];

    var foundItem = null;
    var items = element.getItems();
    for ( var i in items )
        if ( items[i].getName() === key )
            foundItem = items[i];

    if ( foundItem !== null ){
        if ( pathArray.length === 1 ){
            return foundItem;
        }else{
            return getByPathArray(foundItem, pathArray.slice(1));
        }
    }else{
        var foundProperty = null;
        var properties = element.getProperties();
        for ( var i in properties )
            if ( properties[i].getName() === key )
                foundProperty = properties[i];

        if ( foundProperty !== null ){
            if ( pathArray.length === 1 ){
                return foundProperty;
            }else{
                return undefined; 
            }
        }else{
            var foundMethod = null;
            var methods = element.getMethods();
            for ( var i in methods )
                if ( methods[i].getName() === key )
                    foundMethod = methods[i];

            if ( foundMethod !== null ){
                if ( pathArray.length === 1 ){
                    return foundMethod;
                }else{
                    return undefined; 
                }
            }
        }
    }

    return undefined;
}

exports.decodeWoopsaLink = function(woopsaLink){
    var parts = woopsaLink.split("#");
    if ( parts.length == 1 ){
        return {
            server: null,
            path: woopsaLink
        }
    }else{
        return {
            server: parts[0],
            path: parts[1]
        }
    }
}

exports.getPath = function (element){
    var path = "";
    if ( element.getContainer() !== null )
        path += element.getName();
    while ( (element = element.getContainer()) !== null ){
        if ( element.getContainer() !== null )
            path = element.getName() + "/" + path;
        else
            path = "/" + path;
    }
    return path;
}