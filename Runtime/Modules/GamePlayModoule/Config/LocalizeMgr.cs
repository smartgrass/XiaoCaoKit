
namespace XiaoCao
{
    public class LocalizeMgr
    {
        public static IniFile localizeData;
        public const string DirName = "Localize/";
        
        public static void Load()
        {

            IniFile ini = new IniFile();
            ini.LoadFromFile(DirName + "En.ini");
            localizeData = ini;
        }









    }

    //[TestClass]
    //public class UnitTest1
    //{
    //    private const string Expected = "Hello World!";
    //    [TestMethod]
    //    public void TestMethod1()
    //    {

    //    }
    //        }
}


