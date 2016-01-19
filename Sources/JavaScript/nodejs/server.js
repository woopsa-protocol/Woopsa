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
 * Constructor for a Woopsa Server using a specified element and the
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

Server.prototype.handleRequest = function (type, req, res){
    var path = "/" + req.params[0];
    path = woopsaUtils.removeExtraSlashes(path);

    try{
        var element = woopsaUtils.getByPath(this.element, path);
        if ( typeof element === 'undefined' ){
            throw new exceptions.WoopsaNotFoundException("WoopsaElement not found");
        }
        var result = undefined;
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
            this.handleInvoke(element, req.body, function (result, error){
                if ( req.cancelled === true ){
                    return;
                }
                if ( typeof error !== 'undefined' ){
                    // If error is true, then result should be a WoopsaException
                    res.writeHead(500, error.Message);
                    res.end(JSON.stringify(error));
                }else{
                    res.json(result);
                }
            });
        }
        // Necessary for methods
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
            res.writeHead(statusCode, e.message);
            res.end(JSON.stringify(e));
            throw e; // TODO: remove - only for debug
        }
        res.writeHead(statusCode, e.message);
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

Server.prototype.handleInvoke = function (method, arguments, done){
    if ( typeof method.invoke === 'undefined' ){
        throw new exceptions.WoopsaInvalidOperationException("Cannot invoke non-WoopsaMethod " + method.getName());
    }
    return adapter.invokeMethod(method, arguments, done);
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