using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;

namespace ProjectEditorAndroid
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();

            this.textBox1.Text = srcRootFolderName;

            this.folderBrowserDialog1.ShowNewFolderButton = false;
            this.folderBrowserDialog1.RootFolder = Environment.SpecialFolder.Personal;
            folderBrowserDialog1.SelectedPath = srcRootFolderName;
            folderBrowserDialog1.Description = "Please, select Src root folder.";
        }

        // Src Root
        private string srcRootFolderName = @"d:\Dev\Svn\RealmChronicles\RC.2016.0001\RealmChronicles\Classes\";

        // relative to proj file
        private string inputFileName = Application.StartupPath + @"\..\..\" + @"InputData\proj.android-studio\app\jni\Android.mk";
        private string outputFileName = Application.StartupPath + @"\..\..\" + @"OutputData\proj.android-studio\app\jni\Android.mk";

        private string srcFolderPrefix = @"../../../Classes/";

        private List<string> text = new List<string>();
        private int lastCppFileIndex = 0;
        private int folderIndex = 0;

        private SortedDictionary<string, string> filesToAddDict = new SortedDictionary<string, string>();

        private List<string> cppFiles = new List<string>();
        private SortedSet<string> foldersSet = new SortedSet<string>();

        private void button1_Click(object sender, EventArgs e)
        {
            // delete sample files

            // 1. read input as a list of strings
            text = File.ReadAllLines(inputFileName).ToList();

            folderIndex = text.FindIndex(r => r.StartsWith("LOCAL_C_INCLUDES"));
            lastCppFileIndex = folderIndex - 2;

            /*
            LOCAL_SRC_FILES := hellocpp/main.cpp \
                   ../../../Classes/AppDelegate.cpp \
                   ../../../Classes/HelloWorldScene.cpp

            LOCAL_C_INCLUDES := $(LOCAL_PATH)/../../../Classes
             */

            // delete 2 lines
            text.RemoveAt(lastCppFileIndex);
            lastCppFileIndex--;
            folderIndex--;
            text.RemoveAt(lastCppFileIndex);
            lastCppFileIndex--;
            folderIndex--;
        }

        private void replaceWindowsSlashWithUnix(ref string path)
        {
            path = path.Replace("\\\\", @"/");
            path = path.Replace("\\", @"/");
        }

        private void button2_Click(object sender, EventArgs e)
        {
            // parse src files
            parseSrcFiles(ref filesToAddDict);

            foreach (var pair in filesToAddDict)
            {
                string file = pair.Key;
                string folder = pair.Value;
                replaceWindowsSlashWithUnix(ref file);
                replaceWindowsSlashWithUnix(ref folder);

                string folderPrefix = @"$(LOCAL_PATH)/";
                folder = folderPrefix + folder;

                if (file.EndsWith(".cpp"))
                {
                    cppFiles.Add(file);
                }

                foldersSet.Add(folder);
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            // add folders first
            if (foldersSet.Count > 0)
            {
                // add " \\"
                string firstFolder = text[folderIndex];
                firstFolder = firstFolder + " \\";
                text[folderIndex] = firstFolder;
            }

            foreach (string folder in foldersSet)
            {
                string prefix = "                   ";

                string toAdd = prefix + folder;

                bool isLastString = folder == foldersSet.Last();
                if (isLastString == false)
                {
                    // add suffix
                    string suffix = " \\";
                    toAdd = toAdd + suffix;
                }

                text.Insert(folderIndex + 1, toAdd);
                folderIndex = folderIndex + 1;
            }

            // add new src files
            foreach (string file in cppFiles)
            {
                string prefix = "                   ";

                string toAdd = prefix + file;

                bool isLastString = file == cppFiles.Last();
                if (isLastString == false)
                {
                    // add suffix
                    string suffix = " \\";
                    toAdd = toAdd + suffix;
                }

                text.Insert(lastCppFileIndex + 1, toAdd);
                lastCppFileIndex = lastCppFileIndex + 1;
            }

            // write to output file
            // add just LF instead of default windows CR LF
            File.WriteAllText(outputFileName, string.Join("\n", text.ToArray()) + "\n");
        }

        private void parseSrcFiles(ref SortedDictionary<string, string> dict)
        {
            traverseTree(ref dict);
        }

        private void traverseTree(ref SortedDictionary<string, string> dict)
        {
            string root = srcRootFolderName;
            // Data structure to hold names of subfolders to be
            // examined for files.
            Stack<string> dirs = new Stack<string>(20);

            if (!System.IO.Directory.Exists(root))
            {
                throw new ArgumentException();
            }
            dirs.Push(root);

            while (dirs.Count > 0)
            {
                string currentDir = dirs.Pop();
                string[] subDirs;
                try
                {
                    subDirs = System.IO.Directory.GetDirectories(currentDir);
                }
                // An UnauthorizedAccessException exception will be thrown if we do not have
                // discovery permission on a folder or file. It may or may not be acceptable 
                // to ignore the exception and continue enumerating the remaining files and 
                // folders. It is also possible (but unlikely) that a DirectoryNotFound exception 
                // will be raised. This will happen if currentDir has been deleted by
                // another application or thread after our call to Directory.Exists. The 
                // choice of which exceptions to catch depends entirely on the specific task 
                // you are intending to perform and also on how much you know with certainty 
                // about the systems on which this code will run.
                catch (UnauthorizedAccessException e)
                {
                    Console.WriteLine(e.Message);
                    continue;
                }
                catch (System.IO.DirectoryNotFoundException e)
                {
                    Console.WriteLine(e.Message);
                    continue;
                }

                string[] files = null;
                try
                {
                    files = System.IO.Directory.GetFiles(currentDir);
                }

                catch (UnauthorizedAccessException e)
                {

                    Console.WriteLine(e.Message);
                    continue;
                }

                catch (System.IO.DirectoryNotFoundException e)
                {
                    Console.WriteLine(e.Message);
                    continue;
                }
                // Perform the required action on each file here.
                // Modify this block to perform your required task.
                foreach (string file in files)
                {
                    try
                    {
                        // Perform whatever action is required in your scenario.
                        System.IO.FileInfo fi = new System.IO.FileInfo(file);
                        Console.WriteLine("{0}: {1}, {2}", fi.Name, fi.Length, fi.CreationTime);

                        string relativeFolderName = currentDir.Replace(srcRootFolderName, "");
                        string fileName = srcFolderPrefix + relativeFolderName + @"\" + fi.Name;
                        string folderName = srcFolderPrefix + relativeFolderName;
                        dict.Add(fileName, folderName);

                    }
                    catch (System.IO.FileNotFoundException e)
                    {
                        // If file was deleted by a separate application
                        //  or thread since the call to TraverseTree()
                        // then just continue.
                        Console.WriteLine(e.Message);
                        continue;
                    }
                }

                // Push the subdirectories onto the stack for traversal.
                // This could also be done before handing the files.
                foreach (string str in subDirs)
                    dirs.Push(str);
            }
        }
    }
}
