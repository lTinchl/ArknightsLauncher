using System.Collections.Generic;

namespace ArknightsLauncher.Models
{
    public class AppConfig
    {
        public string Theme { get; set; } = "Dark";
        public string Material { get; set; } = "Acrylic";
        public string RootPath { get; set; } = "";                    //游戏根目录
        public string MAA_Official { get; set; } = "";                //MAA官服目录
        public string MAA_Bilibili { get; set; } = "";                //MAA官服目录
        public Dictionary<string, string> Accounts { get; set; } = new();
        public string DefaultAccount { get; set; } = "";
        public bool IsFirstRun { get; set; } = true;                  //首次启动flag
        public bool IsGameExtracted { get; set; } = false;            //首次运行官服flag
        public bool SetupCompleted { get; set; } = false;             //向导是否已完成
    }

    public enum ServerType
    {
        Official,
        Bilibili,
        MAA_Official,
        MAA_Bilibili
    }
}
