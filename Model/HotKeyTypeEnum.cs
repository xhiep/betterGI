using System;

namespace BetterGenshinImpact.Model;

public enum HotKeyTypeEnum
{
    GlobalRegister, // Phím tắt toàn cục
    KeyboardMonitor, // Lắng nghe bàn phím
}

public static class HotKeyTypeEnumExtension
{
    public static string ToChineseName(this HotKeyTypeEnum type)
    {
        return type switch
        {
     // GlobalRegister: Phím tắt có tác dụng ngay cả khi không nhấn vào cửa sổ game
        HotKeyTypeEnum.GlobalRegister => "Toàn cục", 
        
        // KeyboardMonitor: Chế độ theo dõi phím bấm để kích hoạt script
        HotKeyTypeEnum.KeyboardMonitor => "Lắng nghe",
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null),
        };
    }
}