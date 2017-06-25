using System;
using System.Threading;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;
using System.Runtime.InteropServices;

namespace TutClient
{


    class RDesktop
    {


        public static bool isShutdown = false;
        private static ImageConverter convert = new ImageConverter();
        private static byte[] img;
        

        //this code either sends the primary screen or if there is more than one screen will get the other screens too
        public static void StreamScreen()
        {
            
            while (true)
            {
                

                if (isShutdown)
                {
                    break;
                }
                try  // -added the try catch and break ,if the connection got lost the client would still try to send the screen and run on becoming unresponsive
                {

                    img = (byte[])convert.ConvertTo(Desktop(), typeof(byte[])); //--this crashes if its not the main window
                    if (img != null)// added this as sometimes it was null
                        Program.SendScreen(img);
                    Array.Clear(img, 0, img.Length);
                  
                    Thread.Sleep(Program.fps);  //this will get the fps from program.cs

                }
                catch
                {

                    isShutdown = true;
                    break;
                }
            }
        }
       

        private static Bitmap Desktop()
        {

            try
            {


                if (Program.ScreenNumber == 0)
                {
                    Rectangle bounds = Screen.PrimaryScreen.Bounds;
                    Bitmap screenshot = new Bitmap(bounds.Width, bounds.Height, PixelFormat.Format32bppArgb);

                    using (Graphics graph = Graphics.FromImage(screenshot)) //added this using to dispose of memory exceptions
                                                                           

                    {
                        graph.CopyFromScreen(bounds.X, bounds.Y, 0, 0, bounds.Size, CopyPixelOperation.SourceCopy); //--this crashes when ctrl alt del as its not the primary screen

                    }
                    GC.Collect(); //---added to cleanup resources
                    GC.WaitForPendingFinalizers();
                    Thread.SpinWait(5000);

                    return screenshot;
                   
                }
                else
                {
                    Bitmap bmpScreenshot = new Bitmap(Screen.AllScreens[Program.ScreenNumber].Bounds.Width, Screen.AllScreens[Program.ScreenNumber].Bounds.Height, PixelFormat.Format32bppArgb);
                    using (Graphics graph = Graphics.FromImage(bmpScreenshot))
                    {

                        graph.CopyFromScreen(Screen.AllScreens[Program.ScreenNumber].Bounds.X, Screen.AllScreens[1].Bounds.Y, 0, 0, Screen.AllScreens[Program.ScreenNumber].Bounds.Size, CopyPixelOperation.SourceCopy);

                    }                  

                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    Thread.SpinWait(5000);

                    return bmpScreenshot;
                   

                }
         
            }
            catch
            {



                //this works but only for the main window?? desktop window i need the main window handle in this case windows security dialogue window ,this gets the desktop window handle without task bar
                IntPtr desktopHwnd = FindWindowEx(GetDesktopWindow(), IntPtr.Zero, "Progman", "Program Manager"); 
              //  IntPtr desktopHwnd = FindWindowEx(GetDesktopWindow(), IntPtr.Zero, "Winlogon", "0");
               

                // get the desktop dimensions
                // if you don't get the correct values then set it manually
                var rect = new Rectangle();
            GetWindowRect(desktopHwnd, ref rect);

            // saving the screenshot to a bitmap
            var bmp = new Bitmap(rect.Width, rect.Height);
            Graphics memoryGraphics = Graphics.FromImage(bmp);
            IntPtr dc = memoryGraphics.GetHdc();
            PrintWindow(desktopHwnd, dc, 0);
            memoryGraphics.ReleaseHdc(dc);


                GC.Collect(); //----added to cleanup resources
                GC.WaitForPendingFinalizers();
                Thread.SpinWait(5000);


            return bmp;
           
            }


        }

        [DllImport("User32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool PrintWindow(IntPtr hwnd, IntPtr hdc, uint nFlags);

        [DllImport("user32.dll")]
        static extern bool GetWindowRect(IntPtr handle, ref Rectangle rect);

        [DllImport("user32.dll", EntryPoint = "GetDesktopWindow")]
        static extern IntPtr GetDesktopWindow();

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        static extern IntPtr FindWindowEx(IntPtr parentHandle, IntPtr childAfter, string lclassName, string windowTitle);


    }
}
