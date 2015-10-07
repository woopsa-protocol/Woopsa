# Woopsa
The Woopsa libraries for creating servers and clients

## I just want the freaking library!
The latest release is part of the git repository, in the well-named **Release** directory. It contains the .NET and JavaScript versions of the Woopsa library, as well as a few examples to get started!

You can of course also go to the Releases tab of this project, to get version 1.0 here: https://github.com/woopsa-protocol/Woopsa/releases/tag/v1.0

## Getting started
All the information you need is available on www.woopsa.org/get-started

## Building / Making a release
### Windows
Run the make-release-windows.bat file. This will
 * Build the .NET library
 * Build the WoopsaDemo server
 * Minify/uglify the JavaScript library
 * Copy all those things in the ``Release`` directory

System requirements:
 * Visual Studio Professional 2013 or newer (requires devenv to be in your ``PATH`` variable)
 * Uglifyjs (requires nodejs)

### Linux/MacOS
A build/release script is coming soon!
