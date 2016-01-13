var SubscriptionChannel = function SubscriptionChannel(notificationQueueSize){

}

SubscriptionChannel.prototype = {
	registerSubscription: function (propertyLink, monitorInterval, publishInterval){
		return 1;
	},
	unregisterSubscription: function (subscriptionId){
		return true;
	},
	waitNotification: function (lastNotificationId, done){
		setTimeout(function (){
			done({Something:"Lol"});
		}, 2000)
	}
}

exports.SubscriptionChannel = SubscriptionChannel;