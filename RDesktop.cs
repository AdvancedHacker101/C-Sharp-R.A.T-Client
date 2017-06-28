using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace TutClient
{
    class RDesktop
    {

        public static bool isShutdown = false;
        private static System.Drawing.ImageConverter convert = new System.Drawing.ImageConverter();
        private static Byte[] img;

        public static void StreamScreen()
        {
            while (true)
            {
                if (isShutdown) break;
                img = (Byte[])convert.ConvertTo(Desktop(), typeof(Byte[]));
                Program.SendScreen(img);
                Array.Clear(img, 0, img.Length);
                Thread.Sleep(100);
                // Program.mc();
            }
        }


        private static System.Drawing.Bitmap Desktop()
        {
            System.Drawing.Rectangle bounds = System.Windows.Forms.Screen.PrimaryScreen.Bounds;
            System.Drawing.Bitmap screenshot = new System.Drawing.Bitmap(bounds.Width, bounds.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            System.Drawing.Graphics graph = System.Drawing.Graphics.FromImage(screenshot);
            graph.CopyFromScreen(bounds.X, bounds.Y, 0, 0, bounds.Size, System.Drawing.CopyPixelOperation.SourceCopy);
            return screenshot;
        }

    }
}
