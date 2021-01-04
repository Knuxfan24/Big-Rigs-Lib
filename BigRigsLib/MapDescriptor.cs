using System.IO;
using System.Collections.Generic;

namespace BigRigsLib
{
    /// <summary>
    /// <para>File base for the DSC Map Descriptor format. This is literally just a list of strings.</para>
    /// </summary>
    public class MapDescriptor
    {
        public MapDescriptor() { }
        public MapDescriptor(string file)
        {
            Load(file);
        }

        public List<string> Files = new List<string>();

        /// <summary>
        /// <para>Read the list of file references from the specified file.</para>
        /// </summary>
        /// <param name="filepath">The file to read.</param>
        public void Load(string filepath)
        {
            // Read DSC File.
            string[] dsc = File.ReadAllLines(filepath);
            
            // Store all the file references.
            foreach(string line in dsc)
            {
                Files.Add(line);
            }
        }

        /// <summary>
        /// <para>Save the loaded Map Descriptor list to the specified file.</para>
        /// </summary>
        /// <param name="filepath">The file to save to.</param>
        public void Save(string filepath)
        {
            using (StreamWriter dsc = new StreamWriter(filepath))
            {
                foreach(string file in Files)
                {
                    dsc.WriteLine(file);
                }
            }
        }
    }
}
