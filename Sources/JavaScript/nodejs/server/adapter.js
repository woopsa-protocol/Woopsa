var types = require('./types');
var exceptions = require('./exceptions');
var utils = require('./utils');

exports.getByPath = function (element, path){
		// This works with regular JavaScript objects
	var pathParts = path.split("/");
	if ( pathParts[0] == "" )
		pathParts.splice(0, 1);
	if ( pathParts[pathParts.length-1] == "" )
		pathParts.splice(pathParts.length-1, 1);

	var returnElement = element;

	if ( pathParts.length == 0 ){
		return element;
	}else{
		if ( element.constructor.name === 'WoopsaObject' ){
			// This works with WoopsaObjects
			return getByPathArrayOnWoopsaObject(returnElement, pathParts);
		}else{
			return getByPathArrayOnObject(returnElement, pathParts);
		}
	}	
}

function getByPathArrayOnWoopsaObject(element, pathArray){
	var key = pathArray[0];

	var foundItem = null;
	for ( var i in element.Items )
		if ( element.Items[i].Name === key )
			foundItem = element.Items[i];

	if ( foundItem !== null ){
		if ( pathArray.length === 1 ){
			return foundItem;
		}else{
			return getByPathArrayOnWoopsaObject(foundItem, pathArray.slice(1));
		}
	}else{
		var foundProperty = null;
		for ( var i in element.Properties )
			if ( element.Properties[i].Name === key )
				foundProperty = element.Properties[i];

		if ( foundProperty !== null ){
			if ( pathArray.length === 1 ){
				return foundProperty;
			}else{
				return undefined; 
			}
		}else{
			var foundMethod = null;
			for ( var i in element.Methods )
				if ( element.Methods[i].Name === key )
					foundProperty = element.Methods[i];

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

function getByPathArrayOnObject(element, pathArray){
	var key = pathArray[0];
	if ( typeof element[key] !== 'undefined' ){
		if ( pathArray.length === 1 ){
			return element[key];
		}else{
			return getByPathArrayOnObject(element[key], pathArray.slice(1));
		}
	}else{
		return undefined;
	}
}

exports.generateMetaDataObject = function (element){
	if ( element.constructor.name === 'WoopsaObject' )
		return element;

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

exports.readValue = function (element){
	if ( typeof element.read !== 'undefined' ){
		var readResult = element.read();
		return {
			"Value": readResult,
			"Type": (typeof element.Type !== 'undefined')?element.Type:utils.inferWoopsaType(readResult)
		};
	}else{
		return {
			"Value": element,
			"Type": utils.inferWoopsaType(element)
		};
	}
}

exports.writeValue = function (element, value){
	if ( typeof element.write !== 'undefined' ){
		element.write(value);
		var readResult = element.read(value);
		return {
			"Value": readResult,
			"Type": (typeof element.Type !== 'undefined')?element.Type:utils.inferWoopsaType(readResult)
		};
	}else{
		var elementType = utils.inferWoopsaType(element);
		var valueType = utils.inferWoopsaType(element);
		if ( elementType === valueType ){
			element = value;
			return {
				"Value": element,
				"Type": utils.inferWoopsaType(element)
			};
		}else{
			throw new exceptions.WoopsaInvalidOperationException("Cannot typecast " + valueType + " to " + elementType);
		}
	}
}