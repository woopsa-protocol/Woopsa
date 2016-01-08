var exceptions = require('./exceptions')

var WoopsaObject = function WoopsaObject(name){
	this.Properties = {};
	this.Methods = {};
	this.Items = {};
	this.Name = name;
}

WoopsaObject.prototype.addProperty = function (name, type, readOnly){
	if ( typeof this.Properties[name] === 'undefined' ){
		this.Properties[name] = new WoopsaProperty(name, type, readOnly);
		return this.Properties[name];
	}else{
		throw new exceptions.WoopsaException("Tried to add a property with duplicate name " + name);
	}
}

WoopsaObject.prototype.addMethod = function (name, returnType){
	if ( typeof this.Methods[name] === 'undefined' ){
		this.Methods[name] = new WoopsaMethod(name, ReturnType);
		return this.Methods[name];
	}else{
		throw new exceptions.WoopsaException("Tried to add a method with duplicate name " + name);
	}
}

WoopsaObject.prototype.addItem = function (name, item){
	if ( typeof this.Items[name] === 'undefined' ){
		this.Items[name] = item;
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
	if ( typeof this.Arguments[name] === 'undefined' ){
		this.Arguments[name] = new WoopsaMethodArgumentInfo(name, type);
		return this.Arguments[name];
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