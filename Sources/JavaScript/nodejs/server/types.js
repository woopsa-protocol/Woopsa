var exceptions = require('./exceptions')

var WoopsaObject = function WoopsaObject(name){
	this.Properties = [];
	this.Methods = [];
	this.Items = [];
	this.Name = name;
}

function hasElement(list, elementName){
	for ( var i in list )
		if ( list[i].Name === elementName )
			return true;
	return false;
}

WoopsaObject.prototype.addProperty = function (name, type, readOnly){
	if ( !hasElement(this.Properties, name) ){
		var newProperty = new WoopsaProperty(name, type, readOnly);
		this.Properties.push(newProperty);
		return newProperty;
	}else{
		throw new exceptions.WoopsaException("Tried to add a property with duplicate name " + name);
	}
}

WoopsaObject.prototype.addMethod = function (name, returnType){
	if ( !hasElement(this.Methods, name) ){
		var newMethod = new WoopsaMethod(name, returnType);
		this.Methods.push(newMethod);
		return newMethod;
	}else{
		throw new exceptions.WoopsaException("Tried to add a method with duplicate name " + name);
	}
}

WoopsaObject.prototype.addItem = function (item){
	if ( !hasElement(this.Items, item.Name) ){
		this.Items.push(item);
		return item;
	}else{
		throw new exceptions.WoopsaException("Tried to add an item with duplicate name " + name);
	}
}

var WoopsaProperty = function WoopsaProperty(name, type, read, write){
	this.Name = name;
	this.Type = type;
	this.read = read;
	this.write = write;
	if ( typeof write === 'undefined' )
		this.ReadOnly = true;
	else
		this.ReadOnly = false;
}

var WoopsaMethod = function WoopsaMethod(name, returnType){
	this.Name = name;
	this.ReturnType = returnType;
	this.Arguments = {};
}

WoopsaMethod.prototype.addMethodArgument = function (name, type){
	if ( !hasElement(this.Arguments, name) ){
		var newArgument = new WoopsaMethodArgumentInfo(name, type);
		this.Arguments.push(newArgument);
		return newArgument;
	}else{
		throw new exceptions.WoopsaException("Tried to add a method argument with duplicate name " + name + " in method " + this.Name);
	}
}

var WoopsaMethodArgumentInfo = function WoopsaMethodArgumentInfo(name, type){
	this.Name = name;
	this.Type = type;
}

exports.WoopsaObject = WoopsaObject;
exports.WoopsaProperty = WoopsaProperty;
exports.WoopsaMethod = WoopsaMethod;
exports.WoopsaMethodArgumentInfo = WoopsaMethodArgumentInfo;