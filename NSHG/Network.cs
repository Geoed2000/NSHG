using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using NSHG.NetworkInterfaces;
using System.Net;
using System.Net.Sockets;
using System.Linq;
using System.IO;


namespace NSHG
{
    public class Network
    {
        public Dictionary<uint, System> Systems;
        public List<Tuple<uint, uint>> Connections;
        public List<Tuple<string, string>> Flags;
        public DateTime starttime;
        public List<uint> UnallocatedPlayers;
        public List<MAC> TakenMacAddresses;

        public Socket ServerSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        public const int buffersize = 2048;
        public const int port = 25565;
        private static readonly byte[] buffer = new byte[buffersize];
        public List<User> users;
        public SortedList<uint, string> Scenario;

        public Action<string> Log;

        public class User
        {
            public readonly string Username;
            public readonly uint Password;
            public readonly uint SysID;
            public Socket Socket;
            public byte[] Recievebuffer;
            public List<byte[]> packetsnotsent;
            public List<Tuple<string, TimeSpan>> flags;
            Action<string> Log;
            object sendinglock = new object();
            bool sending = false;
            PacketProtocol packetProtocol = new PacketProtocol(buffersize);
            Network network;


            public User(string username, uint passsword, uint sysID,Network network, Action<string> Log)
            {
                Username = username;
                Password = passsword;
                SysID = sysID;
                Socket = null;
                Recievebuffer = new byte[buffersize];
                packetsnotsent = new List<byte[]>();
                flags = new List<Tuple<string, TimeSpan>>();
                this.Log = Log;
                this.network = network;
                packetProtocol.MessageArrived = b => { network.ProcessPacket(b, this); };
            }


            public void PlayerConnectedRecieveCallback(IAsyncResult asyncResult)
            {
                int recieved;

                try
                {
                    recieved = Socket.EndReceive(asyncResult);
                }
                catch (SocketException e)
                {
                    Log("Client Forcefully Disconected " + e.Message);
                    Socket.Close();
                    return;
                }
                catch (ObjectDisposedException e)
                {
                    Log("Client Forcefully Disconected");
                    Socket.Close();
                    return;
                }

                byte[] recieveBuffer = new byte[recieved];
                Array.Copy(Recievebuffer, recieveBuffer, recieved);

                packetProtocol.DataReceived(recieveBuffer);
                
                Socket.BeginReceive(Recievebuffer, 0, buffersize, SocketFlags.None, PlayerConnectedRecieveCallback, null);
            }
            private void SendCallback(IAsyncResult asyncResult)
            {
                Tuple<Socket,Byte[]> clientSocket = (Tuple<Socket,Byte[]>)asyncResult.AsyncState;
                try
                {
                    clientSocket.Item1.EndSend(asyncResult);
                }
                catch (SocketException e)
                {
                    packetsnotsent.Add(clientSocket.Item2);
                    Socket.Close();
                }
                catch (ObjectDisposedException e)
                {

                }
            }

            public void Send(string s)
            {

                byte[] data = Encoding.ASCII.GetBytes(s);
                byte[] wrappedData = PacketProtocol.WrapMessage(data);
                try
                {
                    Socket.BeginSend(wrappedData, 0, wrappedData.Length, SocketFlags.None, SendCallback, new Tuple<Socket, Byte[]>(Socket, data));

                }
                catch (Exception)
                {
                    packetsnotsent.Add(data);
                }
                
            }
            public bool Connect(Socket s)
            {
                if (Socket == null)
                {
                    Socket = s;
                    return true;
                }else if (Socket.Connected == false)
                {
                    Socket = s;
                    return true;
                }
                return false;
            }
        }

        public Network(Action<string> log = null)
        {
            
            Systems = new Dictionary<uint, System>();
            Connections = new List<Tuple<uint, uint>>();
            Flags = new List<Tuple<string, string>>();
            Scenario = new SortedList<uint, string>();
            users = new List<User>();
            UnallocatedPlayers = new List<uint>();
            TakenMacAddresses = new List<MAC>();

            
            Log = log ?? Console.WriteLine;
            
        }

        public void Setupserver()
        {
            Log("Setting up server");
            ServerSocket.Bind(new IPEndPoint(IPAddress.Any, port));
            ServerSocket.Listen(2);
            ServerSocket.BeginAccept(AcceptCallback, null);
            Log("Server setup complete");
        }
        public void AcceptCallback(IAsyncResult asyncResult)
        {
            Socket socket;

            socket = ServerSocket.EndAccept(asyncResult);
            
            socket.BeginReceive(buffer, 0, buffersize, SocketFlags.None, PlayerLoginRecieveCallback, socket);
            Log("Client connected on IP " + ((IPEndPoint)socket.RemoteEndPoint).Address);
            ServerSocket.BeginAccept(AcceptCallback, null);
        }
        public void PlayerLoginRecieveCallback(IAsyncResult asyncResult)
        {
            Socket current = (Socket)asyncResult.AsyncState;
            int recieved;

            try
            {
                recieved = current.EndReceive(asyncResult);
            }
            catch (SocketException)
            {
                Log("Client Forcefully Disconected");
                current.Close();
                return;
            }

            byte[] recieveBuffer = new byte[recieved];
            Array.Copy(buffer, recieveBuffer, recieved);
            string text = Encoding.ASCII.GetString(recieveBuffer);

            string[] textlist = text.Split(' ');

            Log("Packet from client on ip " + ((IPEndPoint)current.RemoteEndPoint).Address);
            Log("    " + text);

            byte[] Data;
            switch(textlist[0])
            {
                case "new":
                    if (textlist.Length > 1)
                    {
                        string username = textlist[1];
                        var query =
                            from User in users
                            where (User.Username == username)
                            select User;
                        if (query.ToArray().Length != 0)
                        {
                            Data = Encoding.ASCII.GetBytes("error username in use");
                            current.BeginSend(Data, 0, Data.Length, SocketFlags.None, SendCallback, current);
                            current.BeginReceive(buffer, 0, buffersize, SocketFlags.None, PlayerLoginRecieveCallback, current);
                            break;
                        }

                        uint password = (uint)new Random().Next();
                        uint sysid = UnallocatedPlayers[0];
                        UnallocatedPlayers.Remove(sysid);

                        User user = new User(username, password, sysid, this, Log);
                        user.Socket = current;
                        users.Add(user);

                        Systems[sysid].LocalLog += s => { user.Send("system " + s); };

                        Data = Encoding.ASCII.GetBytes("success username: " + username + ",password: " + password);
                        current.BeginSend(Data, 0, Data.Length, SocketFlags.None, SendCallback, current);

                        current.BeginReceive(user.Recievebuffer, 0, buffersize, SocketFlags.None, user.PlayerConnectedRecieveCallback, user);
                        break;
                    }
                    else
                    {
                        Data = Encoding.ASCII.GetBytes("error supply username"); 
                        current.BeginSend(Data, 0, Data.Length, SocketFlags.None, SendCallback, current);
                        current.BeginReceive(buffer, 0, buffersize, SocketFlags.None, PlayerLoginRecieveCallback, current);
                   }
                    break;
                case "login":
                    if (textlist.Length >= 3)
                    {
                        string username = textlist[1];
                        var query =
                            from User in users
                            where (User.Username == username)
                            select User;
                        if (query.ToArray().Length == 0)
                        {
                            Data = Encoding.ASCII.GetBytes("error username doesn't exist");
                            current.BeginSend(Data, 0, Data.Length, SocketFlags.None, SendCallback, current);
                            current.BeginReceive(buffer, 0, buffersize, SocketFlags.None, PlayerLoginRecieveCallback, current);
                            break;
                        }
                        foreach (User u in query)
                        {
                            if (u.Password.ToString() == textlist[2])
                            {
                                u.Socket = current;
                                Data = Encoding.ASCII.GetBytes("success");
                                Systems[u.SysID].LocalLog += s => { u.Send("system " + s); };
                                current.BeginSend(Data, 0, Data.Length, SocketFlags.None, SendCallback, current);
                                current.BeginReceive(u.Recievebuffer, 0, buffersize, SocketFlags.None, u.PlayerConnectedRecieveCallback, u);
                                break;

                            }
                        }
                        Data = Encoding.ASCII.GetBytes("error invalid password");
                        current.BeginSend(Data, 0, Data.Length, SocketFlags.None, SendCallback, current);
                        current.BeginReceive(buffer, 0, buffersize, SocketFlags.None, PlayerLoginRecieveCallback, current);
                        break;
                    }
                    else
                    {
                        Data = Encoding.ASCII.GetBytes("error Supply Username and password");
                        current.BeginSend(Data, 0, Data.Length, SocketFlags.None, SendCallback, current);
                        current.BeginReceive(buffer, 0, buffersize, SocketFlags.None, PlayerLoginRecieveCallback, current);
                    }
                    break;
            }
        }
        
        public void ProcessPacket(byte[] recieveBuffer,User current)
        {
            string text = Encoding.ASCII.GetString(recieveBuffer);
            Log("ProessingPacket");
            Log("Packet from client on ip " + ((IPEndPoint)current.Socket.RemoteEndPoint).Address);
            Log("    " + text);

            byte[] data;
            string[] split = text.Split(' ');
            switch (split[0].ToLower())
            {
                case "command":
                    List<string> NetworkCommandList = new List<string>(split);
                    NetworkCommandList.RemoveAt(0);
                    NetworkCommandList.Insert(0, current.SysID.ToString());
                    asSystem(NetworkCommandList.ToArray(), Log);
                    break;

                case "flag":
                    if (split.Length > 1)
                    {
                        var query =
                            from flag in Flags
                            where (flag.Item1.ToString() == split[1])
                            select flag;
                        if (query.ToArray().Length == 0)
                        {
                            data = Encoding.ASCII.GetBytes("flag error invalid flag");
                            current.Socket.BeginSend(data, 0, data.Length, SocketFlags.None, SendCallback, current.Socket);
                            break;
                        }
                        else
                        {
                            foreach (Tuple<string, string> t in query)
                                current.flags.Add(new Tuple<string, TimeSpan>(t.Item1, DateTime.Now - starttime));
                            data = Encoding.ASCII.GetBytes("flag Success!");
                            current.Socket.BeginSend(data, 0, data.Length, SocketFlags.None, SendCallback, current.Socket);
                        }
                    }
                    break;

                case "logout":
                    current.Socket.Shutdown(SocketShutdown.Both);
                    current.Socket.Close();
                    break;
            }
        }
        private void SendCallback(IAsyncResult asyncResult)
        {
            Socket clientSocket = (Socket)asyncResult.AsyncState;
            try
            {
                clientSocket.EndSend(asyncResult);
            }
            catch (SocketException e)
            {
                Log(e.Message);
            }
            catch (ObjectDisposedException e)
            {
                Log(e.Message);
            }
        }

        public void asSystem(string[] commandlist, Action<string> Log)
        {
            if (commandlist.Length > 1)
            {
                try
                {
                    uint id = uint.Parse(commandlist[0]);
                    string newcommand = "";
                    for (int i = 1; i < commandlist.Length; i++)
                    {
                        newcommand += commandlist[i] + " ";
                    }
                    Systems[id].Command(newcommand.Trim(' '));
                }
                catch (FormatException e)
                {
                    Log("error parsing ID");
                }
                catch (OverflowException e)
                {
                    Log("error ID too large");
                }
            }
            else
            {
                Log("error Must specify a system to act as");
            }
        }
        public void message(string text)
        {
            foreach (User u in users)
            {
                u.Send("scenario " + text);
            }
        }
        public bool report(string filepath)
        {
            if (!filepath.EndsWith(".csv"))
            {
                filepath += ".csv";
            }
            string[,] report = new string[Flags.Count + 1,users.Count + 1];
            for(int i = 0; i < users.Count; i++)
            {
                report[0, i+1] = users[i].Username;
            }
            for(int f = 0; f<Flags.Count; f++)
            {
                report[f+1, 0] = Flags[f].Item2;
                for(int u = 0; u < users.Count; u++)
                { 
                    foreach (Tuple<string,TimeSpan> entry in users[u].flags)
                    {
                        if(entry.Item1 == Flags[f].Item1)
                        {
                            report[f + 1, u + 1] = entry.Item2.Hours.ToString() + ":" + entry.Item2.Minutes.ToString() + ":" + entry.Item2.Seconds.ToString();
                        }
                    }
                }
            }
            try
            {
                using (StreamWriter file = new StreamWriter(filepath))
                {
                    for (int i = 0; i < users.Count + 1; i++)
                    {
                        for (int j = 0; j < Flags.Count + 1; j++)
                        {
                            file.Write(report[i, j] + ",");
                        }
                        file.Write("\r\n");
                    }
                    file.Close();
                }
                return true;
            }
            catch
            {
                return false;
            }
            
        }

        public void Tick(uint tick)
        {
            foreach (NSHG.System s in Systems.Values)
            {
                s.Tick(tick);
            }
            if (Scenario.ContainsKey(tick))
            {
                message(Scenario[tick]);
            }
            
        }

        public bool Connect(uint sysA, uint sysB)
        {
            NetworkInterface a, b;
            if (Systems[sysA].GetConnectedUnassociatedAdapter(out a, sysB) && Systems[sysB].GetConnectedUnassociatedAdapter(out b, sysA))
            {
                a.Connect(b);
                b.Connect(a);
                Connections.Add(new Tuple<uint, uint>(sysA, sysB));
                return true;
            }
            else if (Systems[sysA].GetConectableAdapter(out a) && Systems[sysB].GetConectableAdapter(out b))
            {
                a.Connect(b);
                b.Connect(a);
                Connections.Add(new Tuple<uint, uint>(sysA, sysB));
                return true;
            }

            return false;
        }
        public bool Connect(XmlNode Parent)
        {

            uint sys1 = 0;
            uint sys2 = 0;
            List<uint> IDs = new List<uint>();
            foreach (XmlNode n in Parent.ChildNodes)
            {
                if (n.Name == "ID")
                {
                    if (sys1 != 0)
                    {
                        sys2 = uint.Parse(n.InnerText);
                    }
                    else
                    {
                        sys1 = uint.Parse(n.InnerText);
                    }
                }
            }
            return Connect(sys1, sys2);
        }
        public static Network LoadNetwork(string filepath, Action<string> log = null)
        {
            Network network = new Network(log);
            Action<string> Log = log ?? Console.WriteLine;
            
            
            XmlDocument doc = new XmlDocument();
            doc.Load(filepath);
            foreach (XmlNode node in doc.DocumentElement)
            {
                string s = node.Name.ToLower();

                
                NSHG.System sys;

                switch (s)
                {
                    case "system":
                        try
                        {
                            sys = NSHG.System.FromXML(node, Log);
                        }
                        catch(Exception e)
                        {
                            Log("Reading System Failed");
                            Log(e.ToString());
                            break;
                        }
                        try
                        {
                            network.Systems.Add(sys.ID, sys);
                            foreach(Adapter a in sys.NetworkInterfaces.Values)
                            {
                                network.TakenMacAddresses.Add(a.MyMACAddress);
                            }
                            Log("Added System \n    ID:" + sys.ID);
                        }
                        catch(Exception e)
                        {
                            Log("failed adding system to network");
                            Log(e.ToString());
                        }
                        break;
                    case "router":
                        try
                        {
                            sys = NSHG.Router.FromXML(node, Log);
                        }
                        catch(Exception e)
                        {

                            Log("Reading Router Failed");
                            Log(e.ToString());
                            break;
                        }
                        try
                        {
                            network.Systems.Add(sys.ID, sys);
                            foreach (NetworkInterface a in sys.NetworkInterfaces.Values)
                            {
                                network.TakenMacAddresses.Add(a.MyMACAddress);
                            }
                            Log("Added Router \n    ID:" + sys.ID);
                        }
                        catch(Exception e)
                        {
                            Log("failed adding Router to network");
                            Log(e.ToString());
                        }
                        break;
                    case "pc":
                        try
                        {
                            sys = NSHG.System.FromXML(node, Log);
                        }
                        catch
                        {
                            Log("Reading System Failed");
                            break;
                        }
                        try
                        {
                            network.Systems.Add(sys.ID, sys);
                            foreach (NetworkInterface a in sys.NetworkInterfaces.Values)
                            {
                                network.TakenMacAddresses.Add(a.MyMACAddress);
                            }
                            Log("Added System \n    ID:" + sys.ID);
                        }
                        catch
                        {
                            Log("failed adding system to network");
                        }
                        break;

                    case "player":
                        try
                        {
                            sys = NSHG.System.FromXML(node, Log);
                        }
                        catch (Exception e)
                        {
                            Log("Reading System Failed");
                            Log(e.ToString());
                            break;
                        }
                        try
                        {
                            network.Systems.Add(sys.ID, sys);
                            foreach (Adapter a in sys.NetworkInterfaces.Values)
                            {
                                network.TakenMacAddresses.Add(a.MyMACAddress);
                            }
                            Log("Added System \n    ID:" + sys.ID);
                        }
                        catch (Exception e)
                        {
                            Log("failed adding system to network");
                            Log(e.ToString());
                        }
                        network.UnallocatedPlayers.Add(sys.ID);
                        break;
                        
                    case "connection":
                        if (network.Connect(node))
                        {
                            //success
                            Log("Addedd connection\n    " + node.InnerText);
                        }
                        else
                        {
                            //failure
                            Log("Failed to add connection\n    "+node.InnerText);
                        }
                        break;

                    case "flag":
                        string id = "";
                        string mark = "";
                        foreach(XmlNode n in node.ChildNodes)
                        {
                            if(n.Name.ToLower() == "id")
                            {
                                if (n.InnerText == "")
                                {
                                    Log("Invalid Flag id");
                                }
                                else
                                {
                                    id = n.InnerText;
                                }
                            }
                            else if (n.Name.ToLower() == "mark")
                            {
                                mark = n.InnerText;
                            }
                        }
                        if ((id == "") || (mark == ""))
                        {
                            Log("Failed to add flag" + node.InnerText);
                        }
                        else
                        {
                            network.Flags.Add(new Tuple<string, string>(id, mark));
                            Log("Added flag with ID: " + id + "\n    and mark: " + mark);
                        }
                        break;

                    case "scenario":
                        foreach(XmlNode n in node.ChildNodes)
                        {
                            if(n.Name == "message")
                            {
                                uint time = 0;
                                string text= "";
                                foreach (XmlAttribute a in n.Attributes)
                                {
                                    if (a.Name.ToLower() == "tick")
                                    {
                                        try
                                        {
                                            time = uint.Parse(a.InnerText);
                                        }
                                        catch (Exception e)
                                        {
                                            Log(e.Message);
                                        }
                                    }
                                    if (a.Name.ToLower() == "text")
                                    {
                                        text = a.InnerText;
                                    }
                                }
                                Log("Added message at " + time + "Ticks with message \n        " + text);
                                network.Scenario.Add(time,text);
                            }
                        }
                        break;

                    default:
                        Log("Invalid Identifier " + s);
                        break;
                }
            }
            
            network.Setupserver();
            return network;
        }
        public bool SaveNetwork(string filepath, Action<string> log)
        {
            try
            {
                XmlDocument file = new XmlDocument();
                XmlNode rootNode = file.CreateElement("root");
                

                foreach (KeyValuePair<uint, NSHG.System> entry in Systems)
                {
                    rootNode.AppendChild(entry.Value.ToXML(file));
                }

                foreach (Tuple<uint, uint> connection in Connections)
                {
                    XmlNode c1 = file.CreateElement("ID");
                    c1.InnerText = connection.Item1.ToString();
                    XmlNode c2 = file.CreateElement("ID");
                    c2.InnerText = connection.Item2.ToString();
                    XmlNode Connection = file.CreateElement("Connection");
                    Connection.AppendChild(c1);
                    Connection.AppendChild(c2);
                    rootNode.AppendChild(Connection);
                }
                file.AppendChild(rootNode);
                file.Save(filepath);
            }
            catch
            {
                return false;
            }
            return true;
        }
        public void Exit()
        {
            foreach (User u in users)
            {
                u.Socket.Shutdown(SocketShutdown.Both);
                u.Socket.Close();
            }
            ServerSocket.Shutdown(SocketShutdown.Both);
            ServerSocket.Close();
        }
    }
}
