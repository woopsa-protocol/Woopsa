#ifndef __WOOPSA_H_
#define __WOOPSA_H_

#include "woopsa-config.h"

typedef unsigned long	WoopsaBufferUInt;
typedef short			WoopsaInt16;
typedef unsigned short	WoopsaUInt16;
typedef char			WoopsaChar8;
typedef unsigned char	WoopsaUInt8;
typedef void *			WoopsaVoidPtr;

typedef WoopsaChar8		WoopsaBuffer[WOOPSA_BUFFER_SIZE];

#ifdef WOOPSA_ENABLE_METHODS
	typedef	void(*ptrMethodVoid)(void);
	typedef char*(*ptrMethodRetString)(void);
	typedef int(*ptrMethodRetInteger)(void);
	typedef float(*ptrMethodRetReal)(void);
#endif

// JsonData is not supported in Woopsa-C
typedef enum {
	WOOPSA_TYPE_NULL,
	WOOPSA_TYPE_LOGICAL,
	WOOPSA_TYPE_INTEGER,
	WOOPSA_TYPE_REAL,
	WOOPSA_TYPE_TIME_SPAN,
#ifdef WOOPSA_ENABLE_STRINGS
	WOOPSA_TYPE_DATE_TIME,
	WOOPSA_TYPE_TEXT,
	WOOPSA_TYPE_LINK,
	WOOPSA_TYPE_RESOURCE_URL
#endif
} WoopsaType;

typedef struct {
	const WoopsaChar8 *	name;
	union 
	{
		void* data;
		ptrMethodVoid function;
	} address;
	WoopsaUInt8 type;
	WoopsaChar8 readOnly;
	WoopsaChar8 isMethod;
	WoopsaUInt8 size;
} WoopsaEntry;

typedef struct {
	// The prefix for all Woopsa routes. Any client request 
	// made without this prefix will pass the request to
	// the handleRequest function, if it exists
	const WoopsaChar8 * pathPrefix;
	// A function pointer to a function that accepts client
	// requests with a specified path and POST.
	// The function is in charge of copying content to the 
	// content string, and should return an amount of bytes.
	// If the amount of bytes is 0, then the server will send
	// a 404 Not Found error.
	WoopsaBufferUInt(*handleRequest)(WoopsaChar8* requestedPath, WoopsaUInt8 isPost, WoopsaChar8* outputBuffer, WoopsaBufferUInt outputBufferSize);
	// A list of entries to be published by Woopsa
	WoopsaEntry*	entries;
	// A small buffer string used in various places in the server
	WoopsaBuffer buffer;
} WoopsaServer;

#define WOOPSA_BEGIN(woopsaDictionaryName) \
	WoopsaEntry woopsaDictionaryName[] = \
	{

#define WOOPSA_END \
	{ (WoopsaChar8 *)NULL, { (void*)NULL }, 0, 0, 0, 0 }};

#define WOOPSA_PROPERTY_CUSTOM(variable, type, readonly) \
	{ #variable, { &variable }, type, readonly, 0, sizeof variable },

#define WOOPSA_PROPERTY_READONLY(variable, type) \
	WOOPSA_PROPERTY_CUSTOM(variable, type, 1)

#define WOOPSA_PROPERTY(variable, type) \
	WOOPSA_PROPERTY_CUSTOM(variable, type, 0)

#ifdef WOOPSA_ENABLE_METHODS
	#define WOOPSA_METHOD(method, returnType) \
{#method, { (void*)method }, returnType, 0, 1, 0 },

	#define WOOPSA_METHOD_VOID(method) \
		WOOPSA_METHOD(method, WOOPSA_TYPE_NULL)
#endif

#ifdef __cplusplus
extern "C"
{
#endif

// Creates a new Woopsa server using the specified prefix 
// and a list of entries to publish
	void WoopsaServerInit(WoopsaServer* server, const WoopsaChar8* prefix, WoopsaEntry entries[], WoopsaBufferUInt(*handleRequest)(WoopsaChar8*, WoopsaUInt8, WoopsaChar8*, WoopsaBufferUInt));

// Checks if the request contained in inputBuffer
// is finished. This is useful in the case where
// data is received in fragments for some reason.
// You should always call this method on your buffer
// before passing it to WoopsaHandleRequest
#define WOOPSA_REQUEST_MORE_DATA_NEEDED 0
#define WOOPSA_REQUEST_COMLETE 1
WoopsaUInt8	WoopsaCheckRequestComplete(WoopsaServer* server, WoopsaChar8* inputBuffer, WoopsaUInt16 inputBufferLength);

// Parses a request and prepares the reply as well
// Returns:
//  WOOPSA_SUCCESS (0) = success
//  WOOPSA_CLIENT_REQUEST_ERROR (1) = the client made a bad request
//  WOPOSA_OTHER_ERROR (2) = something wrong happened inside Woopsa (it's our fault)
#define WOOPSA_SUCCESS 0
#define WOOPSA_CLIENT_REQUEST_ERROR 1
#define WOOPSA_OTHER_ERROR 2
#define WOOPSA_OTHER_RESPONSE 3

WoopsaUInt8	WoopsaHandleRequest(WoopsaServer* server, const WoopsaChar8* inputBuffer, WoopsaBufferUInt inputBufferLength, WoopsaChar8* outputBuffer, WoopsaBufferUInt outputBufferLength, WoopsaBufferUInt* responseLength);

#ifdef __cplusplus
}
#endif

#endif