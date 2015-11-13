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

int sockInit(void)
{
#ifdef _WIN32
	WSADATA wsa_data;
	return WSAStartup(MAKEWORD(1, 1), &wsa_data);
#else
	return 0;
#endif
}

int sockQuit(void)
{
#ifdef _WIN32
	return WSACleanup();
#else
	return 0;
#endif
}

/* Note: For POSIX, typedef SOCKET as an int. */
int sockClose(SOCKET sock)
{
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

#define WOOPSA_PORT 8000
#define BUFFER_SIZE 65535

int main(int argc, char argv[])
{
	SOCKET sock, clientSock;
	struct sockaddr_in addr, clientAddr;
	char buffer[BUFFER_SIZE];
	int clientAddrSize, readBytes;
	WoopsaServer server;
	WoopsaUInt16 responseLength;

	memset(buffer, 0, sizeof(buffer));
	server = WoopsaInit("woopsa", NULL);

	printf("Woopsa C library v0.1 demo server.\n");

	if (sockInit() != 0)
	{
		printf("Error initializing sockets\n");
		EXIT_ERROR();
	}

	sock = socket(AF_INET, SOCK_STREAM, 0);
	if (!CHECK_SOCKET(sock))
	{
		printf("Error creating socket\n");
		EXIT_ERROR();
	}

	addr.sin_family = AF_INET;
	addr.sin_addr.s_addr = INADDR_ANY;
	addr.sin_port = htons(WOOPSA_PORT);

	if (bind(sock, (struct sockaddr *)&addr, sizeof(addr)) != 0)
	{
		printf("Error binding socket\n");
		EXIT_ERROR();
	}

	listen(sock, 5);
	printf("Server listening on port %d\n", WOOPSA_PORT);

	while (1)
	{
		clientAddrSize = sizeof(struct sockaddr_in);
		clientSock = accept(sock, &clientAddr, &clientAddrSize);
		if (!CHECK_SOCKET(clientSock))
		{
			printf("Received an invalid client socket.\n");
			EXIT_ERROR();
		}

		while (1)
		{
			readBytes = recv(clientSock, buffer, sizeof(buffer), NULL);

			if (WoopsaCheckRequestFinished(buffer, sizeof(buffer)) == 1)
			{
				printf("Found end\n");
			}
			if (readBytes == 0)
			{
				printf("Finished\n");
				break;
			}
			
			if (WoopsaHandleRequest(server, buffer, sizeof(buffer), buffer, sizeof(buffer), &responseLength) == 0)
			{
				send(clientSock, buffer, responseLength, NULL);
			}
		}
	}

	if (sockClose(sock) != 0)
	{
		printf("Error closing socket\n");
		EXIT_ERROR();
	}

	if (sockQuit() != 0)
	{
		printf("Error quitting sockets\n");
		EXIT_ERROR();
	}

	getchar();

	return 0;
}