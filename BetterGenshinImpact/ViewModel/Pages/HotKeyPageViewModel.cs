using BetterGenshinImpact.Core.Config;
using BetterGenshinImpact.Core.Recognition.OCR;
using BetterGenshinImpact.Core.Recognition.OpenCv;
using BetterGenshinImpact.Core.Recorder;
using BetterGenshinImpact.Core.Script;
using BetterGenshinImpact.Core.Simulator;
using BetterGenshinImpact.GameTask;
using BetterGenshinImpact.GameTask.AutoArtifactSalvage;
using BetterGenshinImpact.GameTask.AutoFight;
using BetterGenshinImpact.GameTask.AutoFight.Assets;
using BetterGenshinImpact.GameTask.AutoPathing;
using BetterGenshinImpact.GameTask.AutoPathing.Handler;
using BetterGenshinImpact.GameTask.AutoTrackPath;
using BetterGenshinImpact.GameTask.Common;
using BetterGenshinImpact.GameTask.Common.BgiVision;
using BetterGenshinImpact.GameTask.Common.Job;
using BetterGenshinImpact.GameTask.Common.Map.Maps.Base;
using BetterGenshinImpact.GameTask.Macro;
using BetterGenshinImpact.GameTask.Model.Area;
using BetterGenshinImpact.GameTask.QuickBuy;
using BetterGenshinImpact.GameTask.QuickSereniteaPot;
using BetterGenshinImpact.GameTask.QuickTeleport.Assets;
using BetterGenshinImpact.GameTask.UseRedeemCode;
using BetterGenshinImpact.Helpers;
using BetterGenshinImpact.Helpers.Extensions;
using BetterGenshinImpact.Model;
using BetterGenshinImpact.Service.Interface;
using BetterGenshinImpact.View;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using Microsoft.Extensions.Logging;
using OpenCvSharp;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Vanara.PInvoke;
using HotKeySettingModel = BetterGenshinImpact.Model.HotKeySettingModel;

namespace BetterGenshinImpact.ViewModel.Pages;

public partial class HotKeyPageViewModel : ObservableObject, IViewModel
{
    private readonly ILogger<HotKeyPageViewModel> _logger;
    private readonly TaskSettingsPageViewModel _taskSettingsPageViewModel;
    public AllConfig Config { get; set; }

    [ObservableProperty]
    private ObservableCollection<HotKeySettingModel> _hotKeySettingModels = [];

    public HotKeyPageViewModel(IConfigService configService, ILogger<HotKeyPageViewModel> logger, TaskSettingsPageViewModel taskSettingsPageViewModel)
    {
        _logger = logger;
        _taskSettingsPageViewModel = taskSettingsPageViewModel;
        // 获取配置
        Config = configService.Get();

        // 构建快捷键配置列表
        BuildHotKeySettingModelList();

        var list = GetAllNonDirectoryHotkey(HotKeySettingModels);
        foreach (var hotKeyConfig in list)
        {
            hotKeyConfig.RegisterHotKey();
            hotKeyConfig.PropertyChanged += (sender, e) =>
            {
                if (sender is HotKeySettingModel model)
                {
                    // 反射更新配置

                    // 更新快捷键
                    if (e.PropertyName == "HotKey")
                    {
                        Debug.WriteLine($"{model.FunctionName} 快捷键变更为 {model.HotKey}");
                        var pi = Config.HotKeyConfig.GetType().GetProperty(model.ConfigPropertyName, BindingFlags.Public | BindingFlags.Instance);
                        if (null != pi && pi.CanWrite)
                        {
                            var str = model.HotKey.ToString();
                            if (str == "< None >")
                            {
                                str = "";
                            }

                            pi.SetValue(Config.HotKeyConfig, str, null);
                        }
                    }

                    // 更新快捷键类型
                    if (e.PropertyName == "HotKeyType")
                    {
                        Debug.WriteLine($"{model.FunctionName} 快捷键类型变更为 {model.HotKeyType.ToChineseName()}");
                        model.HotKey = HotKey.None;
                        var pi = Config.HotKeyConfig.GetType().GetProperty(model.ConfigPropertyName + "Type", BindingFlags.Public | BindingFlags.Instance);
                        if (null != pi && pi.CanWrite)
                        {
                            pi.SetValue(Config.HotKeyConfig, model.HotKeyType.ToString(), null);
                        }
                    }

                    RemoveDuplicateHotKey(model);
                    model.UnRegisterHotKey();
                    model.RegisterHotKey();
                }
            };
        }
    }

    /// <summary>
    /// 移除重复的快捷键配置
    /// </summary>
    /// <param name="current"></param>
    private void RemoveDuplicateHotKey(HotKeySettingModel current)
    {
        if (current.HotKey.IsEmpty)
        {
            return;
        }

        var list = GetAllNonDirectoryHotkey(HotKeySettingModels);
        foreach (var hotKeySettingModel in list)
        {
            if (hotKeySettingModel.HotKey.IsEmpty)
            {
                continue;
            }

            if (hotKeySettingModel.ConfigPropertyName != current.ConfigPropertyName && hotKeySettingModel.HotKey == current.HotKey)
            {
                hotKeySettingModel.HotKey = HotKey.None;
            }
        }
    }

    public static List<HotKeySettingModel> GetAllNonDirectoryHotkey(IEnumerable<HotKeySettingModel> modelList)
    {
        var list = new List<HotKeySettingModel>();
        foreach (var hotKeySettingModel in modelList)
        {
            if (!hotKeySettingModel.IsDirectory)
            {
                list.Add(hotKeySettingModel);
            }

            list.AddRange(GetAllNonDirectoryChildren(hotKeySettingModel));
        }

        return list;
    }

    public static List<HotKeySettingModel> GetAllNonDirectoryChildren(HotKeySettingModel model)
    {
        var result = new List<HotKeySettingModel>();

        if (model.Children.Count == 0)
        {
            return result;
        }

        foreach (var child in model.Children)
        {
            if (!child.IsDirectory)
            {
                result.Add(child);
            }

            // 递归调用以获取子节点中的非目录对象
            result.AddRange(GetAllNonDirectoryChildren(child));
        }

        return result;
    }

    private void BuildHotKeySettingModelList()
    {
        // 一级目录/快捷键
        var bgiEnabledHotKeySettingModel = new HotKeySettingModel(
            "Bật/Tắt BetterGI",
            nameof(Config.HotKeyConfig.BgiEnabledHotkey),
            Config.HotKeyConfig.BgiEnabledHotkey,
            Config.HotKeyConfig.BgiEnabledHotkeyType,
            (_, _) => { WeakReferenceMessenger.Default.Send(new PropertyChangedMessage<object>(this, "SwitchTriggerStatus", "", "")); }
        );
        HotKeySettingModels.Add(bgiEnabledHotKeySettingModel);

        var systemDirectory = new HotKeySettingModel(
            "Điều Khiển Hệ Thống"
        );
        HotKeySettingModels.Add(systemDirectory);

        var timerDirectory = new HotKeySettingModel(
            "Tác Vụ Thời Gian Thực"
        );
        HotKeySettingModels.Add(timerDirectory);

        var soloTaskDirectory = new HotKeySettingModel(
            "Tác Vụ Độc Lập"
        );
        HotKeySettingModels.Add(soloTaskDirectory);

        var macroDirectory = new HotKeySettingModel(
            "Hỗ Trợ Điều Khiển"
        );
        HotKeySettingModels.Add(macroDirectory);

        var devDirectory = new HotKeySettingModel(
            "Nhà Phát Triển"
        );
        HotKeySettingModels.Add(devDirectory);

        // 二级快捷键
        systemDirectory.Children.Add(new HotKeySettingModel(
            "Dừng Script/Tác Vụ Hiện Tại",
            nameof(Config.HotKeyConfig.CancelTaskHotkey),
            Config.HotKeyConfig.CancelTaskHotkey,
            Config.HotKeyConfig.CancelTaskHotkeyType,
            (_, _) =>
            {
                _logger.LogInformation("Phát hiện nhấn phím tắt dừng {Key}, dừng tác vụ hiện tại", Config.HotKeyConfig.CancelTaskHotkey);
                CancellationContext.Instance.ManualCancel();
            }
        ));
        systemDirectory.Children.Add(new HotKeySettingModel(
            "Tạm Dừng Script/Tác Vụ Hiện Tại",
            nameof(Config.HotKeyConfig.SuspendHotkey),
            Config.HotKeyConfig.SuspendHotkey,
            Config.HotKeyConfig.SuspendHotkeyType,
            (_, _) => { RunnerContext.Instance.IsSuspend = !RunnerContext.Instance.IsSuspend; }
        ));
        var takeScreenshotHotKeySettingModel = new HotKeySettingModel(
            "Chụp Màn Hình Game",
            nameof(Config.HotKeyConfig.TakeScreenshotHotkey),
            Config.HotKeyConfig.TakeScreenshotHotkey,
            Config.HotKeyConfig.TakeScreenshotHotkeyType,
            (_, _) => { TaskTriggerDispatcher.Instance().TakeScreenshot(); }
        );
        systemDirectory.Children.Add(takeScreenshotHotKeySettingModel);

        systemDirectory.Children.Add(new HotKeySettingModel(
            "Bật/Tắt Cửa Sổ Nhật Ký Và Trạng Thái",
            nameof(Config.HotKeyConfig.LogBoxDisplayHotkey),
            Config.HotKeyConfig.LogBoxDisplayHotkey,
            Config.HotKeyConfig.LogBoxDisplayHotkeyType,
            (_, _) =>
            {
                TaskContext.Instance().Config.MaskWindowConfig.ShowLogBox = !TaskContext.Instance().Config.MaskWindowConfig.ShowLogBox;
                // 与状态窗口同步
                TaskContext.Instance().Config.MaskWindowConfig.ShowStatus = TaskContext.Instance().Config.MaskWindowConfig.ShowLogBox;
            }
        ));

        var autoPickEnabledHotKeySettingModel = new HotKeySettingModel(
            "Bật/Tắt Tự Động Nhặt",
            nameof(Config.HotKeyConfig.AutoPickEnabledHotkey),
            Config.HotKeyConfig.AutoPickEnabledHotkey,
            Config.HotKeyConfig.AutoPickEnabledHotkeyType,
            (_, _) =>
            {
                TaskContext.Instance().Config.AutoPickConfig.Enabled = !TaskContext.Instance().Config.AutoPickConfig.Enabled;
                _logger.LogInformation("Đổi trạng thái {Name} thành [{Enabled}]", "Tự động nhặt", ToChinese(TaskContext.Instance().Config.AutoPickConfig.Enabled));
            }
        );
        timerDirectory.Children.Add(autoPickEnabledHotKeySettingModel);

        var autoSkipEnabledHotKeySettingModel = new HotKeySettingModel(
            "Bật/Tắt Tự Động Cốt Truyện",
            nameof(Config.HotKeyConfig.AutoSkipEnabledHotkey),
            Config.HotKeyConfig.AutoSkipEnabledHotkey,
            Config.HotKeyConfig.AutoSkipEnabledHotkeyType,
            (_, _) =>
            {
                TaskContext.Instance().Config.AutoSkipConfig.Enabled = !TaskContext.Instance().Config.AutoSkipConfig.Enabled;
                _logger.LogInformation("Đổi trạng thái {Name} thành [{Enabled}]", "Tự động cốt truyện", ToChinese(TaskContext.Instance().Config.AutoSkipConfig.Enabled));
            }
        );
        timerDirectory.Children.Add(autoSkipEnabledHotKeySettingModel);

        timerDirectory.Children.Add(new HotKeySettingModel(
            "Bật/Tắt Tự Động Mời Hẹn",
            nameof(Config.HotKeyConfig.AutoSkipHangoutEnabledHotkey),
            Config.HotKeyConfig.AutoSkipHangoutEnabledHotkey,
            Config.HotKeyConfig.AutoSkipHangoutEnabledHotkeyType,
            (_, _) =>
            {
                TaskContext.Instance().Config.AutoSkipConfig.AutoHangoutEventEnabled = !TaskContext.Instance().Config.AutoSkipConfig.AutoHangoutEventEnabled;
                _logger.LogInformation("Đổi trạng thái {Name} thành [{Enabled}]", "Tự động mời hẹn", ToChinese(TaskContext.Instance().Config.AutoSkipConfig.AutoHangoutEventEnabled));
            }
        ));

        var autoFishingEnabledHotKeySettingModel = new HotKeySettingModel(
            "Bật/Tắt Tự Động Câu Cá",
            nameof(Config.HotKeyConfig.AutoFishingEnabledHotkey),
            Config.HotKeyConfig.AutoFishingEnabledHotkey,
            Config.HotKeyConfig.AutoFishingEnabledHotkeyType,
            (_, _) =>
            {
                TaskContext.Instance().Config.AutoFishingConfig.Enabled = !TaskContext.Instance().Config.AutoFishingConfig.Enabled;
                _logger.LogInformation("Đổi trạng thái {Name} thành [{Enabled}]", "Tự động câu cá", ToChinese(TaskContext.Instance().Config.AutoFishingConfig.Enabled));
            }
        );
        timerDirectory.Children.Add(autoFishingEnabledHotKeySettingModel);

        var quickTeleportEnabledHotKeySettingModel = new HotKeySettingModel(
            "Bật/Tắt Dịch Chuyển Nhanh",
            nameof(Config.HotKeyConfig.QuickTeleportEnabledHotkey),
            Config.HotKeyConfig.QuickTeleportEnabledHotkey,
            Config.HotKeyConfig.QuickTeleportEnabledHotkeyType,
            (_, _) =>
            {
                TaskContext.Instance().Config.QuickTeleportConfig.Enabled = !TaskContext.Instance().Config.QuickTeleportConfig.Enabled;
                _logger.LogInformation("Đổi trạng thái {Name} thành [{Enabled}]", "Dịch chuyển nhanh", ToChinese(TaskContext.Instance().Config.QuickTeleportConfig.Enabled));
            }
        );
        timerDirectory.Children.Add(quickTeleportEnabledHotKeySettingModel);
        
        var skillCdEnabledHotKeySettingModel = new HotKeySettingModel(
            "Bật/Tắt Hiển Thị Cooldown",
            nameof(Config.HotKeyConfig.SkillCdEnabledHotkey),
            Config.HotKeyConfig.SkillCdEnabledHotkey,
            Config.HotKeyConfig.SkillCdEnabledHotkeyType,
            (_, _) =>
            {
                TaskContext.Instance().Config.SkillCdConfig.Enabled = !TaskContext.Instance().Config.SkillCdConfig.Enabled;
                _logger.LogInformation("Đổi trạng thái {Name} thành [{Enabled}]", "Gợi ý hồi chiêu", ToChinese(TaskContext.Instance().Config.SkillCdConfig.Enabled));
            }
        );
        timerDirectory.Children.Add(skillCdEnabledHotKeySettingModel);

        var quickTeleportTickHotKeySettingModel = new HotKeySettingModel(
            "Phím Tắt Kích Hoạt Dịch Chuyển Nhanh Thủ Công (Giữ Để Kích Hoạt)",
            nameof(Config.HotKeyConfig.QuickTeleportTickHotkey),
            Config.HotKeyConfig.QuickTeleportTickHotkey,
            Config.HotKeyConfig.QuickTeleportTickHotkeyType,
            (_, _) => { Thread.Sleep(100); },
            true
        );
        timerDirectory.Children.Add(quickTeleportTickHotKeySettingModel);

        var mapMaskEnabledHotKeySettingModel = new HotKeySettingModel(
            "Bật/Tắt Lớp Phủ Bản Đồ",
            nameof(Config.HotKeyConfig.MapMaskEnabledHotkey),
            Config.HotKeyConfig.MapMaskEnabledHotkey,
            Config.HotKeyConfig.MapMaskEnabledHotkeyType,
            (_, _) =>
            {
                TaskContext.Instance().Config.MapMaskConfig.Enabled = !TaskContext.Instance().Config.MapMaskConfig.Enabled;
                _logger.LogInformation("Đổi trạng thái {Name} thành [{Enabled}]", "Lớp phủ bản đồ", ToChinese(TaskContext.Instance().Config.MapMaskConfig.Enabled));
            }
        );
        timerDirectory.Children.Add(mapMaskEnabledHotKeySettingModel);

        var turnAroundHotKeySettingModel = new HotKeySettingModel(
            "Giữ Để Xoay Góc Nhìn - Xoay Neuvillette",
            nameof(Config.HotKeyConfig.TurnAroundHotkey),
            Config.HotKeyConfig.TurnAroundHotkey,
            Config.HotKeyConfig.TurnAroundHotkeyType,
            (_, _) => { TurnAroundMacro.Done(); },
            true
        );
        macroDirectory.Children.Add(turnAroundHotKeySettingModel);

        var enhanceArtifactHotKeySettingModel = new HotKeySettingModel(
            "Nhấn Để Tăng Cường Thánh Di Vật Nhanh",
            nameof(Config.HotKeyConfig.EnhanceArtifactHotkey),
            Config.HotKeyConfig.EnhanceArtifactHotkey,
            Config.HotKeyConfig.EnhanceArtifactHotkeyType,
            (_, _) => { QuickEnhanceArtifactMacro.Done(); },
            true
        );
        macroDirectory.Children.Add(enhanceArtifactHotKeySettingModel);

        macroDirectory.Children.Add(new HotKeySettingModel(
            "Nhấn Để Mua Nhanh Đồ Ở Cửa Hàng",
            nameof(Config.HotKeyConfig.QuickBuyHotkey),
            Config.HotKeyConfig.QuickBuyHotkey,
            Config.HotKeyConfig.QuickBuyHotkeyType,
            (_, _) => { QuickBuyTask.Done(); },
            true
        ));

        macroDirectory.Children.Add(new HotKeySettingModel(
            "Nhấn Để Vào/Ra Bình Trần Ca Nhanh",
            nameof(Config.HotKeyConfig.QuickSereniteaPotHotkey),
            Config.HotKeyConfig.QuickSereniteaPotHotkey,
            Config.HotKeyConfig.QuickSereniteaPotHotkeyType,
            (_, _) => { QuickSereniteaPotTask.Done(); }
        ));

        soloTaskDirectory.Children.Add(new HotKeySettingModel(
            "Bắt Đầu/Dừng Toàn Bộ",
            nameof(Config.HotKeyConfig.OnedragonHotkey),
            Config.HotKeyConfig.OnedragonHotkey,
            Config.HotKeyConfig.OnedragonHotkeyType,
            (_, _) => { SwitchSoloTask(_taskSettingsPageViewModel.SOneDragonFlowCommand); }
        ));

        soloTaskDirectory.Children.Add(new HotKeySettingModel(
            "Bắt Đầu/Dừng Tự Động Bảy Thánh Triệu Hoán",
            nameof(Config.HotKeyConfig.AutoGeniusInvokationHotkey),
            Config.HotKeyConfig.AutoGeniusInvokationHotkey,
            Config.HotKeyConfig.AutoGeniusInvokationHotkeyType,
            (_, _) => { SwitchSoloTask(_taskSettingsPageViewModel.SwitchAutoGeniusInvokationCommand); }
        ));

        soloTaskDirectory.Children.Add(new HotKeySettingModel(
            "Bắt Đầu/Dừng Tự Động Chặt Gỗ",
            nameof(Config.HotKeyConfig.AutoWoodHotkey),
            Config.HotKeyConfig.AutoWoodHotkey,
            Config.HotKeyConfig.AutoWoodHotkeyType,
            (_, _) => { SwitchSoloTask(_taskSettingsPageViewModel.SwitchAutoWoodCommand); }
        ));

        soloTaskDirectory.Children.Add(new HotKeySettingModel(
            "Bắt Đầu/Dừng Tự Động Chiến Đấu",
            nameof(Config.HotKeyConfig.AutoFightHotkey),
            Config.HotKeyConfig.AutoFightHotkey,
            Config.HotKeyConfig.AutoFightHotkeyType,
            (_, _) => { SwitchSoloTask(_taskSettingsPageViewModel.SwitchAutoFightCommand); }
        ));

        soloTaskDirectory.Children.Add(new HotKeySettingModel(
            "Bắt Đầu/Dừng Tự Động Bí Cảnh",
            nameof(Config.HotKeyConfig.AutoDomainHotkey),
            Config.HotKeyConfig.AutoDomainHotkey,
            Config.HotKeyConfig.AutoDomainHotkeyType,
            (_, _) => { SwitchSoloTask(_taskSettingsPageViewModel.SwitchAutoDomainCommand); }
        ));
        soloTaskDirectory.Children.Add(new HotKeySettingModel(
            "Bắt Đầu/Dừng Tự Động Mini Game Âm Nhạc",
            nameof(Config.HotKeyConfig.AutoMusicGameHotkey),
            Config.HotKeyConfig.AutoMusicGameHotkey,
            Config.HotKeyConfig.AutoMusicGameHotkeyType,
            (_, _) => { SwitchSoloTask(_taskSettingsPageViewModel.SwitchAutoMusicGameCommand); }
        ));
        soloTaskDirectory.Children.Add(new HotKeySettingModel(
            "Bắt Đầu/Dừng Tự Động Câu Cá",
            nameof(Config.HotKeyConfig.AutoFishingGameHotkey),
            Config.HotKeyConfig.AutoFishingGameHotkey,
            Config.HotKeyConfig.AutoFishingGameHotkeyType,
            (_, _) => { SwitchSoloTask(_taskSettingsPageViewModel.SwitchAutoFishingCommand); }
        ));

        macroDirectory.Children.Add(new HotKeySettingModel(
            "Nhấp Nhanh Nút Xác Nhận Trong Game",
            nameof(Config.HotKeyConfig.ClickGenshinConfirmButtonHotkey),
            Config.HotKeyConfig.ClickGenshinConfirmButtonHotkey,
            Config.HotKeyConfig.ClickGenshinConfirmButtonHotkeyType,
            (_, _) =>
            {
                if (Bv.ClickConfirmButton(TaskControl.CaptureToRectArea()))
                {
                    TaskControl.Logger.LogInformation("Nhấp nhanh nút {Btn} trong Genshin: Thành công", "Xác nhận");
                }
                else
                {
                    TaskControl.Logger.LogInformation("Nhấp nhanh nút {Btn} trong Genshin: Không tìm thấy hình ảnh nút", "Xác nhận");
                }
            },
            true
        ));

        macroDirectory.Children.Add(new HotKeySettingModel(
            "Nhấp Nhanh Nút Hủy Trong Game",
            nameof(Config.HotKeyConfig.ClickGenshinCancelButtonHotkey),
            Config.HotKeyConfig.ClickGenshinCancelButtonHotkey,
            Config.HotKeyConfig.ClickGenshinCancelButtonHotkeyType,
            (_, _) =>
            {
                if (Bv.ClickCancelButton(TaskControl.CaptureToRectArea()))
                {
                    TaskControl.Logger.LogInformation("Nhấp nhanh nút {Btn} trong Genshin: Thành công", "Hủy");
                }
                else
                {
                    TaskControl.Logger.LogInformation("Nhấp nhanh nút {Btn} trong Genshin: Không tìm thấy hình ảnh nút", "Hủy");
                }
            },
            true
        ));

        macroDirectory.Children.Add(new HotKeySettingModel(
            "Phím Tắt Macro Chiến Đấu Một Chạm",
            nameof(Config.HotKeyConfig.OneKeyFightHotkey),
            Config.HotKeyConfig.OneKeyFightHotkey,
            Config.HotKeyConfig.OneKeyFightHotkeyType,
            null,
            true)
        {
            OnKeyDownAction = (_, _) => { OneKeyFightTask.Instance.KeyDown(); },
            OnKeyUpAction = (_, _) => { OneKeyFightTask.Instance.KeyUp(); }
        });

        devDirectory.Children.Add(new HotKeySettingModel(
            "Bắt Đầu/Dừng Ghi Bàn Phím/Chuột",
            nameof(Config.HotKeyConfig.KeyMouseMacroRecordHotkey),
            Config.HotKeyConfig.KeyMouseMacroRecordHotkey,
            Config.HotKeyConfig.KeyMouseMacroRecordHotkeyType, async (_, _) =>
            {
                var vm = App.GetService<KeyMouseRecordPageViewModel>();
                if (vm == null)
                {
                    _logger.LogError("Không tìm thấy đối tượng singleton KeyMouseRecordPageViewModel!");
                    return;
                }

                if (GlobalKeyMouseRecord.Instance.Status == KeyMouseRecorderStatus.Stop)
                {
                    Thread.Sleep(300); // 防止录进快捷键进去
                    await vm.OnStartRecord();
                }
                else
                {
                    vm.OnStopRecord();
                }
            }
        ));

        devDirectory.Children.Add(new HotKeySettingModel(
            "(Dev) Lấy Vị Trí Trung Tâm Bản Đồ Lớn",
            nameof(Config.HotKeyConfig.RecBigMapPosHotkey),
            Config.HotKeyConfig.RecBigMapPosHotkey,
            Config.HotKeyConfig.RecBigMapPosHotkeyType,
            (_, _) =>
            {
                var p = new TpTask(CancellationToken.None).GetPositionFromBigMap(MapTypes.Teyvat.ToString());
                _logger.LogInformation("Vị trí bản đồ lớn: {Position}", p);
            }
        ));

        var pathRecorder = PathRecorder.Instance;
        var pathRecording = false;

        devDirectory.Children.Add(new HotKeySettingModel(
            "Bắt Đầu/Dừng Ghi Lộ Trình",
            nameof(Config.HotKeyConfig.PathRecorderHotkey),
            Config.HotKeyConfig.PathRecorderHotkey,
            Config.HotKeyConfig.PathRecorderHotkeyType,
            (_, _) =>
            {
                if (pathRecording)
                {
                    pathRecorder.Save();
                }
                else
                {
                    Task.Run(() => { pathRecorder.Start(); });
                }

                pathRecording = !pathRecording;
            }
        ));

        devDirectory.Children.Add(new HotKeySettingModel(
            "Thêm Điểm Đường Dẫn",
            nameof(Config.HotKeyConfig.AddWaypointHotkey),
            Config.HotKeyConfig.AddWaypointHotkey,
            Config.HotKeyConfig.AddWaypointHotkeyType,
            (_, _) =>
            {
                if (pathRecording)
                {
                    Task.Run(() => { pathRecorder.AddWaypoint(); });

                }
            }
        ));

        // DEBUG
        if (RuntimeHelper.IsDebug)
        {
            var debugDirectory = new HotKeySettingModel(
                "Kiểm Tra Nội Bộ"
            );
            HotKeySettingModels.Add(debugDirectory);


            // HotKeySettingModels.Add(new HotKeySettingModel(
            //     "（测试）启动/停止自动追踪",
            //     nameof(Config.HotKeyConfig.AutoTrackHotkey),
            //     Config.HotKeyConfig.AutoTrackHotkey,
            //     Config.HotKeyConfig.AutoTrackHotkeyType,
            //     (_, _) =>
            //     {
            //         // _taskSettingsPageViewModel.OnSwitchAutoTrack();
            //     }
            // ));
            // HotKeySettingModels.Add(new HotKeySettingModel(
            //     "（测试）地图路线录制",
            //     nameof(Config.HotKeyConfig.MapPosRecordHotkey),
            //     Config.HotKeyConfig.MapPosRecordHotkey,
            //     Config.HotKeyConfig.MapPosRecordHotkeyType,
            //     (_, _) =>
            //     {
            //         PathPointRecorder.Instance.Switch();
            //     }));
            // HotKeySettingModels.Add(new HotKeySettingModel(
            //     "（Thử Nghiệm）Tự Động Tìm Đường",
            //     nameof(Config.HotKeyConfig.AutoTrackPathHotkey),
            //     Config.HotKeyConfig.AutoTrackPathHotkey,
            //     Config.HotKeyConfig.AutoTrackPathHotkeyType,
            //     (_, _) =>
            //     {
            //         // _taskSettingsPageViewModel.OnSwitchAutoTrackPath();
            //     }
            // ));
            debugDirectory.Children.Add(new HotKeySettingModel(
                "(Thử Nghiệm) Kiểm Tra",
                nameof(Config.HotKeyConfig.Test1Hotkey),
                Config.HotKeyConfig.Test1Hotkey,
                Config.HotKeyConfig.Test1HotkeyType,
                (_, _) =>
                {
                    Task.Run(async () => { await new AutoArtifactSalvageTask(new AutoArtifactSalvageTaskParam(star: 4, null, null, null, null)).Start(new CancellationToken()); });

                }
            ));
            debugDirectory.Children.Add(new HotKeySettingModel(
                "(Thử Nghiệm) Kiểm Tra 2",
                nameof(Config.HotKeyConfig.Test2Hotkey),
                Config.HotKeyConfig.Test2Hotkey,
                Config.HotKeyConfig.Test2HotkeyType,
                (_, _) =>
                {
                    SetTimeTask setTimeTask = new SetTimeTask();
                    Task.Run(async () => { await setTimeTask.Start(12, 05, new CancellationToken()); });

                    // var pName = SystemControl.GetActiveProcessName();
                    // Debug.WriteLine($"当前处于前台的程序：{pName}，原神是否位于前台：{SystemControl.IsGenshinImpactActive()}");
                    // TaskControl.Logger.LogInformation($"当前处于前台的程序：{pName}");
                }
            ));

            debugDirectory.Children.Add(new HotKeySettingModel(
                "(Thử Nghiệm) Phát Lộ Trình Trong Bộ Nhớ",
                nameof(Config.HotKeyConfig.ExecutePathHotkey),
                Config.HotKeyConfig.ExecutePathHotkey,
                Config.HotKeyConfig.ExecutePathHotkeyType,
                (_, _) =>
                {
                    // if (pathRecording)
                    // {
                    //     new TaskRunner(DispatcherTimerOperationEnum.UseCacheImageWithTrigger)
                    //        .FireAndForget(async () => await new PathExecutor(CancellationContext.Instance.Cts).Pathing(pathRecorder._pathingTask));
                    // }
                }
            ));
        }
    }

    private void SwitchSoloTask(IAsyncRelayCommand asyncRelayCommand)
    {
        if (asyncRelayCommand.IsRunning)
        {
            CancellationContext.Instance.Cancel();
        }
        else
        {
            asyncRelayCommand.Execute(null);
        }
    }

    private string ToChinese(bool enabled)
    {
        return enabled.ToChinese();
    }
}