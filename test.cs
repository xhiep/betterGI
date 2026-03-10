using System;
using System.IO;
using System.Xml.Linq;
using System.Linq;

class Program {
    static void Main() {
        var baseDir = @"c:\Users\xhiep\Downloads\better-genshin-impact-vietnamese-v4\better-genshin-impact-main\BetterGenshinImpact\GameTask";
        foreach (var cnFile in Directory.GetFiles(baseDir, "*.zh-Hans.resx", SearchOption.AllDirectories)) {
            var viFile = cnFile.Replace(".zh-Hans.resx", ".vi.resx");
            
            if (!File.Exists(viFile)) {
                Console.WriteLine($"Missing VI file: {viFile}");
                continue;
            }
            
            var cnDoc = XDocument.Load(cnFile);
            var cnKeys = cnDoc.Descendants("data").Select(d => d.Attribute("name").Value).ToList();

            var viDoc = XDocument.Load(viFile);
            var viKeys = viDoc.Descendants("data").Select(d => d.Attribute("name").Value).ToList();
            
            var missing = cnKeys.Except(viKeys).ToList();
            if (missing.Any()) {
                Console.WriteLine($"In {Path.GetFileName(viFile)}, missing keys:");
                foreach (var k in missing) Console.WriteLine("  " + k);
            }
        }
    }
}
