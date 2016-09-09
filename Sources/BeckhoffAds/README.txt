The WoopsaAds App is a gateway between ADS and Woopsa.
It is a woopsa server that connects to 1 or more ADS server (PLC Beckhoff).
The woopsa server publish all data he found on the ADS server.

		
!!!!!!!!!!!!!!!!!!!!!!!! WARNING !!!!!!!!!!!!!!!!!!!!!!!!

1/ TwinCAT must be installed on your machine if it's not a PLC Beckhoff. 
To download it, you have to use the TwinCAT installer that you can find on the site: www.beckhoff.com
	1.1/ Download the last version of TwinCAT in : Download -> Software -> TwinCAT 3 -> TE1xxx | Engineering
	1.2/ Install it on your machine

2/ The "TwinCAT.Ads.dll" library is used in the project, but it must be added to source code
	2.1/ Copy the TwinCAT.Ads.dll from "C:\TwinCAT\AdsApi\.NET\v4.0" to the root of the WoopsaAds project.


3/ You need to have Framework .NET 4.0 minimum. If you don't have it, install it with "dotNetFx40_Full_x86_x64.exe". 
   You can download this .exe at : https://www.microsoft.com/en-us/download/details.aspx?id=17718



For each server, there is need a Route. 
You can add or remove Static Route on right click on the TwinCAT icon in the traybar -> Router -> Routes editieren
For add : 	1/ Do a Broadcast Search
		2/ Select your PLC in the list
		3/ Check the "IP Address" radio button in "Address Info"
		4/ Click on "Add Route"
		5/ Enter "1" for the Password and click OK

