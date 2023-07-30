#pragma once

// Checks if the given parameter is NULL - if so, logs an error message,
// and makes the expanding function return 0.
#define NULL_CHECK_RETZERO(param)										\
	if(param == NULL) {													\
		LOG_ERROR(__FUNCTION__ ": Parameter '" #param "' was NULL.");	\
		return 0;														\
	}

// Checks if the given parameter is NULL - if so, logs an error message,
// and makes the expanding function return the given value.
#define NULL_CHECK_RET(param, returnValue)							    \
	if(param == NULL) {													\
		LOG_ERROR(__FUNCTION__ ": Parameter '" #param "' was NULL.");	\
		return returnValue;												\
	}

// Checks if the given parameter is NULL - if so, logs an error message,
// and makes the expanding function return with no value.
#define NULL_CHECK_RETVOID(param)									    \
	if(param == NULL) {													\
		LOG_ERROR(__FUNCTION__ ": Parameter '" #param "' was NULL.");	\
		return;															\
	}
