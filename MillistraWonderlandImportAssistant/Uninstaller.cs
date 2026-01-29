namespace MillistraWonderlandImportAssistant
{
    public static class Uninstaller
    {
        public static void Run(string appName)
        {
            try
            {
                Console.WriteLine("正在卸载千星小助手...");

                // 1. 清理注册表
                RegistryMaintainer.RemoveAssociations();

                // 2. 清理缓存文件 (AppData/Local/...)
                string cachePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), appName);
                if (Directory.Exists(cachePath))
                {
                    Directory.Delete(cachePath, true);
                }

                // 3. 尝试发最后一条通知
                string successIcon = ResourceExtractor.ExtractImageForToast("ToastIcon-StatusSuccess.png");
                ToastHelper.ShowToast("卸载完成", "所有关联和残留文件已清理。", successIcon);
            }
            catch (Exception ex)
            {
                Console.WriteLine("卸载出现错误: " + ex.Message);
                Console.ReadLine();
            }
        }
    }
}