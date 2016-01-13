var utils = require('./utils');
var exceptions = require('./exceptions');
var getParameterNames = require('get-parameter-names');

var DEFAULT_METHOD_RETURN_TYPE = 'Text'; 
var DEFAULT_METHOD_ARGUMENT_TYPE = 'Text';

var Reflector = function Reflector(element, name){
    this._extraItems =  [];
    this._element = element;
    if ( typeof name === 'undefined' )
        this._name = element.constructor.name;
    else
        this._name = name;
}

Reflector.prototype = {
    getProperties: function (){
        var result = [];
        for (var key in this._element){
            var value = this._element[key];
            var type = utils.inferWoopsaType(value);
            // Methods
            if ( typeof value !== 'function' && typeof type !== 'undefined' ){
                // Just a regular old WoopsaProperty!
                result.push({
                    getName: function (key){
                        return key;
                    }.bind(this, key),
                    getType: function (element, key){
                        return utils.inferWoopsaType(element[key]);
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
                    getName: function (key){
                        return key;
                    }.bind(this, key),
                    getReturnType: function (){
                        return DEFAULT_METHOD_RETURN_TYPE; // No way to know return type in JS
                    },
                    getArgumentInfos: function (method){
                        var parameters = getParameterNames(method);
                        var argumentInfos = [];
                        for ( var i in parameters ){
                            argumentInfos.push({
                                getName: function (name){
                                    return name;
                                }.bind(this, parameters[i]),
                                getType: function (){
                                    return DEFAULT_METHOD_ARGUMENT_TYPE; // No way to know argument type in JS
                                }
                            });
                        }
                        return argumentInfos;
                    }.bind(this, value),
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
            var type = utils.inferWoopsaType(value);
            // Methods
            if ( typeof value !== 'function' && typeof type === 'undefined' ){
                // If the type is undefined, it's a Woopsa Object
                result.push(new Reflector(this._element[key], key));
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