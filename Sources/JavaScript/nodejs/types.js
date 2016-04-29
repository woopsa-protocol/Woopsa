var exceptions = require('./exceptions')
var util = require('util');

/**
 * @class The WoopsaObject constructor
 * @param {String} name The Woopsa name of this object.
 */
var WoopsaObject = function WoopsaObject(name){
    this._properties = [];
    this._methods = [];
    this._items = [];
    this._name = name;
    this._container = null;
}

/**
 * @type {WoopsaObject}
 */
WoopsaObject.prototype = {
    /**
     * Gets this WoopsaObject's container.
     * Every WoopsaObject belongs to another one,
     * except for the root node which has a null
     * container.
     * @return {WoopsaObject}   The Container. Null if 
     *                          it's to be considered root.
     */
    getContainer: function (){
        return this._container;
    },
    /**
     * Sets this WoopsaObject's container.
     * Setting it to null makes this WoopsaObject behave
     * like a root object.
     * @param {WoopsaObject} value The container
     */
    setContainer: function (value){
        this._container = value;
    },
    /**
     * Returns a list of WoopsaProperties belonging to
     * this WoopsaObject.
     * @return {Array.<WoopsaProperty>} The properties
     */
    getProperties: function (){
        return this._properties;
    },
    /**
     * Returns a list of WoopsaMethods belonging to 
     * this WoopsaObject.
     * @return {Array.<WoopsaMethod>} The methods
     */
    getMethods: function (){
        return this._methods;
    },
    /**
     * Returns a list of WoopsaObjects belonging to
     * this WoopsaObject
     * @return {Array.<WoopsaObject>} The items
     */
    getItems: function (){
        return this._items;
    },
    /**
     * Returns the Woopsa name of this WoopsaObject
     * @return {String} The name
     */
    getName: function (){
        return this._name;
    },
    /**
     * Adds a WoopsaProperty to this WoopsaObject.
     * This method will automatically set the container
     * of the passed WoopsaProperty if it has a setContainer
     * method.
     * @param {WoopsaProperty} property The WoopsaProperty to add
     */
    addProperty: function (property){
        if ( !hasElement(this._properties, property.getName()) ){
            this._properties.push(property);
            if ( typeof property.setContainer !== 'undefined' )
                property.setContainer(this);
            return property;
        }else{
            throw new exceptions.WoopsaException("Tried to add a property with duplicate name " + property.getName());
        }
    },
    /**
     * Adds a WoopsaMethod to this WoopsaObject.
     * This method will automatically set the container
     * of the passed WoopsaMethod if it has a setContainer
     * method.
     * @param {WoopsaMethod} method The WoopsaMethod to add
     */
    addMethod: function (method){
        if ( !hasElement(this._methods, method.getName()) ){
            this._methods.push(method);
            if ( typeof method.setContainer !== 'undefined' )
                method.setContainer(this);
            return method;
        }else{
            throw new exceptions.WoopsaException("Tried to add a method with duplicate name " + property.getName());
        }
    },
    /**
     * Adds a WoopsaObject to this WoopsaObject.
     * This method will automatically set the container
     * of the passed WoopsaObject if it has a setContainer
     * method.
     * @param {WoopsaObject} item The WoopsaObject to add
     */
    addItem: function (item){
        if ( !hasElement(this._items, item.getName()) ){
            this._items.push(item);
            if ( typeof item.setContainer !== 'undefined' )
                item.setContainer(this);
            return item;
        }else{
            throw new exceptions.WoopsaException("Tried to add an item with duplicate name " + item.getName());
        }
    }
}

function hasElement(list, elementname){
    for ( var i in list )
        if ( list[i].getName() === elementname )
            return true;
    return false;
}

/**
 * @class The Constructor for a WoopsaProperty.
 * Every property in node-woopsa is actually asynchronous
 * on the inside. However, to make programming easier,
 * this class will create wrapper read and write methods
 * which will call the the read and write methods passed
 * to this constructor and then call the callback with the
 * result.
 * @param {String} name             The Woopsa name of this property
 * @param {String} type             The Woopsa type of this property
 * @param {Function} read           A function that returns the value
 *                                  of the property.
 * @param {Function} write          A function that writes the passed 
 *                                  value to the property.
 */
var WoopsaProperty = function WoopsaProperty(name, type, read, write){
    this._name = name;
    this._type = type;
    this._syncRead = read;
    if ( typeof write !== 'undefined' ){
        this._syncWrite = write;
        this.write = (function (value, callback){
            this._syncWrite(value)
            callback(value);
        }).bind(this);
    }
    this._container = null;
}

/**
 * @type {WoopsaProperty}
 */
WoopsaProperty.prototype = {
    /**
     * Gets this WoopsaProperty's container.
     * Every WoopsaProperty belongs to another WoopsaObject.
     * @return {WoopsaObject}   The Container. Never null since
     *                          a property can not be orphan.
     */
    getContainer: function (){
        return this._container;
    },
    /**
     * Sets this WoopsaProperty's container.
     * @param {WoopsaObject} value The container
     */
    setContainer: function (value){
        this._container = value;
    },
    /**
     * Returns the Woopsa name of this WoopsaProperty
     * @return {String} The name
     */
    getName: function (){
        return this._name;
    },
    /**
     * Returns the Woopsa type of this property as a string.
     * @return {String} One of the Woopsa types
     */
    getType: function (){
        return this._type;
    },
    /**
     * [read description]
     * @param  {actionCallback} callback The method to call when
     *                                   the read is finished.
     */
    read: function (callback){
        callback(this._syncRead());
    }
}

/**
 * @class An asynchronous property.
 * Every property in node-woopsa is actually asynchronous
 * on the inside. However, to make programming easier,
 * the WoopsaProperty hides this complexity. When using
 * the WoopsaPropertyAsync constructor, you need to pass
 * it a {@link WoopsaPropertyAsync~readCallback} and a 
 * {@link WoopsaPropertyAsync~writeCallback} instead of
 * simple functions that return values.
 * @extends {WoopsaProperty}
 * @param {String} name                             The Woopsa name of this property
 * @param {String} type                             The Woopsa type of this property
 * @param {WoopsaPropertyAsync~readCallback} read   A function that reads the value
 *                                                  and calls the callback when done.
 * @param {WoopsaPropertyAsync~writeCallback} write A function that writes the value
 *                                                  and calls the callback when done.
 */
var WoopsaPropertyAsync = function WoopsaPropertyAsync(name, type, read, write){
    WoopsaPropertyAsync.super_.call(this, name, type, read, write);
    this.read = read;
    delete this._syncRead;
    if ( typeof write !== 'undefined' ){
        this.write = write;
        delete this._syncWrite;
    }
}

util.inherits(WoopsaPropertyAsync, WoopsaProperty);

/**
 * @class  Constructor for a WoopsaMethod.
 * Every method in node-woopsa is actually asynchronous
 * on the inside. However, to make programming easier,
 * this class will create a wrapper invoke method which
 * will call the callback function with the return value
 * of the function passed in argument.
 * This means that you do not have to worry about calling
 * the callback, and you can just return the result of 
 * your method.
 * @param {String} name          The Woopsa name of the method
 * @param {String} returnType    The Woopsa type that this method returns
 * @param {Function} method      The actual JavaScript method that will be
 *                               called when this method is invoked through
 *                               Woopsa. 
 * @param {Object} argumentInfos An array of objects of key-value pair to
 *                               specify the name and type of each argument
 *                               this method accepts.
 */
var WoopsaMethod = function WoopsaMethod(name, returnType, method, argumentInfos){
    this._name = name;
    this._returnType = returnType;
    this._argumentInfos = [];
    this._method = method;
    if ( typeof argumentInfos !== 'undefined' ){
        for ( var i in argumentInfos ){
            var argInfo = argumentInfos[i];
            for ( var k in argInfo ){
                var newArgumentInfo = new WoopsaMethodArgumentInfo(k, argInfo[k]);
                this.addMethodArgumentInfo(newArgumentInfo);
                break;
            }
        }
    }
    this._container = null;
}

WoopsaMethod.prototype = {
    /**
     * Gets this WoopsaMethod's container.
     * Every WoopsaMethod belongs to another WoopsaObject.
     * @return {WoopsaObject}   The Container. Never null since
     *                          a method can not be orphan.
     */
    getContainer: function (){
        return this._container;
    },
    /**
     * Sets this WoopsaMethod's container.
     * @param {WoopsaObject} value The container
     */
    setContainer: function (value){
        this._container = value;
    },
    /**
     * Returns a list of WoopsaMethodArgumentInfos
     * for this method.
     * @return {Array.<WoopsaMethodArgumentInfo>} The argument infos.
     */
    getArgumentInfos: function (){
        return this._argumentInfos;
    },
    /**
     * Returns the Woopsa name of this WoopsaMethod
     * @return {String} The name
     */
    getName: function (){
        return this._name;
    },
    /**
     * Returns the Woopsa type of this method's return value
     * as a string.
     * @return {String} One of the Woopsa types
     */
    getReturnType: function (){
        return this._returnType;
    },
    /**
     * Adds a WoopsaMethodArgumentInfo to this WoopsaObject.
     * This method will automatically set the container
     * of the passed WoopsaObject if it has a setContainer
     * method.
     * @param {WoopsaMethodArgumentInfo} argumentInfo The WoopsaMethodArgumentInfo
     *                                                to add
     */
    addMethodArgumentInfo: function (argumentInfo){
        if ( !hasElement(this._argumentInfos, argumentInfo.getName()) ){
            this._argumentInfos.push(argumentInfo);
            return argumentInfo;
        }else{
            throw new exceptions.WoopsaException("Tried to add a method argument with duplicate name " + argumentInfo.getName() + " in method " + this._name);
        }
    },
    /**
     * Calls this WoopsaMethod's inner method
     * @param  {Array}   args               A list of arguments that will be
     *                                      passed to this method
     * @param  {actionCallback} callback    The function to call when the return
     *                                      value is known.
     */
    invoke: function (args, callback){
        callback(this._method.apply(this, args));
    }
}

/**
 * @class  Constructor for an explicit asynchronous method.
 * Every method in node-woopsa is asynchronous on the
 * inside. However, to make it easier to program, the
 * {@link WoopsaMethod} constructor wraps this complexity.
 * When using the WoopsaMethodAsync constructor, you need
 * to pass it a method that accepts a "done" callback. The
 * rest is the same as {@link WoopsaMethod}.
 * @param {String} name          The Woopsa name of the method
 * @param {String} returnType    The Woopsa type that this method returns
 * @param {Function} method      The actual JavaScript method that will be
 *                               called when this method is invoked through
 *                               Woopsa. This method's last argument must be
 *                               a {@link actionCallback} callback
 *                               function.
 * @param {Object} argumentInfos An array of objects of key-value pair to
 *                               specify the name and type of each argument
 *                               this method accepts.
 */
var WoopsaMethodAsync = function WoopsaMethodAsync(name, returnType, method, argumentInfos){
    WoopsaMethodAsync.super_.call(this, name, returnType, method, argumentInfos);
    this.invoke = function (methodArguments, callback){
        method.apply(this, methodArguments.concat([callback]));
    }
}

util.inherits(WoopsaMethodAsync, WoopsaMethod);

/**
 * @class  Methods can have 0 or more arguments. This
 * allows you to specify the arguments and their types
 * for a method.
 * @param {String} name The name of this argument
 * @param {String} type The Woopsa type of this argument
 */
var WoopsaMethodArgumentInfo = function WoopsaMethodArgumentInfo(name, type){
    this._name = name;
    this._type = type;
}

WoopsaMethodArgumentInfo.prototype = {
    /**
     * Returns the Woopsa name of this WoopsaMethodArgumentInfo
     * @return {String} The name
     */
    getName: function (){
        return this._name;
    },
    /**
     * Returns the Woopsa type of this argument as a string.
     * @return {String} One of the Woopsa types
     */
    getType: function (){
        return this._type;
    }
}

exports.WoopsaObject = WoopsaObject;
exports.WoopsaProperty = WoopsaProperty;
exports.WoopsaPropertyAsync = WoopsaPropertyAsync;
exports.WoopsaMethod = WoopsaMethod;
exports.WoopsaMethodAsync = WoopsaMethodAsync;
exports.WoopsaMethodArgumentInfo = WoopsaMethodArgumentInfo;

/**
 * This callback is used on read, write and invoke methods
 * for WoopsaProperties or WoopsaMethods. Everything in the
 * node implementation of Woopsa is asynchronous, and as such
 * this callback is used everywhere.
 * @callback actionCallback
 * @param {*} value                 The value returned by the action. For reads,
 *                                  this is the read value. For write, this is
 *                                  the value that was written. For invoke, it's
 *                                  the return value of the function.
 * @param {WoopsaException} error   A WoopsaException that must be passed in case
 *                                  something went wrong. In the case an error is
 *                                  present, value will be ignored and as such, it
 *                                  should probably be null.
 */


/**
 * This callback is called when a property is read.
 * @callback WoopsaPropertyAsync~readCallback
 * @param {actionCallback} callback The callback to call with the
 *                                  read value.
 */

/**
 * This callback is called when a property is written to.
 * @callback WoopsaPropertyAsync~writeCallback
 * @param {*} value                 The value to write to the property.
 * @param {actionCallback} callback The callback to call with the
 *                                  write result.
 */