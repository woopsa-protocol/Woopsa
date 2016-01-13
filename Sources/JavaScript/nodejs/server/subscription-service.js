var types = require('./types');
var exceptions = require('./exceptions');
var subscriptionChannel = require('./subscription-channel');

var SubscriptionService = function SubscriptionService(){
    this._createSubscriptionChannel = new types.WoopsaMethod("CreateSubscriptionChannel", "Integer", createSubscriptionChannel, [
        {"NotificationQueueSize": "Integer"}
    ]);

    this._registerSubscription = new types.WoopsaMethod("RegisterSubscription", "Integer", registerSubscription, [
        {"SubscriptionChannel": "Integer"},
        {"PropertyLink": "WoopsaLink"},
        {"MonitorInterval": "TimeSpan"},
        {"PublishInterval": "TimeSpan"}
    ]);

    this._unregisterSubscription = new types.WoopsaMethod("UnregisterSubscription", "Logical", unregisterSubscription, [
        {"SubscriptionChannel": "Integer"},
        {"SubscriptionId": "Integer"}
    ]);

    this._waitNotification = new types.WoopsaMethod("WaitNotification", "JsonData", waitNotification, [
        {"SubscriptionChannel": "Integer"},
        //{"LastNotificationId": "Integer"}
    ]);
    // Makes the server pass us the Response object
    this._waitNotification.invokeAsync = function (arguments, done){
        waitNotification.apply(this, arguments.concat([done]));
    }

    var subscriptionChannels = {};
    var lastSubscriptionChannelId = 0;

    function createSubscriptionChannel(notificationQueueSize){
        var newChannel = new subscriptionChannel.SubscriptionChannel(notificationQueueSize);
        subscriptionChannels[++lastSubscriptionChannelId] = newChannel;
        return lastSubscriptionChannelId;
    }

    function registerSubscription(subscriptionChannel, propertyLink, monitorInterval, publishInterval){
        if ( typeof subscriptionChannels[subscriptionChannel] === 'undefined' ){
            throw new exceptions.WoopsaInvalidSubscriptionChannelException("Tried to call RegisterSubscription on channel with id=" + subscriptionChannel + " that does not exist.");
        }
        var channel = subscriptionChannels[subscriptionChannel];
        return channel.registerSubscription(propertyLink, monitorInterval, publishInterval);
    }

    function unregisterSubscription(subscriptionChannel, subscriptionId){
        if ( typeof subscriptionChannels[subscriptionChannel] === 'undefined' ){
            throw new exceptions.WoopsaInvalidSubscriptionChannelException("Tried to call UnregisterSubscription on channel with id=" + subscriptionChannel + " that does not exist.");
        }
        var channel = subscriptionChannels[subscriptionChannel];
        return channel.unregisterSubscription(subscriptionId);
    }

    function waitNotification(subscriptionChannel, done){
        // if ( typeof subscriptionChannels[subscriptionChannel] === 'undefined' ){
        //    throw new exceptions.WoopsaInvalidSubscriptionChannelException("Tried to call UnregisterSubscription on channel with id=" + subscriptionChannel + " that does not exist.");
        // }
        // var channel = subscriptionChannels[subscriptionChannel];
        setTimeout(function (){
            done({Something:"lol"});
        }, 2000);
        // return channel.waitNotification(1, done);
    }
}

SubscriptionService.prototype = {
    getName: function (){
        return "SubscriptionService";
    },
    getProperties: function (){
        return [];
    },
    getMethods: function (){
        return [
            this._createSubscriptionChannel,
            this._registerSubscription,
            this._unregisterSubscription,
            this._waitNotification,
        ];
    },
    getItems: function (){
        return [];
    }
}

exports.SubscriptionService = SubscriptionService;