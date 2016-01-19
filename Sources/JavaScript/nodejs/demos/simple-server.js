var woopsa = require('../index');

// Create a test object Weather Station
var weatherStation = {
    Temperature: 24.2,
    IsRaining: false,
    Sensitivity: 0.5,
    Altitude: 430,
    City: "Geneva",
    Time: new Date(),
    GetWeatherAtDate: function (date){
        // Because we are using the reflector, arguments are
        // always passed as strings
        //var date = new Date(date);
        if ( date.getDay() === 1 )
            return "rainy";
        else
            return "sunny";
    },
    Thermostat: {
        SetPoint: 24.0
    }
}

var server = new woopsa.Server(weatherStation, {
    port: 80,
    listenCallback: function (err){
        if ( !err ){
            console.log("Woopsa server listening on http://localhost:%d", server.options.port);
            console.log("Some examples of what you can do directly from your browser:");
            console.log(" * View the object hierarchy of the root object:");
            console.log("   http://localhost:%d%smeta/", server.options.port, server.options.pathPrefix);
            console.log(" * Read the value of a property:");
            console.log("   http://localhost:%d%sread/Temperature",  server.options.port, server.options.pathPrefix);
        }else{
            console.log("Error");
        }
    },
    // The typer function allows us to specify the actual
    // Woopsa type for a WoopsaElement when we can't guess (infer)
    // it in JavaScript. For example, all function parameters
    // and return types are set to Text when using the reflector.
    // By returning DateTime for /GetWeatherAtDate/date, we
    // allow the library to directly send us a JavaScript
    // Date object.
    typer: function (path, inferredType){
        if ( path === "/GetWeatherAtDate/date" )
            return "DateTime";
        else
            return inferredType;
    }
});

server.httpServer.on('error', function (err){
    console.log("Error: Could not start Woopsa Server. Most likely because an application is already listening on port %d.", server.options.port);
    console.log("Known culprits:");
    console.log(" - On Windows 10, IIS is on by default on some configurations.");
    console.log(" - Skype");
    console.log(" - Apache, nginx, etc.");
    console.log("%s", err);
});

// Make the temperature of our WeatherStation fluctuate randomly
setInterval(function (){
    weatherStation.Temperature += Math.random() - 0.5;
}, 20);
