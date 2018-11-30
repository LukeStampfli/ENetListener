using System;
using System.Collections.Generic;
using DarkRift;
using DarkRift.Server;
using ENet;
using UnityEngine;
using Event = ENet.Event;
using EventType = ENet.EventType;

public class EnetListenerPlugin : NetworkListener
{

    public EnetListenerPlugin(NetworkListenerLoadData pluginLoadData) : base(pluginLoadData)
    {
        Version = new Version(1,0,0);
    }

    public override Version Version { get; }

    public override void StartListening()
    {
        Library.Initialize();
        server = new Host();
        Address address = new Address();
        address.Port = 4296;
        server.Create(address, 50);
    }

    private Host server;
    private Dictionary<Peer, EnetServerConnection> connections = new Dictionary<Peer, EnetServerConnection>();

    public void ServerTick()
    {
        bool hasEvents = true;
        while (hasEvents)
        {   
            Event netEvent;
            server.Service(0,out netEvent);
            //Debug.Log("server: "+ netEvent.Type);
            switch (netEvent.Type)
            {
                case EventType.None:
                    hasEvents = false;
                    break;

                case EventType.Connect:
                    EnetServerConnection con = new EnetServerConnection(netEvent.Peer);
                    RegisterConnection(con);
                    connections.Add(netEvent.Peer, con);
                    Console.WriteLine("Client: "+ netEvent.Peer.IP + netEvent.Peer.Port+" connected.");
                    break;

                case EventType.Disconnect:
                    Console.WriteLine("Client: " + netEvent.Peer.IP + netEvent.Peer.Port + " disconnected.");
                    connections[netEvent.Peer].OnDisconnect();
                    connections.Remove(netEvent.Peer);
                    break;

                case EventType.Timeout:
                    break;

                case EventType.Receive:
                    Console.WriteLine("Packet received from client - Channel ID: " + netEvent.ChannelID +
                                      ", Data length: " + netEvent.Packet.Length);
                    if (netEvent.ChannelID == 1)
                    {
                        connections[netEvent.Peer].HandleEnetMessageReceived(netEvent, SendMode.Reliable);
                    }
                    else if (netEvent.ChannelID == 2)
                    {
                        connections[netEvent.Peer].HandleEnetMessageReceived(netEvent, SendMode.Unreliable);
                    }

                    netEvent.Packet.Dispose();
                    break;
            }

        }
        server.Flush();
    }
}
