using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Text;
using System.Xml.Linq;
using System.Xml;

namespace ProterOver
{
    public class PorterMain
    {
        /// <summary>
        /// 当前文件的运行目录
        /// </summary>
        public static string CurrentDirectory => Directory.GetCurrentDirectory();

        string filePath => Path.Combine(CurrentDirectory, "Config.xml");

        XDocument doc;

        XElement outPath;

        XElement inportPath;

        public static XmlDocument Doc = new XmlDocument();

        public void ResetConfig()
        {
            XDocument newDoc = new XDocument(
                    new XDeclaration("1.0", "utf-8", null),
                    new XElement("Config",
                    new XElement("OutPath", "-1"),
                    new XElement("InportPath", "-1")
                )
            );
            newDoc.Save(filePath);
            Console.WriteLine($"已经重新生成 Config 文件 ---> {filePath}" + "\n按下任意键继续");
            Console.ReadKey();
            Console.WriteLine("————————————————————————————————————————————————");
        }

        public static PorterMain instance;

        public void Load()
        {
            instance = this;

            try
            {
                if (!File.Exists(filePath))
                {
                    Console.WriteLine("Config 文件不存在，将重新生成");
                    ResetConfig();
                    Load();
                }

                doc = XDocument.Load(filePath);
                outPath = doc.Root.Element("OutPath");
                inportPath = doc.Root.Element("InportPath");

                if (inportPath.Value == "-1")
                    inportPath.Value = CurrentDirectory;

                if (outPath.Value == "-1")
                    outPath.Value = Path.Combine(CurrentDirectory, "Dates\\");

                doc.Save(filePath);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"发生异常：{ex.Message}");
                ResetConfig();
                Load();
            }
        }

        public static void Main()
        {
            PorterMain porterMain = new PorterMain();
            porterMain.Load();

            string directoryPath = instance.inportPath.Value;
            ReplaceCodeInFiles(directoryPath);
            Console.ReadKey();
        }

        static void ReplaceCodeInFiles(string directoryPath)
        {
            var csFiles = Directory.GetFiles(directoryPath, "*.cs", SearchOption.AllDirectories);

            foreach (var csFile in csFiles)
            {
                string fileContent = File.ReadAllText(csFile);

                // 使用正则表达式替换模式，传递自定义的 MatchEvaluator 方法
                string pattern = @"Mod\.Find<(\w+)>\(""(.*?)""\)\.Type";
                string newFileContent = Regex.Replace(fileContent, pattern, MatchEvaluator);

                // 检查是否有代码发生了修改
                if (newFileContent != fileContent)
                {
                    // 将修改后的内容写回文件
                    File.WriteAllText(csFile, newFileContent, Encoding.UTF8);
                    Console.WriteLine($"File updated: {csFile}");
                }
            }
        }

        static string MatchEvaluator(Match match)
        {
            string typeName = match.Groups[1].Value;
            string stringValue = match.Groups[2].Value;

            if (typeName == "ModNPC")
                typeName = "NPC";
            if (typeName == "ModProjectile")
                typeName = "Projectile";
            if (typeName == "ModDust")
                typeName = "Dust";
            if (typeName == "ModItem")
                typeName = "Item";

            // 返回替换后的字符串
            return $"ModContent.{typeName}Type<{stringValue}>()";
        }

        static void DrawProgressBar(int currentStep, int totalSteps)
        {
            Console.CursorVisible = false;
            int barLength = 30;
            int progress = (int)((double)currentStep / totalSteps * barLength);

            Console.Write("[");

            for (int i = 0; i < barLength; i++)
            {
                if (i < progress)
                    Console.Write("=");
                else
                    Console.Write(" ");
            }

            Console.Write($"] {currentStep}/{totalSteps}");
            Console.SetCursorPosition(0, Console.CursorTop);
        }
    }
}
