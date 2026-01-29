using Microsoft.Toolkit.Uwp.Notifications;
using System.Diagnostics;

namespace MillistraWonderlandImportAssistant
{
    class Program
    {
        // ================= 配置区域 =================
        static string AppName = "MillistraWonderlandImportAssistant";
        static string ResourceDllName = "GenshinHelperResources.dll";

        // 安全位置的错误图标路径
        static string SafeErrorIconPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            AppName, "ToastIcon-StatusError.png");
        // ===========================================

        static void Main(string[] args)
        {
            try
            {
                // 0. 启动自检
                ResourceExtractor.EnsureSafeErrorIconExists(SafeErrorIconPath);

                // 1. 检查卸载模式
                if (args.Length > 0 && args[0] == "--uninstall")
                {
                    Uninstaller.Run(AppName);
                    return;
                }

                // 2. 首次运行初始化
                if (args.Length == 0)
                {
                    FirstRunSetup();
                }
                else
                {
                    // 3. 处理文件导入
                    string filePath = args[0];
                    HandleFileImport(filePath);
                }

                // 4. 定期维护
                RegistryMaintainer.CheckAndRestoreAssociations(AppName);
            }
            catch (Exception ex)
            {
                ToastHelper.ShowErrorToast("程序遇到未知错误", ex.Message, SafeErrorIconPath);
            }
        }

        // --- 获取所有有效的原神目标路径 (同时支持国服和国际服) ---
        static List<string> GetValidDestinationPaths(bool createIfMissing = false)
        {
            var validPaths = new List<string>();
            string userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            string baseLocalLow = Path.Combine(userProfile, @"AppData\LocalLow\miHoYo");

            // 定义可能的路径列表
            var possiblePaths = new Dictionary<string, string>
            {
                { "CN", Path.Combine(baseLocalLow, @"原神\BeyondLocal\Beyond_Local_Export") },
                { "Global", Path.Combine(baseLocalLow, @"Genshin Impact\BeyondLocal\Beyond_Local_Export") }
            };

            foreach (var kvp in possiblePaths)
            {
                string fullPath = kvp.Value;

                // 检查父级目录是否存在 (例如 "miHoYo\原神")
                // 我们通过检查父级目录来判断用户是否安装了该版本的游戏
                string gameDir = Path.GetFullPath(Path.Combine(fullPath, @"..\..\"));

                if (Directory.Exists(gameDir))
                {
                    // 如果 createIfMissing 为 true，我们强制创建深层目录
                    if (createIfMissing && !Directory.Exists(fullPath))
                    {
                        Directory.CreateDirectory(fullPath);
                    }

                    // 只要最终目录存在（或刚被创建），就加入有效列表
                    if (Directory.Exists(fullPath))
                    {
                        validPaths.Add(fullPath);
                    }
                }
            }

            return validPaths;
        }

        // --- 首次运行逻辑 ---
        static void FirstRunSetup()
        {
            Console.Title = "千星奇域小助手 - 初始化（Azure Inkstone Theme Design）";
            Console.WriteLine("正在初始化千星奇域小助手...");
            Console.WriteLine("本程序由Bilibili @StudentOP和他的AI小助手一起开发");
            Console.WriteLine("请打开《原神》(国服或国际服)，以便小砚确认旅行者的游戏环境...");
            Console.WriteLine("(如果在游戏中，请切换出来等待几秒)");

            // 等待原神进程
            bool genshinFound = false;
            while (!genshinFound)
            {
                Process[] cnProcess = Process.GetProcessesByName("YuanShen");
                Process[] globalProcess = Process.GetProcessesByName("GenshinImpact");

                if (cnProcess.Length > 0)
                {
                    genshinFound = true;
                    Console.WriteLine("\n[成功] 检测到原神(国服)已运行！");
                }
                else if (globalProcess.Length > 0)
                {
                    genshinFound = true;
                    Console.WriteLine("\n[成功] 检测到原神(国际服)已运行！");
                }
                else
                {
                    Thread.Sleep(2000);
                }
            }

            Console.WriteLine("正在配置环境...");

            // 获取并创建必要的文件夹 (参数 true 表示强制创建)
            var paths = GetValidDestinationPaths(true);

            if (paths.Count == 0)
            {
                Console.WriteLine("[警告] 未能自动定位到标准数据目录。旅行者您是第一次游玩千星奇域......？");
                Console.WriteLine("我们将尝试创建默认国服路径...");
                // 保底创建一个国服路径，防止报错
                string fallback = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                    @"AppData\LocalLow\miHoYo\原神\BeyondLocal\Beyond_Local_Export");
                Directory.CreateDirectory(fallback);
            }
            else
            {
                Console.WriteLine($"已定位到 {paths.Count} 个游戏数据目录。");
            }

            // 注册文件关联
            string myExePath = Process.GetCurrentProcess().MainModule.FileName;
            RegistryMaintainer.RegisterAssociations(myExePath);

            Console.WriteLine("配置完成！正在验证注册...");
            Thread.Sleep(1000);

            // 发送成功通知
            string successIcon = ResourceExtractor.ExtractImageForToast("ToastIcon-StatusSuccess.png");
            ToastHelper.ShowToast("恭喜！", "千星小助手已经成功安装！", successIcon);
        }

        // --- 文件导入处理逻辑 ---
        static void HandleFileImport(string filePath)
        {
            string ext = Path.GetExtension(filePath).ToLower();
            string fileName = Path.GetFileName(filePath);

            if (ext == ".gil" || ext == ".gia")
            {
                try
                {
                    // 获取所有有效路径 (不强制创建，因为如果游戏没装，就不应该往那复制)
                    // 但考虑到用户可能清理过缓存，我们这里设为 true 比较保险，
                    // 只要 "miHoYo\原神" 这种父级文件夹存在，我们就认为游戏在，就建子文件夹。
                    var targets = GetValidDestinationPaths(true);

                    if (targets.Count == 0)
                    {
                        throw new DirectoryNotFoundException("未找到原神(国服/国际服)的数据目录，请旅行者先运行一次游戏~！");
                    }

                    int successCount = 0;
                    foreach (var targetFolder in targets)
                    {
                        string destPath = Path.Combine(targetFolder, fileName);
                        File.Copy(filePath, destPath, true); // 覆盖
                        successCount++;
                    }

                    string toastIcon = ResourceExtractor.ExtractImageForToast("ToastIcon-StatusSuccess.png");

                    string msg = (successCount > 1)
                        ? $"文件已同步导入到 {successCount} 个游戏版本中！"
                        : $"文件 {fileName} 已就绪。";

                    ToastHelper.ShowToast("文件已经导入成功~", msg, toastIcon);
                }
                catch (FileNotFoundException ex)
                {
                    ToastHelper.ShowErrorToast("失败 - 找不到文件", "咦？小砚找不到旅行者刚刚指定的文件了qwq......\n"+ex.Message, SafeErrorIconPath);
                }
                catch (DirectoryNotFoundException ex)
                {
                    ToastHelper.ShowErrorToast("失败 - 找不到目录", "咦？小砚找不到旅行者刚刚指定的文件在哪儿了qwq......\n"+ex.Message, SafeErrorIconPath);
                }
                catch (IOException ex)
                {
                    ToastHelper.ShowErrorToast("失败 - 读写异常", "脾气真差！文件似乎被霸占了！操作不了！\n" + ex.Message, SafeErrorIconPath);
                }
                catch (UnauthorizedAccessException ex)
                {
                    ToastHelper.ShowErrorToast("失败 - 权限不足", "小砚没有权限操作这个文件呢qwq......\n" + ex.Message, SafeErrorIconPath);
                }
                catch (Exception ex)
                {
                    ToastHelper.ShowErrorToast("导入失败", ex.Message, SafeErrorIconPath);
                }
            }
            else if (ext == ".gir" )
            {
                string puzzledIcon = ResourceExtractor.ExtractImageForToast("ToastIcon-StatusPuzzled.png");
                ToastHelper.ShowToast("导入异常", "这是千星奇域的运行时（Runtime）文件，不是可供导入的关卡/元件文件呢~", puzzledIcon);
            } else if(ext == ".gip"){
                string puzzledIcon = ResourceExtractor.ExtractImageForToast("ToastIcon-StatusPuzzled.png");
                ToastHelper.ShowToast("导入异常", "这是千星奇域的玩家游玩数据（Player）文件，不是可供导入的关卡/元件文件呢~", puzzledIcon);

            } else
            {
                ToastHelper.ShowErrorToast("格式错误", "不支持的文件格式qwq...", SafeErrorIconPath);
            }
        }
    }
}