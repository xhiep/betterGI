using System;
using BetterGenshinImpact.GameTask.Common;
using BetterGenshinImpact.Genshin.Settings;
using Microsoft.Extensions.Logging;

namespace BetterGenshinImpact.Genshin.Settings2;

public class GameSettingsChecker
{
    public static void LoadGameSettingsAndCheck()
    {
        try
        {
            var settingStr = GenshinGameSettings.GetStrFromRegistry();
            if (settingStr == null)
            {
                TaskControl.Logger.LogDebug("Lấy cài đặt game Genshin Impact thất bại");
                return;
            }

            GenshinGameSettings? settings = GenshinGameSettings.Parse(settingStr);
            if (settings == null)
            {
                TaskControl.Logger.LogDebug("Lấy cài đặt game Genshin Impact thất bại");
                return;
            }

            GenshinGameInputSettings? inputSettings = GenshinGameInputSettings.Parse(settings.InputData);
            if (inputSettings == null)
            {
                TaskControl.Logger.LogError("Lấy cài đặt đầu vào game Genshin Impact thất bại");
                return;
            }
            
            if (settings.GammaValue != "2.200000047683716")
            {
                TaskControl.Logger.LogError("Phát hiện độ sáng game không phải giá trị mặc định, sẽ ảnh hưởng đến hoạt động bình thường. Vui lòng khôi phục độ sáng mặc định tại Genshin: Cài Đặt Game → Hình Ảnh → Độ Sáng!");
            }

            if (inputSettings.MouseSenseIndex != 2
                || inputSettings.MouseSenseIndexY != 2
                || inputSettings.MouseFocusSenseIndex != 2
                || inputSettings.MouseFocusSenseIndexY != 2)
            {
                TaskControl.Logger.LogInformation("Hiện tại: Độ nhạy ngang camera {X1}, dọc {Y1}, ngang (chế độ ngắm) {X2}, dọc (chế độ ngắm) {Y2}",
                    inputSettings.MouseSenseIndex + 1, inputSettings.MouseSenseIndexY + 1,
                    inputSettings.MouseFocusSenseIndex + 1, inputSettings.MouseFocusSenseIndexY + 1);
                TaskControl.Logger.LogError("Phát hiện độ nhạy camera không phải giá trị mặc định 3. Điều này sẽ ảnh hưởng đến tất cả chức năng di chuyển góc nhìn. Vui lòng khôi phục tại Genshin: Cài Đặt Game → Điều Khiển!");
            }

            var lang = (TextLanguage)settings.DeviceLanguageType;
            if (lang != TextLanguage.SimplifiedChinese)
            {
                TaskControl.Logger.LogWarning("Ngôn ngữ game hiện tại là {Lang} (không phải Tiếng Trung giản thể). Một số chức năng nhận diện hình ảnh có thể hoạt động không chính xác.", lang);
            }
        }
        catch (Exception e)
        {
            TaskControl.Logger.LogDebug(e, "Lấy cài đặt game Genshin Impact thất bại");
        }
    }
}