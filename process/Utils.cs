using Microsoft.Extensions.Logging;
using process.Types;
using System.IO;

namespace process
{
    public static class Utils
    {
        public static string BuildPath(string type = null) =>
            string.IsNullOrWhiteSpace(type) || type == "Base"
                ? Program.Configuration["paths:Base"]
                : Path.Combine(
                    Program.Configuration["paths:Base"],
                    Program.Configuration[$"paths:{type}"]);

        public static string BuildPath(PathTypes type = PathTypes.Base)
            => BuildPath(type.ToString());

        public static string ChangePathType(string oldType, string newType, string path)
            => path.Replace(BuildPath(oldType), BuildPath(newType));

        public static string ChangePathType(PathTypes oldType, PathTypes newType, string path)
            => ChangePathType(oldType.ToString(), newType.ToString(), path);

        public static void MoveFile(PathTypes oldType, PathTypes newType, string filePath, ILogger logger = null)
        {
            var newPath = ChangePathType(
                            oldType,
                            newType,
                            filePath);

            logger.LogDebug($"{filePath} -> {newPath}");

            // ensure newPath exists
            Directory.CreateDirectory(Path.GetDirectoryName(newPath));

            File.Move(filePath, newPath);
            if(logger != null) logger.LogInformation($"moved to: {newPath}");
        }
    }
}
