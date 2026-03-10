using BetterGenshinImpact.GameTask.AutoFishing;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using TorchSharp;

namespace BetterGenshinImpact.UnitTest.GameTaskTests.AutoFishingTests
{
    public class TorchFixture
    {
        private readonly Lazy<TorchLoader> torch = new Lazy<TorchLoader>();
        public bool UseTorch
        {
            get
            {
                return torch.Value.UseTorch;
            }
        }
    }

    internal class TorchLoader
    {
        public TorchLoader()
        {
            // 查找主项目编译输出下的 User/config.json（兼容是否存在 x64、RID 目录等差异）
            var repoRoot = Path.GetFullPath(@"..\..\..\..\");
            var binRoot = Path.Combine(repoRoot, "BetterGenshinImpact", "bin");
            string? configFullPath = null;
            if (Directory.Exists(binRoot))
            {
                configFullPath = Directory.EnumerateFiles(binRoot, "config.json", SearchOption.AllDirectories)
                    .FirstOrDefault(p => p.EndsWith(Path.Combine("User", "config.json"), StringComparison.OrdinalIgnoreCase));
            }

            if (string.IsNullOrEmpty(configFullPath))
            {
                UseTorch = false;
                return;
            }

            var configurationRoot = new ConfigurationBuilder().AddJsonFile(configFullPath, optional: true).Build();
            var section = configurationRoot.GetSection("autoFishingConfig");
            var autoFishingConfig = section.Exists() ? section.Get<AutoFishingConfig>() : new AutoFishingConfig();

            try
            {
                NativeLibrary.Load(autoFishingConfig.TorchDllFullPath);
                if (torch.TryInitializeDeviceType(DeviceType.CUDA))
                {
                    torch.set_default_device(new torch.Device(DeviceType.CUDA));
                }
                UseTorch = true;
            }
            catch (Exception e) when (e is DllNotFoundException || e is NotSupportedException)
            {
                UseTorch = false;
            }
        }

        public bool UseTorch { get; private set; }
    }
}
