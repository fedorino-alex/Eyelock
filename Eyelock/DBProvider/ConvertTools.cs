using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Eyelock.Service;

namespace Eyelock.Database
{
    public static class ConvertTools
    {
        public static Eyelock.Service.User User(Eyelock.Database.User user)
        {
            return new Service.User()
            {
                DateOfBirth = user.DateOfBirth.ToUniversalTime().ToString("dd-MM-yyyy"),
                FirstName = user.FirstName ?? "",
                LastName = user.LastName ?? "",
                LeftIris = ToBase64(user.Iris.Image_LL, Eyelock.Net.Video.Frame.DefaultSize.Width, Eyelock.Net.Video.Frame.DefaultSize.Height),
                RightIris = ToBase64(user.Iris.Image_RR, Eyelock.Net.Video.Frame.DefaultSize.Width, Eyelock.Net.Video.Frame.DefaultSize.Height)
            };
        }

        public static string ToBase64(byte[] bytes, int width, int height)
        {
            System.Drawing.Image image = Eyelock.Imaging.Conversion.Converter.FrameToImage(bytes, width, height);
            System.Drawing.Bitmap b = new System.Drawing.Bitmap(image, new System.Drawing.Size(144, 144));
            using (System.IO.MemoryStream ms = new System.IO.MemoryStream())
            {
                b.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                bytes = ms.ToArray();
            }

            return string.Format("data:image/png;base64,{0}", Convert.ToBase64String(bytes));
        }

        public static string ToBase64(Eyelock.Eye.Sorting.Eye eye)
        {
            return ToBase64(eye.Data, eye.Width, eye.Height);
        }
    }
}
