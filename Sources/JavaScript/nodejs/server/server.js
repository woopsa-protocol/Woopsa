var woopsaUtils = require('./woopsa-utils');
var exceptions = require('./exceptions');
var types = require('./types')
var adapter = require('./adapter');
var reflector = require('./reflector');
var subscriptionService = require('./extensions/subscription-service');

var http = require('http');
var express = require('express');
var bodyParser = require('body-parser');

var Server = function Server(element, options){
    var defaultOptions = {
        port: 80,
        pathPrefix: '/woopsa/',
        expressApp: null,
        typer: null,
    }
    options = woopsaUtils.defaults(options, defaultOptions);

    // If there is no express app already, create it
    var expressApp = options.expressApp;
    if ( expressApp === null ){
        expressApp = express();
        expressApp.listen(options.port, function (){
            console.log("Woopsa server running on port %d", options.port);
        })
    }

    // Is this already a WoopsaObject?
    if ( typeof element.getItems !== 'undefined' )
        this.element = element;
    else{
        if ( options.typer !== null )
            this.element = new reflector.Reflector(null, element, null, options.typer);
        else
            this.element = new reflector.Reflector(null, element);
    }

    this.element.addItem(new subscriptionService.SubscriptionService(this.element));

    // Add some pre-processors
    expressApp.use(options.pathPrefix, this.addHeaders.bind(this));
    var urlencodedParser = bodyParser.urlencoded({extended: false});

    // Create our 4 basic Woopsa routes
    expressApp.get(new RegExp(options.pathPrefix + "meta/(.*)"), this.handleRequest.bind(this, "meta"));
    expressApp.get(new RegExp(options.pathPrefix + "read/(.*)"), this.handleRequest.bind(this, "read"));
    expressApp.post(new RegExp(options.pathPrefix + "write/(.*)"), urlencodedParser, this.handleRequest.bind(this, "write"));
    expressApp.post(new RegExp(options.pathPrefix + "invoke/(.*)"), urlencodedParser, this.handleRequest.bind(this, "invoke"));
};

var i = 0;

Server.prototype.handleRequest = function (type, req, res){
    var path = "/" + req.params[0];
    path = woopsaUtils.removeExtraSlashes(path);

    try{
        var element = woopsaUtils.getByPath(this.element, path);
        if ( typeof element === 'undefined' ){
            throw new exceptions.WoopsaNotFoundException("WoopsaElement not found");
        }
        var result = {};
        if ( type === "meta" ){
            result = this.handleMeta(element);
        }else if ( type === "read" ){
            result = this.handleRead(element);
        }else if ( type === "write" ){
            result = this.handleWrite(element, (typeof req.body.Value !== 'undefined')?req.body.Value:req.body.value);
        }else if ( type === "invoke" ){
            req.on("close", function (){
                req.cancelled = true;
            });
            result = this.handleInvoke(res, element, req.body, function (result, error){
                if ( req.cancelled === true ){
                    return;
                }
                if ( error === true ){
                    // If error is true, then result should be a WoopsaException
                    res.writeHead(500, result.Message, {'Content-Type': 'application/json'});
                    res.end(JSON.stringify(result));
                }else{
                    res.json(result);
                }
            });
        }
        // Necessary for asynchronous methods
        if ( typeof result !== 'undefined' )
            res.json(result);
    }catch (e){
        var statusCode;
        if ( e.Type === "WoopsaNotFoundException" ){
            statusCode = 404; // 404 Not found
        }else if ( e.Type == "WoopsaInvalidOperationException" ){
            statusCode = 400; // 400 Bad request
        }else{
            statusCode = 500; // 500 Internal server error
            res.writeHead(statusCode, e.message, {'Content-Type': 'application/json'});
            res.end(JSON.stringify(e));
            throw e; // TODO: remove - only for debug
        }
        res.writeHead(statusCode, e.message, {'Content-Type': 'application/json'});
        res.end(JSON.stringify(e));
    }
};

Server.prototype.handleMeta = function (element){
    if ( typeof element.getItems === 'undefined' ){
        throw new exceptions.WoopsaInvalidOperationException("Cannot get metadata for a non-WoopsaObject " + element.getName());
    }
    return adapter.generateMetaObject(element);
};

Server.prototype.handleRead = function (property){
    if ( typeof property.read === 'undefined' ){
        throw new exceptions.WoopsaInvalidOperationException("Cannot read non-WoopsaProperty " + property.getName());
    }
    return adapter.readProperty(property);
};

Server.prototype.handleWrite = function (property, value){
    if ( typeof property.read === 'undefined' ){
        throw new exceptions.WoopsaInvalidOperationException("Cannot write non-WoopsaProperty " + property.getName());
    }
    return adapter.writeProperty(property, value);
};

Server.prototype.handleInvoke = function (response, method, arguments, done){
    if ( typeof method.invoke === 'undefined' ){
        throw new exceptions.WoopsaInvalidOperationException("Cannot invoke non-WoopsaMethod " + method.getName());
    }
    return adapter.invokeMethod(response, method, arguments, done);
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
    EchoString: function (text){
        return text;
    },
    Thermostat: {
        SetPoint: 24.0,
        Embedded: {
            SomeString: "Hello?"
        }
    },
    Stuffs: [
        {
            Something: "Hehe",
            OtherThing: 2
        },
        {
            SomethingElse: "Hoho",
            OtherThing: 2
        }
    ]
}

var testObj = new types.WoopsaObject("Something");
testObj.addProperty("SomeReal", "Real", function (){return 3.14});
testObj.addProperty("SomeText", "Text", function (){return "Hello"});
var method = testObj.addMethod("SomeMethod", "Text", function (someArgument){
    return "Hey this is me and SomeArgument = " + someArgument;
}, [
    {SomeArgument: "Text"}
]);
var innerObj = new types.WoopsaObject("SomethingElse");
innerObj.addProperty("SomeDate", "DateTime", function (){return new Date()});
testObj.addItem(innerObj);
var server = new Server(testObj, {port: 80});

setInterval(function (){
    weatherStation.Temperature += Math.random() - 0.5;
}, 20);
