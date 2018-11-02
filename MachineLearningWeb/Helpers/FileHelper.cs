using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace MachineLearningWeb.Helpers
{
    public class FileHelper
    {
        public static string GetSecureFileName(string fileName)
        {
            var invalids = Path.GetInvalidFileNameChars();
            var newName = string.Join("_", fileName.Split(invalids, StringSplitOptions.RemoveEmptyEntries)).TrimEnd('.');
            return newName;
        }
    }
}
