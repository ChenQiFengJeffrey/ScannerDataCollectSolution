using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ScannerDataCollect.Common
{
    public sealed class ObjectCreateAssistant
    {
        public static T CreateObjectInstance<T>(string objFullName)
        {
            return (T)CreateObjectInstance(objFullName);
        }

        /// <summary>  
        /// 根据指定的类全名，返回对象实例  
        /// </summary>  
        /// <param name="objFullName">对象完整名称（包名和类名），如：com.xxx.Test</param>  
        public static object CreateObjectInstance(string objFullName)
        {
            //获取当前目录  
            string currentDir = Assembly.GetExecutingAssembly().Location;
            currentDir = currentDir.Substring(0, currentDir.LastIndexOf('\\'));
            DirectoryInfo di = new DirectoryInfo(currentDir);
            //获取当前目录下的所有DLL文件  
            FileInfo[] files = di.GetFiles("*.dll");//只查.dll文件  
                                                    //遍历所有文件，查找需要对象的实现定义  
            Type type = Type.GetType(objFullName);
            if (type == null)
            {
                foreach (FileInfo fi in files)
                {
                    type = GetObjectType(fi.FullName, objFullName);
                    if (type != null)
                    {
                        break;
                    }
                }
            }
            if (type == null)
            {
                //throw new Exception("can not find class define of " + objFullName);  
                return null;
            }
            //将对象实例化  
            object obj = Activator.CreateInstance(type);
            return obj;
        }
        /// <summary>  
        /// 从DLL文件中查找指定的对象定义  
        /// </summary>  
        /// <param name="dllFile">DLL文件路径</param>  
        /// <param name="objFullName">对象完整名称（包名和类名），如：com.xxx.Test</param>  
        /// <returns>如果找到，返回其对应的Type；如果没找到，则返回null</returns>  
        private static Type GetObjectType(string dllFile, string objFullName)
        {
            Type type = Assembly.LoadFile(dllFile).GetType(objFullName);
            if (type != null)
            {
                Console.WriteLine("find obj in dll[" + dllFile + "]");
                return type;
            }
            return null;
        }
    }
}
