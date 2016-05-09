/** 
 * @class A generic exception type for when something went wrong.
 */
exports.WoopsaException = function (message){
    this.Type = "WoopsaException";
    this.Message = message;
    this.Error = true;
}

/** @class Thrown when the client requests something that doesn't exist in the hierarchy. */
exports.WoopsaNotFoundException = function (message){
    this.Type = "WoopsaNotFoundException";
    this.Message = message;
    this.Error = true;
}

/** @class Thrown when the client tries to do something forbidden like writing a Text to an Integer */
exports.WoopsaInvalidOperationException = function (message){
    this.Type = "WoopsaInvalidOperationException";
    this.Message = message;
    this.Error = true;
}

/** @class Thrown when notifications were lost and the client needs to acknowledge */
exports.WoopsaNotificationsLostException = function (message){
    this.Type = "WoopsaNotificationsLostException";
    this.Message = message;
    this.Error = true;
}

/** @class Thrown when the client references an inexisting Subscription Channel */
exports.WoopsaInvalidSubscriptionChannelException = function (message){
    this.Type = "WoopsaInvalidSubscriptionChannelException";
    this.Message = message;
    this.Error = true;
}