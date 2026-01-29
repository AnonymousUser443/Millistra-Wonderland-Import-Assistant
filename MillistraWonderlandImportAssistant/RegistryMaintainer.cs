using Microsoft.Toolkit.Uwp.Notifications;
using Microsoft.Win32;

namespace MillistraWonderlandImportAssistant
{
    public static class RegistryMaintainer
    {
        private static string[] Extensions = { ".gil", ".gia", ".gir", ".gip" };
        private static string RegistryCheckPath = @"Software\MillistraWonderlandImportAssistant";
        private static string LastCheckValueName = "LastCheckDate";

        #region // 注册关联 (双击打开功能)
        public static void RegisterAssociations(string exePath)
        {
            try
            {
                foreach (var ext in Extensions)
                {
                    string progId = "Millistra.Level" + ext; // 唯一标识符

                    // 1. 扩展名指向 ProgId
                    using (var key = Registry.CurrentUser.CreateSubKey($@"Software\Classes\{ext}"))
                    {
                        key.SetValue("", progId);
                    }

                    // 2. 配置 ProgId 详情
                    using (var key = Registry.CurrentUser.CreateSubKey($@"Software\Classes\{progId}"))
                    {
                        key.SetValue("", "千星奇域关卡文件");

                        using (var iconKey = key.CreateSubKey("DefaultIcon"))
                        {
                            // 1. 尝试从 DLL 提取并获取 .ico 文件的物理路径
                            string iconPath = ResourceExtractor.ExtractIconFile(ext);

                            // 2. 如果提取成功，就指向那个 .ico 文件
                            if (!string.IsNullOrEmpty(iconPath) && File.Exists(iconPath))
                            {
                                iconKey.SetValue("", $"\"{iconPath}\"");
                            }
                            else
                            {
                                // 保底方案：如果找不到 ico，还是指向主程序
                                iconKey.SetValue("", $"\"{exePath}\",0");
                            }
                        }


                        // 设置打开命令："EXE路径" "%1"
                        using (var cmdKey = key.CreateSubKey(@"shell\open\command"))
                        {
                            cmdKey.SetValue("", $"\"{exePath}\" \"%1\"");
                        }
                    }
                }
                UpdateLastCheckTime();
            }
            catch (Exception ex)
            {
                throw new Exception("注册表关联失败: " + ex.Message);
            }
        }
        #endregion


        #region // 3天定期检查
        public static void CheckAndRestoreAssociations(string appName)
        {
            try
            {
                using (var key = Registry.CurrentUser.CreateSubKey(RegistryCheckPath))
                {
                    var lastCheckObj = key.GetValue(LastCheckValueName);
                    DateTime lastCheck = DateTime.MinValue;
                    if (lastCheckObj != null)
                    {
                        long binaryDate = (long)lastCheckObj;
                        lastCheck = DateTime.FromBinary(binaryDate);
                    }

                    // 超过3天检查一次
                    if ((DateTime.Now - lastCheck).TotalDays >= 3)
                    {
                        // 简单检查：看 .gil 是否还指向我们
                        bool broken = false;
                        string myProgId = "Millistra.Level.gil";
                        using (var checkKey = Registry.CurrentUser.OpenSubKey(@"Software\Classes\.gil"))
                        {
                            if (checkKey == null || checkKey.GetValue("")?.ToString() != myProgId)
                            {
                                broken = true;
                            }
                        }

                        if (broken)
                        {
                            // 修复
                            RegisterAssociations(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName);

                            // 警告用户
                            string iconPath = ResourceExtractor.ExtractImageForToast("ToastIcon-StatusError.png");
                            ToastHelper.ShowToast("不对头！", "文件注册被改了！旅行者是不是正在使用注册表管理工具？小砚已尝试自动修复文件关联~", iconPath);
                        }

                        UpdateLastCheckTime();
                    }
                }
            }
            catch { /* 检查过程如果不重要，可以忽略错误 */ }
        }
        #endregion

        private static void UpdateLastCheckTime()
        {
            using (var key = Registry.CurrentUser.CreateSubKey(RegistryCheckPath))
            {
                key.SetValue(LastCheckValueName, DateTime.Now.ToBinary());
            }
        }

        // 卸载清理
        public static void RemoveAssociations()
        {
            foreach (var ext in Extensions)
            {
                Registry.CurrentUser.DeleteSubKeyTree($@"Software\Classes\{ext}", false);
                Registry.CurrentUser.DeleteSubKeyTree($@"Software\Classes\Millistra.Level{ext}", false);
            }
            Registry.CurrentUser.DeleteSubKeyTree(RegistryCheckPath, false);
        }
    }
}