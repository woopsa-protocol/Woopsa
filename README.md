# Woopsa
Woopsa is a protocol that's simple, lightweight, free, open-source, web and object-oriented, publish-subscribe, real-time capable and Industry 4.0 ready. It contributes to the revolution of the Internet of Things.

Woopsa allows you to share the complete object model of your application in a way that's similar to OPC-UA. It's based on rock-solid foundations such as HTTP and JSON, which makes it work on the web out-of-the-box. Our mission is to get Woopsa on as many platforms as possible. Today, C# and JavaScript implementations exist, but there are much more to come!

**Example JavaScript Client**

```javascript
var woopsa = new WoopsaClient("http://demo.woopsa.org/woopsa", jQuery);
woopsa.read("/Temperature", function(result){
//result = 24.2
});
```


**Example C# Server**

```csharp
WeatherStation station = new WeatherStation();
WoopsaServer server = new WoopsaServer(station);

station.Temperature = 24.2;
```

**Example node Server**
```javascript
var woopsa = require('woopsa');
var weatherStation = {
    Temperature: 24.2,
    IsRaining: false,
    Sensitivity: 0.5,
    Altitude: 430,
    City: "Geneva",
    Time: new Date(),
    GetWeatherAtDate: function (date){
        var date = new Date(date);
        if ( date.getDay() === 1 )
            return "rainy";
        else
            return "sunny";
    },
    Thermostat: {
        SetPoint: 24.0
    }
}
var server = new woopsa.Server(weatherStation, {port: 80});
```

**Example Embedded C Server (Arduino, others)**

```c
double Temperature;
int Altitude;
float GetAirPressure() {
  return 42.2;
}

WOOPSA_BEGIN(woopsa_entries)
  WOOPSA_PROPERTY_READONLY(Temperature, WOOPSA_TYPE_REAL)
  WOOPSA_PROPERTY(Altitude, WOOPSA_TYPE_INTEGER)
  WOOPSA_METHOD(GetAirPressure, WOOPSA_TYPE_REAL)
WOOPSA_END
...
```

## Getting the library
The latest version is part of the git repository. It contains the .NET and JavaScript versions of the Woopsa library, as well as a few examples to get started!

## Getting started
Our [Getting Started](http://www.woopsa.org/get-started/) tutorial allows you to get started quickly with Woopsa. It's really easy !

## Compatibility
Woopsa has been tested and works on:
 * .NET Framework 4.0
 * .NET Framework 4.5
 * .NET Core 2.0
 * Mono 4.0+
 
