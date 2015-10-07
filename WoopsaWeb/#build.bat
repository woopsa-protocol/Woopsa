set OUTPUT_DIR=dist
set OUTPUT_FILE=woopsa.web.js
set OUTPUT_FILE_MINIFIED=woopsa.web.min.js
if not exist %OUTPUT_DIR% mkdir %OUTPUT_DIR%
type nul > %OUTPUT_DIR%\%OUTPUT_FILE%
type woopsa.client.js >> %OUTPUT_DIR%\%OUTPUT_FILE%
uglifyjs %OUTPUT_DIR%\%OUTPUT_FILE% -o %OUTPUT_DIR%\%OUTPUT_FILE_MINIFIED%