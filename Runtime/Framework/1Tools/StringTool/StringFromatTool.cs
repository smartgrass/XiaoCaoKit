using System;

namespace XiaoCao
{
    public static class StringFromatTool
    {
        public static void Example()
        {
            //string.fromat {0:P0}百分比, {0:0}整数格式化 {0:0.#}自动有效数字
            //{0:F2} 两位小数

            //使用vs右键/交互模式验证
            //都是四舍五入原则
            float number = 234.567f;
            string integerFormat = number.ToString("N0"); // 输出 "235"
            Console.WriteLine(integerFormat);
            string twoDecimalPlaces = number.ToString("N2"); // 输出 "123.47"
            Console.WriteLine(twoDecimalPlaces);
            number = 0.45678f;
            string percentageFormat = number.ToString("P"); // 输出 "45.68%"
            Console.WriteLine(percentageFormat);
        }
    }
}


