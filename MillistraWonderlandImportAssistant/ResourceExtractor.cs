using System.Reflection;

namespace MillistraWonderlandImportAssistant
{
    public static class ResourceExtractor
    {
        // 缓存目录：把DLL里的图片解压到这里，方便Toast读取
        private static string CacheFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MillistraWonderlandImportAssistant", "Cache");
        private static string DllName = "GenshinHelperResources.dll";

        // 核心任务：提取图片用于Toast显示
        public static string ExtractImageForToast(string resourceName)
        {
            try
            {
                if (!Directory.Exists(CacheFolder)) Directory.CreateDirectory(CacheFolder);
                string targetPath = Path.Combine(CacheFolder, resourceName);

                // 如果缓存里已经有了，就不重复解压，提高速度
                if (File.Exists(targetPath)) return targetPath;

                ExtractResourceToFile(resourceName, targetPath);
                return targetPath;
            }
            catch
            {
                // 如果提取失败（例如DLL丢失），返回null
                return null;
            }
        }

        // 核心任务：确保那个保命的错误图标一定存在
        public static void EnsureSafeErrorIconExists(string savePath)
        {
            try
            {
                string dir = Path.GetDirectoryName(savePath);
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

                // 如果安全位置没有这个图标
                if (!File.Exists(savePath))
                {
                    // 尝试1：从DLL里解压
                    try
                    {
                        ExtractResourceToFile("ToastIcon-StatusError.png", savePath);
                    }
                    catch
                    {
                        // 尝试2：如果DLL也没了，看看EXE旁边有没有
                        string localPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ToastIcon-StatusError.png");
                        if (File.Exists(localPath))
                        {
                            File.Copy(localPath, savePath, true);
                        }
                    }
                }
            }
            catch { /* 如果这都失败了，那真的没办法了，但不应该崩溃 */ }
        }

        // 新方法！处理icon提取，返回提取后的文件路径
        public static string ExtractIconFile(string extension)
        {
            // 根据后缀名决定文件名
            string iconName = "";
            switch (extension.ToLower())
            {
                case ".gil": iconName = "GILFile.ico"; break;
                case ".gia": iconName = "GIAFile.ico"; break;
                case ".gir": iconName = "GIRFile.ico"; break;
                case ".gip": iconName = "GIPFile.ico"; break;
                default: return null;
            }

            // 定义存放路径 (AppData/Local/Millistra.../Icons/)
            // 我们把图标和缓存分开存，显得更整洁
            string iconFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "MillistraWonderlandImportAssistant", "Icons");

            if (!Directory.Exists(iconFolder)) Directory.CreateDirectory(iconFolder);

            string targetPath = Path.Combine(iconFolder, iconName);

            // 只有当文件不存在时才提取 (避免每次运行都覆盖，防止闪烁)
            if (!File.Exists(targetPath))
            {
                try
                {
                    ExtractResourceToFile(iconName, targetPath);
                }
                catch
                {
                    // 如果提取失败，返回 null
                    return null;
                }
            }

            return targetPath;
        }


        // 私有辅助方法：真正的解压逻辑
        private static void ExtractResourceToFile(string imageName, string outputPath)
        {
            // 加载资源DLL
            string dllPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, DllName);
            if (!File.Exists(dllPath)) throw new FileNotFoundException("资源DLL丢失！确认把程序的所有文件都解压出来了？");

            Assembly assembly = Assembly.LoadFrom(dllPath);

            // 在DLL的所有资源里查找名字匹配的 (例如包含 "GILFileIcon-64x64.png")
            string resourcePath = assembly.GetManifestResourceNames()
                .FirstOrDefault(r => r.EndsWith(imageName, StringComparison.OrdinalIgnoreCase));

            if (resourcePath != null)
            {
                using (Stream stream = assembly.GetManifestResourceStream(resourcePath))
                using (FileStream fileStream = new FileStream(outputPath, FileMode.Create))
                {
                    stream.CopyTo(fileStream);
                }
            }
            else
            {
                throw new Exception($"在DLL中找不到资源: {imageName}，您修改的时候对动态链接库的完整性造成了破坏qwq~");
            }
        }
    }
}