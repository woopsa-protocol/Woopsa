(function ($){
	$(document).ready(function (){	
		//var woopsa = new WoopsaClient("/woopsa", jQuery);
		//The more complicated, more customizeable way
		/*
		woopsa.createSubscriptionChannel(200, function (channelId) {
			console.log("Created subscription channel with id " + channelId);
			this.registerSubscription("/Child1/Property1", function (value) {
				console.log("The new value is " + value);
			})
		})
		*/
	
		var subscriptionRegistered = null;
		
		$("#urlForm").submit(function (){
			woopsa = new WoopsaClient($("#serverUrl").val(), jQuery);
			
			//The very easy way of making a subscription
			woopsa.onChange($("#subscribePath").val(), function (value){
				console.log("Received notification, new value = " + value);
				$("#variableValue").html(value);
			},1,1, function (subscription){
				console.log("Created ok");
				console.log(subscription);
				subscriptionRegistered = subscription;
			});
			
			$("input, textarea").each(function (){
				$(this).removeAttr("disabled");
			})
			
			$("#subscribePath").attr("disabled","disabled");
			$("#urlForm input").each(function (){ $(this).attr("disabled","disabled")});
		
			woopsa.onError(function (type, errorThown){
				$(".log").prepend("<b>" + (new Date().toUTCString()) + ": </b>" + type + " - " + errorThown + "<br>");
			})
			
			return false;
		});
		
		$("#readForm").submit(function (){
			woopsa.read($("#readPath").val(), function(value){
				$("#readValue").html(value);
			});
			return false;
		});
		
		$("#writeForm").submit(function (){
			woopsa.write($("#writePath").val(), $("#writeValue").val(), function(value){
				console.log(value);
			});
			return false;
		});		
		
		$("#invokeForm").submit(function (){
			var invArgs = {};
			invArgs[$("#invokeArgumentName").val()] = $("#invokeArgumentValue").val();
			woopsa.invoke($("#invokePath").val(), invArgs, function (value){
				$("#invokeValue").html(JSON.stringify(value, null, 2));
			});
			return false;
		})
		
		$("#metaForm").submit(function (){
			woopsa.meta($("#metaPath").val(), function (value){
				$("#metaValue").html(JSON.stringify(value, null, 2));
			})
			return false;
		});
	});
})(jQuery);