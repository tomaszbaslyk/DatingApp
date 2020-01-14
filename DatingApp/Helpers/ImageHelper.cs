using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;

namespace DatingApp.Helpers
{
    public static class ImageHelper
    {

        public static void Save(HttpPostedFileBase file)
        {
            string imageDirectory = HttpContext.Current.Server.MapPath("~/Images");

            if (Directory.Exists(imageDirectory))
            {
                string path = Path.Combine(imageDirectory, Path.GetFileName(file.FileName));
                file.SaveAs(path);
            } else
            {
                Directory.CreateDirectory(imageDirectory);
                string path = Path.Combine(imageDirectory, Path.GetFileName(file.FileName));
                file.SaveAs(path);
            }
        }

        public static bool IsValidExtension(HttpPostedFileBase file)
        {
            var splitFile = file.FileName.Split('.');
            var extension = splitFile[splitFile.Length - 1];

            if (extension.Equals("png") || extension.Equals("jpg") || extension.Equals("jpeg"))
            {
                return true;
            }

            return false;
        }
    }
}