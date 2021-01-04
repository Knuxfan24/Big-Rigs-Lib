using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BigRigsLib;

namespace BigRigsTest
{
    class Program
    {
        static void Main(string[] args)
        {
            string[] files = Directory.GetFiles(@"Y:\filesssssss\Data", "*.*", SearchOption.AllDirectories);
            List<string> extensions = new List<string>();

            foreach(string file in files)
            {
                if (!extensions.Contains(Path.GetExtension(file).ToLower()))
                {
                    extensions.Add(Path.GetExtension(file).ToLower());
                }
            }
        }
    }
}
