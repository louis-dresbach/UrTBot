using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Collections.Specialized;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;

namespace UrTBot
{
    public class urt_server_bot
    {
        public String RCON_IP;
        public String RCON_PORT;
        public String RCON_PASSWORD;
        public String Server_Name;
        public String log_path;
        public Boolean Log_Over_FTP = false;
        public Boolean Connected = false;
        public Boolean Running = false;

        public String FTP_Username;
        public String FTP_Password;

        TimeSpan LastServerTimeParsed = TimeSpan.FromMilliseconds(0);

        public String CurrentMap = "-";
        public String Nextmap = "-";

        public StringDictionary PlayerLevels;

        System.Timers.Timer timer = new System.Timers.Timer();
        System.Timers.Timer spammer = new System.Timers.Timer(); 

        public List<Player> Players = new List<Player>();

        public urt_server_bot(String ip = "", String port = "", String password = "", String Path_to_log_file = "")
        {
            RCON_IP = ip;
            RCON_PORT = port;
            RCON_PASSWORD = password;
            log_path = Path_to_log_file;
            timer.Interval = 1000;
            timer.Elapsed += new System.Timers.ElapsedEventHandler(timer_Elapsed);
            spammer.Interval = 120000;
            spammer.Elapsed += new System.Timers.ElapsedEventHandler(spammer_Elapsed);
            spammer.Start();
            LoadPlayerLevels();
        }

        public urt_server_bot(String ip, String port, String password, String FTP_Path_to_log_file, String FTP_Username, String FTP_Password)
        {
            RCON_IP = ip;
            RCON_PORT = port;
            RCON_PASSWORD = password;
            Log_Over_FTP = true;
            log_path = FTP_Path_to_log_file;
            this.FTP_Username = FTP_Username;
            this.FTP_Password = FTP_Password;
            timer.Interval = 1000;
            timer.Elapsed += new System.Timers.ElapsedEventHandler(timer_Elapsed);
            spammer.Interval = 120000;
            spammer.Elapsed += new System.Timers.ElapsedEventHandler(spammer_Elapsed);
            spammer.Start();
            LoadPlayerLevels();
        }

        void spammer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (Running)
            {
                String Spammmessage = Properties.Settings.Default.Spammessages[new Random().Next(0, Properties.Settings.Default.Spammessages.Count)];
                if (Spammmessage.Contains("$admins"))
                {
                    String admins = "";
                    foreach (Player p in Players)
                    {
                        if (p.Level > 0) admins += p.Name + " (" + p.Level + "), ";
                    }
                    if (admins != String.Empty) admins = admins.Remove(admins.Length - 2, 2);
                    else admins = "None.";

                    Spammmessage = Spammmessage.Replace("$admins", admins);
                }
                Spammmessage = Spammmessage.Replace("$map", CurrentMap).Replace("$nextmap", Nextmap);
                sendRcon("say " + Spammmessage);
            }
        }

        public void LoadPlayerLevels()
        {
            if (Properties.Settings.Default.PlayerLevels == String.Empty) { PlayerLevels = new StringDictionary(); return; }
            using (MemoryStream ms = new MemoryStream(Convert.FromBase64String(Properties.Settings.Default.PlayerLevels)))
            {
                BinaryFormatter bf = new BinaryFormatter();
                PlayerLevels = (StringDictionary)bf.Deserialize(ms);
            }
        }

        public void SavePlayerLevels()
        {
            using (MemoryStream ms = new MemoryStream())
            {
                using (StreamReader sr = new StreamReader(ms))
                {
                    BinaryFormatter bf = new BinaryFormatter();
                    bf.Serialize(ms, PlayerLevels);
                    ms.Position = 0;
                    byte[] buffer = new byte[(int)ms.Length];
                    ms.Read(buffer, 0, buffer.Length);
                    Properties.Settings.Default.PlayerLevels = Convert.ToBase64String(buffer);
                    Properties.Settings.Default.Save();
                }
            }
        }

        public void Log(String message)
        {

        }

        public void Restart()
        {
            Stop();
            Start(log_path);
        }

        void timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            ParseLog();
        }

        public void checkConnection()
        {
            bool success;
            Socket client = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

            IAsyncResult result = client.BeginConnect(IPAddress.Parse(RCON_IP), Convert.ToInt32(RCON_PORT), null, null);
            success = result.AsyncWaitHandle.WaitOne(5000, true);

            if (success)
            {
                string command;
                command = "getstatus";
                byte[] bufferTemp = Encoding.ASCII.GetBytes(command);
                byte[] bufferSend = new byte[bufferTemp.Length + 4];

                bufferSend[0] = byte.Parse("255");
                bufferSend[1] = byte.Parse("255");
                bufferSend[2] = byte.Parse("255");
                bufferSend[3] = byte.Parse("255");
                int j = 4;

                for (int i = 0; i < bufferTemp.Length; i++)
                {
                    bufferSend[j++] = bufferTemp[i];
                }

                IPEndPoint RemoteIpEndPoint = new IPEndPoint(IPAddress.Any, 0);
                client.Send(bufferSend, SocketFlags.None);

                byte[] bufferRec = new byte[65000];
                try
                {
                    client.Receive(bufferRec);
                    string answer = Encoding.ASCII.GetString(bufferRec).Remove(0, 9);
                    Connected = true;
                }
                catch (Exception e)
                {
                    client.Close();
                    Connected = false;
                }
            }
            else
            {
                client.Close();
                Connected = false;
            }
        }

        public string sendRcon(String _cmd)
        {
            bool success;
            Socket client = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

            IAsyncResult result = client.BeginConnect(IPAddress.Parse(RCON_IP), Convert.ToInt32(RCON_PORT), null, null);
            success = result.AsyncWaitHandle.WaitOne(3000, true);

            if (success)
            {
                string command;
                command = "rcon \"" + RCON_PASSWORD + "\" " + _cmd + "";
                byte[] bufferTemp = Encoding.ASCII.GetBytes(command);
                byte[] bufferSend = new byte[bufferTemp.Length + 4];

                bufferSend[0] = byte.Parse("255");
                bufferSend[1] = byte.Parse("255");
                bufferSend[2] = byte.Parse("255");
                bufferSend[3] = byte.Parse("255");
                int j = 4;

                for (int i = 0; i < bufferTemp.Length; i++)
                {
                    bufferSend[j++] = bufferTemp[i];
                }

                IPEndPoint RemoteIpEndPoint = new IPEndPoint(IPAddress.Any, 0);
                client.Send(bufferSend, SocketFlags.None);
                System.Threading.Thread.Sleep(300);

                byte[] bufferRec = new byte[65000];
                try
                {
                    client.Receive(bufferRec);
                    string answer = Encoding.ASCII.GetString(bufferRec).Remove(0, 9).Replace("\0", "");
                    client.Close();
                    Connected = true;
                    return answer;
                }
                catch (Exception e)
                {
                    client.Close();
                    Connected = false;
                    return null;
                }
            }
            else
            {
                client.Close();
                Connected = false;
                return null;
            }
        }

        public void ParseLog(bool firstTimeParsing = false)
        {
            String[] arr = LastNLinesOfFile(log_path, 15);
            foreach (String s in arr)
            {
                String line = s;
                if (line != null)
                {
                    if (line.Length > 6)
                    {
                        String time = line.Trim().Split(' ').First();
                        TimeSpan Time = TimeSpan.FromMinutes(Convert.ToDouble(time.Split(':')[0])) + TimeSpan.FromSeconds(Convert.ToDouble(time.Split(':')[1]));
                        if (Time < LastServerTimeParsed && !firstTimeParsing)
                        {
                            return;
                        }
                        LastServerTimeParsed = Time + TimeSpan.FromSeconds(1);
                        line = line.Trim().Remove(0, time.Length).Trim();
                        if ((line.StartsWith("say:") || line.StartsWith("tell:") || line.StartsWith("sayteam:")))
                        {
                            if (!firstTimeParsing)
                            {
                                String[] info = line.Split(' ');
                                String PlayerID = info[1];
                                String message = String.Empty;
                                for (int i = 3; i < info.Count(); i++)
                                {
                                    message += info[i] + " ";
                                }
                                message = message.Trim();
                                Say(PlayerID, message);
                            }
                        }
                        else if (line.StartsWith("Kill:"))
                        {
                            String[] info = line.Split(' ');
                            if (info[1] != "1022") // Killed by world
                            {
                                Player Killer = FindPlayer(info[1]);
                                Killer.Kills++;
                            }
                            FindPlayer(info[2]).Deaths++;
                        }
                        else if (line.StartsWith("ShutdownGame:"))
                        {
                            Time = TimeSpan.FromSeconds(0);
                            CurrentMap = String.Empty;
                        }
                        else if (line.StartsWith("InitGame:") || line.StartsWith("InitRound:"))
                        {
                            String[] info = line.Split(' ');
                            String Info = String.Empty;
                            StringDictionary Infos = new StringDictionary();
                            for (int i = 1; i < info.Count(); i++)
                            {
                                Info += info[i] + " ";
                            }
                            Info = Info.Trim().TrimStart('\\');

                            int integer = 1;
                            String Key = String.Empty;
                            foreach (String str in Info.Split('\\'))
                            {
                                if (integer == 1)
                                {
                                    Key = str;
                                    integer = 2;
                                }
                                else if (integer == 2)
                                {
                                    Infos.Add(Key, str);
                                    integer = 1;
                                }
                            }
                            CurrentMap = Infos["mapname"];
                            Infos.Clear();
                        }
                        else if (line.StartsWith("ClientConnect:"))
                        {
                            String[] info = line.Split(' ');
                            String PlayerID = info[1];
                            Int32 ID;

                            Boolean success = Int32.TryParse(PlayerID, out ID);
                            Players.Add(new Player() { ClientID = ID });
                        }
                        else if (line.StartsWith("ClientUserinfo:"))
                        {
                            String[] info = line.Split(' ');
                            String PlayerID = info[1];
                            String UserInfo = String.Empty;
                            StringDictionary UserInfos = new StringDictionary();
                            Int32 playerID;

                            if (Int32.TryParse(PlayerID, out playerID))
                            {
                                for (int i = 2; i < info.Count(); i++)
                                {
                                    UserInfo += info[i] + " ";
                                }
                                UserInfo = UserInfo.Trim().TrimStart('\\');

                                int integer = 1;
                                String Key = String.Empty;
                                foreach (String str in UserInfo.Split('\\'))
                                {
                                    if (integer == 1)
                                    {
                                        Key = str;
                                        integer = 2;
                                    }
                                    else if (integer == 2)
                                    {
                                        UserInfos.Add(Key, str);
                                        integer = 1;
                                    }
                                }

                                int Level = 0;
                                if (PlayerLevels != null) Level = Convert.ToInt32(PlayerLevels[UserInfos["ip"]]);

                                WebClient client = new WebClient();
                                String Geo2IP = client.DownloadString("http://api.ipinfodb.com/v3/ip-city/?key=a928f781dec5c77501e0bc727e7e5fcc68f9333fa7026e59ac1e9bf1e7d4a836&ip=" + UserInfos["ip"]);
                                String[] locationInfo = Geo2IP.Split(';');

                                Player Client = FindPlayer(playerID.ToString());
                                Client.Name = UserInfos["name"];
                                Client.IP = UserInfos["ip"];
                                Client.Level = Level;
                                Client.Country = locationInfo[4];
                                Client.City = locationInfo[6];
                            }
                        }
                        else if (line.StartsWith("ClientUserinfoChanged:"))
                        {
                            String[] info = line.Split(' ');
                            String PlayerID = info[1];
                            String UserInfo = String.Empty;
                            StringDictionary UserInfos = new StringDictionary();
                            for (int i = 2; i < info.Count(); i++)
                            {
                                UserInfo += info[i] + " ";
                            }
                            UserInfo = UserInfo.Trim().TrimStart('\\');

                            int integer = 1;
                            String Key = String.Empty;
                            foreach (String str in UserInfo.Split('\\'))
                            {
                                if (integer == 1)
                                {
                                    Key = str;
                                    integer = 2;
                                }
                                else if (integer == 2)
                                {
                                    UserInfos.Add(Key, str);
                                    integer = 1;
                                }
                            }

                            Player p = FindPlayer(PlayerID);
                            if (p != null)
                            {
                                p.Name = UserInfos["n"];
                                p.Team = Convert.ToInt32(UserInfos["t"]);
                            }
                        }
                        else if (line.StartsWith("ClientDisconnect:"))
                        {
                            Player Target = FindPlayer(line.Split(' ')[1].Trim());
                            Players.Remove(Target);
                        }
                    }
                }
                System.Threading.Thread.Sleep(200);
            }
        }

        string[] LastNLinesOfFile(String FilePath, int N)
        {
            Stream stream;
            StreamReader sr;

            if (Log_Over_FTP)
            {
                System.Net.FtpWebRequest tmpReq = (System.Net.FtpWebRequest)System.Net.FtpWebRequest.Create(log_path);
                tmpReq.Credentials = new System.Net.NetworkCredential(FTP_Username, FTP_Password);
                System.Net.WebResponse tmpRes = tmpReq.GetResponse();
                using (stream = tmpRes.GetResponseStream())
                {
                    using (sr = new StreamReader(stream))
                    {
                        String line;
                        StringCollection lines = new StringCollection();
                        while ((line = sr.ReadLine()) != null)
                        {
                            lines.Add(line);
                        }
                        String[] Lines = new String[N];
                        for (int i = 1; i <= N; i++)
                        {
                            if(lines.Count-i >= 0 && lines[lines.Count-i] != null)
                                Lines[i-1] = lines[lines.Count - i];
                        }
                        return Lines;
                    }
                }
            }
            else
            {
                using (stream = File.Open(FilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    using (sr = new StreamReader(stream))
                    {
                        sr.BaseStream.Seek(0, SeekOrigin.End);
                        int count = 0;
                        while ((count < N) && (sr.BaseStream.Position > 0))
                        {
                            sr.BaseStream.Position--;
                            int c = sr.BaseStream.ReadByte();
                            if (sr.BaseStream.Position > 0)
                                sr.BaseStream.Position--;
                            if (c == Convert.ToInt32('\n'))
                            {
                                ++count;
                            }
                        }
                        string str = sr.ReadToEnd();
                        sr.Close();
                        return str.Replace("\r", "").Split('\n');
                    }
                }
            }
        }

        public void UpdatePlayerList()
        {
            /*
            String g_redTeamList = sendRcon("g_redTeamList").Remove(0, 21);
            g_redTeamList = g_redTeamList.Remove(g_redTeamList.Length - 17, 17).ToLower().Replace("a", "1,").Replace("b", "2,").Replace("c", "3,").Replace("d", "4,").Replace("e", "5,").Replace("f", "6,").Replace("g", "7,").Replace("h", "8,").Replace("i", "9,").Replace("j", "10,").Replace("k", "11,").Replace("l", "12,").Replace("m", "13,").Replace("n", "14,").Replace("o", "15,").Replace("p", "16,").Replace("q", "17,").Replace("r", "18,").Replace("s", "19,").Replace("t", "20,").Replace("u", "21,").Replace("v", "22,").Replace("w", "23,").Replace("x", "24,").Replace("y", "25,").Replace("z", "26,");

            String g_blueTeamList = sendRcon("g_blueTeamList").Remove(0, 22);
            g_blueTeamList = g_blueTeamList.Remove(g_blueTeamList.Length - 17, 17).ToLower().Replace("a", "1,").Replace("b", "2,").Replace("c", "3,").Replace("d", "4,").Replace("e", "5,").Replace("f", "6,").Replace("g", "7,").Replace("h", "8,").Replace("i", "9,").Replace("j", "10,").Replace("k", "11,").Replace("l", "12,").Replace("m", "13,").Replace("n", "14,").Replace("o", "15,").Replace("p", "16,").Replace("q", "17,").Replace("r", "18,").Replace("s", "19,").Replace("t", "20,").Replace("u", "21,").Replace("v", "22,").Replace("w", "23,").Replace("x", "24,").Replace("y", "25,").Replace("z", "26,");
            */
            try
            {
                String Status = sendRcon("players").TrimStart('\n');
                CurrentMap = Status.Split('\n')[0].Split(' ')[1];
                int RedTeamScore = Convert.ToInt32(Status.Split('\n')[2].Split(' ')[1].Remove(0, 2));
                int BlueTeamScore = Convert.ToInt32(Status.Split('\n')[2].Split(' ')[2].Remove(0, 2));
                Status = Status.Remove(0, CurrentMap.Length + 5).Replace("\0", "").TrimStart('\n');
                if (Status.Length > 10)
                {
                    int remove = Status.Split('\n')[0].Length + Status.Split('\n')[1].Length;
                    Status = Status.Remove(0, remove);
                    Players.Clear();
                    foreach (String player in Status.Split('\n'))
                    {
                        if (player != String.Empty && player.Length > 5)
                        {
                            String info = player.Trim().Replace("      ", " ").Replace("     ", " ").Replace("    ", " ").Replace("   ", " ").Replace("  ", " ");
                            String[] Info = info.Split(' ');
                            int ID = Convert.ToInt32(Info[0].Remove(Info[0].Length - 1, 1));
                            String Name = Info[1];
                            String IP = Info[6].Split(':')[0];
                            int Level = 0;
                            if (PlayerLevels[IP] != null) Level = Convert.ToInt32(PlayerLevels[IP]);

                            int Team = 0;
                            if (Info[2] == "RED") Team = 1;
                            else if (Info[2] == "BLUE") Team = 2;

                            int Kills = Convert.ToInt32(Info[3].Remove(0, 2));
                            int Deaths = Convert.ToInt32(Info[4].Remove(0, 2));

                            WebClient client = new WebClient();
                            String Geo2IP = client.DownloadString("http://api.ipinfodb.com/v3/ip-city/?key=a928f781dec5c77501e0bc727e7e5fcc68f9333fa7026e59ac1e9bf1e7d4a836&ip=" + IP);
                            String[] locationInfo = Geo2IP.Split(';');
                            Players.Add(new Player() { ClientID = ID, Name = Name, IP = IP, Level = Level, Country = locationInfo[4], City = locationInfo[6], Team = Team, Kills = Kills, Deaths = Deaths });
                        }
                    }
                }
            }
            catch
            {
                String Status = sendRcon("status").TrimStart('\n');
                CurrentMap = Status.Split('\n')[0].Split(' ')[1];
                Status = Status.Remove(0, CurrentMap.Length + 5).Replace("\0", "").TrimStart('\n');
                if (Status.Length > 10)
                {
                    int remove = Status.Split('\n')[0].Length + Status.Split('\n')[1].Length;
                    Status = Status.Remove(0, remove);
                    Players.Clear();
                    foreach (String player in Status.Split('\n'))
                    {
                        if (player != String.Empty && player.Length > 5)
                        {
                            String info = player.Trim().Replace("      ", " ").Replace("     ", " ").Replace("    ", " ").Replace("   ", " ").Replace("  ", " ");
                            String[] Info = info.Split(' ');

                            int ID = Convert.ToInt32(Info[0]);
                            String Name = Info[3];
                            String IP = Info[5].Split(':')[0];
                            int Level = 0;
                            if (PlayerLevels[IP] != null) Level = Convert.ToInt32(PlayerLevels[IP]);

                            int Kills = Convert.ToInt32(Info[1]);
                            
                            WebClient client = new WebClient();
                            String Geo2IP = client.DownloadString("http://api.ipinfodb.com/v3/ip-city/?key=a928f781dec5c77501e0bc727e7e5fcc68f9333fa7026e59ac1e9bf1e7d4a836&ip=" + IP);
                            String[] locationInfo = Geo2IP.Split(';');
                            Players.Add(new Player() { ClientID = ID, Name = Name, IP = IP, Level = Level, Country = locationInfo[4], City = locationInfo[6], Kills = Kills });
                        }
                    }
                }
            }
            System.Threading.Thread.Sleep(500);
        }

        public void Start(String pathToLog)
        {
            log_path = pathToLog;
            String[] arr = LastNLinesOfFile(log_path, 6);
            foreach (String l in arr)
            {
                if (l.Length > 6)
                {
                    String time = l.Substring(0, 6).Trim();
                    TimeSpan Time = TimeSpan.FromMinutes(Convert.ToDouble(time.Split(':')[0])) + TimeSpan.FromSeconds(Convert.ToDouble(time.Split(':')[1]));
                    if (Time > LastServerTimeParsed)
                        LastServerTimeParsed = Time;
                }
            }
            UpdatePlayerList();
            sendRcon("say " + Properties.Settings.Default.StartMessage);
            timer.Start();
            Running = true;
        }

        public void Start(String FTP_PathToLog, String FTP_Username, String FTP_Password)
        {
            Log_Over_FTP = true;
            this.FTP_Username = FTP_Username;
            this.FTP_Password = FTP_Password;
            log_path = FTP_PathToLog;
            String[] arr = LastNLinesOfFile(log_path, 6);
            foreach (String l in arr)
            {
                if (l != null)
                {
                    if (l.Length > 6)
                    {
                        String time = l.Substring(0, 6).Trim();
                        TimeSpan Time = TimeSpan.FromMinutes(Convert.ToDouble(time.Split(':')[0])) + TimeSpan.FromSeconds(Convert.ToDouble(time.Split(':')[1]));
                        if (Time > LastServerTimeParsed)
                            LastServerTimeParsed = Time;
                    }
                }
            }
            UpdatePlayerList();
            sendRcon("say " + Properties.Settings.Default.StartMessage);
            timer.Start();
            Running = true;
        }

        public void Stop()
        {
            SavePlayerLevels();
            timer.Stop();
            Running = false;
        }

        public Player FindPlayer(String Search)
        {
            if (Search == String.Empty) return null;
            List<Player> target;
            int ID;
            if (Int32.TryParse(Search, out ID))
            {
                target = Players.FindAll(delegate(Player p) { return p.ClientID == ID; });
            }
            else
            {
                target = Players.FindAll(delegate(Player p) { return p.CleanName.ToLower().Contains(Search.ToLower()); });
            }
            if (target.Count == 1)
                return target[0];
            else return null;
        }

        public void ParseCommand(Player Player, String command)
        {
            String[] splitted = command.Split(' ');
            Player target;
            switch (splitted[0].ToLower())
            {
                // Functions target another player:
                case "level":
                    if (Player.Level >= Commands.Default.LevelLevel)
                    {
                        int Level;
                        target = FindPlayer(splitted[1]);
                        if (target != null)
                        {
                            if (Int32.TryParse(splitted[2], out Level))
                            {
                                target.Level = Level;
                                sendRcon("tell " + Player.ClientID + " Changed " + target.Name + "'s level to " + splitted[2]);
                            }
                        }
                        else sendRcon("tell " + Player.ClientID + " Player " + splitted[1]+ " could not be found.");
                    }
                    else NotEnoughPerms(Player);
                    break;
                case "kick":
                    if (Player.Level >= Commands.Default.KickLevel)
                    {
                        target = FindPlayer(splitted[1]);
                        if (target != null)
                        {
                            sendRcon("clientkick " + target.ClientID);
                            sendRcon(@"say " + Player.Name + " has kicked player " + target.Name + ".");
                            if (splitted[2] != null && splitted[2] != String.Empty) sendRcon(@"say Reason: " + splitted[2]);
                        }
                        else sendRcon("tell " + Player.ClientID + " Player " + splitted[1] + " could not be found.");
                    }
                    else NotEnoughPerms(Player);
                    break;
                case "kill":
                    if (Player.Level >= Commands.Default.KickLevel)
                    {
                        target = FindPlayer(splitted[1]);
                        if (target != null)
                        {
                            sendRcon("kill " + target.ClientID);
                        }
                        else sendRcon("tell " + Player.ClientID + " Player " + splitted[1] + " could not be found.");
                    }
                    else NotEnoughPerms(Player);
                    break;
                case "slap":
                    if (Player.Level >= Commands.Default.SlapLevel)
                    {
                        target = FindPlayer(splitted[1]);
                        if (target != null)
                        {
                            int Count;
                            if (Int32.TryParse(splitted[2], out Count))
                            {
                                for (int i = 1; i < Count; i++)
                                    sendRcon("slap " + target.ClientID);
                            }
                            sendRcon("slap " + target.ClientID);
                        }
                        else sendRcon("tell " + Player.ClientID + " Player " + splitted[1] + " could not be found.");
                    }
                    else NotEnoughPerms(Player);
                    break;
                case "infinislap":
                case "islap":
                    if (Player.Level >= Commands.Default.SlapLevel)
                    {
                        target = FindPlayer(splitted[1]);
                        if (target != null)
                        {
                            int Count;
                            if (Int32.TryParse(splitted[2], out Count))
                            {
                                for (int i = 1; i < Count; i++)
                                {
                                    sendRcon("gh " + target.ClientID + " +8");
                                    sendRcon("slap " + target.ClientID);
                                }
                            }
                            sendRcon("gh " + target.ClientID + " 5");
                            sendRcon("slap " + target.ClientID);
                        }
                        else sendRcon("tell " + Player.ClientID + " Player " + splitted[1] + " could not be found.");
                    }
                    else NotEnoughPerms(Player);
                    break;
                case "deathslap":
                case "dslap":
                    if (Player.Level >= Commands.Default.SlapLevel)
                    {
                        target = FindPlayer(splitted[1]);
                        if (target != null)
                        {
                            sendRcon("gh " + target.ClientID + " -100");
                            sendRcon("slap " + target.ClientID);
                        }
                        else sendRcon("tell " + Player.ClientID + " Player " + splitted[1] + " could not be found.");
                    }
                    else NotEnoughPerms(Player);
                    break;
                case "mute":
                    if (Player.Level >= Commands.Default.MuteLevel)
                    {
                        target = FindPlayer(splitted[1]);
                        if (target != null)
                        {
                            sendRcon("mute " + target.ClientID);
                        }
                        else sendRcon("tell " + Player.ClientID + " Player " + splitted[1] + " could not be found.");
                    }
                    else NotEnoughPerms(Player);
                    break;
                case "bitchslap":
                    if (Player.Level >= Commands.Default.SlapLevel)
                    {
                        target = FindPlayer(splitted[1]);
                        if (target != null)
                        {
                            for (int i = 0; i < 25; i++)
                                sendRcon("slap " + target.ClientID);
                        }
                        else sendRcon("tell " + Player.ClientID + " Player " + splitted[1] + " could not be found.");
                    }
                    else NotEnoughPerms(Player);
                    break;
                case "nuke":
                    if (Player.Level >= Commands.Default.NukeLevel)
                    {
                        target = FindPlayer(splitted[1]);
                        if (target != null)
                        {
                            sendRcon("nuke " + target.ClientID);
                        }
                        else sendRcon("tell " + Player.ClientID + " Player " + splitted[1] + " could not be found.");
                    }
                    else NotEnoughPerms(Player);
                    break;
                case "ban":
                    if (Player.Level >= Commands.Default.BanLevel)
                    {
                        target = FindPlayer(splitted[1]);
                        if (target != null)
                        {
                            sendRcon("set filterBan 1");
                            sendRcon("addIP  " + target.IP);
                            sendRcon(@"say " + Player.Name + " has banned player " + target.Name + ".");
                            if (splitted[2] != null && splitted[2] != String.Empty) sendRcon(@"say Reason: " + splitted[2]);
                        }
                        else sendRcon("tell " + Player.ClientID + " Player " + splitted[1] + " could not be found.");
                    }
                    else NotEnoughPerms(Player);
                    break;
                case "info":
                    if (Player.Level >= Commands.Default.InfoLevel)
                    {
                        target = FindPlayer(splitted[1]);
                        if (target != null)
                        {
                            sendRcon("tell " + Player.ClientID + " " + target.Name + ": ");
                            sendRcon("tell " + Player.ClientID + " Connected from: " + target.City + ", " + target.Country);
                            sendRcon("tell " + Player.ClientID + " IP: " + target.IP);
                            sendRcon("tell " + Player.ClientID + " ID: " + target.ClientID);
                        }
                        else sendRcon("tell " + Player.ClientID + " Player " + splitted[1] + " could not be found.");
                    }
                    else NotEnoughPerms(Player);
                    break;
                case "ip":
                    if (Player.Level >= Commands.Default.InfoLevel)
                    {
                        target = FindPlayer(splitted[1]);
                        if (target != null)
                        {
                            sendRcon("tell " + Player.ClientID + " " + target.Name + "'s IP: " + target.IP);
                        }
                        else sendRcon("tell " + Player.ClientID + " Player " + splitted[1] + " could not be found.");
                    }
                    else NotEnoughPerms(Player);
                    break;
                case "tell":
                case "pm":
                    if (Player.Level >= Commands.Default.TellLevel)
                    {
                        target = FindPlayer(splitted[1]);
                        if (target != null)
                        { }
                        else sendRcon("tell " + Player.ClientID + " Player " + splitted[1] + " could not be found.");
                    }
                    else NotEnoughPerms(Player);
                    break;
                case "whereis":
                case "location":
                    if (Player.Level >= Commands.Default.LocationLevel)
                    {
                        target = FindPlayer(splitted[1]);
                        if (target != null)
                        {
                            sendRcon("tell " + Player.ClientID + " Player " + target.Name + " is connecting from " + target.City + ", " + target.Country + ".");
                        }
                        else sendRcon("tell " + Player.ClientID + " Player " + splitted[1] + " could not be found.");
                    }
                    else NotEnoughPerms(Player);
                    break;
                case "force":
                case "team":
                    if (Player.Level >= Commands.Default.ForceLevel)
                    {
                        target = FindPlayer(splitted[1]);
                        if (target != null)
                        {
                            sendRcon("forceteam " + target.ClientID + " " + splitted[2]);
                        }
                        else sendRcon("tell " + Player.ClientID + " Player " + splitted[1] + " could not be found.");
                    }
                    else NotEnoughPerms(Player);
                    break;

                //All other functions:
                case "veto":
                    if (Player.Level >= Commands.Default.VetoLevel)
                    {
                        sendRcon("veto");
                    }
                    else NotEnoughPerms(Player);
                    break;
                case "rcon":
                    if (Player.Level >= Commands.Default.RCONLevel)
                    {
                        String response = sendRcon(command.Remove(0, 5)).Replace("\0", "");
                        sendRcon("tell " + Player.ClientID + " " + response);
                    }
                    else NotEnoughPerms(Player);
                    break;
                case "bigtext":
                    if (Player.Level >= Commands.Default.BigtextLevel)
                    {
                        sendRcon("bigtext \"" + command.Remove(0, 8)+"\"");
                    }
                    else NotEnoughPerms(Player);
                    break;
                case "scream":
                    if (Player.Level >= Commands.Default.SayLevel)
                    {
                        for (int i = 0; i < 15; i++)
                        {
                            int random = new Random().Next(0, 8);
                            sendRcon("say ^" + random + command.Remove(0, 7));
                            System.Threading.Thread.Sleep(200);
                        }
                    }
                    else NotEnoughPerms(Player);
                    break;
                case "say":
                    if (Player.Level >= Commands.Default.SayLevel)
                    {
                        sendRcon("say ^3" + command.Remove(0, 4));
                    }
                    else NotEnoughPerms(Player);
                    break;
                case "time":
                    if (Player.Level >= Commands.Default.TimeLevel)
                    {
                        sendRcon("tell " + Player.ClientID + " Current servertime is: " + DateTime.Now.Hour + ":" + DateTime.Now.Minute + ":" + DateTime.Now.Second + " | " + DateTime.Now.Year + "/" + DateTime.Now.Month + "/" + DateTime.Now.Day);
                    }
                    else NotEnoughPerms(Player);
                    break;
                case "help":
                        sendRcon("tell " + Player.ClientID + " Your level is: " + Player.Level + ".");
                        sendRcon("tell " + Player.ClientID + " List of available commands: ");
                        if (Player.Level >= Commands.Default.AdminsLevel) sendRcon("tell " + Player.ClientID + " Admins - Displays a list of all online admins");
                        if (Player.Level >= Commands.Default.BanLevel) sendRcon("tell " + Player.ClientID + " Ban - Bans a player");
                        if (Player.Level >= Commands.Default.BigtextLevel) sendRcon("tell " + Player.ClientID + " Bigtext - Displays a bigtext in the center of the screen");
                        if (Player.Level >= Commands.Default.ForceLevel) sendRcon("tell " + Player.ClientID + " Force - Forces a player into the specified team");
                        if (Player.Level >= Commands.Default.InfoLevel) sendRcon("tell " + Player.ClientID + " Info - Shows information about a player");
                        if (Player.Level >= Commands.Default.KickLevel) sendRcon("tell " + Player.ClientID + " Kick - Kicks a player");
                        if (Player.Level >= Commands.Default.LevelLevel) sendRcon("tell " + Player.ClientID + " Level - Changes level of a given player");
                        if (Player.Level >= Commands.Default.LocationLevel) sendRcon("tell " + Player.ClientID + " Location - Shows the location of a player");
                        if (Player.Level >= Commands.Default.MapLevel) sendRcon("tell " + Player.ClientID + " Map - Changes the current map");
                        if (Player.Level >= Commands.Default.MapreloadLevel) sendRcon("tell " + Player.ClientID + " Mapreload - Reloads the current map");
                        if (Player.Level >= Commands.Default.MaprestartLevel) sendRcon("tell " + Player.ClientID + " Maprestart - Restarts current map");
                        if (Player.Level >= Commands.Default.MuteLevel) sendRcon("tell " + Player.ClientID + " Mute - Mutes a player");
                        if (Player.Level >= Commands.Default.NextmapLevel) sendRcon("tell " + Player.ClientID + " Nextmap Shows/Sets the next map");
                        if (Player.Level >= Commands.Default.NukeLevel) sendRcon("tell " + Player.ClientID + " Nuke - Nukes a player");
                        if (Player.Level >= Commands.Default.PauseLevel) sendRcon("tell " + Player.ClientID + " Pause - Pauses the game");
                        if (Player.Level >= Commands.Default.RCONLevel) sendRcon("tell " + Player.ClientID + " Rcon - Sends any RCON Command to the server");
                        if (Player.Level >= Commands.Default.SayLevel) sendRcon("tell " + Player.ClientID + " Say - Prints a message to the server");
                        if (Player.Level >= Commands.Default.ShuffleteamsLevel) sendRcon("tell " + Player.ClientID + " Shuffleteams - Shuffles teams and restarts game");
                        if (Player.Level >= Commands.Default.SlapLevel) sendRcon("tell " + Player.ClientID + " Slap - Slaps a player");
                        if (Player.Level >= Commands.Default.StartStopBotLevel) sendRcon("tell " + Player.ClientID + " Die - Kills the bot");
                        if (Player.Level >= Commands.Default.StartStopBotLevel) sendRcon("tell " + Player.ClientID + " Restartbot - Restarts the bot");
                        if (Player.Level >= Commands.Default.Swapteamslevel) sendRcon("tell " + Player.ClientID + " Swapteams - Swaps teams and restarts game");
                        if (Player.Level >= Commands.Default.TeamsLevel) sendRcon("tell " + Player.ClientID + " Teams - Autobalances teams");
                        if (Player.Level >= Commands.Default.TellLevel) sendRcon("tell " + Player.ClientID + " Tell - Sends a pm to a player");
                        if (Player.Level >= Commands.Default.TimeLevel) sendRcon("tell " + Player.ClientID + " Time - Shows servertime");
                        if (Player.Level >= Commands.Default.VetoLevel) sendRcon("tell " + Player.ClientID + " Veto - Cancels ongoing vote");
                        break;
                case "admins":
                    if (Player.Level >= Commands.Default.AdminsLevel)
                    {
                        String admins = "";
                        foreach (Player p in Players)
                        {
                            if (p.Level > 0) admins += p.Name + " (" + p.Level + "), ";
                        }
                        if (admins != String.Empty) admins = admins.Remove(admins.Length - 2, 2);
                        else admins = "None.";
                        sendRcon("tell " + Player.ClientID + " Admins online: " + admins);
                    }
                    else NotEnoughPerms(Player);
                    break;
                case "restartbot":
                    if (Player.Level >= Commands.Default.StartStopBotLevel)
                    {
                        Restart();
                    }
                    else NotEnoughPerms(Player);
                    break;
                case "die":
                    if (Player.Level >= Commands.Default.StartStopBotLevel)
                    {
                        sendRcon("say Stopping UrT Bot...");
                    }
                    else NotEnoughPerms(Player);
                    Stop();
                    break;
                case "shuffle":
                case "shuffleteams":
                    if (Player.Level >= Commands.Default.ShuffleteamsLevel)
                    {
                        sendRcon("shuffleteams");
                    }
                    else NotEnoughPerms(Player);
                    break;
                case "swap":
                case "swapteams":
                    if (Player.Level >= Commands.Default.Swapteamslevel)
                    { }
                    else NotEnoughPerms(Player);
                    break;
                case "map":
                    if (Player.Level >= Commands.Default.MapLevel)
                    {
                        sendRcon("map " + splitted[1]);
                    }
                    else NotEnoughPerms(Player);
                    break;
                case "nextmap":
                    if (splitted[1] == null || splitted[1] == String.Empty)
                    {
                        String nextmap = sendRcon("g_nextmap").Remove(0, 16);
                        nextmap = nextmap.Remove(nextmap.Length - 14, 14);
                        sendRcon("tell " + Player.ClientID + " Next map is: " + nextmap);
                    }
                    else
                    {
                        if (Player.Level >= Commands.Default.NextmapLevel)
                        {
                            sendRcon("set g_nextmap " + splitted[1]);
                            Nextmap = splitted[1];
                        }
                        else NotEnoughPerms(Player);
                    }
                    break;
                case "maprestart":
                    if (Player.Level >= Commands.Default.MaprestartLevel)
                    {
                        sendRcon("restart");
                    }
                    else NotEnoughPerms(Player);
                    break;
                case "mapreload":
                case "reloadmap":
                    if (Player.Level >= Commands.Default.MapreloadLevel)
                    {
                        sendRcon("reload");
                    }
                    else NotEnoughPerms(Player);
                    break;
                case "teams":
                    if (Player.Level >= Commands.Default.TeamsLevel)
                    {
                        int BlueTeamsPlayers = 0;
                        int RedTeamsPlayers = 0;
                        Player lastRed = new Player();
                        Player lastBlue = new Player();
                        foreach (Player pl in Players)
                        {
                            if (pl.Team == 1)
                            {
                                RedTeamsPlayers++;
                                lastRed = pl;
                            }
                            else if (pl.Team == 2)
                            {
                                BlueTeamsPlayers++;
                                lastBlue = pl;
                            }
                        }
                        if (RedTeamsPlayers > BlueTeamsPlayers + 1)
                        {
                            sendRcon("bigtext \"^7Autobalancing teams!\"");
                            sendRcon("forceteam " + lastRed.ClientID + " blue");
                        }
                        else if (BlueTeamsPlayers > RedTeamsPlayers + 1)
                        {
                            sendRcon("bigtext \"^7Autobalancing teams!\"");
                            sendRcon("forceteam " + lastBlue.ClientID + " red");
                        }
                    }
                    else NotEnoughPerms(Player);
                    break;
                case "balance":
                    if (Player.Level >= Commands.Default.MaprestartLevel)
                    {
                        List<Player> OldRedTeam = Players.FindAll(delegate(Player p) { return p.Team == 1; });
                        List<Player> OldBlueTeam = Players.FindAll(delegate(Player p) { return p.Team == 2; });
                        String Status = sendRcon("players").TrimStart('\n');
                        int RedTeamScore = Convert.ToInt32(Status.Split('\n')[2].Split(' ')[1].Remove(0, 2));
                        int BlueTeamScore = Convert.ToInt32(Status.Split('\n')[2].Split(' ')[2].Remove(0, 2));
                        int TeamScoreDifference = Math.Abs(RedTeamScore - BlueTeamScore);

                        int bestDiff = 10000;
                        Player bestRed = null;
                        Player bestBlue = null;
                        foreach (Player p1 in OldRedTeam)
                        {
                            foreach (Player p2 in OldBlueTeam)
                            {
                                int diff = Math.Abs(p1.Kills - p2.Kills);
                                if (diff < bestDiff)
                                {
                                    bestRed = p1;
                                    bestBlue = p2;
                                    bestDiff = diff;
                                }
                            }
                        }
                        sendRcon("bigtext ^1Skuffling!");
                        sendRcon("forceteam " + bestRed.ClientID + " blue");
                        sendRcon("forceteam " + bestBlue.ClientID + " red");
                    }
                    else NotEnoughPerms(Player);
                    break;
                case "pause":
                    if (Player.Level >= Commands.Default.PauseLevel)
                    {
                        sendRcon("pause");
                    }
                    else NotEnoughPerms(Player);
                    break;
                case "serverinfo":
                    if (Player.Level >= Commands.Default.ServerInfoLevel)
                    {

                    }
                    else NotEnoughPerms(Player);
                    break;
                case "list":
                    Players.Sort((a, b) => a.ClientID.CompareTo(b.ClientID));
                    sendRcon(@"tell " + Player.ClientID + " Online players:");
                    foreach (Player p in Players)
                    {
                        sendRcon(@"tell " + Player.ClientID + " #" + p.ClientID + ": " + p.Name + " ^7(" + p.IP + ")");
                    }
                    break;
                default:
                    sendRcon(@"tell " + Player.ClientID + " Couldn't find the command you just typed. (" + splitted[0] + ")");
                    break;
            }
        }

        private void NotEnoughPerms(Player player)
        {
            sendRcon(@"tell " + player.ClientID + " You don't have enough permissions to perform that action. ");
        }

        public void Say(String PlayerID, String Message)
        {
            if (Properties.Settings.Default.CommandPrefix.Length == 0 && Properties.Settings.Default.CommandSuffix.Length == 0) return;
            String[] info = Message.Split(' ');
            int ID;
            if (Int32.TryParse(PlayerID, out ID))
            {
                Player curPlayer = Players.Find(delegate(Player p) { return p.ClientID == ID; });
                if (curPlayer != null)
                {
                    if (Properties.Settings.Default.CommandPrefix.Length > 0 && Properties.Settings.Default.CommandSuffix.Length > 0)
                    {
                        if (info[0].StartsWith(Properties.Settings.Default.CommandPrefix) && info[0].EndsWith(Properties.Settings.Default.CommandSuffix))
                        {
                            info[0] = info[0].Remove(0, Properties.Settings.Default.CommandPrefix.Length).Remove(info[0].Length - Properties.Settings.Default.CommandSuffix.Length - 1, Properties.Settings.Default.CommandSuffix.Length);
                            Message = String.Empty;
                            for (int i = 0; i < info.Count(); i++)
                            {
                                Message += info[i] + " ";
                            }
                            Message.Trim();
                            ParseCommand(curPlayer, Message);
                        }
                    }
                    else if (Properties.Settings.Default.CommandPrefix.Length > 0 && Properties.Settings.Default.CommandSuffix.Length == 0)
                    {
                        if (info[0].StartsWith(Properties.Settings.Default.CommandPrefix))
                        {
                            info[0] = info[0].Remove(0, Properties.Settings.Default.CommandPrefix.Length);
                            Message = String.Empty;
                            for (int i = 0; i < info.Count(); i++)
                            {
                                Message += info[i] + " ";
                            }
                            Message.Trim();
                            ParseCommand(curPlayer, Message);
                        }
                    }
                    else if (Properties.Settings.Default.CommandPrefix.Length == 0 && Properties.Settings.Default.CommandSuffix.Length > 0)
                    {
                        if (info[0].EndsWith(Properties.Settings.Default.CommandSuffix))
                        {
                            info[0] = info[0].Remove(info[0].Length - Properties.Settings.Default.CommandSuffix.Length, Properties.Settings.Default.CommandSuffix.Length);
                            Message = String.Empty;
                            for (int i = 0; i < info.Count(); i++)
                            {
                                Message += info[i] + " ";
                            }
                            Message.Trim();
                            ParseCommand(curPlayer, Message);
                        }
                    }
                }
            }
        }
    }

    public class Player
    {
        String name;
        String cleanName;

        public Int32 ClientID { get; set; }
        public Int32 Team { get; set; }
        public String Name
        {
            get
            {
                return name;
            }
            set
            {
                this.name = value;
                this.cleanName = Name.Replace("^1", "").Replace("^2", "").Replace("^3", "").Replace("^4", "").Replace("^5", "").Replace("^6", "").Replace("^7", "").Replace("^8", "").Replace("^9", "");
            }
        }
        public String CleanName { get { return cleanName; } }
        public String GUID { get; set; }
        public String IP { get; set; }
        public Int32 Level { get; set; }

        public String City { get; set; }
        public String Country { get; set; }

        public Int32 Kills { get; set; }
        public Int32 Deaths { get; set; }
    }
}
