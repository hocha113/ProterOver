using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;

namespace ProterOver
{
    public class PorterMain
    {
        /// <summary>
        /// 当前文件的运行目录
        /// </summary>
        public static string CurrentDirectory => Directory.GetCurrentDirectory();

        static int count;

        static List<string> oldNmmos = new List<string>();

        static List<string> ifyNmmos = new List<string>();

        static List<string> issueFillCodes = new List<string>();

        static int issueFillCount;

        static int issueFillCodeLineCount;

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
            List<string> files = new List<string>();
            string directoryPath = instance.inportPath.Value;
            Utils.TraverseFolder(directoryPath, files);
            Console.WriteLine($"找到{files.Count}个.cs文件");
            Console.WriteLine("输入目标功能：\n-1 索引替换\n-2 递归报警");
            if (Console.Read() == 1)
            {
                ReplaceCodeInFiles(files);
                Console.WriteLine($"修改完成，一共修改了 {count} 处代码，按下任意键查看修改的文本");
                Console.ReadKey();
                foreach (string a in ifyNmmos)
                {
                    Console.WriteLine($"{oldNmmos} --> {ifyNmmos}");
                }
                Console.ReadKey();
                using (FileStream createFile = new FileStream(instance.outPath.Value + "Ify.txt", FileMode.Create))
                using (StreamWriter writer = new StreamWriter(createFile))
                {
                    foreach (string item in ifyNmmos)
                    {
                        // 写入每个条目，并在条目之间添加换行
                        writer.WriteLine(item);
                    }
                }

                Console.WriteLine("修改对比文档已经创建: " + instance.outPath.Value);
            }
            else
            {
                foreach (string code in files)
                {
                    Console.WriteLine($"当前目标文件: {code}");
                    string fileContent = File.ReadAllText(code);
                    bool issueKeys = AnalyzeCode(fileContent, code);
                    Console.WriteLine("——————————————————————");
                }
                string issueReportText1 = $"总共有{issueFillCount}个.cs文件中的{issueFillCodeLineCount}行代码引发了分析器的怀疑";
                Console.WriteLine($"扫描完成，{issueReportText1} \n按下任意键查看项目危险代码排查报告...");
                Console.ReadKey();
                Console.WriteLine("——————————————————————");
                foreach (string leng in issueFillCodes)
                {
                    Console.WriteLine(leng);
                }
                Console.ReadKey();
                using (FileStream createFile = new FileStream(instance.outPath.Value + "IssueCode.txt", FileMode.Create))
                using (StreamWriter writer = new StreamWriter(createFile))
                {
                    foreach (string leng in issueFillCodes)
                    {
                        // 写入每个条目，并在条目之间添加换行
                        writer.WriteLine(leng);
                    }
                    writer.WriteLine(issueReportText1);
                }
                Console.WriteLine($"文件报告已经生成----> {instance.outPath.Value + "IssueCode.txt"}");
            }
            Console.ReadKey();
        }

        static bool AnalyzeCode(string fileContent, string targetPath)
        {
            SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(fileContent);
            var root = syntaxTree.GetRoot();
            var analyzer = new RecursiveCallAnalyzer();
            analyzer.Visit(root);
            var issues = analyzer.GetIssues();

            if (issues.Count > 0)
            {
                issueFillCodes.Add($"怀疑文件：{targetPath}");
                issueFillCount++;
                foreach (var issue in issues)
                {
                    string issueLang = $"以下代码疑似发生了无限递归 {issue.FilePath}, 在第 {issue.LineNumber} 行";
                    Console.WriteLine(issueLang);
                    issueFillCodes.Add(issueLang);
                    issueFillCodeLineCount++;
                }
                issueFillCodes.Add("——————————————————————");
            }
            else
            {
                Console.WriteLine("这个文件是安全的");
            }
            return issues.Count > 0;
        }

        static void ReplaceCodeInFiles(List<string> directoryPath)
        {
            foreach (var csFile in directoryPath)
            {
                Console.WriteLine($"当前目标文件: {csFile}");
                string fileContent = File.ReadAllText(csFile);

                // 使用正则表达式替换模式，传递自定义的 MatchEvaluator 方法
                string pattern = @"Mod\.Find<(\w+)>\(""(.*?)""\)\.Type";
                string newFileContent = Regex.Replace(fileContent, pattern, MatchEvaluator);

                // 检查是否有代码发生了修改
                if (newFileContent != fileContent)
                {
                    Console.WriteLine($"正在写入....");
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
            if (typeName == "ModTile")
                typeName = "Tile";
            if (typeName == "ModWall")
                typeName = "Wall";
            if (typeName == "ModBuff")
                typeName = "Buff";
            Console.WriteLine($"--> ModContent.{typeName}Type<{stringValue}>()");
            count++;
            oldNmmos.Add($"Mod.Find<{typeName}>(\"{stringValue}\").Type");
            ifyNmmos.Add($"ModContent.{typeName}Type<{stringValue}>()");
            // 返回替换后的字符串
            return $"ModContent.{typeName}Type<{stringValue}>()";
        }
    }
}
