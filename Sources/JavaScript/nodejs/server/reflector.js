var woopsaUtils = require('./woopsa-utils');
var exceptions = require('./exceptions');
var getParameterNames = require('get-parameter-names');

var DEFAULT_METHOD_RETURN_TYPE = 'Text'; 
var DEFAULT_METHOD_ARGUMENT_TYPE = 'Text';

var Reflector = function Reflector(container, element, name, typerFunction){
    this._container = container;
    this._extraItems =  [];
    this._element = element;

    if ( typeof name === 'undefined' || name === null )
        this._name = element.constructor.name;
    else
        this._name = name;

    if ( typeof typerFunction !== 'undefined' )
        this._typerFunction = typerFunction;
    else
        this._typerFunction = undefined;
}

Reflector.prototype = {
    getContainer: function (){
        return this._container;
    },
    getProperties: function (){
        var result = [];
        for (var key in this._element){
            var value = this._element[key];
            var type = woopsaUtils.inferWoopsaType(value);
            // Methods
            if ( typeof value !== 'function' && typeof type !== 'undefined' ){
                // Just a regular old WoopsaProperty!
                result.push({
                    getContainer: function (){
                        return this;
                    }.bind(this),
                    getName: function (key){
                        return key;
                    }.bind(this, key),
                    getType: function (element, key){
                        if ( typeof this._typerFunction !== 'undefined' ){
                            var path = woopsaUtils.getPath(this) + "/" + key;
                            return this._typerFunction(path, woopsaUtils.inferWoopsaType(element[key]));
                        }else{
                            return woopsaUtils.inferWoopsaType(element[key]);
                        }   
                    }.bind(this, this._element, key),
                    write: function (element, key, value){
                        element[key] = value;
                    }.bind(this, this._element, key),
                    read: function (element, key){
                        return element[key];
                    }.bind(this, this._element, key)
                });
            }
        }
        return result;
    },
    getMethods: function (){
        var result = [];
        for (var key in this._element){
            var value = this._element[key];
            // Methods
            if ( typeof value === 'function' ){
                result.push({
                    getContainer: function (){
                        return this;
                    }.bind(this),
                    getName: function (key){
                        return key;
                    }.bind(this, key),
                    getReturnType: function (key){
                        if ( typeof this._typerFunction !== 'undefined' ){
                            var path = woopsaUtils.getPath(this) + "/" + key;
                            return this._typerFunction(path, DEFAULT_METHOD_RETURN_TYPE);
                        }else{
                            return DEFAULT_METHOD_RETURN_TYPE; // No way to know return type in JS
                        }
                    }.bind(this, key),
                    getArgumentInfos: function (method, methodName){
                        var parameters = getParameterNames(method);
                        var argumentInfos = [];
                        for ( var i in parameters ){
                            argumentInfos.push({
                                getName: function (name){
                                    return name;
                                }.bind(this, parameters[i]),
                                getType: function (methodName, argumentName){
                                    if ( typeof this._typerFunction !== 'undefined' ){
                                        var path = woopsaUtils.getPath(this) + "/" + methodName + "/" + argumentName;
                                        return this._typerFunction(path, DEFAULT_METHOD_ARGUMENT_TYPE);
                                    }else{
                                        return DEFAULT_METHOD_ARGUMENT_TYPE; // No way to know argument type in JS
                                    }
                                }.bind(this, methodName, parameters[i])
                            });
                        }
                        return argumentInfos;
                    }.bind(this, value, key),
                    invoke: function (method, args){
                        return method.apply(this, args);
                    }.bind(this, value)
                });
            }
        }
        return result;
    },
    getItems: function (){
        var result = [];
        for (var key in this._element){
            var value = this._element[key];
            var type = woopsaUtils.inferWoopsaType(value);
            // Methods
            if ( typeof value !== 'function' && typeof type === 'undefined' ){
                // If the type is undefined, it's a Woopsa Object
                var name = key;
                if ( Array.isArray(this._element) ){
                    name = this._name + "[" + key + "]";
                }
                result.push(new Reflector(this, this._element[key], name, this._typerFunction));
            }
        }
        return result.concat(this._extraItems);

    },
    getName: function (){
        return this._name;
    },
    addItem: function (item){
        var isFound = false;
        for ( var i in this._extraItems ){
            if ( this._extraItems[i].getName() === item.getName() ){
                isFound = true;
                break;
            }
        }
        if ( !isFound && typeof this._element[item.getName()] === 'undefined' ){
            this._extraItems.push(item);
        }else{
            throw new exceptions.WoopsaException("Tried to add an item with duplicate name " + item.getName());
        }
    }
}

exports.Reflector = Reflector;