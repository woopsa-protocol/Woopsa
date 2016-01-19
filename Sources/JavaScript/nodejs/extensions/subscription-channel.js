var woopsaUtils = require('../woopsa-utils');
var exceptions = require('../exceptions')

/**
 * Default timeout for the WaitNotification method in 
 * milliseconds (normally it is 5000)
 * @type {Number}
 */
var DEFAULT_WAIT_NOTIFICATION_TIMEOUT = 5000;

/**
 * The amount of time, in minutes, before a subscription
 * channel is considered dead, and we delete it.
 * @type {Number}
 */
var INACTIVE_TIME = 1;

/**
 * The maximum value for a Notification's ID. After that,
 * the ID will be 1 again.
 * @type {Number}
 */
var MAX_NOTIFICATION_ID = 1000000000;

var SubscriptionChannel = function SubscriptionChannel(notificationsQueueSize){
    this._subscriptions = {};
    this._notifications = [];
    this._notificationsQueueSize = notificationsQueueSize;
    this._lastSubscriptionId = 1;
    this._lastNotificationId = 1;
    this._notificationsLost = false;
    this._lastActivity = new Date();
}

SubscriptionChannel.prototype = {
    isActive: function (){
        return ((new Date().getTime()) - this._lastActivity.getTime()) < INACTIVE_TIME * 1000 * 60;
    },
    registerSubscription: function (woopsaObject, propertyLink, monitorInterval, publishInterval){
        var newSubscription = new Subscription(
            this._lastSubscriptionId, 
            woopsaObject, 
            propertyLink, 
            monitorInterval, 
            publishInterval, 
            this.onPublish.bind(this)
        );
        this._subscriptions[this._lastSubscriptionId] = newSubscription;
        return this._lastSubscriptionId++;
    },
    unregisterSubscription: function (subscriptionId){
        if ( typeof this._subscriptions[subscriptionId] !== 'undefined' ){
            this._subscriptions[subscriptionId].dispose();
            delete this._subscriptions[subscriptionId];
            return true;
        }else{
            return false;
        }
    },
    waitNotification: function (lastNotificationId, done){
        this._lastActivity = new Date();
        // We create a new callback that is bound with the
        // last notification Id called by the client
        this._doneCallback = function (lastNotificationId){
            done(this.getNotifications(lastNotificationId));
            clearTimeout(this._doneTimeoutId);
            delete this._doneCallback;
        }.bind(this, lastNotificationId);

        // We set a timeout to call our done callback
        // after at most 5 seconds. If notifications arrived
        // in the meantime, we will clear this timeout
        // and call the newly created done callback
        this._doneTimeoutId = setTimeout(function (){
            done(this.getNotifications(lastNotificationId));
            delete this._doneCallback;
            delete this._doneTimeoutId;
        }.bind(this), DEFAULT_WAIT_NOTIFICATION_TIMEOUT);
    },
    onPublish: function (notifications){
        for ( var i in notifications ){
            notifications[i].Id = this._lastNotificationId++;
            if ( this._lastNotificationId > MAX_NOTIFICATION_ID )
                this._lastNotificationId = 1;
            this._notifications.push(notifications[i]);
            if ( this._notifications.length >= this._notificationsQueueSize ){
                // If the queue is full, raise the notificationsLost flag
                // and remove the oldest notification in the queue.
                // The WaitNotification method will then throw an exception
                // until the client has acknowledged the loss of notifications
                this._notificationsLost = true;
                this._notifications.splice(0, 1);
            }
        }
        if ( typeof this._doneCallback !== 'undefined' ){
            this._doneCallback();
        }
    },
    getNotifications: function (lastNotificationId){
        if ( this._notificationsLost === true && lastNotificationId !== 0 ){
            return new exceptions.WoopsaNotificationsLostException("Notifications have been lost because the queue was full. Acknowledge the error by calling WaitNotification with LastNotificationId = 0");
        }else{
            this._notificationsLost = false;
        }
        if ( this._notifications.length === 0 ){
            return {};
        }
        var returnNotifications = [];
        for ( var i = 0; i < this._notifications.length; i++ ){
            if ( this._notifications[i].Id > lastNotificationId ){
                returnNotifications.push(this._notifications[i]);
            }
        }
        this._notifications = returnNotifications;
        return returnNotifications;
    },
    dispose: function (){
        for ( var i in this._subscriptions ){
            this._subscriptions[i].dispose();
            delete this._subscriptions[i];
        }
        delete this._doneCallback;
        clearTimeout(this._doneTimeoutId);
        delete this._doneTimeoutId;
    }
}

var Subscription = function Subscription(id, woopsaObject, propertyLink, monitorInterval, publishInterval, onPublishCallback){
    var link = woopsaUtils.decodeWoopsaLink(propertyLink);
    if ( link.server !== null ){
        throw new exceptions.WoopsaException("Creating subscriptions on other servers is not supported.");
    }
    this._property = woopsaUtils.getByPath(woopsaObject, link.path);
    if ( typeof this._property === 'undefined' ){
        throw new exceptions.WoopsaNotFoundException("WoopsaElement not found for path " + link.path);
    }

    this._monitorIntervalId = setInterval(monitor.bind(this), monitorInterval*1000);
    this._publishIntervalId = setInterval(publish.bind(this), publishInterval*1000);

    this._notifications = [];
    this._id = id;
    this._onPublishCallback = onPublishCallback;

    var oldValue = undefined;

    function monitor(){
        var currentValue = this._property.read();
        if ( currentValue != oldValue ){
            oldValue = currentValue;
            this._notifications.push({
                Value: {
                    Value: currentValue,
                    Type: this._property.getType(),
                    TimeStamp: new Date()
                },
                SubscriptionId: this._id
            })
        }
    }

    function publish(){
        if ( this._notifications.length !== 0 ){
            var notificationsList = [];
            for ( var i in this._notifications ){
                notificationsList.push(this._notifications[i]);
            }
            this._notifications = [];
            this._onPublishCallback(notificationsList);
        }
    }
}

Subscription.prototype = {
    dispose: function (){
        clearInterval(this._monitorIntervalId);
        clearInterval(this._publishIntervalId);
    }
}

exports.SubscriptionChannel = SubscriptionChannel;
