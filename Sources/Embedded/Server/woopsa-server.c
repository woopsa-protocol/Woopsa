#include "woopsa-server.h"
#include <string.h>
#include <stdio.h>

// Thanks microsoft for not supporting snprintf!
#if defined(_MSC_VER) && _MSC_VER < 1900
#define snprintf _snprintf
#endif

// Woopsa constants
#define VERB_META	"meta"
#define VERB_READ	"read"
#define VERB_WRITE	"write"

#define TYPE_STRING_NULL            "Null"
#define TYPE_STRING_INTEGER         "Integer"
#define TYPE_STRING_REAL            "Real"
#define TYPE_STRING_DATE_TIME       "DateTime"
#define TYPE_STRING_TIME_SPAN       "TimeSpan"
#define TYPE_STRING_TEXT            "Text"
#define TYPE_STRING_LINK            "Link"
#define TYPE_STRING_RESOURCE_URL    "ResourceUrl"

#define TYPE_FORMAT_INTEGER			"%d"
#define TYPE_FORMAT_REAL			"%f"
#define TYPE_FORMAT_TEXT			"\"%s\""

typedef struct {
	WoopsaType	type;
	WoopsaChar8* string;
	WoopsaChar8* format;
} TypesDictionaryEntry;

TypesDictionaryEntry Types[] = {
	{ WOOPSA_TYPE_NULL, TYPE_STRING_NULL, TYPE_FORMAT_INTEGER },
	{ WOOPSA_TYPE_INTEGER, TYPE_STRING_INTEGER, TYPE_FORMAT_INTEGER },
	{ WOOPSA_TYPE_REAL, TYPE_STRING_REAL, TYPE_FORMAT_REAL },
	{ WOOPSA_TYPE_DATE_TIME, TYPE_STRING_DATE_TIME, TYPE_FORMAT_TEXT },
	{ WOOPSA_TYPE_TIME_SPAN, TYPE_STRING_TIME_SPAN, TYPE_FORMAT_REAL },
	{ WOOPSA_TYPE_TEXT, TYPE_STRING_TEXT, TYPE_FORMAT_TEXT },
	{ WOOPSA_TYPE_LINK, TYPE_STRING_LINK, TYPE_FORMAT_TEXT },
	{ WOOPSA_TYPE_RESOURCE_URL, TYPE_STRING_RESOURCE_URL, TYPE_FORMAT_TEXT }
};

// HTTP constants
#define HTTP_METHOD_GET "GET"
#define HTTP_METHOD_POST "POST"
#define HTTP_VERSION_STRING "HTTP/1.1"

#define HTTP_CODE_NOT_FOUND "404"
#define HTTP_TEXT_NOT_FOUND "Not found"

#define HTTP_CODE_INTERNAL_ERROR "500"
#define HTTP_TEXT_INTERNAL_ERROR "Internal server error"

#define HTTP_CODE_NOT_IMPLEMENTED "501"
#define HTTP_TEXT_METHOD_NOT_IMPLEMENTED "%s method not implemented"

#define HTTP_CODE_OK "200"
#define HTTP_TEXT_OK "OK"

#define HEADER_SEPARATOR "\r\n"
#define HEADER_CONTENT_LENGTH "Content-Length: "
#define HEADER_CONTENT_LENGTH_SPACE "        "
#define HEADER_CONTENT_LENGTH_FORMAT "%8d"
#define EXTRA_HEADERS "Content-Type: application/json" HEADER_SEPARATOR "Access-Control-Allow-Origin: *" HEADER_SEPARATOR "Connection: close" HEADER_SEPARATOR

// Serialization constants
#define JSON_VALUE_VALUE "{\"Value\":"
#define JSON_VALUE_TYPE ",\"Type\":\""
#define JSON_VALUE_END "\"}"
#define JSON_ERROR "{\"Error\":\"%s\"}"
#define JSON_META_START "{\"Name\":\"Root\",\"Properties\":"
#define JSON_META_END ",\"Items\":[],\"Methods\":[]}"
#define JSON_PROPERTY_NAME "{\"Name\":\""
#define JSON_PROPERTY_TYPE "\",\"Type\":\""
#define JSON_PROPERTY_READONLY "\",\"ReadOnly\":"
#define JSON_PROPERTY_END "}"
#define JSON_ARRAY_START "["
#define JSON_ARRAY_END "]"
#define JSON_ARRAY_DELIMITER ","
#define JSON_TRUE "true"
#define JSON_FALSE "false"
#define JSON_STRING_DELIMITER "\""
#define JSON_STRING_DELIMITER_CHAR '"'
#define JSON_ESCAPE_CHAR '\\'


// Memory-specific constants
#define MAX_NUMERICAL_VALUE_LENGTH 10

// Gets a Woopsa Property by name
// Returns a pointer to the WoopsaProperty, or null if not found
WoopsaProperty* GetPropertyByNameOrNull(WoopsaProperty properties[], WoopsaChar8 name[]) {
	WoopsaUInt16 i = 0;
	while (properties[i].name != NULL) {
		if (strcmp(properties[i].name, name) == 0)
			return &properties[i];
		i++;
	}
	return NULL;
}

// Gets the type string for a given type
// Returns a pointer to a string or NULL if not found (why ??)
TypesDictionaryEntry * GetTypeEntry(WoopsaType type) {
	WoopsaUInt8 i;
	for (i = 0; i < sizeof(Types) / sizeof(TypesDictionaryEntry); i++)
	if (Types[i].type == type)
		return &Types[i];
	return NULL;
}

// Finds the string length of the first HTTP header found in searchString
// Also sets nextHeader to be the start of the next header
// Returns the length of the *current* header, or -1 if this header is empty
// (which means this is the double new-line at the end of HTTP request)
WoopsaInt16 NextHTTPHeader(const WoopsaChar8* searchString, const WoopsaChar8** nextHeader) {
	WoopsaUInt16 i = 0, carriageFound = 0;
	while (searchString[i] != '\0') {
		if (carriageFound == 1 && searchString[i] == '\n') {
			*nextHeader = searchString + i + 1;
			return i - 1;
		} else if (carriageFound == 1 && searchString[i] != '\n')
			carriageFound = 0;
		else if (searchString[i] == '\r')
			carriageFound = 1;
		i++;
	}
	return -1;
}

// Appends source to destination, while keeping destination under num length
// Returns amount of characters actually appended
WoopsaUInt16 Append(WoopsaChar8 destination[], const WoopsaChar8 source[], WoopsaUInt16 num) {
	WoopsaUInt16 i = 0, cnt = 0;
	// go to end of destination
	while (destination[i] != '\0')
		i++;
	//append characters one-by-one
	while (source[cnt] != '\0' && i < num - 1)
		destination[i++] = source[cnt++];
	destination[i] = '\0';
	return cnt;
}

// Appends source to destination, while keeping destination under num length
// and escaping specified character
// Returns amount of characters actually appended
WoopsaUInt16 AppendEscape(WoopsaChar8 destination[], const WoopsaChar8 source[], WoopsaChar8 special, WoopsaChar8 escape, WoopsaUInt16 num) {
	WoopsaUInt16 i = 0, cnt = 0, extras = 0;
	// go to end of destination
	while (destination[i] != '\0')
		i++;
	//append characters one-by-one
	while (source[cnt] != '\0' && i < num - 1) {
		if (source[cnt] == special) {
			// TODO: Watch out for buffer overflow!
			destination[i++] = escape;
			destination[i++] = source[cnt++];
			extras++;
		} else {
			destination[i++] = source[cnt++];
		}
	}
	destination[i] = '\0';
	return cnt + extras;
}

// Prepares an HTTP response in the specified outputBuffer
// Will send the specified HTTP status code and a status string
// Will also add the content
// This method will basically prepare the entire string that can be
// send out to the client, including all HTTP headers.
// Returns the length of the prepared string
WoopsaUInt16 PrepareResponse(
		WoopsaChar8* outputBuffer,
		const WoopsaUInt16 outputBufferLength,
		const WoopsaChar8* httpStatusCode,
		const WoopsaChar8* httpStatusStr,
		WoopsaUInt16* contentLengthPosition) {
	WoopsaUInt16 size = 0;

	outputBuffer[0] = '\0';

	// HTTP/1.1
	size = Append(outputBuffer, HTTP_VERSION_STRING " ", outputBufferLength);
	// 200 OK
	size += Append(outputBuffer, httpStatusCode, outputBufferLength);
	size += Append(outputBuffer, " ", outputBufferLength);
	size += Append(outputBuffer, httpStatusStr, outputBufferLength);
	size += Append(outputBuffer, HEADER_SEPARATOR, outputBufferLength);
	// Extra headers
	size += Append(outputBuffer, EXTRA_HEADERS, outputBufferLength);
	// Content-Length:
	size += Append(outputBuffer, HEADER_CONTENT_LENGTH, outputBufferLength);
	*contentLengthPosition = size;
	// We leave a few spaces for the Content-Length
	size += Append(outputBuffer, HEADER_CONTENT_LENGTH_SPACE, outputBufferLength);
	// Final double new lines
	size += Append(outputBuffer, HEADER_SEPARATOR HEADER_SEPARATOR, outputBufferLength);

	return size;
}

void SetContentLength(WoopsaChar8* outputBuffer, const WoopsaUInt16 outputBufferLength, WoopsaUInt16 contentLengthPosition, WoopsaUInt16 contentLength) {
	sprintf(outputBuffer + contentLengthPosition, HEADER_CONTENT_LENGTH_FORMAT, contentLength);
	outputBuffer[contentLengthPosition + strlen(HEADER_CONTENT_LENGTH_SPACE)] = '\r';
}

WoopsaUInt16 PrepareResponseWithContent(
		WoopsaChar8* outputBuffer,
		const WoopsaUInt16 outputBufferLength,
		const WoopsaChar8* httpStatusCode,
		const WoopsaChar8* httpStatusStr,
		const WoopsaChar8* content) {
	WoopsaUInt16 len, pos, contentLength;

	len = PrepareResponse(outputBuffer, outputBufferLength, httpStatusCode, httpStatusStr, &pos);
	contentLength = strlen(content);
	SetContentLength(outputBuffer, outputBufferLength, pos, contentLength);
	len += Append(outputBuffer, content, outputBufferLength);
	return len;
}

// Shortcut to generate an HTTP error. The contents
// of the response is the error string itself.
WoopsaUInt16 PrepareError(WoopsaChar8* outputBuffer, const WoopsaUInt16 outputBufferLength, WoopsaChar8 errorCode[], const WoopsaChar8 errorStr[]) {
	return PrepareResponseWithContent(outputBuffer, outputBufferLength, errorCode, errorStr, errorStr);
}

///////////////////////////////////////////////////////////////////////////////
//                   BEGIN PUBLIC WOOPSA IMPLEMENTATION                      //
///////////////////////////////////////////////////////////////////////////////

void WoopsaServerInit(WoopsaServer* server, WoopsaChar8* pathPrefix, WoopsaProperty properties[]) {
	server->pathPrefix = pathPrefix;
	server->properties = properties;
}

WoopsaUInt8 WoopsaCheckRequestComplete(WoopsaChar8* inputBuffer, WoopsaUInt16 inputBufferLength) {
	if (strstr(inputBuffer, "\r\n\r\n") == 0)
		return 0;
	else
		return 1;
}

WoopsaUInt8 WoopsaHandleRequest(WoopsaServer* server, const WoopsaChar8* inputBuffer, WoopsaUInt16 inputBufferLength, WoopsaChar8* outputBuffer, WoopsaUInt16 outputBufferLength, WoopsaUInt16* responseLength) {
	WoopsaChar8* header = NULL;
	WoopsaChar8* woopsaPath = NULL;
	//HttpHeader parsedHeader;
	WoopsaChar8 numericValueBuffer[MAX_NUMERICAL_VALUE_LENGTH];
	WoopsaChar8 oldChar = '\0';
	WoopsaUInt16 i = 0, pos = 0, isPost = 0, contentLengthPosition = 0, contentLength = 0;
	WoopsaInt16 headerSize = 0;
	WoopsaProperty* woopsaProperty = NULL;
	TypesDictionaryEntry* typeEntry = NULL;
	WoopsaChar8* buffer = server->buffer;

	memset(buffer, 0, sizeof(WoopsaBuffer));

	headerSize = NextHTTPHeader(inputBuffer, &header);
	if (headerSize == -1)
		return WOOPSA_CLIENT_REQUEST_ERROR;

	for (i = 0; i < headerSize && i < sizeof(WoopsaBuffer); i++) {
		if (inputBuffer[i] == ' ') {
			pos = i + 1;
			break;
		}
		buffer[i] = inputBuffer[i];
	}
	if (strcmp(buffer, "POST") == 0)
		isPost = 1;

	for (i = 0; (i < headerSize - pos) && i < sizeof(WoopsaBuffer); i++) {
		if (inputBuffer[pos + i] == ' ')
			break;
		buffer[i] = inputBuffer[pos + i];
	}

	while ((headerSize = NextHTTPHeader(header, &header)) != -1) {
		// Do some work on the headers. We don't need to really do anything now actually
	}

	if ((strstr(buffer, server->pathPrefix)) != buffer) {
		// This request does not start with the prefix, return 404
		*responseLength = PrepareError(outputBuffer, outputBufferLength, HTTP_CODE_NOT_FOUND, HTTP_TEXT_NOT_FOUND);
		return WOOPSA_CLIENT_REQUEST_ERROR;
	}

	woopsaPath = &(buffer[strlen(server->pathPrefix)]);
	if (strstr(woopsaPath, VERB_META) == woopsaPath && isPost == 0) {
		// Meta request
		buffer[0] = '\0';

		// TODO : Do something useful with that path
		woopsaPath = &(woopsaPath[sizeof(VERB_META)]);

		*responseLength = PrepareResponse(outputBuffer, outputBufferLength, HTTP_CODE_OK, HTTP_TEXT_OK, &contentLengthPosition);
		
		contentLength += Append(outputBuffer, JSON_META_START JSON_ARRAY_START, outputBufferLength);
		for(i = 0; server->properties[i].name != NULL; i++) {
			woopsaProperty = &server->properties[i];
			typeEntry = GetTypeEntry(woopsaProperty->type);
			contentLength += Append(outputBuffer, JSON_PROPERTY_NAME, outputBufferLength);
			contentLength += AppendEscape(outputBuffer, woopsaProperty->name, JSON_STRING_DELIMITER_CHAR, JSON_ESCAPE_CHAR, outputBufferLength);
			contentLength += Append(outputBuffer, JSON_PROPERTY_TYPE, outputBufferLength);
			contentLength += Append(outputBuffer, typeEntry->string, outputBufferLength);
			contentLength += Append(outputBuffer, JSON_PROPERTY_READONLY, outputBufferLength);
			contentLength += Append(outputBuffer, (woopsaProperty->readOnly == 1) ? JSON_TRUE : JSON_FALSE, outputBufferLength);
			contentLength += Append(outputBuffer, JSON_PROPERTY_END, outputBufferLength);
			if (server->properties[i + 1].name != NULL)
				contentLength += Append(outputBuffer, JSON_ARRAY_DELIMITER, outputBufferLength);
		}
		contentLength += Append(outputBuffer, JSON_ARRAY_END JSON_META_END, outputBufferLength);
	} else if (strstr(woopsaPath, VERB_READ) == woopsaPath && isPost == 0) {
		// Read request
		buffer[0] = '\0';
		woopsaPath = &(woopsaPath[sizeof(VERB_READ)]);
		if ((woopsaProperty = GetPropertyByNameOrNull(server->properties, woopsaPath)) == NULL) {
			*responseLength = PrepareError(outputBuffer, outputBufferLength, HTTP_CODE_NOT_FOUND, HTTP_TEXT_NOT_FOUND);
			return WOOPSA_CLIENT_REQUEST_ERROR;
		}
		typeEntry = GetTypeEntry(woopsaProperty->type);

		*responseLength = PrepareResponse(outputBuffer, outputBufferLength, HTTP_CODE_OK, HTTP_TEXT_OK, &contentLengthPosition);

		contentLength += Append(outputBuffer, JSON_VALUE_VALUE, outputBufferLength);
		if (woopsaProperty->type == WOOPSA_TYPE_TEXT
			|| woopsaProperty->type == WOOPSA_TYPE_LINK
			|| woopsaProperty->type == WOOPSA_TYPE_RESOURCE_URL
			|| woopsaProperty->type == WOOPSA_TYPE_DATE_TIME) {
			contentLength += Append(outputBuffer, JSON_STRING_DELIMITER, outputBufferLength);
			contentLength += AppendEscape(outputBuffer, (WoopsaChar8*)woopsaProperty->address, JSON_STRING_DELIMITER_CHAR, JSON_ESCAPE_CHAR, outputBufferLength);
			contentLength += Append(outputBuffer, JSON_STRING_DELIMITER, outputBufferLength);
		} else {
			snprintf(numericValueBuffer, MAX_NUMERICAL_VALUE_LENGTH, typeEntry->format, *(woopsaProperty->address));
			contentLength += Append(outputBuffer, numericValueBuffer, outputBufferLength);
		}

		contentLength += Append(outputBuffer, JSON_VALUE_TYPE, outputBufferLength);
		contentLength += Append(outputBuffer, typeEntry->string, outputBufferLength);
		contentLength += Append(outputBuffer, JSON_VALUE_END, outputBufferLength);
	} else if (strstr(woopsaPath, VERB_WRITE) == woopsaPath && isPost == 1) {
		// Write request
		buffer[0] = '\0';
		woopsaPath = &(woopsaPath[sizeof(VERB_READ)]);
		if ((woopsaProperty = GetPropertyByNameOrNull(server->properties, woopsaPath)) == NULL) {
			*responseLength = PrepareError(outputBuffer, outputBufferLength, HTTP_CODE_NOT_FOUND, HTTP_TEXT_NOT_FOUND);
			return WOOPSA_CLIENT_REQUEST_ERROR;
		}
		typeEntry = GetTypeEntry(woopsaProperty->type);

		// Decode POST data
	} else {
		// Invalid request
		*responseLength = PrepareError(outputBuffer, outputBufferLength, HTTP_CODE_NOT_FOUND, HTTP_TEXT_NOT_FOUND);
		return WOOPSA_CLIENT_REQUEST_ERROR;
	}

	SetContentLength(outputBuffer, outputBufferLength, contentLengthPosition, contentLength);
	*responseLength += contentLength;

	return WOOPSA_SUCCESS;
}