using System.Collections.Generic;
using System.Linq;

namespace BetterGenshinImpact.GameTask.AutoFishing.Model;

/// <summary>
/// 模仿Java实现的多属性枚举类
/// 按形态大类分类的原神鱼类枚举
/// </summary>
public class BigFishType
{
    public static readonly BigFishType Medaka = new("medaka", BaitType.FruitPasteBait, "Cá Khổng Tước", 0);
    public static readonly BigFishType LargeMedaka = new("large medaka", BaitType.FruitPasteBait, "Cá Khổng Tước - Lớn", 1);
    public static readonly BigFishType Stickleback = new("stickleback", BaitType.RedrotBait, "Cá Gai", 2);
    public static readonly BigFishType Koi = new("koi", BaitType.FakeFlyBait, "Giả Long", 3);
    public static readonly BigFishType KoiHead = new("koi head", BaitType.FakeFlyBait, "Giả Long - Đầu", 3);
    public static readonly BigFishType Butterflyfish = new("butterflyfish", BaitType.FalseWormBait, "Cá Bướm", 4);
    public static readonly BigFishType Pufferfish = new("pufferfish", BaitType.FakeFlyBait, "Cá Nóc", 5);

    public static readonly BigFishType Ray = new("ray", BaitType.FakeFlyBait, "Cá Đuối", 6);

    // public static readonly BigFishType FormaloRay = new("formalo ray", "飞蝇假饵", "佛玛洛鳐");
    // public static readonly BigFishType DivdaRay = new("divda ray", "飞蝇假饵", "迪芙妲鳐");
    public static readonly BigFishType Angler = new("angler", BaitType.SugardewBait, "Cá Sừng", 7);
    public static readonly BigFishType AxeMarlin = new("axe marlin", BaitType.SugardewBait, "Cá Rìu", 8);
    public static readonly BigFishType HeartfeatherBass = new("heartfeather bass", BaitType.SourBait, "Cá Vược", 9);
    public static readonly BigFishType MaintenanceMek = new("maintenance mek", BaitType.FlashingMaintenanceMekBait, "Cơ Quan Bảo Trì", 10);
    public static readonly BigFishType Unihornfish = new("unihornfish", BaitType.SpinelgrainBait, "Cá Một Sừng", 10);
    public static readonly BigFishType Sunfish = new("sunfish", BaitType.SpinelgrainBait, "Cá Mặt Trăng", 7);
    public static readonly BigFishType Rapidfish = new("rapidfish", BaitType.SpinelgrainBait, "Cá Lướt Sóng", 9);
    public static readonly BigFishType PhonyUnihornfish = new("phony unihornfish", BaitType.EmberglowBait, "Cá Một Sừng Nhiên Tố", 10);
    public static readonly BigFishType MagmaRapidfish = new("magma rapidfish", BaitType.EmberglowBait, "Cá Lướt Sóng Dung Nham", 9);
    public static readonly BigFishType SecretSourceScoutSweeper = new ("secret source", BaitType.EmberglowBait, "Cơ Quan Bí Nguồn - Tuần Tra", 9);

    public static readonly BigFishType MaulerShark = new ("mauler shark", BaitType.RefreshingLakkaBait, "Cá Mập", 9);
    public static readonly BigFishType CrystalEye = new("crystal eye", BaitType.RefreshingLakkaBait, "Cá Mắt Kính", 9);
    public static readonly BigFishType AxeheadFish = new ("axehead", BaitType.BerryBait, "Cá Đầu Rìu", 9);

    public static IEnumerable<BigFishType> Values
    {
        get
        {
            yield return Medaka;
            yield return LargeMedaka;
            yield return Stickleback;
            yield return Koi;
            yield return KoiHead;
            yield return Butterflyfish;
            yield return Pufferfish;
            yield return Ray;
            // yield return FormaloRay;
            // yield return DivdaRay;
            yield return Angler;
            yield return AxeMarlin;
            yield return HeartfeatherBass;
            yield return MaintenanceMek;
            yield return Unihornfish;
            yield return Sunfish;
            yield return Rapidfish;
            yield return PhonyUnihornfish;
            yield return MagmaRapidfish;
            yield return SecretSourceScoutSweeper;
            yield return MaulerShark;
            yield return CrystalEye;
            yield return AxeheadFish;
        }
    }

    public string Name { get; private set; }
    public BaitType BaitType { get; private set; }
    public string ChineseName { get; private set; }

    public int NetIndex { get; private set; }

    private BigFishType(string name, BaitType baitType, string chineseName, int netIndex)
    {
        Name = name;
        BaitType = baitType;
        ChineseName = chineseName;
        NetIndex = netIndex;
    }

    public static BigFishType FromName(string name)
    {
        foreach (var fishType in Values)
        {
            if (fishType.Name == name)
            {
                return fishType;
            }
        }

        throw new KeyNotFoundException($"BigFishType {name} not found");
    }

    public static int GetIndex(BigFishType e)
    {
        return e.NetIndex;
    }
}
