set OUTPUT_DIR=dist
set OUTPUT_FILE=woopsa-client.js
set OUTPUT_FILE_MINIFIED=woopsa-client.min.js
if not exist %OUTPUT_DIR% mkdir %OUTPUT_DIR%
type nul > %OUTPUT_DIR%\%OUTPUT_FILE%
type woopsa-client.js >> %OUTPUT_DIR%\%OUTPUT_FILE%
call uglifyjs %OUTPUT_DIR%\%OUTPUT_FILE% -o %OUTPUT_DIR%\%OUTPUT_FILE_MINIFIED%