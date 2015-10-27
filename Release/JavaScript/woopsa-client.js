(function (global){	
	var nop = function () {};

	var Subscription = function (channel, path, onChangeCallback, successCallback, monitorInterval, publishInterval) {
		this.channel = channel;
		this.path = path;
		this.onChange = onChangeCallback || nop;		
		
		this.subscriptionId = null;
		
		monitorInterval = monitorInterval || 20;
		publishInterval = publishInterval || 50;
		successCallback = successCallback || nop;
				
		this.unregister = function (successCallback) {
			return channel.client.invoke(
				"/SubscriptionService/UnregisterSubscription",
				{
					SubscriptionChannel: channel.channelId,
					SubscriptionId: this.subscriptionId
				},
				(function (result) {
					successCallback();
				}).bind(this)
			);
		}
		
		this.register = function (callback){
			return channel.client.invoke(
				"/SubscriptionService/RegisterSubscription",
				{
					SubscriptionChannel: channel.channelId,
					PropertyLink: path,
					MonitorInterval: monitorInterval,
					PublishInterval: publishInterval
				},
				(function (result) {
					this.subscriptionId = result;
					successCallback();
					if ( callback ){
						callback(this);
					}
				}).bind(this)
			);
		}
		
		this.register.call(this);
	};

	var SubscriptionChannel = function (client, notificationQueueSize, onCreateCallback) {
		this.client = client;
		this.onCreate = onCreateCallback;
		
		this.channelId = null;
		this.subscriptions = [];
		this.listenStarted = false;
		this.subscriptionsDictionary = {};
		
		var created = false;
		
		this.registerSubscription = function (path, onChangeCallback, successCallback, monitorInterval, publishInterval) {
			var newSubscription = new Subscription(this, path, onChangeCallback, successCallback, monitorInterval, publishInterval);
			this.subscriptions.push(newSubscription);
			if ( !this.listenStarted ) {
				this.listenStarted = true;
				waitNotification.call(this);
			}
			this.subscriptionsDictionary[path] = newSubscription;
			return newSubscription;
		};
		
		this.unregisterSubscription = function (subscription, successCallback) {
			var subscriptionIndex = this.subscriptions.indexOf(subscription);
			if ( subscriptionIndex != -1 ) {
				this.subscriptions[subscriptionIndex].unregister((function (subscriptionIndex, successCallback){
					this.subscriptions.splice(subscriptionIndex, 1);
					successCallback();
				}).bind(this, subscriptionIndex, successCallback))
			}
		}
		
		this.register = function (){
			return client.invoke(
				"/SubscriptionService/CreateSubscriptionChannel", 
				{NotificationQueueSize: notificationQueueSize},
				(function (result){
					this.channelId = result;
					if ( !created )
						this.onCreate.call(this, this.channelId);
					created = true;
				}).bind(this)
			);
		}
		
		this.register.call(this);
		
		var retryFrequency = 10000; //The frequency at which to retry connecting, in milliseconds
		
		var waitNotification = function () {
			return client.invoke(
				"/SubscriptionService/WaitNotification",
				{SubscriptionChannel: this.channelId},
				(function (notifications) {
					for ( var i = 0; i < notifications.length; i++ ) {
						var notification = notifications[i];
						if ( this.subscriptionsDictionary[notification.PropertyLink.Value] ) {
							this.subscriptionsDictionary[notification.PropertyLink.Value].onChange(notification.Value.Value);
						}
					}
					waitNotification.call(this);
				}).bind(this)
			).fail(function (request, status, error){
				if ( request.readyState == 4 ){
					//We have reconnected but the server has forgotten our channel
					this.register().done(function (){
						for ( var i = 0; i < this.subscriptions.length; i++ ) {
							this.subscriptions[i].register()
						}
					}.bind(this))
				}
				setTimeout((function (){
					waitNotification.call(this);
				}).bind(this), retryFrequency);
			}.bind(this));
		}
	};
	
	global.WoopsaClient = function (url, jQuery) {
		var $ = jQuery;
		var subscriptionChannel = null;
		var errorCallbacks = [];
		
		if ( url.lastIndexOf('/') == url.length-1 ) {
			this.url = url;
		} else {
			this.url = url + "/";
		}
		
		this.read = function (path, callback) {
			return $.get(this.url + "read" + path, {}, function (data, textStatus, jqXHR) {
				callback(data.Value, path);
			}).fail(function (request, status, errorThrown){
				raiseError(status, errorThrown);
			});
		};
		
		this.write = function (path, value, callback) {
			return $.post(this.url + "write" + path, {value: value}, function (data, textStatus, jqXHR) {
				callback(true, path);
			})
			.fail(function (request, status, errorThrown){
				raiseError(status, errorThrown);
			});
		};
	
		this.meta = function (path, callback) {
			return $.get(this.url + "meta" + path, {}, function (data, textStatus, jqXHR) {
				callback(data, path);
			})
			.fail(function (request, status, errorThrown){
				raiseError(status, errorThrown);
			});
		};
		
		this.invoke = function (path, arguments, callback, timeout) {
			timeout = timeout || 10000;
			arguments = arguments || {};
			callback = callback || (function (){});
			return $.ajax({
				type: 'POST',
				timeout: timeout,
				url: this.url + "invoke" + path,
				data: arguments
			})
			.done(function (data){
				callback(data.Value);
			})
			.fail(function (request, status, errorThrown){
				raiseError(status, errorThrown);
			});
		};
		
		this.createSubscriptionChannel = function (notificationQueueSize, callback) {
			if ( subscriptionChannel != null ) {
				callback.call(subscriptionChannel, subscriptionChannel.channelId);
				return;
			}
			var newChannel = new SubscriptionChannel(this, notificationQueueSize, function (channelId){
				callback.call(this, channelId);
			})
			subscriptionChannel = newChannel;
			return newChannel;
		};
		
		var subscriptionChannelCreating = false;
		var queue = [];
		this.onChange = function (path, callback, monitorInterval, publishInterval) {
			if ( subscriptionChannelCreating ){
				queue.push({
					path: path,
					callback: callback,
					monitorInterval: monitorInterval,
					publishInterval: publishInterval
				});
				return;
			}
			if ( subscriptionChannel == null && !subscriptionChannelCreating ) {
				subscriptionChannelCreating = true;
				this.createSubscriptionChannel(200, (function (){
					this.onChange(path, callback, monitorInterval, publishInterval);
					subscriptionChannelCreating = false;
					for(var i = queue.length-1; i >= 0; i--){
						var elem = queue[i];
						subscriptionChannel.registerSubscription(
							elem.path,
							elem.callback,
							elem.monitorInterval,
							elem.publishInterval
						);
						queue.splice(i,1);
					}
				}).bind(this))
			}else{
				subscriptionChannel.registerSubscription(path, callback, nop, monitorInterval, publishInterval);
			}
		};
		
		this.onError = function (callback){
			errorCallbacks.push(callback);
		};
		
		function raiseError(type, errorThrown){
			for ( var i = 0; i < errorCallbacks.length; i++ ) {
				errorCallbacks[i](type, errorThrown);
			}
		}
	};
})(window);