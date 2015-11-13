#ifndef __WOOPSA_H_
#define __WOOPSA_H_

#include "woopsa-config.h"

typedef short			WoopsaInt16;
typedef unsigned short	WoopsaUInt16;
typedef char			WoopsaChar8;
typedef unsigned char	WoopsaUInt8;

// JsonData is not supported in Woopsa-C
typedef enum
{
	WOOPSA_TYPE_NULL,
	WOOPSA_TYPE_INTEGER,
	WOOPSA_TYPE_REAL,	
	WOOPSA_TYPE_DATE_TIME,
	WOOPSA_TYPE_TIME_SPAN,
	WOOPSA_TYPE_TEXT,
	WOOPSA_TYPE_LINK,
	WOOPSA_TYPE_RESOURCE_URL
} WoopsaType;

typedef struct
{
	const char *	name;
	void *			address;
	unsigned char	type;
	char			readOnly;
	unsigned char	size;
} WoopsaProperty;

typedef struct
{
	char *			pathPrefix;
	WoopsaProperty*	properties;
} WoopsaServer;

#define WOOPSA_BEGIN(woopsaDictionaryName) \
	WoopsaProperty woopsaDictionaryName[] = \
	{

#define WOOPSA_END \
	{ 0, 0, 0, 0, 0}};

#define WOOPSA_PROPERTY_CUSTOM(variable, type, readonly) \
	{ #variable, &variable, type, readonly, sizeof variable }

#define WOOPSA_PROPERTY_READONLY(variable, type) \
	WOOPSA_PROPERTY_CUSTOM(variable, type, 1)

#define WOOPSA_PROPERTY(variable, type) \
	WOOPSA_PROPERTY_CUSTOM(variable, type, 0)

#ifdef __cplusplus
extern "C"
{
#endif

// Creates a new Woopsa server using the specified prefix 
// and a list of properties to publish
WoopsaServer	WoopsaInit(char* prefix, WoopsaProperty properties[]);

// Checks if the request contained in inputBuffer
// is finished. This is useful in the case where
// data is received in fragments for some reason.
// You should always call this method on your buffer
// before passing it to WoopsaHandleRequest
WoopsaChar8		WoopsaCheckRequestFinished(WoopsaChar8* inputBuffer, WoopsaUInt16 inputBufferLength);

// Parses a request and prepares the reply as well
// Returns:
//  0 = success
//  1 = client request error
//  2 = some other error
WoopsaChar8*	WoopsaHandleRequest(WoopsaServer server, WoopsaChar8* inputBuffer, WoopsaUInt16 inputBufferLength, WoopsaChar8* outputBuffer, WoopsaUInt16 outputBufferLength, WoopsaUInt16* responseLength);

#ifdef __cplusplus
}
#endif

#endif