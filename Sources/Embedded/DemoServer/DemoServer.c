#include <stdio.h>
#include <stdlib.h>
#include "../Server/woopsa-server.h"

#ifdef _WIN32
/* See http://stackoverflow.com/questions/12765743/getaddrinfo-on-win32 */
#ifndef _WIN32_WINNT
#define _WIN32_WINNT 0x0501  /* Windows XP. */
#define CHECK_SOCKET(socket) \
	(!(socket == INVALID_SOCKET))
#define EXIT_ERROR() \
	exit(WSAGetLastError())
#endif
#include <winsock2.h>
#include <Ws2tcpip.h>
#else
/* Assume that any non-Windows platform uses POSIX-style sockets instead. */
#include <errno.h>
#include <sys/socket.h>
#include <arpa/inet.h>
#include <netdb.h>  /* Needed for getaddrinfo() and freeaddrinfo() */
#include <unistd.h> /* Needed for close() */
#define CHECK_SOCKET(socket) \
	(!(socket < 0))
#define EXIT_ERROR() \
	exit(errno)
#endif

int sockInit(void) {
#ifdef _WIN32
	WSADATA wsa_data;
	return WSAStartup(MAKEWORD(1, 1), &wsa_data);
#else
	return 0;
#endif
}

int sockQuit(void) {
#ifdef _WIN32
	return WSACleanup();
#else
	return 0;
#endif
}

/* Note: For POSIX, typedef SOCKET as an int. */
int sockClose(SOCKET sock) {
	int status = 0;
#ifdef _WIN32
	status = shutdown(sock, SD_BOTH);
	if (status == 0) { status = closesocket(sock); }
#else
	status = shutdown(sock, SHUT_RDWR);
	if (status == 0) { status = close(sock); }
#endif

	return status;
}

float Temperature = 24.2;
char IsRaining = 1;
int Altitude = 430;
float Sensitivity = 0.5;
char City[20] = "Geneva";
float TimeSinceLastRain = 11;


char weatherBuffer[20];
char* GetWeather() {
	sprintf(weatherBuffer, "sunny");
	return weatherBuffer;
}

WOOPSA_BEGIN(woopsaEntries)
	WOOPSA_PROPERTY_READONLY(Temperature, WOOPSA_TYPE_REAL)
	WOOPSA_PROPERTY(IsRaining, WOOPSA_TYPE_LOGICAL)
	WOOPSA_PROPERTY(Altitude, WOOPSA_TYPE_INTEGER)
	WOOPSA_PROPERTY(Sensitivity, WOOPSA_TYPE_REAL)
	WOOPSA_PROPERTY(City, WOOPSA_TYPE_TEXT)
	WOOPSA_PROPERTY(TimeSinceLastRain, WOOPSA_TYPE_TIME_SPAN)
	WOOPSA_METHOD(GetWeather, WOOPSA_TYPE_TEXT)
WOOPSA_END;

#define WOOPSA_PORT 8000
#define BUFFER_SIZE 1024


WoopsaUInt16 ServeHTML(WoopsaChar8 path[], WoopsaUInt8 isPost, WoopsaChar8 dataBuffer[], WoopsaUInt16 dataBufferSize) {
	strcpy(dataBuffer, "Hello world!");
	return strlen("Hellow world!");
}

int main(int argc, char argv[]) {
	SOCKET sock, clientSock;
	struct sockaddr_in addr, clientAddr;
	char buffer[BUFFER_SIZE];
	int clientAddrSize = 0, readBytes = 0;
	WoopsaServer server;
	WoopsaUInt16 responseLength;

	memset(buffer, 0, sizeof(buffer));
	WoopsaServerInit(&server, "/woopsa/", woopsaEntries, NULL);

	printf("Woopsa C library v0.1 demo server.\n");

	if (sockInit() != 0) {
		printf("Error initializing sockets\n");
		EXIT_ERROR();
	}

	sock = socket(AF_INET, SOCK_STREAM, 0);
	if (!CHECK_SOCKET(sock)) {
		printf("Error creating socket\n");
		EXIT_ERROR();
	}

	addr.sin_family = AF_INET;
	addr.sin_addr.s_addr = INADDR_ANY;
	addr.sin_port = htons(WOOPSA_PORT);

	if (bind(sock, (struct sockaddr *)&addr, sizeof(addr)) != 0) {
		printf("Error binding socket\n");
		EXIT_ERROR();
	}

	listen(sock, 5);
	printf("Server listening on port %d\n", WOOPSA_PORT);

	while (1) {
		clientAddrSize = sizeof(struct sockaddr_in);
		clientSock = accept(sock, &clientAddr, &clientAddrSize);
		if (!CHECK_SOCKET(clientSock)) {
			printf("Received an invalid client socket.\n");
			EXIT_ERROR();
		}

		while (1) {
			readBytes = recv(clientSock, buffer + readBytes, sizeof(buffer), NULL);

			if (readBytes == SOCKET_ERROR) {
				printf("Error %d", WSAGetLastError());
				break;
			}

			if (readBytes == 0) {
				printf("Finished\n");
				break;
			}

			if (WoopsaCheckRequestComplete(&server, buffer, sizeof(buffer)) != WOOPSA_REQUEST_COMLETE) {
				// If the request is not complete, it means more data needs 
				// to be -added- to the buffer
				continue;
			}

			if (WoopsaHandleRequest(&server, buffer, sizeof(buffer), buffer, sizeof(buffer), &responseLength) >= WOOPSA_SUCCESS) {
				send(clientSock, buffer, responseLength, NULL);
			}
			readBytes = 0;
			memset(buffer, 0, sizeof(buffer));
		}
	}

	if (sockClose(sock) != 0) {
		printf("Error closing socket\n");
		EXIT_ERROR();
	}

	if (sockQuit() != 0) {
		printf("Error quitting sockets\n");
		EXIT_ERROR();
	}

	getchar();

	return 0;
}