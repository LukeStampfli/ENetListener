using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using DarkRift;
using DarkRift.Client;
using ENet;
using UnityEngine;
using Event = ENet.Event;
using EventType = ENet.EventType;

class EnetClientConnection : NetworkClientConnection
{


    public EnetClientConnection(string ip, int port)
    {
        Library.Initialize();
        this.ip = ip;
        this.port = port;

        remoteEndPoints = new[] {new IPEndPoint(IPAddress.Parse(ip), port)};
    }

    private string ip;
    private int port;
    private Host client;
    private Peer peer;
    private Task clientTask;
    private bool disposedValue = false;
    private readonly IPEndPoint[] remoteEndPoints;

    //Whether we're connected
    public override ConnectionState ConnectionState
    {
        get { return connectionState; }
    }
    private ConnectionState connectionState;

    //A list of endpoints we're connected to on the server
    public override IEnumerable<IPEndPoint> RemoteEndPoints
    {
        get
        {
            return remoteEndPoints;
        }
    }

    //Given a named endpoint this should return that
    public override IPEndPoint GetRemoteEndPoint(string name)
    {
            throw new ArgumentException("Not a valid endpoint name!");
    }

    //Called when DarkRiftClient.Connect is called
    public override void Connect()
    {
        client = new Host();
        client.Create(null, 1);
        Address address = new Address();
        address.SetHost(ip);
        address.Port = (ushort) port;
        peer = client.Connect(address, 200);
    }


    //Sends a message reliably...
    public override bool SendMessageReliable(MessageBuffer message)
    {
        byte[] data = new byte[message.Count];
        Array.Copy(message.Buffer, message.Offset,data,0, message.Count);
        bool r = SendReliable(data, 1, peer);
        client.Flush();
        message.Dispose();
        return r;
    }

    //...Sends a message unreliably!
    public override bool SendMessageUnreliable(MessageBuffer message)
    {
        byte[] data = new byte[message.Count];
        Array.Copy(message.Buffer, message.Offset, data, 0, message.Count);
        bool r = SendUnreliable(data, 2, peer);
        client.Flush();
        message.Dispose();
        return r;
    }

    //Called when the server wants to disconnect the client
    public override bool Disconnect()
    {
        peer.DisconnectNow(0);
        client.Dispose();
        return true;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposedValue)
        {
            return;
        }

        if (disposing)
        {
            Disconnect();
        }

        disposedValue = true;
    }

    //We should call HandleMessageReceived(MessageBuffer message, SendMode sendMode) when we get a new message from the client
    //And HandleDisconnection(...) if the client disconnects

    private bool SendReliable(byte[] data, byte channelID, Peer peer)
    {
        Packet packet = default(Packet);

        packet.Create(data, data.Length, PacketFlags.Reliable | PacketFlags.NoAllocate); // Reliable Sequenced
        return peer.Send(channelID, ref packet);
    }

    private bool SendUnreliable(byte[] data, byte channelID, Peer peer)
    {
        Packet packet = default(Packet);

        packet.Create(data, data.Length, PacketFlags.None | PacketFlags.NoAllocate); // Unreliable Sequenced
        return peer.Send(channelID, ref packet);
    }

    private void HandleEnetMessageReceived(Event netEvent, SendMode mode)
    {
        MessageBuffer message = MessageBuffer.Create(netEvent.Packet.Length);
        netEvent.Packet.CopyTo(message.Buffer);
        message.Offset = 0;
        message.Count = netEvent.Packet.Length;
        HandleMessageReceived(message, mode);
        message.Dispose();
    }

    public void PerformUpdate()
    {
        Event netEvent;
        client.Service(0,out netEvent);
        //Debug.Log("client: " + netEvent.Type);
        switch (netEvent.Type)
        {
            case EventType.None:
                break;

            case EventType.Connect:
                Console.WriteLine("Client connected to server - ID: " + peer.ID);
                connectionState = ConnectionState.Connected;
                break;

            case EventType.Disconnect:
                Console.WriteLine("Client disconnected from server");
                connectionState = ConnectionState.Disconnected;
                HandleDisconnection(new ArgumentException("Enet disconnected"));
                break;

            case EventType.Timeout:
                Console.WriteLine("Client connection timeout");
                break;

            case EventType.Receive:
                Console.WriteLine("Packet received from server - Channel ID: " + netEvent.ChannelID +
                                  ", Data length: " + netEvent.Packet.Length);
                if (netEvent.ChannelID == 1)
                {
                    HandleEnetMessageReceived(netEvent, SendMode.Reliable);
                }
                else if (netEvent.ChannelID == 2)
                {
                    HandleEnetMessageReceived(netEvent, SendMode.Unreliable);
                }

                netEvent.Packet.Dispose();
                break;
        }
    }
}
