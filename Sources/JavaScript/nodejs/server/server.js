var utils = require('./utils');
var exceptions = require('./exceptions');
var reflector = require('./reflector');
var types = require('./types')

var http = require('http');
var express = require('express');

var Server = function Server(element, options){
	var defaultOptions = {
		port: 80,
		pathPrefix: '/woopsa/',
		expressApp: null
	}
	options = utils.defaults(options, defaultOptions);

	// If there is no express app already, create it
	var expressApp = options.expressApp;
	if ( expressApp === null ){
		expressApp = express();
		expressApp.listen(options.port, function (){
			console.log("Woopsa server running on port %d", options.port);
		})
	}

	this.element = element;

	// Add some pre-processors
	expressApp.use(options.pathPrefix, this.addHeaders.bind(this));

	// Create our 4 basic Woopsa routes
	expressApp.get(new RegExp(options.pathPrefix + "meta/(.*)"), this.handleRequest.bind(this, "meta"));
	expressApp.get(new RegExp(options.pathPrefix + "read/(.*)"), this.handleRequest.bind(this, "read"));
	expressApp.post(new RegExp(options.pathPrefix + "write/(.*)"), this.handleRequest.bind(this, "write"));
	expressApp.post(new RegExp(options.pathPrefix + "invoke/(.*)"), this.handleRequest.bind(this, "invoke"));
};

Server.prototype.handleRequest = function (type, req, res){
	var path = "/" + req.params[0];
	try{
		var element = getByPath(this.element, path);
		if ( typeof element === 'undefined' ){
			throw new exceptions.WoopsaNotFoundException("Woopsa Element not found");
		}
		var result = {};
		if ( type === "meta" )
			result = this.handleMeta(element);
		else if ( type === "read" )
			result = this.handleRead(element);
		else if ( type === "write" )
			result = this.handleWrite(element, "");
		else if ( type === "invoke" )
			result = this.handleInvoke(element, {});
		res.json(result);
	}catch (e){
		var statusCode;
		if ( e.Type === "WoopsaNotFoundException" ){
			statusCode = 404; // 404 Not found
		}else if ( e.Type == "WoopsaInvalidOperationException" ){
			statusCode = 400; // 400 Bad request
		}else{
			statusCode = 500; // 500 Internal server error
		}
		res.writeHead(statusCode, e.message, {'Content-Type': 'application/json'});
		res.end(JSON.stringify(e));
	}
};

Server.prototype.handleMeta = function (element){
	var elementType = utils.inferWoopsaType(element);
	if ( typeof elementType !== 'undefined' ){
		throw new exceptions.WoopsaInvalidOperationException("Cannot get metadata for a WoopsaElement of type %s", elementType);
	}
	if ( element.constructor.name !== 'WoopsaObject' )
		return reflector.generateMetaDataFromObject(element);
	else
		return element;
};

Server.prototype.handleRead = function (element){
	var path = "/" + req.params[0];
};

Server.prototype.handleWrite = function (element, value){
	var path = "/" + req.params[0];
};

Server.prototype.handleInvoke = function (element, arguments){
	var path = "/" + req.params[0];
};

Server.prototype.addHeaders = function (req, res, next){
	res.setHeader("Access-Control-Allow-Headers", "Authorization");
	res.setHeader("Access-Control-Allow-Origin", "*");
	res.setHeader("Access-Control-Allow-Credentials", "true");
	// Max age for caching, in days. Default = 20
	var maxAge = 20;
	res.setHeader("Access-Control-Max-Age", maxAge * 24 * 3600);
	next();
}

// Gets an object or a property of an element
// from a path
var getByPath = function (element, path){
	var pathParts = path.split("/");
	if ( pathParts[0] == "" )
		pathParts.splice(0, 1);
	if ( pathParts[pathParts.length-1] == "" )
		pathParts.splice(pathParts.length-1, 1);

	var returnElement = element;

	if ( pathParts.length == 0 ){
		return element;
	}else{
		function getByPathArray(element, pathArray){
			var key = pathArray[0];
			if ( typeof element[key] !== 'undefined' ){
				if ( pathArray.length === 1 ){
					return element[key];
				}else{
					return getByPathArray(element[key], pathArray.slice(1));
				}
			}else{
				return undefined;
			}
		}

		return getByPathArray(returnElement, pathParts);
	}
}

exports.Server = Server;

////////////////////////////////////////////////////

// this is just to test
var weatherStation = {
  Temperature: 24.2,
  IsRaining: false,
  Sensitivity: 0.5,
  Altitude: 430,
  City: "Geneva",
  Time: new Date(),
  GetWeatherAtDate: function (date){
    return "sunny";
  },
  Thermostat: {
  	SetPoint: 24.0,
  	Embedded: {
  		SomeString: "Hello?"
  	}
  }
}

var testObj = new types.WoopsaObject("Something");
testObj.addProperty("Someshit", "Real", false);
testObj.addProperty("Someothershit", "Text", false);
var innerObj = new types.WoopsaObject("SomethingElse");
innerObj.addProperty("Sometoughassshit", "DateTime", false);
testObj.addItem(innerObj);

var server = new Server(testObj, {port: 80});
console.log("AAA " + server.constructor.name);