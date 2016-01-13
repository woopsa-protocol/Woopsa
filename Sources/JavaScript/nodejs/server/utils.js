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