using Microsoft.Toolkit.Uwp.Notifications;

namespace MillistraWonderlandImportAssistant
{
    public static class ToastHelper
    {
        public static void ShowToast(string title, string content, string iconPath)
        {
            var builder = new ToastContentBuilder()
                .AddText(title)
                .AddText(content);

            if (!string.IsNullOrEmpty(iconPath) && File.Exists(iconPath))
            {
                // 使用默认效果显示图标
                builder.AddAppLogoOverride(new Uri(iconPath), ToastGenericAppLogoCrop.Default);
            }

            builder.Show();
        }

        public static void ShowErrorToast(string title, string content, string fallbackIconPath)
        {
            // 优先尝试从缓存获取Error图标
            string icon = ResourceExtractor.ExtractImageForToast("ToastIcon-StatusError.png");

            // 如果缓存获取失败，使用传入的 SafeFallback 路径
            if (string.IsNullOrEmpty(icon) || !File.Exists(icon))
            {
                icon = fallbackIconPath;
            }

            ShowToast(title, content, icon);
        }
    }
}