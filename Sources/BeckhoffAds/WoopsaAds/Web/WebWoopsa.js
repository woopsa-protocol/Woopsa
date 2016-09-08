var nop = function () { };
var client;
var newSubscription;
jQuery(document).ready(function () {
    var i;
    var path = "";
    var url = window.location.href;
    var urlSplit = url.split('/');
    for (i = 0 ; i < urlSplit.length - 2; i++){
        path += urlSplit[i] + "/";
    }
    path += "woopsa";
    client = new WoopsaClient(path, jQuery);
    newSubscription = null;
})

function ReadValueClick() {
    var valuePath = document.getElementById("readValuePath").value;
    client.read(valuePath, function (value) {
        document.getElementById("ReadedValue").textContent = value;
    });

}

function WriteValueClick() {
    var valuePath = document.getElementById("writeValuePath").value;
    var value = document.getElementById("writeValue").value;
    client.write(valuePath, value, function (response) {
    });
}

function SubscribeVariableClick() {
    var valuePath = document.getElementById("subscribeVariablePath").value;
    if (newSubscription != null)
        newSubscription.unregister();
    client.onChange(valuePath, function (value) {
        document.getElementById("SubscribeValue").textContent = value;
    }, 0.05, 0.05, function (subscription) {
        newSubscription = subscription;
    });
}