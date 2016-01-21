var woopsaUtils = require('./woopsa-utils');
var exceptions = require('./exceptions');
var types = require('./types')
var adapter = require('./adapter');
var reflector = require('./reflector');
var subscriptionService = require('./extensions/subscription-service');
var multiRequest = require('./extensions/multi-request');

var http = require('http');
var express = require('express');
var bodyParser = require('body-parser');

/**
 * @class  Constructor for a WoopsaServer using a specified element and the
 * passed options.
 * Because Woopsa uses Express 4.0, you can give this constructor an
 * already existing Express server and it will just add its own routes
 * to your app.
 * The available options are:
 *  - {Object} expressApp       (optional) An already existing express app
 *                              onto which the Woopsa server will add its
 *                              routes. 
 *                              If not present, the Woopsa server will create
 *                              an express app itself. 
 *                              Default: null
 *  - {Number} port             The port to run the Woopsa server on, if
 *                              expressApp is not specified. Otherwise this
 *                              is ignored. 
 *                              Default: 80.
 *  - {String} pathPrefix       The path with which all Woopsa paths will 
 *                              start. For example, specifying /woopsa/ will
 *                              make the Woopsa server available on
 *                              /woopsa/meta/
 *                              /woopsa/read/... and so on.
 *                              Default: /woopsa/
 *  - {Function} typer          A function that accepts two parameters:
 *                              path and inferredType. If present, this
 *                              method will be called every time the Woopsa
 *                              object reflector needs to know the type of
 *                              a WoopsaElement, including method arguments.
 *                              This function MUST always return a value. You
 *                              can simply return inferredType if you want to
 *                              let the object reflector take a guess.
 *                              Default: null
 *  - {Function} listenCallback A function to be called when the express app
 *                              is created and the underlying HTTP server is
 *                              listening.
 * @param {Object} element The element to publish through the Woopsa protocol.
 *                         If there is no getItems() function on this object,
 *                         then it means you are trying to publish a regular
 *                         JavaScript object. In that case, the Woopsa server
 *                         will automatically create an Object Reflector, which
 *                         converts any JavaScript object into a WoopsaObject!
 *                         On the other hand, if the getItems() function is 
 *                         available on this object, then the server will consider
 *                         it a WoopsaObject. 
 * @param {Object} options The options (see above)
 */
var Server = function Server(element, options){
    var defaultOptions = {
        port: 80,
        pathPrefix: '/woopsa/',
        expressApp: null,
        typer: null,
        listenCallback: nop
    }
    options = woopsaUtils.defaults(options, defaultOptions);

    // If there is no express app already, create it
    var expressApp = options.expressApp;
    if ( expressApp === null ){
        expressApp = express();
        this.httpServer = expressApp.listen(options.port, options.listenCallback);
    }else{
        // We have no way of knowing the inner node HTTP server
        this.httpServer = null;
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
    new multiRequest.MultiRequestHandler(this.element);

    // Add some pre-processors
    expressApp.use(options.pathPrefix, this.addHeaders.bind(this));
    var urlencodedParser = bodyParser.urlencoded({extended: false});

    // Create our 4 basic Woopsa routes
    expressApp.get(new RegExp(options.pathPrefix + "meta/(.*)"), this.handleRequest.bind(this, "meta"));
    expressApp.get(new RegExp(options.pathPrefix + "read/(.*)"), this.handleRequest.bind(this, "read"));
    expressApp.post(new RegExp(options.pathPrefix + "write/(.*)"), urlencodedParser, this.handleRequest.bind(this, "write"));
    expressApp.post(new RegExp(options.pathPrefix + "invoke/(.*)"), urlencodedParser, this.handleRequest.bind(this, "invoke"));

    this.options = options;
};

function nop(){ }

Server.prototype.handleRequest = function (type, request, response){
    var path = "/" + request.params[0];
    path = woopsaUtils.removeExtraSlashes(path);

    // Detect requests that were cancelled by the client,
    // this happens frequently with subscriptions when the
    // page is refreshed fro example.
    request.on("close", function (){
        request.cancelled = true;
    });

    try{
        var element = woopsaUtils.getByPath(this.element, path);
        if ( typeof element === 'undefined' ){
            throw new exceptions.WoopsaNotFoundException("WoopsaElement not found");
        }
        if ( type === "meta" ){
            var result = this.handleMeta(element);
            response.json(result);
        }else if ( type === "read" ){
            result = this.handleRead(element, respond.bind(this, request, response));
        }else if ( type === "write" ){
            // Case-insensitive for value
            var value = (typeof request.body.Value !== 'undefined')?request.body.Value:request.body.value;
            result = this.handleWrite(element, value, respond.bind(this, request, response));
        }else if ( type === "invoke" ){
            this.handleInvoke(element, request.body, respond.bind(this, request, response));
        }
    }catch (e){
        respondError(response, e);
    }
};

function respondError(response, exception){
    var statusCode;
    if ( exception.Type === "WoopsaNotFoundException" ){
        statusCode = 404; // 404 Not found
    }else if ( exception.Type == "WoopsaInvalidOperationException" ){
        statusCode = 400; // 400 Bad request
    }else{
        statusCode = 500; // 500 Internal server error
    }
    response.writeHead(statusCode, exception.message);
    response.end(JSON.stringify(exception));    
}

function respond(request, response, result, error){
    if ( request.cancelled === true ){
        return;
    }
    if ( typeof error !== 'undefined' ){
        // If error is true, then result should be a WoopsaException
        respondError(response, error);
    }else{
        response.json(result);
    }
}

Server.prototype.handleMeta = function (element){
    if ( typeof element.getItems === 'undefined' ){
        throw new exceptions.WoopsaInvalidOperationException("Cannot get metadata for a non-WoopsaObject " + element.getName());
    }
    return adapter.generateMetaObject(element);
};

Server.prototype.handleRead = function (property, done){
    if ( typeof property.read === 'undefined' ){
        throw new exceptions.WoopsaInvalidOperationException("Cannot read non-WoopsaProperty " + property.getName());
    }
    return adapter.readProperty(property, done);
};

Server.prototype.handleWrite = function (property, value, done){
    if ( typeof property.read === 'undefined' ){
        throw new exceptions.WoopsaInvalidOperationException("Cannot write non-WoopsaProperty " + property.getName());
    }
    return adapter.writeProperty(property, value, done);
};

Server.prototype.handleInvoke = function (method, methodArguments, done){
    if ( typeof method.invoke === 'undefined' ){
        throw new exceptions.WoopsaInvalidOperationException("Cannot invoke non-WoopsaMethod " + method.getName());
    }
    return adapter.invokeMethod(method, methodArguments, done);
};

Server.prototype.addHeaders = function (req, res, next){
    // Woopsa is always in json
    res.setHeader("Content-Type", "application/json");
    res.setHeader("Access-Control-Allow-Headers", "Authorization");
    res.setHeader("Access-Control-Allow-Origin", "*");
    res.setHeader("Access-Control-Allow-Credentials", "true");
    // Max age for caching, in days. Default = 20
    var maxAge = 20;
    res.setHeader("Access-Control-Max-Age", maxAge * 24 * 3600);
    next();
}

exports.Server = Server;