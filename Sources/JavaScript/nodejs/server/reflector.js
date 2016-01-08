var types = require('./types');

exports.generateMetaDataFromObject = function (element){
	var result = {
		"Name": element.constructor.name,
		"Properties": []
	};
	for (var key in element){
		var value = element[key];
		var type = utils.inferWoopsaType(value);
		// Methods
		if ( typeof value === 'function' ){
			if ( typeof result["Methods"] === 'undefined' ){
				result["Methods"] = [];
			}
			result["Methods"].push({
				"Name": key,
				"Arguments": [],
				"ReturnType": 'Text' // We can never know the return type of a function in JS
			});
		}else if ( typeof type === 'undefined' ){
			// If the type is undefined, it's a Woopsa Object
			if ( typeof result["Items"] === 'undefined' )
				result["Items"] = [];
			result["Items"].push(key);
		}else{
			// Just a regular old WoopsaProperty!
			result["Properties"].push({
				"Name": key,
				"Type": type,
				"ReadOnly": false // There is no such thing as read-only in JS
			});
		}
	}
	return result;
}