var woopsa = require("../server/server");

var weatherStation = {
  Temperature: 24.2,
  IsRaining: false,
  Sensitivity: 0.5,
  Altitude: 430,
  City: "Geneva",
  Time: new Date(),
  GetWeatherAtDate: function (date){
    return "sunny";
  }
}

var server = new woopsa.Server("Server1", {port: 80});