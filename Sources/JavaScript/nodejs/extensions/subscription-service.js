var types = require('../types');
var exceptions = require('../exceptions');
var subscriptionChannel = require('./subscription-channel');

/**
 * The maximum ID a Subscription Channel can be attributed.
 * The Subscription Channel ID is random so as to prevent
 * edge cases when servers crash or are rebooted and clients
 * try to get Notifications for subscription channels that
 * don't exist anymore.
 * @type {Number}
 */
var RANGE_SUBSCRIPTION_CHANNEL_ID = 10000;

/**
 * The interval, in minutes, at which to check
 * and remove inactive subscription channels.
 * @type {Number}
 */
var CLEANUP_INTERVAL = 20;

var SubscriptionService = function SubscriptionService(woopsaObject){
    this._createSubscriptionChannel = new types.WoopsaMethod("CreateSubscriptionChannel", "Integer", createSubscriptionChannel.bind(this), [
        {"NotificationQueueSize": "Integer"}
    ]);
    this._createSubscriptionChannel.setContainer(this);

    this._registerSubscription = new types.WoopsaMethod("RegisterSubscription", "Integer", registerSubscription.bind(this), [
        {"SubscriptionChannel": "Integer"},
        {"PropertyLink": "WoopsaLink"},
        {"MonitorInterval": "TimeSpan"},
        {"PublishInterval": "TimeSpan"}
    ]);
    this._registerSubscription.setContainer(this);

    this._unregisterSubscription = new types.WoopsaMethod("UnregisterSubscription", "Logical", unregisterSubscription.bind(this), [
        {"SubscriptionChannel": "Integer"},
        {"SubscriptionId": "Integer"}
    ]);
    this._unregisterSubscription.setContainer(this);

    this._waitNotification = new types.WoopsaMethodAsync("WaitNotification", "JsonData", waitNotification.bind(this), [
        {"SubscriptionChannel": "Integer"},
        {"LastNotificationId": "Integer"}
    ]);
    this._waitNotification.setContainer(this);

    this._woopsaObject = woopsaObject;

    var subscriptionChannels = {};
    var lastSubscriptionChannelId = Math.floor(Math.random() * RANGE_SUBSCRIPTION_CHANNEL_ID);

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
        return channel.registerSubscription(this._woopsaObject, propertyLink, monitorInterval, publishInterval);
    }

    function unregisterSubscription(subscriptionChannel, subscriptionId){
        if ( typeof subscriptionChannels[subscriptionChannel] === 'undefined' ){
            throw new exceptions.WoopsaInvalidSubscriptionChannelException("Tried to call UnregisterSubscription on channel with id=" + subscriptionChannel + " that does not exist.");
        }
        var channel = subscriptionChannels[subscriptionChannel];
        return channel.unregisterSubscription(subscriptionId);
    }

    function waitNotification(subscriptionChannel, lastNotificationId, done){
        if ( typeof subscriptionChannels[subscriptionChannel] === 'undefined' ){
           throw new exceptions.WoopsaInvalidSubscriptionChannelException("Tried to call WaitNotification on channel with id=" + subscriptionChannel + " that does not exist.");
        }
        var channel = subscriptionChannels[subscriptionChannel];
        return channel.waitNotification(lastNotificationId, done);
    }

    function cleanupInactiveSubscriptionChannels(){
        var activeSubscriptionChannels = []
        for ( var i in subscriptionChannels ){
            if ( subscriptionChannels[i].isActive() === false ){
                subscriptionChannels[i].dispose();
                delete subscriptionChannels[i];
            }
        }
    }

    setInterval(cleanupInactiveSubscriptionChannels.bind(this), CLEANUP_INTERVAL * 1000 * 60);
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