# NetSync
A fully dynamic, high performant networking library for games and applications.


## Dynamic? How?
NetSync has a really dynamic protocol implementation process built in. All you have to do is override the TransportBase class and viola! Check [SyncTcp](https://github.com/EmreBugday99/NetSync/blob/main/NetSync/NetSync/Transport/SyncTcp/SyncTcp.cs) implementation for example.
<br>
Each NetworkClient and NetworkServer uses can use any protocol implementation they want. 
<br>
Due to the dynamic nature of these protocol implementations I decided to name them as Transport for easier communication between us :)

## No Server/Client limit
A process/application can host as many servers and clients as you want. 
<br>
A single executable can contain dozens of servers and clients.
<br>
This is also due to the dynamic nature of NetSync.

## Server And Client Inside One Code Base
You write your own server and client logic inside one project as they share the same code base. 
<br>
This increases your producitivity a lot compared to traditional networking applications where you seperate the two completely.
<br>
This is yet again was possible to the 'highly dynamic' design principle I have followed.

## Multi-Threaded
Your server and client runs on a different thread from your application.
<br>
This ensures that any long running tasks or freezes will not also freeze/halt your server and client. 
<br>
No matter what you do in your application logic, NetSync will keep running until the process stops working/exits or you tell it to stop.
<br>
NetSync uses a handle system(it's just a random name I've come up with for the system.) for inter-thread interactions which utilizes delegates and events under the hood.
<br>
You can subscribe your handlers for networked events and listen to them. When a networked event happens it will invoke your handle if it was subscribed.
<br>
### Most Importantly:
# Performance Over Safety
Unlike my previous library(InteractiveSync) NetSync will not hold your hand. If you do something which may crash the application, it will crash!
<br>
It will not have dozens of safety checks to sugar daddy you just in case you/me/someone fucks up something in their application logic.
<br>
That kind of 'safety first' approach just slows the hell of the system. I created this library because I needed speed.
<br>
One of the golden rules of programming: Safety and performance are inversely proportional. If you want one you must sacrifice the other.
<br>
If you want a library which is going to handle all sort of errors for you NetSync is not the good choice. This is not my focus and that is not where I am taking this library to.
