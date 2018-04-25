var woopsaUtils = require('./woopsa-utils');
var exceptions = require('./exceptions');
var getParameterNames = require('get-parameter-names');
var types = require('./types');

var DEFAULT_METHOD_RETURN_TYPE = 'Text'; 
var DEFAULT_METHOD_ARGUMENT_TYPE = 'Text';

/** 
 * @class  Allows to turn any JavaScript object into a WoopsaObject
 * @param {WoopsaObject} container      An optional WoopsaObject as container
 * @param {Object} element              The actual JavaScript object to reflect
 * @param {string} [name]               The name to give this WoopsaObject. If not
 *                                      present, will use the constructor name
 * @param {Reflector~typer} [typer]     A function that helps determine the type
 *                                      of the Woopsa Element specified by a path.
 */
var Reflector = function Reflector(container, element, name, typerFunction){
    this._container = container;
    this._extraItems =  [];
    this._extraMethods = [];
    this._extraProperties = [];
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

/** @type {Reflector} */
Reflector.prototype = {
    /** Gets the WoopsaObject that contains it, or null if root */
    getContainer: function (){
        return this._container;
    },
    /** Returns a list of WoopsaProperties */
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
                    write: function (element, key, value, callback){
                        element[key] = value;
                        callback(value);
                    }.bind(this, this._element, key),
                    read: function (element, key, callback){
                        callback(element[key]);
                    }.bind(this, this._element, key)
                });
            }
        }
        return result;
    },
    /** Returns a list of WoopsaMethods */
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
                    invoke: function (method, args, callback){
                        callback(method.apply(this, args));
                    }.bind(this, value)
                });
            }
        }
        return result.concat(this._extraMethods);
    },
    /** Returns a list of WoopsaObjects (will be Reflectors) */
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
    /** Gets the Woopsa name for this object. Will be deduced from its constructor */
    getName: function (){
        return this._name;
    },
    /** 
     * Adds an additional WoopsaProperty. Will not modify the underlying object
     * @param {WoopsaProperty} property The property
     */
    addProperty: function (property){
        var isFound = false;
        for ( var i in this._extraProperties ){
            if ( this._extraProperties[i].getName() === property.getName() ){
                isFound = true;
                break;
            }
        }
        if ( !isFound && typeof this._element[property.getName()] === 'undefined' ){
            this._extraProperties.push(property);
            if ( typeof property.setContainer !== 'undefined' )
                property.setContainer(this);
            return property;
        }else{
            throw new exceptions.WoopsaException("Tried to add a property with duplicate name " + name);
        }
    },
    /** 
     * Adds an additional WoopsaMethod. Will not modify the underlying object
     * @param {WoopsaMethod} method The method
     */
    addMethod: function (method){
        var isFound = false;
        for ( var i in this._extraMethods ){
            if ( this._extraMethods[i].getName() === method.getName() ){
                isFound = true;
                break;
            }
        }
        if ( !isFound && typeof this._element[method.getName()] === 'undefined' ){
            this._extraMethods.push(method);
            if ( typeof method.setContainer !== 'undefined' )
                method.setContainer(this);
            return method;
        }else{
            throw new exceptions.WoopsaException("Tried to add a method with duplicate name " + name);
        }
    },
    /** 
     * Adds an additional WoopsaObject. Will not modify the underlying object
     * @param {WoopsaObject} item The item
     */
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

/**
 * A method that allows to manually specify the Woopsa Type
 * of a specific element when the reflector is unable to 
 * determine it.
 * Every time Woopsa needs to know the type of an element,
 * this method will be called with the path as well as the 
 * type that the Reflector tried to guess.
 * You must return a Woopsa type in the form of a string.
 * If you don't know the type, just return the inferredType.
 * @callback Reflector~typer
 * @param {String} path         The Woopsa path of the element
 * @param {String} inferredType The inferred (guessed) type
 *                              of the current element.
 * @return {String}             A Woopsa type for this element
 */