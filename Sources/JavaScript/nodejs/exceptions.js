exports.WoopsaException = function (message){
    this.Type = "WoopsaException";
    this.Message = message;
    this.Error = true;
}

exports.WoopsaNotFoundException = function (message){
    this.Type = "WoopsaNotFoundException";
    this.Message = message;
    this.Error = true;
}

exports.WoopsaInvalidOperationException = function (message){
    this.Type = "WoopsaInvalidOperationException";
    this.Message = message;
    this.Error = true;
}

exports.WoopsaNotificationsLostException = function (message){
    this.Type = "WoopsaNotificationsLostException";
    this.Message = message;
    this.Error = true;
}

exports.WoopsaInvalidSubscriptionChannelException = function (message){
    this.Type = "WoopsaInvalidSubscriptionChannelException";
    this.Message = message;
    this.Error = true;
}