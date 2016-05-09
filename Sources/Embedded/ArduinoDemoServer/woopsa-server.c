#include "woopsa-server.h"
#include <string.h>
#include <stdio.h>



// Woopsa constants
#define VERB_META	"meta"
#define VERB_READ	"read"
#define VERB_WRITE	"write"
#define VERB_INVOKE "invoke"

#define TYPE_STRING_NULL            "Null"
#define TYPE_STRING_LOGICAL			"Logical"
#define TYPE_STRING_INTEGER         "Integer"
#define TYPE_STRING_REAL            "Real"
#define TYPE_STRING_DATE_TIME       "DateTime"
#define TYPE_STRING_TIME_SPAN       "TimeSpan"
#define TYPE_STRING_TEXT            "Text"
#define TYPE_STRING_LINK            "Link"
#define TYPE_STRING_RESOURCE_URL    "ResourceUrl"

typedef struct {
	WoopsaType	type;
	WoopsaChar8* string;
} TypesDictionaryEntry;

TypesDictionaryEntry Types[] = {
	{ WOOPSA_TYPE_NULL, TYPE_STRING_NULL },
	{ WOOPSA_TYPE_LOGICAL, TYPE_STRING_LOGICAL },
	{ WOOPSA_TYPE_INTEGER, TYPE_STRING_INTEGER },
	{ WOOPSA_TYPE_REAL, TYPE_STRING_REAL },
	{ WOOPSA_TYPE_TIME_SPAN, TYPE_STRING_TIME_SPAN },
#ifdef WOOPSA_ENABLE_STRINGS
	{ WOOPSA_TYPE_DATE_TIME, TYPE_STRING_DATE_TIME },
	{ WOOPSA_TYPE_TEXT, TYPE_STRING_TEXT },
	{ WOOPSA_TYPE_LINK, TYPE_STRING_LINK },
	{ WOOPSA_TYPE_RESOURCE_URL, TYPE_STRING_RESOURCE_URL }
#endif
};

// HTTP constants
#define HTTP_METHOD_GET "GET"
#define HTTP_METHOD_POST "POST"
#define HTTP_VERSION_STRING "HTTP/1.1"

#define HTTP_CODE_BAD_REQUEST "400"
#define HTTP_TEXT_BAD_REQUEST "Bad request"

#define HTTP_CODE_NOT_FOUND "404"
#define HTTP_TEXT_NOT_FOUND "Not found"

#define HTTP_CODE_INTERNAL_ERROR "500"
#define HTTP_TEXT_INTERNAL_ERROR "Internal server error"

#define HTTP_CODE_NOT_IMPLEMENTED "501"
#define HTTP_TEXT_METHOD_NOT_IMPLEMENTED "%s method not implemented"

#define HTTP_CODE_OK "200"
#define HTTP_TEXT_OK "OK"

#define HEADER_SEPARATOR "\r\n"
#define HEADER_VALUE_SEPARATOR ":"
#define HEADER_CONTENT_LENGTH "content-length"
#define HEADER_CONTENT_LENGTH_SPACE "        "
#define HEADER_CONTENT_LENGTH_PADDING 8
#define HEADER_CONTENT_TYPE "Content-Type: "
#define EXTRA_HEADERS "Access-Control-Allow-Origin: *" HEADER_SEPARATOR "Connection: close" HEADER_SEPARATOR
#define CONTENT_TYPE_JSON "application/json"
#define CONTENT_TYPE_HTML "text/html"

// POST constants
#define POST_VALUE_KEY "value"
#define URLENCODE_KEY_SEPARATOR '&'
#define URLENCODE_VALUE_SEPARATOR '='
#define URLENCODE_VALUE_ENCODER '%'

// Serialization constants
#define JSON_VALUE_VALUE "{\"Value\":"
#define JSON_VALUE_TYPE ",\"Type\":\""
#define JSON_VALUE_END "\"}"
#define JSON_META_PROPERTIES "{\"Name\":\"Root\",\"Properties\":"
#define JSON_META_METHODS ",\"Methods\":"
#define JSON_META_END ",\"Items\":[]}"
#define JSON_PROPERTY_NAME "{\"Name\":\""
#define JSON_PROPERTY_TYPE "\",\"Type\":\""
#define JSON_PROPERTY_READONLY "\",\"ReadOnly\":"
#define JSON_PROPERTY_END "}"
#define JSON_METHOD_NAME "{\"Name\":\""
#define JSON_METHOD_RETURN_TYPE "\",\"ReturnType\":\""
#define JSON_METHOD_END "\",\"ArgumentInfos\":[]}"
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
// Returns a pointer to the WoopsaEntry, or null if not found
WoopsaEntry* GetPropertyByNameOrNull(WoopsaEntry entries[], WoopsaChar8 name[]) {
	WoopsaUInt16 i = 0;
	while (entries[i].name != NULL) {
		if (WOOPSA_STRING_EQUAL(entries[i].name, name) && entries[i].isMethod == 0)
			return &entries[i];
		i++;
	}
	return NULL;
}

#ifdef WOOPSA_ENABLE_METHODS
// Gets a Woopsa Method by name
// Returns a pointer to the WoopsaEntry, or null if not found
WoopsaEntry* GetMethodByNameOrNull(WoopsaEntry entries[], WoopsaChar8 name[]) {
	WoopsaUInt16 i = 0;
	while (entries[i].name != NULL) {
		if (WOOPSA_STRING_EQUAL(entries[i].name, name) && entries[i].isMethod == 1)
			return &entries[i];
		i++;
	}
	return NULL;
}
#endif

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
	WoopsaInt16 i = 0, carriageFound = 0;
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

// Finds the next key/value pair in a URLEncoded string
// and decodes the actual value. Note: keys are lowercased
// Returns the length of the *current* key/value pair, or -1 if none found
WoopsaInt16 NextURLDecodedValue(const WoopsaChar8* searchString, WoopsaChar8* key, WoopsaChar8* value) {
	WoopsaInt16 i = 0, inKey = 1, keyAt = 0, valueAt = 0;
	WoopsaUInt8 specialCharAt = 0, inSpecialChar = 0;
	WoopsaChar8 charBuff[2] = { 0, 0 };
	WoopsaChar8 specialChar = '\0', curChar = '\0';
	for (i = 0; searchString[i] != '\0' ; i++) {
		if (searchString[i] == URLENCODE_VALUE_ENCODER){
			// We're entering a %-encoded value
			inSpecialChar = 1;
			continue;
		} else if (inSpecialChar && specialCharAt < 2 ) {
			// We're inside a %-encoded value, add to the buffer
			curChar = WOOPSA_CHAR_TO_LOWER(searchString[i]);
			if ((curChar >= '0' && curChar <= '9') || (curChar >= 'a' && curChar <= 'f'))
				charBuff[specialCharAt++] = curChar;
			else
				charBuff[specialCharAt++] = '4'; // must be something.. just not null!
			continue;
		} else if (inSpecialChar && specialCharAt == 2) {
			// We're out of the %-encoded value - convert it to a char
			if (charBuff[0] >= 'a')
				specialChar = (charBuff[0] - 'a' + 0xa) * 0x10;
			else
				specialChar = (charBuff[0] - '0') * 0x10;
			if (charBuff[1] >= 'a') 
				specialChar += charBuff[1] - 'a' + 0xa;
			else
				specialChar += charBuff[1] - '0';
			if (inKey) 
				key[keyAt++] = specialChar;
			else
				value[valueAt++] = specialChar;
			inSpecialChar = 0;
			specialCharAt = 0;
		}
		curChar = searchString[i];
		if (curChar == URLENCODE_VALUE_SEPARATOR) {
			inKey = 0;
			continue;
		}
		if (curChar == URLENCODE_KEY_SEPARATOR) {
			key[keyAt] = '\0';
			value[valueAt] = '\0';
			return i;
		}
		if (curChar == '+')
			curChar = ' ';
		if (inKey == 1)
			key[keyAt++] = WOOPSA_CHAR_TO_LOWER(curChar);
		else
			value[valueAt++] = curChar;
	}
	if (keyAt > 0 || valueAt > 0) {
		key[keyAt] = '\0';
		value[valueAt] = '\0';
		return i;
	} else {
		return -1;
	}
}


// Appends source to destination, while keeping destination under num length
// Returns amount of characters actually appended
WoopsaBufferUInt Append(WoopsaChar8 destination[], const WoopsaChar8 source[], WoopsaBufferUInt num) {
	WoopsaBufferUInt i = 0, cnt = 0;
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
WoopsaBufferUInt AppendEscape(WoopsaChar8 destination[], const WoopsaChar8 source[], WoopsaChar8 special, WoopsaChar8 escape, WoopsaBufferUInt num) {
	WoopsaBufferUInt i = 0, cnt = 0, extras = 0;
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
WoopsaBufferUInt PrepareResponse(
		WoopsaChar8* outputBuffer,
		const WoopsaBufferUInt outputBufferLength,
		const WoopsaChar8* httpStatusCode,
		const WoopsaChar8* httpStatusStr,
		WoopsaBufferUInt* contentLengthPosition,
		const WoopsaChar8* contentType
		) {
	WoopsaBufferUInt size = 0;
	outputBuffer[0] = '\0';
	// HTTP/1.1
	size = Append(outputBuffer, HTTP_VERSION_STRING " ", outputBufferLength);
	// 200 OK
	size += Append(outputBuffer, httpStatusCode, outputBufferLength);
	size += Append(outputBuffer, " ", outputBufferLength);
	size += Append(outputBuffer, httpStatusStr, outputBufferLength);
	size += Append(outputBuffer, HEADER_SEPARATOR, outputBufferLength);
	// Content type
	size += Append(outputBuffer, HEADER_CONTENT_TYPE, outputBufferLength);
	size += Append(outputBuffer, contentType, outputBufferLength);
	size += Append(outputBuffer, HEADER_SEPARATOR, outputBufferLength);
	// Extra headers
	size += Append(outputBuffer, EXTRA_HEADERS, outputBufferLength);
	// Content-Length:
	size += Append(outputBuffer, HEADER_CONTENT_LENGTH HEADER_VALUE_SEPARATOR, outputBufferLength);
	*contentLengthPosition = size;
	// We leave a few spaces for the Content-Length
	size += Append(outputBuffer, HEADER_CONTENT_LENGTH_SPACE, outputBufferLength);
	// Final double new lines
	size += Append(outputBuffer, HEADER_SEPARATOR HEADER_SEPARATOR, outputBufferLength);
	return size;
}

void SetContentLength(WoopsaChar8* outputBuffer, const WoopsaBufferUInt outputBufferLength, WoopsaBufferUInt contentLengthPosition, WoopsaBufferUInt contentLength) {
	if (contentLengthPosition + 8 > outputBufferLength)
		return;
	WOOPSA_INTEGER_TO_PADDED_STRING((int)contentLength, outputBuffer + contentLengthPosition, 8);
	// snprintf usually adds a null byte, remove it and put a \r instead
	outputBuffer[contentLengthPosition + WOOPSA_STRING_LENGTH(HEADER_CONTENT_LENGTH_SPACE)] = '\r';
}

WoopsaBufferUInt PrepareResponseWithContent(
		WoopsaChar8* outputBuffer,
		const WoopsaBufferUInt outputBufferLength,
		const WoopsaChar8* httpStatusCode,
		const WoopsaChar8* httpStatusStr,
		const WoopsaChar8* content) {
	WoopsaBufferUInt len, pos, contentLength;
	len = PrepareResponse(outputBuffer, outputBufferLength, httpStatusCode, httpStatusStr, &pos, CONTENT_TYPE_JSON);
	contentLength = WOOPSA_STRING_LENGTH(content);
	SetContentLength(outputBuffer, outputBufferLength, pos, contentLength);
	len += Append(outputBuffer, content, outputBufferLength);
	return len;
}

// Shortcut to generate an HTTP error. The contents
// of the response is the error string itself.
WoopsaBufferUInt PrepareError(WoopsaChar8* outputBuffer, const WoopsaBufferUInt outputBufferLength, WoopsaChar8 errorCode[], const WoopsaChar8 errorStr[]) {
	return PrepareResponseWithContent(outputBuffer, outputBufferLength, errorCode, errorStr, errorStr);
}

void StringToLower(WoopsaChar8 str[]) {
	WoopsaBufferUInt i = 0;
	for (i = 0; str[i]; i++) {
		str[i] = WOOPSA_CHAR_TO_LOWER(str[i]);
	}
}

WoopsaBufferUInt OutputSerializedValue(WoopsaChar8* outputBuffer, const WoopsaBufferUInt outputBufferLength, WoopsaChar8 stringValue[], WoopsaChar8 typeString[], WoopsaChar8 isStringValue) {
	WoopsaBufferUInt contentLength = 0;
	contentLength += Append(outputBuffer, JSON_VALUE_VALUE, outputBufferLength);
#ifdef WOOPSA_ENABLE_STRINGS
	if (isStringValue) {
		contentLength += Append(outputBuffer, JSON_STRING_DELIMITER, outputBufferLength);
		contentLength += AppendEscape(outputBuffer, stringValue, JSON_STRING_DELIMITER_CHAR, JSON_ESCAPE_CHAR, outputBufferLength);
		contentLength += Append(outputBuffer, JSON_STRING_DELIMITER, outputBufferLength);
	} else {
#endif
		contentLength += Append(outputBuffer, stringValue, outputBufferLength);
#ifdef WOOPSA_ENABLE_STRINGS
	}
#endif
	contentLength += Append(outputBuffer, JSON_VALUE_TYPE, outputBufferLength);
	contentLength += Append(outputBuffer, typeString, outputBufferLength);
	contentLength += Append(outputBuffer, JSON_VALUE_END, outputBufferLength);
	return contentLength;
}

WoopsaBufferUInt OutputProperty(WoopsaChar8* outputBuffer, const WoopsaBufferUInt outputBufferLength, WoopsaEntry* woopsaEntry, TypesDictionaryEntry* typeEntry, WoopsaChar8 numericValueBuffer[]) {
	WoopsaBufferUInt contentLength = 0;
#ifdef WOOPSA_ENABLE_STRINGS
	if (woopsaEntry->type == WOOPSA_TYPE_TEXT
		|| woopsaEntry->type == WOOPSA_TYPE_LINK
		|| woopsaEntry->type == WOOPSA_TYPE_RESOURCE_URL
		|| woopsaEntry->type == WOOPSA_TYPE_DATE_TIME) {
		WOOPSA_LOCK
			contentLength += OutputSerializedValue(outputBuffer, outputBufferLength, (WoopsaChar8*)woopsaEntry->address.data, typeEntry->string, 1);
		WOOPSA_UNLOCK
	} else if ( woopsaEntry->type == WOOPSA_TYPE_LOGICAL ){
		WOOPSA_LOCK
			contentLength += OutputSerializedValue(outputBuffer, outputBufferLength, *(WoopsaChar8*)(woopsaEntry->address.data)?JSON_TRUE:JSON_FALSE, typeEntry->string, 0);
		WOOPSA_UNLOCK
	} else {
#endif
		WOOPSA_LOCK
		if (woopsaEntry->type == WOOPSA_TYPE_INTEGER)
			WOOPSA_INTEGER_TO_STRING(*(int*)woopsaEntry->address.data, numericValueBuffer, MAX_NUMERICAL_VALUE_LENGTH);
		else
			WOOPSA_REAL_TO_STRING(*(float*)(woopsaEntry->address.data), numericValueBuffer, MAX_NUMERICAL_VALUE_LENGTH);
		WOOPSA_UNLOCK
		contentLength += OutputSerializedValue(outputBuffer, outputBufferLength, numericValueBuffer, typeEntry->string, 0);
#ifdef WOOPSA_ENABLE_STRINGS
	}
#endif
	return contentLength;
}

///////////////////////////////////////////////////////////////////////////////
//                   BEGIN PUBLIC WOOPSA IMPLEMENTATION                      //
///////////////////////////////////////////////////////////////////////////////

void WoopsaServerInit(WoopsaServer* server, const WoopsaChar8* prefix, WoopsaEntry entries[], WoopsaBufferUInt(*handleRequest)(WoopsaChar8*, WoopsaUInt8, WoopsaChar8*, WoopsaBufferUInt)) {
	server->pathPrefix = prefix;
	server->entries = entries;
	server->handleRequest = handleRequest;
}

WoopsaUInt8 WoopsaCheckRequestComplete(WoopsaServer* server, WoopsaChar8* inputBuffer, WoopsaUInt16 inputBufferLength) {
	WoopsaUInt8 contentLengthLength = 0;
	WoopsaChar8 *buffer = server->buffer, *header = NULL, *contentLengthPosition = NULL, *contentPosition = NULL;
	WoopsaBufferUInt headerSize = 0;
	WoopsaBufferUInt contentLength = 0, i = 0;
	// Zero-out the buffer
	memset(buffer, 0, sizeof(WoopsaBuffer));
	header = inputBuffer;
	while ((headerSize = NextHTTPHeader(header, &header)) != -1 && header < inputBuffer + inputBufferLength) {
		WOOPSA_STRING_N_COPY(buffer, header, headerSize); 
		StringToLower(buffer);
		// Is there a "Content-Length" header?
		if (WOOPSA_STRING_POSITION(buffer, HEADER_CONTENT_LENGTH) == buffer) {
			// Yes, find the content-length
			for (i = 0; buffer[i] != '\r'; i++) {
				if (contentLengthPosition == NULL && buffer[i] == ':') {
					contentLengthPosition = buffer + i;
				} else if (contentLengthPosition != NULL && buffer[i] == ' ') {
					contentLengthPosition++;
				} else if (contentLengthPosition != NULL && buffer[i] != ' ') {
					contentLengthLength++;
				}
			}
			contentLengthPosition[contentLengthLength+1] = '\0';
			WOOPSA_STRING_TO_INTEGER(contentLength, contentLengthPosition);
			break;
		}
	}
	if (contentLength == 0) {
		// If there is no content and we have the terminator, all done.
		if (WOOPSA_STRING_POSITION(inputBuffer, HEADER_SEPARATOR HEADER_SEPARATOR))
			return WOOPSA_REQUEST_COMLETE;
		else
			return WOOPSA_REQUEST_MORE_DATA_NEEDED;
	} else {
		// Otherwise it's a bit more technical, we have to make sure all
		// the content is there
		contentPosition = WOOPSA_STRING_POSITION(inputBuffer, HEADER_SEPARATOR HEADER_SEPARATOR) + WOOPSA_STRING_LENGTH(HEADER_SEPARATOR HEADER_SEPARATOR);
		if (WOOPSA_STRING_LENGTH(contentPosition) == contentLength)
			return WOOPSA_REQUEST_COMLETE;
		else
			return WOOPSA_REQUEST_MORE_DATA_NEEDED;
	}
}

WoopsaUInt8 WoopsaHandleRequest(WoopsaServer* server, const WoopsaChar8* inputBuffer, WoopsaBufferUInt inputBufferLength, WoopsaChar8* outputBuffer, WoopsaBufferUInt outputBufferLength, WoopsaBufferUInt* responseLength) {
	WoopsaChar8* header = NULL;
	WoopsaChar8* woopsaPath = NULL;
	WoopsaChar8* requestContent = NULL;
	WoopsaChar8 numericValueBuffer[MAX_NUMERICAL_VALUE_LENGTH];
	WoopsaBufferUInt contentLengthPosition = 0;
	WoopsaBufferUInt contentLength = 0, i = 0, pos = 0;
	WoopsaUInt8 isPost = 0, valueFound = 0, entryAt = 0;
	WoopsaBufferUInt headerSize = 0, keypairSize = 0;
	WoopsaEntry* woopsaEntry = NULL;
	TypesDictionaryEntry* typeEntry = NULL;
	WoopsaChar8* buffer = server->buffer;
	// Zero-out the buffers
	memset(buffer, 0, sizeof(WoopsaBuffer));
	memset(numericValueBuffer, 0, MAX_NUMERICAL_VALUE_LENGTH);
	// Parse the first header (GET/POST)
	headerSize = NextHTTPHeader(inputBuffer, &header);
	if (headerSize == -1)
		return WOOPSA_CLIENT_REQUEST_ERROR;
	// Copy the HTTP method in the buffer and check if POST
	for (i = 0; i < headerSize && i < sizeof(WoopsaBuffer); i++) {
		if (inputBuffer[i] == ' ') {
			pos = i + 1;
			break;
		}
		buffer[i] = inputBuffer[i];
	}
	if (WOOPSA_STRING_EQUAL(buffer, "POST"))
		isPost = 1;
	// Extract the requested path into the buffer
	for (i = 0; (i < headerSize - pos) && i < sizeof(WoopsaBuffer); i++) {
		if (inputBuffer[pos + i] == ' ')
			break;
		buffer[i] = inputBuffer[pos + i];
	}
	// Extract all headers (but do nothing with them)
	while ((headerSize = NextHTTPHeader(header, &header)) != -1 && header < inputBuffer + inputBufferLength) {
		// Do some work on the headers. We don't need to really do anything now actually
	}
	// Check if the path is a Woopsa path
	if ((WOOPSA_STRING_POSITION(buffer, server->pathPrefix)) != buffer) {
		// It's not, so we try to handle it with the handleRequest func pointer
		if (server->handleRequest != NULL) {
			*responseLength = PrepareResponse(outputBuffer, outputBufferLength, HTTP_CODE_OK, HTTP_TEXT_OK, &contentLengthPosition, CONTENT_TYPE_HTML);
			contentLength = server->handleRequest(buffer, isPost, outputBuffer + *responseLength, outputBufferLength - *responseLength);
			if (contentLength == 0) {
				*responseLength = PrepareError(outputBuffer, outputBufferLength, HTTP_CODE_NOT_FOUND, HTTP_TEXT_NOT_FOUND);
				return WOOPSA_CLIENT_REQUEST_ERROR;
			} else {
				SetContentLength(outputBuffer, outputBufferLength, contentLengthPosition, contentLength);
				*responseLength += contentLength;
				return WOOPSA_OTHER_RESPONSE;
			}
		} else {
			// This request does not start with the prefix, return 404
			*responseLength = PrepareError(outputBuffer, outputBufferLength, HTTP_CODE_NOT_FOUND, HTTP_TEXT_NOT_FOUND);
			return WOOPSA_CLIENT_REQUEST_ERROR;
		}
	}
	// Remove the Woopsa prefix and handle each Woopsa verb
	woopsaPath = &(buffer[WOOPSA_STRING_LENGTH(server->pathPrefix)]);
	if (WOOPSA_STRING_POSITION(woopsaPath, VERB_META) == woopsaPath && isPost == 0) {
		// Meta request, start the HTTP response
		*responseLength = PrepareResponse(outputBuffer, outputBufferLength, HTTP_CODE_OK, HTTP_TEXT_OK, &contentLengthPosition, CONTENT_TYPE_JSON);
		// Output the serialized response
		contentLength += Append(outputBuffer, JSON_META_PROPERTIES JSON_ARRAY_START, outputBufferLength);
		for(i = 0; server->entries[i].name != NULL; i++) {
			woopsaEntry = &server->entries[i];
			if (woopsaEntry->isMethod)
				continue;
			if ( entryAt != 0 )
				contentLength += Append(outputBuffer, JSON_ARRAY_DELIMITER, outputBufferLength);
			entryAt++;
			typeEntry = GetTypeEntry(woopsaEntry->type);
			contentLength += Append(outputBuffer, JSON_PROPERTY_NAME, outputBufferLength);
			contentLength += AppendEscape(outputBuffer, woopsaEntry->name, JSON_STRING_DELIMITER_CHAR, JSON_ESCAPE_CHAR, outputBufferLength);
			contentLength += Append(outputBuffer, JSON_PROPERTY_TYPE, outputBufferLength);
			contentLength += Append(outputBuffer, typeEntry->string, outputBufferLength);
			contentLength += Append(outputBuffer, JSON_PROPERTY_READONLY, outputBufferLength);
			contentLength += Append(outputBuffer, (woopsaEntry->readOnly == 1) ? JSON_TRUE : JSON_FALSE, outputBufferLength);
			contentLength += Append(outputBuffer, JSON_PROPERTY_END, outputBufferLength);
		}
		contentLength += Append(outputBuffer, JSON_ARRAY_END JSON_META_METHODS JSON_ARRAY_START, outputBufferLength);
#ifdef WOOPSA_ENABLE_METHODS
		entryAt = 0;
		for (i = 0; server->entries[i].name != NULL; i++) {
			woopsaEntry = &server->entries[i];
			if (!woopsaEntry->isMethod)
				continue;
			if (entryAt != 0)
				contentLength += Append(outputBuffer, JSON_ARRAY_DELIMITER, outputBufferLength);
			entryAt++;
			typeEntry = GetTypeEntry(woopsaEntry->type);
			contentLength += Append(outputBuffer, JSON_METHOD_NAME, outputBufferLength);
			contentLength += AppendEscape(outputBuffer, woopsaEntry->name, JSON_STRING_DELIMITER_CHAR, JSON_ESCAPE_CHAR, outputBufferLength);
			contentLength += Append(outputBuffer, JSON_METHOD_RETURN_TYPE, outputBufferLength);
			contentLength += Append(outputBuffer, typeEntry->string, outputBufferLength);
			contentLength += Append(outputBuffer, JSON_METHOD_END, outputBufferLength);
		}
#endif
		contentLength += Append(outputBuffer, JSON_ARRAY_END JSON_META_END, outputBufferLength);
	} else if (WOOPSA_STRING_POSITION(woopsaPath, VERB_READ) == woopsaPath && isPost == 0) {
		// Read request - Get the property for this read
		buffer[0] = '\0';
		woopsaPath = &(woopsaPath[sizeof(VERB_READ)]);
		if ((woopsaEntry = GetPropertyByNameOrNull(server->entries, woopsaPath)) == NULL) {
			*responseLength = PrepareError(outputBuffer, outputBufferLength, HTTP_CODE_NOT_FOUND, HTTP_TEXT_NOT_FOUND);
			return WOOPSA_CLIENT_REQUEST_ERROR;
		}
		typeEntry = GetTypeEntry(woopsaEntry->type);
		// Start the HTTP response
		*responseLength = PrepareResponse(outputBuffer, outputBufferLength, HTTP_CODE_OK, HTTP_TEXT_OK, &contentLengthPosition, CONTENT_TYPE_JSON);
		// Output the serialized response
		contentLength += OutputProperty(outputBuffer, outputBufferLength, woopsaEntry, typeEntry, numericValueBuffer);
	} else if (WOOPSA_STRING_POSITION(woopsaPath, VERB_WRITE) == woopsaPath && isPost == 1) {
		// Write request - Get the property for this write
		buffer[0] = '\0';
		woopsaPath = &(woopsaPath[sizeof(VERB_WRITE)]);
		if ((woopsaEntry = GetPropertyByNameOrNull(server->entries, woopsaPath)) == NULL) {
			*responseLength = PrepareError(outputBuffer, outputBufferLength, HTTP_CODE_NOT_FOUND, HTTP_TEXT_NOT_FOUND);
			return WOOPSA_CLIENT_REQUEST_ERROR;
		}
		typeEntry = GetTypeEntry(woopsaEntry->type);
		// Decode POST data
		requestContent = WOOPSA_STRING_POSITION(inputBuffer, "\r\n\r\n");
		requestContent = &requestContent[4];
		// Cheating here - using the numericValueBuffer to store the key (10 chars should be enough)
		while ((keypairSize = NextURLDecodedValue(&requestContent[keypairSize], numericValueBuffer, buffer)) != -1) {
			if (WOOPSA_STRING_EQUAL(numericValueBuffer, POST_VALUE_KEY)) {
				valueFound = 1;
			}
		}
		if (!valueFound) {
			*responseLength = PrepareError(outputBuffer, outputBufferLength, HTTP_CODE_BAD_REQUEST, HTTP_TEXT_BAD_REQUEST);
			return WOOPSA_CLIENT_REQUEST_ERROR;
		}
		// Write the value
		if (woopsaEntry->type == WOOPSA_TYPE_INTEGER) {
			WOOPSA_LOCK
				WOOPSA_STRING_TO_INTEGER(*(int*)woopsaEntry->address.data, buffer);
			WOOPSA_UNLOCK
		} else if (woopsaEntry->type == WOOPSA_TYPE_REAL || woopsaEntry->type == WOOPSA_TYPE_TIME_SPAN) {
			WOOPSA_LOCK
				WOOPSA_STRING_TO_FLOAT(*(float*)woopsaEntry->address.data, buffer);
			WOOPSA_UNLOCK
		} 
		else if (woopsaEntry->type == WOOPSA_TYPE_LOGICAL) {
			StringToLower(buffer);
			WOOPSA_LOCK
			if (WOOPSA_STRING_EQUAL(buffer, JSON_TRUE))
				*(char*)woopsaEntry->address.data = 1;
			else
				*(char*)woopsaEntry->address.data = 0;
			WOOPSA_UNLOCK
		}
#ifdef WOOPSA_ENABLE_STRINGS
		else 
		{
			if (woopsaEntry->size > WOOPSA_STRING_LENGTH(buffer)) {
				WOOPSA_LOCK
					WOOPSA_STRING_COPY((char*)woopsaEntry->address.data, buffer);
				WOOPSA_UNLOCK
			} else {
				*responseLength = PrepareError(outputBuffer, outputBufferLength, HTTP_CODE_BAD_REQUEST, HTTP_TEXT_BAD_REQUEST);
				return WOOPSA_CLIENT_REQUEST_ERROR;
			}
		}
#endif
		// Start the HTTP response
		*responseLength = PrepareResponse(outputBuffer, outputBufferLength, HTTP_CODE_OK, HTTP_TEXT_OK, &contentLengthPosition, CONTENT_TYPE_JSON);
		// Output the serialized response
		contentLength += OutputProperty(outputBuffer, outputBufferLength, woopsaEntry, typeEntry, numericValueBuffer);
	} 
#ifdef WOOPSA_ENABLE_METHODS
	else if (WOOPSA_STRING_POSITION(woopsaPath, VERB_INVOKE) == woopsaPath && isPost == 1) 
	{
		// Write request - Get the property for this write
		buffer[0] = '\0';
		woopsaPath = &(woopsaPath[sizeof(VERB_INVOKE)]);
		if ((woopsaEntry = GetMethodByNameOrNull(server->entries, woopsaPath)) == NULL) {
			*responseLength = PrepareError(outputBuffer, outputBufferLength, HTTP_CODE_NOT_FOUND, HTTP_TEXT_NOT_FOUND);
			return WOOPSA_CLIENT_REQUEST_ERROR;
		}
		typeEntry = GetTypeEntry(woopsaEntry->type);
		// Start the HTTP response
		*responseLength = PrepareResponse(outputBuffer, outputBufferLength, HTTP_CODE_OK, HTTP_TEXT_OK, &contentLengthPosition, CONTENT_TYPE_JSON);
		// Invoke the method
		if (woopsaEntry->type == WOOPSA_TYPE_NULL) {
			(*(ptrMethodVoid)woopsaEntry->address.function)();
			contentLength = 0;
		} else {
			if (woopsaEntry->type == WOOPSA_TYPE_TEXT
				|| woopsaEntry->type == WOOPSA_TYPE_LINK
				|| woopsaEntry->type == WOOPSA_TYPE_RESOURCE_URL
				|| woopsaEntry->type == WOOPSA_TYPE_DATE_TIME) {
				WOOPSA_LOCK
					contentLength += OutputSerializedValue(outputBuffer, outputBufferLength, (*(ptrMethodRetString)woopsaEntry->address.function)(), typeEntry->string, 1);
				WOOPSA_UNLOCK
			} else {
				if (woopsaEntry->type == WOOPSA_TYPE_INTEGER) {
					WOOPSA_LOCK
						WOOPSA_INTEGER_TO_STRING((*(ptrMethodRetInteger)woopsaEntry->address.function)(), numericValueBuffer, MAX_NUMERICAL_VALUE_LENGTH);
					WOOPSA_UNLOCK
				} else {
					WOOPSA_LOCK
						WOOPSA_REAL_TO_STRING((*(ptrMethodRetReal)woopsaEntry->address.function)(), numericValueBuffer, MAX_NUMERICAL_VALUE_LENGTH);
					WOOPSA_UNLOCK
				}
				contentLength += OutputSerializedValue(outputBuffer, outputBufferLength, numericValueBuffer, typeEntry->string, 0);
			}
		}
	} 
#endif
	else 
	{
		// Invalid request
		*responseLength = PrepareError(outputBuffer, outputBufferLength, HTTP_CODE_NOT_FOUND, HTTP_TEXT_NOT_FOUND);
		return WOOPSA_CLIENT_REQUEST_ERROR;
	}
	// Re-inject the content-length into the HTTP headers
	SetContentLength(outputBuffer, outputBufferLength, contentLengthPosition, contentLength);
	*responseLength += contentLength;
	return WOOPSA_SUCCESS;
}
