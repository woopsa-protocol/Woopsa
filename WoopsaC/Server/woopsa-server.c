#include "woopsa-server.h"
#include <string.h>

#define VERB_META	"meta"
#define VERB_READ	"read"
#define VERB_WRITE	"write"

#define HTTP_METHOD_GET "GET"
#define HTTP_METHOD_POST "POST"
#define HTTP_VERSION_STRING "HTTP/1.1"

#define CLIENT_REQUEST_ERROR 1
#define OTHER_ERROR 2

#define HTTP_CODE_NOT_FOUND 404
#define TEXT_NOT_FOUND "Not found"

#define HTTP_CODE_INTERNAL_ERROR 500
#define TEXT_INTERNAL_ERROR "Internal server error"

#define HTTP_CODE_NOT_IMPLEMENTED 501
#define TEXT_METHOD_NOT_IMPLEMENTED "%s method not implemented"

#define RESPONSE_VALUE "{\"Value\":%s,\"Type\":\"%s\"}"
#define RESPONSE_ERROR "{\"Error\":\"%s\"}"
#define RESPONSE_META "{\"Name\":\"%s\",\"Properties\":%s,\"Items\":[],\"Methods\":[]}"
#define RESPONSE_PROPERTY "{\"Name\":\"%s\",\"Type\":\"%s\",\"ReadOnly\":\"%s\"}"
#define RESPONSE_ARRAY_START '['
#define RESPONSE_ARRAY_END ']'
#define RESPONSE_ARRAY_DELIMITER ','

#define OK_CODE 200

int NextHTTPHeader(char* searchString, char** nextHeader)
{
	int i = 0, carriageFound = 0;
	while (searchString[i] != '\0')
	{
		if (carriageFound == 1 && searchString[i] == '\n'){
			*nextHeader = searchString + i + 1;
			return i - 1;
		}
		else if (carriageFound == 1 && searchString[i] != '\n')
			carriageFound = 0;
		else if (searchString[i] == '\r')
			carriageFound = 1;
		i++;
	}
	return -1;
}

WoopsaServer WoopsaInit(char* pathPrefix, WoopsaProperty properties[])
{
	WoopsaServer server;
	server.pathPrefix = pathPrefix;
	server.properties = properties;
	return server;
}

WoopsaUInt8 WoopsaCheckRequestFinished(WoopsaChar8* inputBuffer, WoopsaUInt16 inputBufferLength)
{
	if (strstr(inputBuffer, "\r\n\r\n") == 0)
		return 0;
	else
		return 1;
}

#define MAX_PATH_LENGTH 254
#define MAX_VERB_LENGTH 16

WoopsaChar8* WoopsaHandleRequest(WoopsaServer server, WoopsaChar8* inputBuffer, WoopsaUInt16 inputBufferLength, WoopsaChar8* outputBuffer, WoopsaUInt16 outputBufferLength, WoopsaUInt16* responseLength)
{
	char* header;
	char oldChar, path[MAX_PATH_LENGTH], verb[MAX_VERB_LENGTH];
	int cnt, i, pos;

	memset(path, NULL, sizeof(path));
	memset(verb, NULL, sizeof(verb));

	cnt = NextHTTPHeader(inputBuffer, &header);

	for (i = 0; i < cnt && i < MAX_VERB_LENGTH; i++)
	{
		if (inputBuffer[i] == ' ')
		{
			pos = i + 1;
			break;
		}
		verb[i] = inputBuffer[i];
	}
	for (i = 0; (i < cnt - pos) && (i < MAX_PATH_LENGTH); i++)
	{
		if (inputBuffer[pos + i] == ' ')
			break;
		path[i] = inputBuffer[pos + i];
	}

	if (strcmp("GET", verb) == 0){
		strncpy(outputBuffer, "HTTP/1.1 200 OK\r\nContent-Length:5\r\n\r\nHello", outputBufferLength);
	}

	*responseLength = strlen(outputBuffer);
	return 0;
}
