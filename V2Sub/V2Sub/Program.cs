using System;
using V2Sub.Sub;
using V2Sub.Tools;

namespace V2Sub
{
    class Program
    {
        static void Main(string[] args)
        {
            string url = "";
            var result = GetSub.GetSubN().vmess;
            result.ForEach(x=> {
                Console.WriteLine($"【{result.IndexOf(x)}】 【{Utils.Ping(x.address)}ms】  {x.remarks}");
            });
            Console.WriteLine("请选择序号：");
            GetSub.GetSubN(Convert.ToInt32(Console.ReadLine()));
            Console.WriteLine("Hello World!");
        }
    }
}
