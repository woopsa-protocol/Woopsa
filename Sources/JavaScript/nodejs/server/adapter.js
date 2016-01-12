var types = require('./types');
var exceptions = require('./exceptions');
var utils = require('./utils');

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
			Type: (typeof property.getType !== 'undefined')?property.getType():utils.inferWoopsaType(property.read()),
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
		Type: (typeof property.getType !== 'undefined')?property.getType():utils.inferWoopsaType(readResult)
	};
}

exports.writeProperty = function (property, value){
	if ( typeof property.write === 'undefined' ){
		throw new exceptions.WoopsaInvalidOperationException("Cannot write to read-only property " + element.getName());
	}else{
		// TODO : basic type checking
		property.write(value);
		var readResult = property.read(value);
		return {
			Value: readResult,
			Type: (typeof property.getType !== 'undefined')?property.getType():utils.inferWoopsaType(readResult)
		};
	}
}

exports.invokeMethod = function (method, arguments){
	// Convert the name-value pair arguments into a simple array in the right order
	var argumentInfos = method.getArgumentInfos();
	var args = [];
	for ( var i in argumentInfos ){
		var argInfo = argumentInfos[i];
		for ( var key in arguments ){
			if ( argInfo.getName() === key ){
				args.push(arguments[key]);
			}
		}
	}
	if ( args.length != method.getArgumentInfos().length ){
		throw new exceptions.WoopsaInvalidOperationException("Wrong parameters for WoopsaMethod " + method.getName());
	}
	var invokeResult = method.invoke(args);
	return {
		Value: invokeResult,
		Type: method.getReturnType()
	}
}