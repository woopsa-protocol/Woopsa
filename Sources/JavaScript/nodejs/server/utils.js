exports.defaults = function (options, defaultOptions){
  var result = typeof options !== 'undefined' ? options : defaultOptions;
  for(var key in defaultOptions){
    result[key] = typeof result[key] !== 'undefined' ? result[key] : defaultOptions[key];
  }
  return result;
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