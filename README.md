# NetSync
A fully dynamic, high performant networking library for games and applications.


## Dynamic? How?
NetSync has a really dynamic protocol implementation process built in. All you have to do is override the TransportBase class and viola! Check [AsyncTcp](https://github.com/EmreBugday99/NetSync/blob/main/NetSync/NetSync/Transport/AsyncTcp/AsyncTcp.cs) implementation for example.
<br>
Each NetworkClient and NetworkServer can use any protocol implementation they want. 
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

## Asynchronous Networking
NetSync focuses on scalability. You can't scale well if you run thousands of connections inside a single thread. You just simply can't. That's why I went %100 asynchronous with AsynTcp Transport. 
<br>
You still can create your own transports and make it synchronous. 
<br>
I created this library for my specific needs and scalability comes first amongst those needs. 
<br>Due to the dynamic nature of the library you can customize it however you want. 
<br>
Transports simply functions like plugins. You plug your own transport and viola, you are ready to go.

## Handler Queue System
If your application has a complex multi-threaded architecture or if you simply need your application to run on a single thread, you can use the queue execution system.
<br>
Any handler that is marked as queued will not execute right away when they get received. They will be stored in memory and wait for execution. When you call ExecuteQueuedHandlers method it will execute the entire queue from the thread you specifically called the method. This ensures thread safety amongst your application.
<br>
You can get more information from [this link](https://github.com/EmreBugday99/NetSync/issues/14)
<br>

## Network Synced Objects
You can create synced classes during runtime. Synced classes will get constructed/instantiated on all clients during runtime.
<br>
You can also mark your classes/objects as late comer synced. This will ensure them to be also instantiated on clients that connected after the networked class got created.
<br>
For safety reasons this feature can only be executed from server.
<br>
You can get more information from [this link](https://github.com/EmreBugday99/NetSync/pull/7)

## Multi Channel Support
NetSync supports multi channel system integration. Channels are a great way to categorize your packets according to your needs. 
<br>
Maybe you want to use channel specifically for encrypted transmission? You can easily integrate this into your Transport with channels.
<br>
By default you use the channel 1 for transmission. You can easily change that with specifying a new channel when sending a packet / registering a handler.
<br>
### WARNING: YOU SHOULD NEVER USE CHANNEL 0 AS IT IS RESERVED FOR NETSYNC'S INTERNAL PROTOCOL!
<br>
