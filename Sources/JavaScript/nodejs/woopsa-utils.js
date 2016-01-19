var exceptions = require('./exceptions');

/**
 * Creates an object based on its default values.
 * @param  {Object} options        Any object with key/value pairs
 * @param  {Object} defaultOptions The default key/value pairs
 * @return {Object}                An object with the default key/value pairs
 *                                 if the key/value pair in not present in
 *                                 options
 */
exports.defaults = function (options, defaultOptions){
  var result = typeof options !== 'undefined' ? options : defaultOptions;
  for(var key in defaultOptions){
    result[key] = typeof result[key] !== 'undefined' ? result[key] : defaultOptions[key];
  }
  return result;
}

/**
 * Simple helper method to remove double slashes in a path
 * @param  {String} value The string in which to remove slashes
 * @return {String}       A string with extra slashes removed
 */
exports.removeExtraSlashes = function (value){
    return value.replace(/\/+/g,"/");
}

/**
 * Try to guess (infer) the WoopsaType of a given
 * value. Obviously, because of JavaScript's dynamic
 * nature, this isn't always accurate.
 * This means that we can only reliably detect the 
 * following types:
 *  - Real
 *  - Logical
 *  - Text
 *  - DateTime
 * Anything else will just return undefined, meaning
 * this value is probably a WoopsaObject.
 * @param  {*} value The value to check
 * @return {[type]}  A string, or undefined if
 *                   the type is unkown
 */
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

/**
 * Tries to convert a given value to another type
 * based on the passed WoopsaType.
 * @param  {*} value      The value to try and convert
 * @param  {String} type  The WoopsaType string representation
 *                        to convert the value to.
 * @return {*}            The converted value, or the passed value
 *                        if there is nothing to do.
 */
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
        var parsedDate = new Date(value);
        if ( isNaN(parsedDate) ) // Warning: might not be consistent across implementations
            throw new exceptions.WoopsaInvalidOperationException("Cannot convert value " + value + " to type " + type);
        else
            return parsedDate;
    }else{
        return value;
    }
}

/**
 * Gets a WoopsaElement from a specified path, relative to another
 * WoopsaElement passed as the first argument.
 * @param  {Object} element The WoopsaObject to search in
 * @param  {String} path    The slashes-delimited path to search for
 * @return {Object}         The found object, or undefined if not found
 */
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

/**
 * Converts the serialized representation of a WoopsaLink to
 * a more usable object.
 * @param  {String} woopsaLink A pound(#)-delimited string consisting of
 *                             {server-address}#{woopsa-path}
 * @return {Object}            The decoded link, containing:
 *                                 - {String} server    The URL of the server or null if not present
 *                                 - {String} path      The Woopsa path of the element
 */
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

/**
 * Tries to get the WoopsaPath of a WoopsaElement by traversing
 * their containers using getContainer() until it returns null.
 * @param  {Object} element The element for which to get the path
 * @return {String}         The calculated Woopsa path.
 */
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