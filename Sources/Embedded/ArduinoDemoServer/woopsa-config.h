#ifndef __WOOPSA_CONFIG_H_
#define __WOOPSA_CONFIG_H_

#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <ctype.h>


// Woopsa uses these internally, allowing you to use Woopsa in a
// thread-safe manner, or disabling interrupts. Just fill this in
// if needed with whatever locking mechanism your environment has.
#define WOOPSA_LOCK				// disable interrupts
#define WOOPSA_UNLOCK			// enable interrupts

// If you are on a system with very low memory, you can reduce the
// buffer size that the Woopsa server uses internally.
// This value changes the maximum length of URLs you can parse.
// You should not go under 128 bytes to be 100% safe.
#define WOOPSA_BUFFER_SIZE 256

// Thanks microsoft for not supporting snprintf!
#if defined(_MSC_VER) && _MSC_VER < 1900
#define snprintf _snprintf
#endif

#define WOOPSA_ENABLE_STRINGS
#define WOOPSA_ENABLE_METHODS

// 99% of systems will have the standard C library but in case 
// you end up in the 1%, you can always re-define these functions
// to work for you.
#define WOOPSA_INTEGER_TO_PADDED_STRING(value, string, padding)		sprintf(string, "%" #padding "d", value)
#define WOOPSA_INTEGER_TO_STRING(value, string, max_length)			snprintf(string, max_length-1, "%d", value)
#define WOOPSA_REAL_TO_STRING(value, string, max_length)			snprintf(string, max_length-1, "%f", value)

#define WOOPSA_STRING_TO_INTEGER(value, string)						(value = atoi(string))
#define WOOPSA_STRING_TO_FLOAT(value, string)						(value = (float)atof(string))

#define WOOPSA_STRING_POSITION(haystack, needle)					strstr(haystack, needle)
#define WOOPSA_STRING_EQUAL(string1, string2)						(strcmp(string1, string2) == 0)
#define WOOPSA_STRING_LENGTH(string)								strlen(string)
#define WOOPSA_CHAR_TO_LOWER(character)								(WoopsaChar8)tolower(character)
#define WOOPSA_STRING_COPY(destination, source)						strcpy(destination, source)
#define WOOPSA_STRING_N_COPY(destination, source, n)				strncpy(destination, source, n)

#endif