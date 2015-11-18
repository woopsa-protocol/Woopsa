# Woopsa
Woopsa is a protocol that's simple, lightweight, free, open-source, web and object-oriented, publish-subscribe, real-time capable and Industry 4.0 ready. It contributes to the revolution of the Internet of Things.

Woopsa allows you to share the complete object model of your application in a way that's similar to OPC-UA. It's based on rock-solid foundations such as HTTP and JSON, which makes it work on the web out-of-the-box. Our mission is to get Woopsa on as many platforms as possible. Today, C# and JavaScript implementations exist, but there are much more to come!

**Example JavaScript Client**

```javascript
var woopsa = new WoopsaClient("http://demo.woopsa.org/woopsa", jQuery);
woopsa.read("/Temperature", function(result){
//result = 42
});
```


**Example C# Server**

```csharp
WeatherStation station = new WeatherStation();
WoopsaServer server = new WoopsaServer(station);

station.Temperature = 42;
```

## Getting the library
The latest release is part of the git repository, in the well-named **Release** directory. It contains the .NET and JavaScript versions of the Woopsa library, as well as a few examples to get started!

You can of course also go to the Releases tab of this project, to get version 1.0 here: https://github.com/woopsa-protocol/Woopsa/releases/tag/v1.0

## Getting started
Our [Getting Started](http://www.woopsa.org/get-started/) tutorial allows you to get started quickly with Woopsa. It's really easy and we promise you'll be convinced!

## Building / Making a release
### Windows
Run the make-release-windows.bat file. This will
 * Build the .NET library
 * Build the WoopsaDemo server
 * Minify/uglify the JavaScript library
 * Copy all those things in the ``Release`` directory

System requirements:
 * Visual Studio Professional 2013 or newer (requires devenv to be in your ``PATH`` variable)
 * Uglifyjs (requires nodejs)

### Linux/MacOS
A build/release script is coming soon!

## Compatibility
Woopsa has been tested and works on:
 * .NET Framework 4.5
 * Mono 4.0+
