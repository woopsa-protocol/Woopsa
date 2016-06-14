/*
==================================================================

                   WOOPSA ARDUINO DEMO SERVER
              
==================================================================

 THIS SAMPLE REQUIRES THE ETHERNET2 LIBRARY BUNDLED ONLY WITH THE
 ARDUINO 1.7+ IDE AVAILABLE FROM ARDUINO.ORG (AND NOT ARDUINO.CC).

 This little piece of software creates a Woopsa server on
 an Arduino board that allows you to view Analog input values
 and manage digital I/Os (read/write and set pin mode).
 
 Since Woopsa uses HTTP (over TCP/IP), you will obviously need an
 Arduino Ethernet shield.
 
 If you are using version 1 of the Ethernet Shield, you might have 
 to change "Ethernet2.h" to "Ethernet.h" in the include directives.
 
 Instructions for use:
  - Choose an IP address below that matches your local network
  - Upload the sketch to the arduino
  - Point your browser to http://{ip-address}/
  - Have fun playing with the Arduino's I/Os!
  
 NOTES: This code uses about 4k of RAM, which means it won't fit
 in the Arduino Uno without a lot of optimizations. Sorry.
 
==================================================================
*/
#include <SPI.h>
#include <Ethernet2.h>
#include <avr/pgmspace.h>

#include "woopsa-server.h"
#include "html.h"

// Enter a MAC address and IP address for your controller below.
// The IP address will be dependent on your local network:
byte mac[] = {
	0x90, 0xA2, 0xDA, 0x10, 0x32, 0x35
};
IPAddress ip(192, 168, 42, 3);

// Initialize the Ethernet server library
// with the IP address and port you want to use
// (port 80 is default for HTTP):
EthernetServer server(80);

// The struct for holding all woopsa information
WoopsaServer woopsaServer;

// The properties that will be published over Woopsa
int AnalogIn0, AnalogIn1, AnalogIn2, AnalogIn3, AnalogIn4, AnalogIn5;
int Digital0, Digital1, Digital2, Digital3, Digital4, Digital5, Digital6, Digital7, Digital8, Digital9, Digital10, Digital11, Digital12, Digital13;
int PinMode0, PinMode1, PinMode2, PinMode3, PinMode4, PinMode5, PinMode6, PinMode7, PinMode8, PinMode9, PinMode10, PinMode11, PinMode12, PinMode13;

// These macros publish properties over the Woopsa protocol
WOOPSA_BEGIN(woopsaEntries)
	WOOPSA_PROPERTY_READONLY(AnalogIn0, WOOPSA_TYPE_INTEGER)
	WOOPSA_PROPERTY_READONLY(AnalogIn1, WOOPSA_TYPE_INTEGER)
	WOOPSA_PROPERTY_READONLY(AnalogIn2, WOOPSA_TYPE_INTEGER)
	WOOPSA_PROPERTY_READONLY(AnalogIn3, WOOPSA_TYPE_INTEGER)
	WOOPSA_PROPERTY_READONLY(AnalogIn4, WOOPSA_TYPE_INTEGER)
	WOOPSA_PROPERTY_READONLY(AnalogIn5, WOOPSA_TYPE_INTEGER)
	WOOPSA_PROPERTY(Digital0, WOOPSA_TYPE_INTEGER)
	WOOPSA_PROPERTY(Digital1, WOOPSA_TYPE_INTEGER)
	WOOPSA_PROPERTY(Digital2, WOOPSA_TYPE_INTEGER)
	WOOPSA_PROPERTY(Digital3, WOOPSA_TYPE_INTEGER)
	WOOPSA_PROPERTY(Digital4, WOOPSA_TYPE_INTEGER)
	WOOPSA_PROPERTY(Digital5, WOOPSA_TYPE_INTEGER)
	WOOPSA_PROPERTY(Digital6, WOOPSA_TYPE_INTEGER)
	WOOPSA_PROPERTY(Digital7, WOOPSA_TYPE_INTEGER)
	WOOPSA_PROPERTY(Digital8, WOOPSA_TYPE_INTEGER)
	WOOPSA_PROPERTY(Digital9, WOOPSA_TYPE_INTEGER)
	WOOPSA_PROPERTY(Digital10, WOOPSA_TYPE_INTEGER)
	WOOPSA_PROPERTY(Digital11, WOOPSA_TYPE_INTEGER)
	WOOPSA_PROPERTY(Digital12, WOOPSA_TYPE_INTEGER)
	WOOPSA_PROPERTY(Digital13, WOOPSA_TYPE_INTEGER)
	WOOPSA_PROPERTY(PinMode0, WOOPSA_TYPE_INTEGER)
	WOOPSA_PROPERTY(PinMode1, WOOPSA_TYPE_INTEGER)
	WOOPSA_PROPERTY(PinMode2, WOOPSA_TYPE_INTEGER)
	WOOPSA_PROPERTY(PinMode3, WOOPSA_TYPE_INTEGER)
	WOOPSA_PROPERTY(PinMode4, WOOPSA_TYPE_INTEGER)
	WOOPSA_PROPERTY(PinMode5, WOOPSA_TYPE_INTEGER)
	WOOPSA_PROPERTY(PinMode6, WOOPSA_TYPE_INTEGER)
	WOOPSA_PROPERTY(PinMode7, WOOPSA_TYPE_INTEGER)
	WOOPSA_PROPERTY(PinMode8, WOOPSA_TYPE_INTEGER)
	WOOPSA_PROPERTY(PinMode9, WOOPSA_TYPE_INTEGER)
	WOOPSA_PROPERTY(PinMode10, WOOPSA_TYPE_INTEGER)
	WOOPSA_PROPERTY(PinMode11, WOOPSA_TYPE_INTEGER)
	WOOPSA_PROPERTY(PinMode12, WOOPSA_TYPE_INTEGER)
	WOOPSA_PROPERTY(PinMode13, WOOPSA_TYPE_INTEGER)
WOOPSA_END;

void IoLoop() {
	// Read values from Analog inputs
	AnalogIn0 = analogRead(A0);
	AnalogIn1 = analogRead(A1);
	AnalogIn2 = analogRead(A2);
	AnalogIn3 = analogRead(A3);
	AnalogIn4 = analogRead(A4);
	AnalogIn5 = analogRead(A5);

	// Auto-generated code, copies I/Os
	pinMode(0,PinMode0);
	if (PinMode0 == OUTPUT)
		digitalWrite(0, Digital0);
	else
		Digital0 = digitalRead(0);
	
	pinMode(1,PinMode1);
	if (PinMode1 == OUTPUT)
		digitalWrite(1, Digital1);
	else
		Digital1 = digitalRead(1);
	
	pinMode(2,PinMode2);
	if (PinMode2 == OUTPUT)
		digitalWrite(2, Digital2);
	else
		Digital2 = digitalRead(2);
	
	pinMode(3,PinMode3);
	if (PinMode3 == OUTPUT)
		digitalWrite(3, Digital3);
	else
		Digital3 = digitalRead(3);
	
	pinMode(4,PinMode4);
	if (PinMode4 == OUTPUT)
		digitalWrite(4, Digital4);
	else
		Digital4 = digitalRead(4);
	
	pinMode(5,PinMode5);
	if (PinMode5 == OUTPUT)
		digitalWrite(5, Digital5);
	else
		Digital5 = digitalRead(5);
	
	pinMode(6,PinMode6);
	if (PinMode6 == OUTPUT)
		digitalWrite(6, Digital6);
	else
		Digital6 = digitalRead(6);
	
	pinMode(7,PinMode7);
	if (PinMode7 == OUTPUT)
		digitalWrite(7, Digital7);
	else
		Digital7 = digitalRead(7);
	
	pinMode(8,PinMode8);
	if (PinMode8 == OUTPUT)
		digitalWrite(8, Digital8);
	else
		Digital8 = digitalRead(8);
	
	pinMode(9,PinMode9);
	if (PinMode9 == OUTPUT)
		digitalWrite(9, Digital9);
	else
		Digital9 = digitalRead(9);

	// Pins reserved for ethernet shield		
	// pinMode(10,PinMode10);
	if (PinMode10 == OUTPUT)
		digitalWrite(10, Digital10);
	else
		Digital10 = digitalRead(10);

	// Pins reserved for ethernet shield		
	// pinMode(11,PinMode11);
	if (PinMode11 == OUTPUT)
		digitalWrite(11, Digital11);
	else
		Digital11 = digitalRead(11);

	// Pins reserved for ethernet shield		
	// pinMode(12,PinMode12);
	if (PinMode12 == OUTPUT)
		digitalWrite(12, Digital12);
	else
		Digital12 = digitalRead(12);

	// Pins reserved for ethernet shield 
	// pinMode(13,PinMode13);
	if (PinMode13 == OUTPUT)
		digitalWrite(13, Digital13);
	else
		Digital13 = digitalRead(13);
}


WoopsaBufferSize ServeHTML(WoopsaChar8 path[], WoopsaUInt8 isPost, WoopsaChar8 dataBuffer[], WoopsaBufferSize dataBufferSize) {
	// Normally, we would write inside the dataBuffer
	// However, due to the Arduino's extremely limited
	// RAM (a few kB), we can't write a "huge" 14kB HTML
	// file. So we just return the length of the HTML
	// and we will directly write the HTML stored in flash
	// memory to the TCP stream in the woopsaLoop. See below.
	return sizeof(HTML)-1;
}

void WoopsaLoop() {
	char dataBuffer[2048];
	int bufferAt = 0;
	WoopsaBufferSize responseLength;
	memset(dataBuffer, 0, sizeof(dataBuffer));
	// Listen for incoming clients
	EthernetClient client = server.available();
	if (client) {
		while (client.connected()) {
			if (client.available()) {
				// We _append_ data to our buffer when it is received
				bufferAt += client.read((unsigned char*)(dataBuffer + bufferAt), sizeof(dataBuffer));
				
				// When we get a request from a client, we need
				// to make sure it's complete before we pass it
				// to the Woopsa server. This allows us to handle
				// cases where packets are fragmented.
				if (WoopsaCheckRequestComplete(&woopsaServer, dataBuffer, sizeof(dataBuffer)) != WOOPSA_REQUEST_COMLETE) {
					continue;
				}

				if ( WoopsaHandleRequest(&woopsaServer, dataBuffer, sizeof(dataBuffer), dataBuffer, sizeof(dataBuffer), &responseLength) == WOOPSA_OTHER_RESPONSE ) {
					// This was a non-Woopsa request, serve the HTML page
					client.print(dataBuffer);
					// The F() macro specifies the const string is stored
					// in flash memory
					client.print(F(HTML));
				} else {
					client.print(dataBuffer);
				}
				break;
			}
		}
	}
	client.stop();
}

void setup() {
        int error = 0;
	// Open serial communications and wait for port to open:
	Serial.begin(9600);
	while (!Serial); // wait for serial port to connect. Needed for native USB port only

        // start the Ethernet connection and the server:
	error = Ethernet.begin(mac); // Start at DHCP IP address
        if (error == 0) // if DHCP fails
        {
            Serial.println("DHCP failure, continue with fixed IP address ...");
	    Ethernet.begin(mac, ip); // Start at the defined IP address
        }
	server.begin();
	
	// Initialize the woopsa server with a path prefix of 
	// /woopsa/ and the specified woopsa entries.
	// The ServeHTML function allows us to serve an HTML
	// page which you can access
	WoopsaServerInit(&woopsaServer, "/woopsa/", woopsaEntries, ServeHTML);
	
	Serial.print(F("Woopsa server listening on http://"));
	Serial.print(Ethernet.localIP());
	Serial.println(F("/woopsa/"));
	Serial.print(F("You can check out a demo on http://"));
	Serial.print(Ethernet.localIP());
	Serial.println(F("/ which allows you to play with the Arduino's IOs."));
}

void loop() {
	IoLoop();
	WoopsaLoop();	 
}

