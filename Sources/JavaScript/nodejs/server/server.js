var utils = require('./utils');
var exceptions = require('./exceptions');
var types = require('./types')
var adapter = require('./adapter');

var http = require('http');
var express = require('express');
var bodyParser = require('body-parser');

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
	var urlencodedParser = bodyParser.urlencoded({extended: false});

	// Create our 4 basic Woopsa routes
	expressApp.get(new RegExp(options.pathPrefix + "meta/(.*)"), this.handleRequest.bind(this, "meta"));
	expressApp.get(new RegExp(options.pathPrefix + "read/(.*)"), this.handleRequest.bind(this, "read"));
	expressApp.post(new RegExp(options.pathPrefix + "write/(.*)"), urlencodedParser, this.handleRequest.bind(this, "write"));
	expressApp.post(new RegExp(options.pathPrefix + "invoke/(.*)"), urlencodedParser, this.handleRequest.bind(this, "invoke"));
};

Server.prototype.handleRequest = function (type, req, res){
	var path = "/" + req.params[0];
	try{
		var element = adapter.getByPath(this.element, path);
		if ( typeof element === 'undefined' ){
			throw new exceptions.WoopsaNotFoundException("Woopsa Element not found");
		}
		var result = {};
		if ( type === "meta" )
			result = this.handleMeta(element);
		else if ( type === "read" )
			result = this.handleRead(element);
		else if ( type === "write" )
			result = this.handleWrite(element, req.body.value);
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
	return adapter.generateMetaDataObject(element);
};

Server.prototype.handleRead = function (element){
	var elementType = utils.inferWoopsaType(element);
	if ( typeof elementType === 'undefined' ){
		throw new exceptions.WoopsaInvalidOperationException("Cannot read non-WoopsaValue %s", elementType);
	}
	return adapter.readValue(element);
};

Server.prototype.handleWrite = function (element, value){
	var elementType = utils.inferWoopsaType(element);
	if ( typeof elementType === 'undefined' ){
		throw new exceptions.WoopsaInvalidOperationException("Cannot write non-WoopsaValue %s", elementType);
	}
	return adapter.write(element, value);
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
testObj.addProperty("Someshit", "Real", function (){return 3.14});
testObj.addProperty("Someothershit", "Text", function (){return "Hello"});
var innerObj = new types.WoopsaObject("SomethingElse");
innerObj.addProperty("Sometoughassshit", "DateTime", false);
testObj.addItem(innerObj);

var server = new Server(testObj, {port: 80});