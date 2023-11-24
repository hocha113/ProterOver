using System.Collections.Generic;
using System.IO;
using System;

namespace ProterOver
{
    internal class Utils
    {
        public static void TraverseFolder(string folderPath, List<string> csFilePaths)
        {
            try
            {
                // 获取当前文件夹下的所有.cs文件
                string[] csFiles = Directory.GetFiles(folderPath, "*.cs");
                csFilePaths.AddRange(csFiles);

                // 获取当前文件夹下的所有子文件夹
                string[] subdirectories = Directory.GetDirectories(folderPath);
                foreach (string subdirectory in subdirectories)
                {
                    // 递归调用，遍历子文件夹
                    TraverseFolder(subdirectory, csFilePaths);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
            }
        }

        public static void DrawProgressBar(int currentStep, int totalSteps)
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
