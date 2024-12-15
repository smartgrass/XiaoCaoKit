	
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using UnityEngine;
using Debug = UnityEngine.Debug;
namespace XiaoCao
{
    /// <summary>
    /// 日志级别
    /// </summary>
    public enum LogLevel : byte
    {
        /// <summary>
        /// 信息级别
        /// </summary>
        Info,

        /// <summary>
        /// 警告级别
        /// </summary>
        Warn,

        /// <summary>
        /// 错误级别
        /// </summary>
        Error
    }

    public class Debuger
    {
        /// <summary>
        /// 日志级别(默认Info)
        /// </summary>
        public static LogLevel LogLevel { get;set; }
        /// <summary>
        /// 是否使用Unity打印
        /// </summary>
        public static bool UseUnityEngine = true;
        /// <summary>
        /// 是否显示时间
        /// </summary>
        public static bool EnableTime = false;
        /// <summary>
        /// 是否显示堆栈信息
        /// </summary>
        public static bool EnableStack = false;
        /// <summary>
        /// 是否保存到文本
        /// </summary>
        public static bool EnableSave = false;
        /// <summary>
        /// 打印文本流
        /// </summary>
        public static StreamWriter LogFileWriter = null;
        /// <summary>
        /// 日志保存路径(文件夹)
        /// </summary>
        public static string LogFileDir = "";
        /// <summary>
        /// 日志文件名
        /// </summary>
        public static string LogFileName = "";

        //打印格式: {0}-时间 {1}-标签/类名/TAGNAME字段值 {2}-内容
        private static string InfoFormat = "<color=#008000>[Info] {0}<color=#00BFFF>{1}</color> {2}</color>";
        private static string WarnFormat = "<color=#FFFF00>[Warn] {0}<color=#00BFFF>{1}</color> {2}</color>";
        private static string ErrorFormat = "<color=#FF0000>[Error] {0}<color=#00BFFF>{1}</color> {2}</color>";

        private static void Internal_Log(string msg, object context = null)
        {
            bool useUnityEngine = UseUnityEngine;
            if (useUnityEngine)
            {
                UnityEngine.Debug.Log(msg, (UnityEngine.Object)context);
            }
            else
            {
                Console.WriteLine(msg);
            }
        }

        private static void Internal_LogWarning(string msg, object context = null)
        {
            bool useUnityEngine = UseUnityEngine;
            if (useUnityEngine)
            {
                //不做Warm
                UnityEngine.Debug.Log(msg, (UnityEngine.Object)context);
            }
            else
            {
                Console.WriteLine(msg);
            }
        }

        private static void Internal_Error(string msg, object context = null)
        {
            bool useUnityEngine = UseUnityEngine;
            if (useUnityEngine)
            {
                UnityEngine.Debug.LogError(msg, (UnityEngine.Object)context);
            }
            else
            {
                Console.WriteLine(msg);
            }
        }

        #region Info
        //[Conditional("EnableLog")]
        public static void Log(object message)
        {
            if (LogLevel >= LogLevel.Info)
            {
                string msg = string.Format(InfoFormat, GetLogTime(), "", message);
                Internal_Log(msg, null);
                WriteToFile(msg, false);
            }
        }

        //[Conditional("EnableLog")]
        public static void Log(object message, object context)
        {
            if (LogLevel >= LogLevel.Info)
            {
                string msg = string.Format(InfoFormat, GetLogTime(), "", message);
                Internal_Log(msg, context);
                WriteToFile(msg, false);
            }
        }

        //[Conditional("EnableLog")]
        public static void Log(string tag, string message)
        {
            if (LogLevel >= LogLevel.Info)
            {
                string msg = string.Format(InfoFormat, GetLogTime(), tag, message);
                Internal_Log(msg, null);
                WriteToFile(msg, false);
            }
        }

        //[Conditional("EnableLog")]
        public static void Log(string tag, string format, params object[] args)
        {
            if (LogLevel >= LogLevel.Info)
            {
                string msg = string.Format(format, args);
                msg = string.Format(InfoFormat, GetLogTime(), tag, msg);
                Internal_Log(msg, null);
                WriteToFile(msg, false);
            }
        }
        #endregion

        #region Warn
        //[Conditional("EnableLog")]
        public static void LogWarning(object message)
        {
            if (LogLevel >= LogLevel.Warn)
            {
                string msg = string.Format(WarnFormat, GetLogTime(), "", message);
                Internal_LogWarning(msg, null);
                WriteToFile(msg, false);
            }
        }

        //[Conditional("EnableLog")]
        public static void LogWarning(object message, object context)
        {
            if (LogLevel >= LogLevel.Warn)
            {
                string msg = string.Format(WarnFormat, GetLogTime(), "", message);
                Internal_LogWarning(msg, context);
                WriteToFile(msg, false);
            }
        }

        //[Conditional("EnableLog")]
        public static void LogWarning(string tag, string message)
        {
            if (LogLevel >= LogLevel.Warn)
            {
                string msg = string.Format(WarnFormat, GetLogTime(), tag, message);
                Internal_LogWarning(msg, null);
                WriteToFile(msg, false);
            }
        }

        //[Conditional("EnableLog")]
        public static void LogWarning(string tag, string format, params object[] args)
        {
            if (LogLevel >= LogLevel.Warn)
            {
                string msg = string.Format(format, args);
                msg = string.Format(WarnFormat, GetLogTime(), tag, msg);
                Internal_LogWarning(msg, null);
                WriteToFile(msg, false);
            }
        }
        #endregion

        #region Error
        //[Conditional("EnableLog")]
        public static void LogError(object message)
        {
            if (LogLevel >= LogLevel.Error)
            {
                string msg = string.Format(ErrorFormat, GetLogTime(), "", message);
                Internal_Error(msg, null);
                WriteToFile(msg, true);
            }
        }

        //[Conditional("EnableLog")]
        public static void LogError(object message, object context)
        {
            if (LogLevel >= LogLevel.Error)
            {
                string msg = string.Format(ErrorFormat, GetLogTime(), "", message);
                Internal_Error(msg, context);
                WriteToFile(msg, true);
            }
        }

        //[Conditional("EnableLog")]
        public static void LogError(string tag, string message)
        {
            if (LogLevel >= LogLevel.Error)
            {
                string msg = string.Format(ErrorFormat, GetLogTime(), tag, message);
                Internal_Error(msg, null);
                WriteToFile(msg, true);
            }
        }

        //[Conditional("EnableLog")]
        public static void LogError(string tag, string format, params object[] args)
        {
            if (LogLevel >= LogLevel.Error)
            {
                string msg = string.Format(format, args);
                msg = string.Format(ErrorFormat, GetLogTime(), tag, msg);
                Internal_Error(msg, null);
                WriteToFile(msg, true);
            }
        }
        #endregion

        /// <summary>
        /// 获取时间
        /// </summary>
        /// <returns></returns>
        private static string GetLogTime()
        {
            string result = "";
            if (EnableTime)
            {
                result = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss:fff") + " ";
            }
            return result;
        }

        /// <summary>
        /// 序列化打印信息
        /// </summary>
        /// <param name="message">打印信息</param>
        /// <param name="EnableStack">是否开启堆栈打印</param>
        private static void WriteToFile(string message, bool EnableStack = false)
        {
            bool flag = !EnableSave;
            if (!flag)
            {
                bool flag2 = LogFileWriter == null;
                if (flag2)
                {
                    LogFileName = DateTime.Now.GetDateTimeFormats('s')[0].ToString();
                    LogFileName = LogFileName.Replace("-", "_");
                    LogFileName = LogFileName.Replace(":", "_");
                    LogFileName = LogFileName.Replace(" ", "");
                    LogFileName += ".log";
                    bool flag3 = string.IsNullOrEmpty(LogFileDir);
                    if (flag3)
                    {
                        try
                        {
                            bool useUnityEngine = UseUnityEngine;
                            if (useUnityEngine)
                            {
                                LogFileDir = Application.persistentDataPath + "/DebugerLog/";
                            }
                            else
                            {
                                string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
                                LogFileDir = baseDirectory + "/DebugerLog/";
                            }
                        }
                        catch (Exception ex)
                        {
                            string msg = string.Format(ErrorFormat, "", "", "获取 Application.persistentDataPath 报错！" + ex.Message);
                            Internal_Error(msg, null);
                            return;
                        }
                    }
                    string path = LogFileDir + LogFileName;
                    try
                    {
                        bool flag4 = !Directory.Exists(LogFileDir);
                        if (flag4)
                        {
                            Directory.CreateDirectory(LogFileDir);
                        }
                        LogFileWriter = File.AppendText(path);
                        LogFileWriter.AutoFlush = true;
                    }
                    catch (Exception ex2)
                    {
                        LogFileWriter = null;
                        string msg = string.Format(ErrorFormat, "", "", "LogToCache()" + ex2.Message + ex2.StackTrace);
                        Internal_Error(msg, null);
                        return;
                    }
                }
                bool flag5 = LogFileWriter != null;
                if (flag5)
                {
                    try
                    {
                        LogFileWriter.WriteLine(message);
                        bool flag6 = (EnableStack || Debuger.EnableStack) && UseUnityEngine;
                        if (flag6)
                        {
                            LogFileWriter.WriteLine(StackTraceUtility.ExtractStackTrace());
                        }
                    }
                    catch (Exception)
                    {
                    }
                }
            }
        }
    }


    /// <summary>
    /// 自定义Debuger类的扩展类
    /// </summary>
    public static class DebugerExtension
    {
        //[Conditional("EnableLog")]
        public static void Log(this object obj, string message)
        {
            if (Debuger.LogLevel >= LogLevel.Info)
            {
                Debuger.Log(GetLogTag(obj), message);
            }
        }

        //[Conditional("EnableLog")]
        public static void Log(this object obj, string format, params object[] args)
        {
            if (Debuger.LogLevel >= LogLevel.Info)
            {
                string message = string.Format(format, args);
                Debuger.Log(GetLogTag(obj), message);
            }
        }

        //[Conditional("EnableLog")]
        public static void Warning(this object obj, string message)
        {
            if (Debuger.LogLevel >= LogLevel.Warn)
            {
                Debuger.LogWarning(GetLogTag(obj), message);
            }
        }

        //[Conditional("EnableLog")]
        public static void LogWarning(this object obj, string format, params object[] args)
        {
            if (Debuger.LogLevel >= LogLevel.Warn)
            {
                string message = string.Format(format, args);
                Debuger.LogWarning(GetLogTag(obj), message);
            }
        }

        //[Conditional("EnableLog")]
        public static void LogError(this object obj, string message)
        {
            if (Debuger.LogLevel >= LogLevel.Error)
            {
                Debuger.LogError(GetLogTag(obj), message);
            }
        }

        //[Conditional("EnableLog")]
        public static void LogError(this object obj, string format, params object[] args)
        {
            if (Debuger.LogLevel >= LogLevel.Error)
            {
                string message = string.Format(format, args);
                Debuger.LogError(GetLogTag(obj), message);
            }
        }
        /// <summary>
        /// 获取调用打印的类名称或者标记有TAGNAME的字段
        /// 有TAGNAME字段的，触发类名称用TAGNAME字段对应的赋值
        /// 没有用类的名称代替
        /// </summary>
        /// <param name="obj">触发Log对应的类</param>
        /// <returns></returns>
        private static string GetLogTag(object obj)
        {
            FieldInfo field = obj.GetType().GetField("TAGNAME");
            bool flag = field != null;
            string result;
            if (flag)
            {
                result = (string)field.GetValue(obj);
            }
            else
            {
                result = obj.GetType().Name;
            }
            return result;
        }
    }
}
