using System;
using System.Collections.Generic;
using System.Net;
using DarkRift;
using DarkRift.Server;
using ENet;

public class EnetServerConnection : NetworkServerConnection {

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
            return new List<IPEndPoint>();
        }
    }

    public EnetServerConnection(Peer peer)
    {
        this.peer = peer;
    }

    private string ip;
    private int port;
    private Peer peer;


    //Given a named endpoint this should return that
    public override IPEndPoint GetRemoteEndPoint(string name)
    {
        throw new ArgumentException("Not a valid endpoint name!");
    }

    public override void StartListening()
    {
        
    }

    public override bool SendMessageReliable(MessageBuffer message)
    {
        byte[] data = new byte[message.Count];
        Array.Copy(message.Buffer, message.Offset, data, 0, message.Count);
        return SendReliable(data, 1, peer);
    }

    public override bool SendMessageUnreliable(MessageBuffer message)
    {
        byte[] data = new byte[message.Count];
        Array.Copy(message.Buffer, message.Offset, data, 0, message.Count);
        return SendUnreliable(data, 2, peer);
    }

    //Called when the server wants to disconnect the client
    public override bool Disconnect()
    {
        peer.Disconnect(0);
        return true;
    }


    public void OnDisconnect()
    {
        HandleDisconnection();
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

    public void HandleEnetMessageReceived(Event netEvent, SendMode mode)
    {
        MessageBuffer message = MessageBuffer.Create(netEvent.Packet.Length);
        netEvent.Packet.CopyTo(message.Buffer);
        message.Offset = 0;
        message.Count = netEvent.Packet.Length;
        HandleMessageReceived(message, SendMode.Reliable);
    }
}
