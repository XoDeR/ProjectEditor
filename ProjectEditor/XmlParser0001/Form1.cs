using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Linq;
using System.IO;

namespace XmlParser0001
{
    public struct BasicXmlData
    {
        public XmlDocument doc;
        public XmlNode root;
        public XmlNamespaceManager ns;
    }

    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();

            this.textBox1.Text = projFileName;
            this.textBox2.Text = filtersFileName;
            this.textBox3.Text = srcRootFolderName;

            openFileDialog1.Filter = "VS proj files (*.vcxproj)|*.vcxproj";
            openFileDialog1.InitialDirectory = @"d:\Dev\Svn\RealmChronicles\RC.2016.0001\RealmChronicles\proj.win32\";
            openFileDialog1.Title = "Please select proj file.";

            openFileDialog2.Filter = "VS filters files (*.filters)|*.filters";
            openFileDialog2.InitialDirectory = @"d:\Dev\Svn\RealmChronicles\RC.2016.0001\RealmChronicles\proj.win32\";
            openFileDialog2.Title = "Please select filters file.";

            this.folderBrowserDialog1.ShowNewFolderButton = false;
            this.folderBrowserDialog1.RootFolder = Environment.SpecialFolder.Personal;
            folderBrowserDialog1.SelectedPath = @"d:\Dev\Svn\RealmChronicles\RC.2016.0001\RealmChronicles\Classes\";
            folderBrowserDialog1.Description = "Please, select Src root folder.";
        }

        private string srcFolderPrefix = @"..\Classes\";
        // Input files:
        // Proj
        private string projFileName = @"d:\Dev\Svn\RealmChronicles\RC.2016.0001\RealmChronicles\proj.win32\RealmChronicles.vcxproj";
        // Filters
        private string filtersFileName = @"d:\Dev\Svn\RealmChronicles\RC.2016.0001\RealmChronicles\proj.win32\RealmChronicles.vcxproj.filters";
        // Src Root
        private string srcRootFolderName = @"d:\Dev\Svn\RealmChronicles\RC.2016.0001\RealmChronicles\Classes\";

        // PC Project Configuration
        private string pcProjectConfiguration = "Release";
        // PC Project Platform
        private string pcProjectPlatform = "Win32";

        //private string configAttribute = @"'$(Configuration)|$(Platform)'=='Debug|Win32'";
        private string configAttributePrefix = @"'$(Configuration)|$(Platform)'=='";
        private string configAttributeSeparator = @"|";
        private string configAttributeSuffix = @"'";

        private string hFileSearchXpath = "ItemGroup/ClInclude[@Include]";
        private string cppFileSearchXpath = "ItemGroup/ClCompile[@Include]";
            
        // key: fileName; value: filter}
        private SortedDictionary<string, string> releaseFilesInProjectDict = new SortedDictionary<string, string>();
        private SortedDictionary<string, string> debugFilesInProjectDict = new SortedDictionary<string, string>();
        private SortedDictionary<string, string> filesToAddDict = new SortedDictionary<string, string>();
        // key filterName; value: XmlNode
        private SortedDictionary<string, List<XmlNode>> x1FilterDict = new SortedDictionary<string, List<XmlNode>>();
        private SortedDictionary<string, List<XmlNode>> pcFilterDict = new SortedDictionary<string, List<XmlNode>>();

        private SortedDictionary<string, SortedSet<string>> filesToAddSortedByFilters = new SortedDictionary<string, SortedSet<string>>();

        private SortedDictionary<string, string> dict1 = new SortedDictionary<string, string>();
        private SortedDictionary<string, string> dict2 = new SortedDictionary<string, string>();

        private List<string> filesToAdd = new List<string>();

        void openXml(string fileName, ref BasicXmlData basicXmlData)
        {
            basicXmlData.doc = new XmlDocument();
            readXmlFromFile(fileName, ref basicXmlData.doc);
            basicXmlData.root = basicXmlData.doc.DocumentElement;
            basicXmlData.ns = new XmlNamespaceManager(basicXmlData.doc.NameTable);
            basicXmlData.ns.AddNamespace("", "http://schemas.microsoft.com/developer/msbuild/2003");
        }

        private List<int> highList1 = new List<int>();
        private List<int> highList2 = new List<int>();

        private void highlight(ref RichTextBox rtb, int index, bool action)
        {
            int firstCharPosition = rtb.GetFirstCharIndexFromLine(index);
            int lastCharPosition = rtb.GetFirstCharIndexFromLine(index + 1);
            if (firstCharPosition == -1)
            {
                firstCharPosition = 0;
            }
            if (lastCharPosition == -1)
            {
                lastCharPosition = rtb.TextLength;
            }
            if (action == true)
            {
                rtb.SelectionStart = firstCharPosition;
                rtb.SelectionLength = lastCharPosition - firstCharPosition;
                rtb.SelectionBackColor = Color.PaleTurquoise;
            }
            else
            {
                rtb.SelectionStart = firstCharPosition;
                rtb.SelectionLength = lastCharPosition - firstCharPosition;
                rtb.SelectionBackColor = SystemColors.Window;
            }
        }

        private void unhighlightPrevious()
        {
            if (highList1.Count > 0)
            {
                foreach (int i in highList1)
                {
                    highlight(ref richTextBox1, i, false);
                }
                highList1.Clear();
            }

            if (highList2.Count > 0)
            {
                foreach (int i in highList2)
                {
                    highlight(ref richTextBox2, i, false);
                }
                highList2.Clear();
            }
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

        private void parseProj(ref SortedDictionary<string, string> dict, ref BasicXmlData xml, string projectConfiguration, string projectPlatform)
        {
            // src (cpp, c files)
            XmlNodeList cppFileNodes = xml.root.SelectNodes(cppFileSearchXpath, xml.ns);
            // headers (hpp, h files)
            XmlNodeList hFileNodes = xml.root.SelectNodes(hFileSearchXpath, xml.ns);
            List<XmlNode> fileNodes = new List<XmlNode>(cppFileNodes.Cast<XmlNode>());
            List<XmlNode> fileNodesH = new List<XmlNode>(hFileNodes.Cast<XmlNode>());
            fileNodes.AddRange(fileNodesH);

            foreach(XmlNode fileNode in fileNodes)
            {
                string fileName = fileNode.Attributes["Include"].Value;

                if (dict.ContainsKey(fileName) == false)
                {
                    dict.Add(fileName, "");
                }
            }
        }

        private void parseFilters(ref SortedDictionary<string, string> dict, ref SortedDictionary<string, List<XmlNode>> filterDict, ref BasicXmlData xml, string projectConfiguration, string projectPlatform)
        {
            // src (cpp, c files)
            XmlNodeList cppFileNodes = xml.root.SelectNodes(cppFileSearchXpath, xml.ns);
            // headers (hpp, h files)
            XmlNodeList hFileNodes = xml.root.SelectNodes(hFileSearchXpath, xml.ns);
            List<XmlNode> fileNodes = new List<XmlNode>(cppFileNodes.Cast<XmlNode>());
            List<XmlNode> fileNodesH = new List<XmlNode>(hFileNodes.Cast<XmlNode>());
            fileNodes.AddRange(fileNodesH);

            foreach (XmlNode fileNode in fileNodes)
            {
                string fileName = fileNode.Attributes["Include"].Value;
                string folderName = "";

                if (fileNode.HasChildNodes == true)
                {
                    XmlNode filterNode = fileNode.SelectSingleNode("Filter");
                    folderName = filterNode.InnerText;
                }

                if (dict.ContainsKey(fileName) == false)
                {
                    dict.Add(fileName, folderName);
                }
            }

            XmlNodeList filterNodes = xml.root.SelectNodes("ItemGroup/Filter[@Include]", xml.ns);
            List<XmlNode> filterNodesList = new List<XmlNode>(filterNodes.Cast<XmlNode>());

            filterDict.Clear();
            foreach (XmlNode filterNode in filterNodesList)
            {
                string filterName = filterNode.Attributes["Include"].Value;

                List<XmlNode> filterProps = null;

                if (filterNode.HasChildNodes == true)
                {
                    XmlNodeList xmlNodeList = filterNode.ChildNodes;
                    filterProps = new List<XmlNode>(xmlNodeList.Cast<XmlNode>());
                }
                filterDict.Add(filterName, filterProps);
            }
            
        }

        private void populateFilesToAddToX1(ref List<string> fileList, ref SortedDictionary<string, string> releaseFilesInProjectDict, ref SortedDictionary<string, string> filesToAddDict)
        {
            fileList.Clear();

            foreach (KeyValuePair<string, string> entry in filesToAddDict)
            {
                if (releaseFilesInProjectDict.ContainsKey(entry.Key) == false)
                {
                   // remember key
                    fileList.Add(entry.Key);
                }
            }
        }

        #region GUI event handlers

        private void textBox1_Enter(object sender, EventArgs e)
        {
            if(openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                projFileName = openFileDialog1.FileName;
                this.textBox1.Text = projFileName;
            }
        }

        private void textBox2_Enter(object sender, EventArgs e)
        {
            if (openFileDialog2.ShowDialog() == DialogResult.OK)
            {
                filtersFileName = openFileDialog2.FileName;
                this.textBox2.Text = filtersFileName;
            }
        }

        private void textBox3_Enter(object sender, EventArgs e)
        {
            if (folderBrowserDialog1.ShowDialog() == DialogResult.OK)
            {
                srcRootFolderName = folderBrowserDialog1.SelectedPath;
                this.textBox3.Text = srcRootFolderName;
            }
        }

        #endregion // GUI event handlers

        private void parseFilesButtonClick(object sender, EventArgs e)
        {
            releaseFilesInProjectDict.Clear();
            debugFilesInProjectDict.Clear();

            filesToAddDict.Clear();
            x1FilterDict.Clear();
            pcFilterDict.Clear();

            BasicXmlData projXml = new BasicXmlData();
            openXml(projFileName, ref projXml);

            parseProj(ref debugFilesInProjectDict, ref projXml, "Debug", "Win32");
            parseProj(ref releaseFilesInProjectDict, ref projXml, "Release", "Win32");

            BasicXmlData filtersXml = new BasicXmlData();
            openXml(filtersFileName, ref filtersXml);

            parseFilters(ref debugFilesInProjectDict, ref x1FilterDict, ref filtersXml, "Debug", "Win32");
            parseFilters(ref releaseFilesInProjectDict, ref x1FilterDict, ref filtersXml, "Release", "Win32");

            parseSrcFiles(ref filesToAddDict);
        }

        private int selectedFileIndex = -1;

        private bool isHeader(string fileName)
        {
            bool result = false;
            if (fileName.EndsWith(".h") || fileName.EndsWith(".hpp"))
            {
                result = true;
            }
            return result;
        }

        private bool readXmlFromFile(string xmlFileName, ref XmlDocument doc)
        {
            bool read = false;

            string contents = File.ReadAllText(xmlFileName);
            // need to remove this string unless Xml parser will fail
            string toRemove = @"xmlns=""http://schemas.microsoft.com/developer/msbuild/2003""";
            contents = contents.Replace(toRemove, "");
            doc.LoadXml(contents);

            return read;
        }

        private void correctNameSpace(string xmlFileName)
        {
            // correct namespace explicitly
            string[] readText = File.ReadAllLines(xmlFileName);
            // <Project DefaultTargets="Build" ToolsVersion="12.0" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">

            int lineToModifyNumber = 1;

            readText[lineToModifyNumber] = readText[lineToModifyNumber].Replace(@">", @" xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"">");
            File.WriteAllLines(xmlFileName, readText);
        }

        private void writeXmlToFile(string xmlFileName, ref XmlDocument doc)
        {
            var writerSettings = new XmlWriterSettings
            {
                Indent = true,
                CloseOutput = true,
            };

            var writer = XmlWriter.Create(xmlFileName, writerSettings);
            doc.Save(writer);
            writer.Flush();
            writer.Close();
            writer = null;
        }

        private string constructXpathForFilterName(string filterName)
        {
            string result = "";

            // XPath for the node to find

            // "ItemGroup/Filter[@Include = "<filterName>"]

            string prefix = "";
            string suffix = @"""]";

            prefix = @"ItemGroup/Filter[@Include = """;

            result = prefix + filterName + suffix;

            return result;
        }

        private string constructXpathForFileName(string fileName)
        {
            string result = "";
            bool isH = isHeader(fileName);

            // XPath for the node to find

            // <ClInclude Include="..\Http\HttpRequest.h" />
            // <ClCompile Include="..\Common\BasicTimer.cpp" />
            // "ItemGroup/ClInclude[@Include]
            // "ItemGroup/ClInclude[@Include = "<fileName>"]

            string prefix = "";
            string suffix = @"""]";

            if (isH == true)
            {
                prefix = @"ItemGroup/ClInclude[@Include = """;
            }
            else
            {
                prefix = @"ItemGroup/ClCompile[@Include = """;
            }

            result = prefix + fileName + suffix;

            return result;
        }

        private string getLastFileInProj(bool isX1, bool isH)
        {
            string result = "";

            if (isX1 == true)
            {
                string candidate = "";
                for (int i = releaseFilesInProjectDict.Count - 1; i >= 0; --i)
                {
                    candidate = releaseFilesInProjectDict.ElementAt(i).Key;
                    if (isHeader(candidate) == isH)
                    {
                        // match
                        break;
                    }
                }
                if (candidate == "")
                {
                    // not found any appropriate
                }
                else
                {
                    // found
                    result = candidate;
                }
            }
            else
            {

            }

            return result;
        }

        private string getLastFileInFilters(bool isX1, bool isH)
        {
            string result = "";

            // temp
            result = getLastFileInProj(isX1, isH);

            return result;
        }

        private void addNewFileToProj(string fileName, ref BasicXmlData xml)
        {
            // 1. Define cpp or h
            bool isH = isHeader(fileName);

            string pcFolder = filesToAddDict[fileName];

            // find parent node to append to
            string prefix = "ItemGroup";
            string suffix = "";
            if (isH == true)
            {
                suffix = "ClInclude";
            }
            else
            {
                suffix = "ClCompile";
            }
            string xPath = prefix +  @"/" + suffix;

            // get last added filter node
            List<XmlNode> fileNodesList = null;
            {
                string fileNodesXPath = xPath;

                XmlNodeList fileNodes = xml.root.SelectNodes(fileNodesXPath, xml.ns);
                fileNodesList = new List<XmlNode>(fileNodes.Cast<XmlNode>());
            }


            // Create a new node.
            XmlElement nodeFile = xml.doc.CreateElement(suffix); // ClCompile or ClInclude

            XmlAttribute attrInclude = xml.doc.CreateAttribute("Include");
            attrInclude.Value = fileName;
            nodeFile.Attributes.Append(attrInclude);

            if (fileNodesList.Count > 0)
            {
                fileNodesList[0].ParentNode.AppendChild(nodeFile);
            }


            // the fileName to add after
            string lastFileName = "";

            // 2. Define if there are already files in the same folder
            bool isPcFolderPresentOnX1 = x1FilterDict.ContainsKey(pcFolder);
            if (isPcFolderPresentOnX1 == true)
            {
                // 3. If there are ==> append
                List<string> allFilesInFolder = new List<string>();

                foreach (KeyValuePair<string, string> entry in releaseFilesInProjectDict)
                {
                    if (entry.Value == pcFolder)
                    {
                        // remember fileName
                        allFilesInFolder.Add(entry.Key);
                    }
                }
                // select all nodes (they should be in order)
                // discard (remove) mismatches
                // take the last (order should be preserved then)
                if (allFilesInFolder.Count > 0)
                {
                    // take the last one
                    string candidate = "";
                    for (int i = allFilesInFolder.Count - 1; i >= 0; --i)
                    {
                        candidate = allFilesInFolder[i];
                        if (isHeader(candidate) == isH)
                        {
                            // match
                            break;
                        }
                    }
                    if (candidate == "")
                    {
                        // not found any appropriate
                        lastFileName = getLastFileInProj(true, isH);
                    }
                    else
                    {
                        // found
                        lastFileName = candidate;
                    }
                }
                else
                {
                    // we should not be there
                }
            }
            else
            {
                // 4.2. Append file to the end of all file list in proj
                lastFileName = getLastFileInProj(true, isH);
            }
        }

        private void addNewFileToFilters(string fileName, ref BasicXmlData xml)
        {
            // 1. Define cpp or h
            bool isH = isHeader(fileName);

            string pcFolder = filesToAddDict[fileName];

            // find parent node to append to
            string prefix = "ItemGroup";
            string suffix = "";
            if (isH == true)
            {
                suffix = "ClInclude";
            }
            else
            {
                suffix = "ClCompile";
            }
            string xPath = prefix + @"/" + suffix;

            // get last added filter node
            List<XmlNode> fileNodesList = null;
            {
                string fileNodesXPath = xPath;

                XmlNodeList fileNodes = xml.root.SelectNodes(fileNodesXPath, xml.ns);
                fileNodesList = new List<XmlNode>(fileNodes.Cast<XmlNode>());
            }

            // Create a new node.
            XmlElement nodeFile = xml.doc.CreateElement(suffix); // ClCompile or ClInclude

            XmlAttribute attrInclude = xml.doc.CreateAttribute("Include");
            attrInclude.Value = fileName;
            nodeFile.Attributes.Append(attrInclude);

            XmlElement nodeFilter = xml.doc.CreateElement("Filter");
            string folderRelative = pcFolder.Replace(@"..\Classes\", "");
            nodeFilter.InnerText = folderRelative;

            nodeFile.AppendChild(nodeFilter);


            if (fileNodesList.Count > 0)
            {
                fileNodesList[0].ParentNode.AppendChild(nodeFile);
            }
        }

        private void showFilesToAddGroupedByFolders(ref SortedDictionary<string, SortedSet<string>> filesToAddSortedByFilters, ref RichTextBox rtb)
        {
            string newText = "";

            foreach (var kv in filesToAddSortedByFilters)
            {
                string line = "filter: " + "\t" + kv.Key + "\n\n";
                newText += line;

                foreach(string file in kv.Value)
                {
                    line = "\tfile: " + "\t" + file + "\n";
                    newText += line;
                }
            }

            rtb.Text = newText;
        }

        private void showFilesToAddSortedByFoldersButtonClick(object sender, EventArgs e)
        {
            // show sorted by folders
            filesToAddSortedByFilters.Clear();
            populateFilesToAddToX1(ref filesToAdd, ref releaseFilesInProjectDict, ref filesToAddDict);

            for (int i = 0; i < filesToAdd.Count; ++i)
            {
                string fileName = filesToAdd[i];
                string folder = filesToAddDict[fileName];
                if (filesToAddSortedByFilters.ContainsKey(folder) == true)
                {
                    filesToAddSortedByFilters[folder].Add(fileName);
                }
                else
                {
                    SortedSet<string> newSet = new SortedSet<string>();
                    newSet.Add(fileName);
                    filesToAddSortedByFilters.Add(folder, newSet);
                }
            }

            showFilesToAddGroupedByFolders(ref filesToAddSortedByFilters, ref richTextBox2);
        }

        private void addExtraSecondLevelBehaviorFolder(ref BasicXmlData filtersXml)
        {
            Guid guid = Guid.NewGuid();
            string filter = @"..\Classes\BehaviorTree";

            string guidStr = "{" + guid.ToString().ToUpper() + "}";

            // Create a new node.
            XmlElement nodeFilter = filtersXml.doc.CreateElement("Filter");

            XmlAttribute attrInclude = filtersXml.doc.CreateAttribute("Include");
            string relativeFolderName = filter.Replace(@"..\Classes\", "");
            attrInclude.Value = relativeFolderName;
            nodeFilter.Attributes.Append(attrInclude);

            XmlElement nodeUniqueIdentifier = filtersXml.doc.CreateElement("UniqueIdentifier");
            nodeUniqueIdentifier.InnerText = guidStr;

            nodeFilter.AppendChild(nodeUniqueIdentifier);

            // get last added filter node
            List<XmlNode> folderNodesList = null;
            {
                string folderNodesXPath = @"ItemGroup/Filter";

                XmlNodeList folderNodes = filtersXml.root.SelectNodes(folderNodesXPath, filtersXml.ns);
                folderNodesList = new List<XmlNode>(folderNodes.Cast<XmlNode>());
            }
            if (folderNodesList.Count > 0)
            {
                folderNodesList[0].ParentNode.AppendChild(nodeFilter);
            }
        }

        private void makeNewFoldersAvailable(ref BasicXmlData projXml, ref BasicXmlData filtersXml)
        {
            if (filesToAddSortedByFilters.Count > 0)
            {
                // remove src filter from folders
                List<XmlNode> srcFolderNodesList = null;
                {
                    string prefix = @"ItemGroup/Filter[@Include = """;
                    string suffix = @"""]";
                    string srcFolderXPath = prefix + @"src" + suffix;

                    XmlNodeList srcFolderNodes = filtersXml.root.SelectNodes(srcFolderXPath, filtersXml.ns);
                    srcFolderNodesList = new List<XmlNode>(srcFolderNodes.Cast<XmlNode>());
                }
                if (srcFolderNodesList.Count == 1)
                {
                    srcFolderNodesList[0].ParentNode.RemoveChild(srcFolderNodesList[0]);
                }

                addExtraSecondLevelBehaviorFolder(ref filtersXml);

                string toAddToProj = "";
                Guid guid;
                foreach (var kv in filesToAddSortedByFilters)
                {
                    string filter = kv.Key;
                    toAddToProj += (filter + ";"); 

                    // generate next unique id
                    guid = Guid.NewGuid();

                    string guidStr = "{" + guid.ToString().ToUpper() + "}";

                    // Create a new node.
                    XmlElement nodeFilter = filtersXml.doc.CreateElement("Filter");

                    XmlAttribute attrInclude = filtersXml.doc.CreateAttribute("Include");
                    string relativeFolderName = filter.Replace(@"..\Classes\", "");
                    attrInclude.Value = relativeFolderName;
                    nodeFilter.Attributes.Append(attrInclude);

                    XmlElement nodeUniqueIdentifier = filtersXml.doc.CreateElement("UniqueIdentifier");
                    nodeUniqueIdentifier.InnerText = guidStr;

                    nodeFilter.AppendChild(nodeUniqueIdentifier);

                    // get last added filter node
                    List<XmlNode> folderNodesList = null;
                    {
                        string folderNodesXPath = @"ItemGroup/Filter";

                        XmlNodeList folderNodes = filtersXml.root.SelectNodes(folderNodesXPath, filtersXml.ns);
                        folderNodesList = new List<XmlNode>(folderNodes.Cast<XmlNode>());
                    }
                    if (folderNodesList.Count > 0)
                    {
                        folderNodesList[0].ParentNode.AppendChild(nodeFilter);
                    }
                
                }

                // add string to proj

                // Debug
                // <ItemDefinitionGroup Condition="'$(Configuration)|$(Platform)'=='Debug|Win32'">
                // ClCompile
                // AdditionalIncludeDirectories
                //
                // insert before @"%(AdditionalIncludeDirectories);" string
                List<XmlNode> debugIncludeDirsNodesList = null;
                {
                    string debugIncludeDirsXPath = @"ItemDefinitionGroup[@Condition=""" + @"'$(Configuration)|$(Platform)'=='Debug|Win32'""" + @"]/ClCompile/AdditionalIncludeDirectories";
                    XmlNodeList debugIncludeDirsNodes = projXml.root.SelectNodes(debugIncludeDirsXPath, projXml.ns);
                    debugIncludeDirsNodesList = new List<XmlNode>(debugIncludeDirsNodes.Cast<XmlNode>());
                }
                if (debugIncludeDirsNodesList.Count == 1)
                {
                    string innerText = debugIncludeDirsNodesList[0].InnerText;
                    innerText = innerText.Replace(@"%(AdditionalIncludeDirectories);", toAddToProj + @"%(AdditionalIncludeDirectories);");
                    debugIncludeDirsNodesList[0].InnerText = innerText;
                }

                // Release
                List<XmlNode> releaseIncludeDirsNodesList = null;
                {
                    string releaseIncludeDirsXPath = @"ItemDefinitionGroup[@Condition=""" + @"'$(Configuration)|$(Platform)'=='Release|Win32'""" + @"]/ClCompile/AdditionalIncludeDirectories";
                    XmlNodeList releaseIncludeDirsNodes = projXml.root.SelectNodes(releaseIncludeDirsXPath, projXml.ns);
                    releaseIncludeDirsNodesList = new List<XmlNode>(releaseIncludeDirsNodes.Cast<XmlNode>());
                }
                if (releaseIncludeDirsNodesList.Count == 1)
                {
                    string innerText = releaseIncludeDirsNodesList[0].InnerText;
                    innerText = innerText.Replace(@"%(AdditionalIncludeDirectories);", toAddToProj + @"%(AdditionalIncludeDirectories);");
                    releaseIncludeDirsNodesList[0].InnerText = innerText;
                }
            }
        }

        private void removeSampleFilesReferences(ref BasicXmlData projXml, ref BasicXmlData filtersXml)
        {
            List<string> filesToRemove = new List<string>();
            filesToRemove.Add(@"..\Classes\AppDelegate.cpp");
            filesToRemove.Add(@"..\Classes\HelloWorldScene.cpp");
            filesToRemove.Add(@"..\Classes\AppDelegate.h");
            filesToRemove.Add(@"..\Classes\HelloWorldScene.h");

            foreach (string fileName in filesToRemove)
            {
                string xPath = constructXpathForFileName(fileName);

                // project
                {
                    XmlNodeList foundFileNodes = projXml.root.SelectNodes(xPath, projXml.ns);
                    List<XmlNode> foundFileNodesList = new List<XmlNode>(foundFileNodes.Cast<XmlNode>());

                    foreach (XmlNode node in foundFileNodesList)
                    {
                        node.ParentNode.RemoveChild(node);
                    }
                }

                // filters
                {
                    XmlNodeList foundFileNodes = filtersXml.root.SelectNodes(xPath, filtersXml.ns);
                    List<XmlNode> foundFileNodesList = new List<XmlNode>(foundFileNodes.Cast<XmlNode>());

                    foreach (XmlNode node in foundFileNodesList)
                    {
                        node.ParentNode.RemoveChild(node);
                    }
                }
            }
        }

        private void addAllFilesButtonClick(object sender, EventArgs e)
        {
            if (filesToAddSortedByFilters.Count > 0)
            {
                {
                    // open vcxproj file
                    BasicXmlData projXml = new BasicXmlData();
                    openXml(projFileName, ref projXml);

                    // open filters file
                    BasicXmlData filtersXml = new BasicXmlData();
                    openXml(filtersFileName, ref filtersXml);

                    makeNewFoldersAvailable(ref projXml, ref filtersXml);
                    removeSampleFilesReferences(ref projXml, ref filtersXml);

                    foreach (var kv in filesToAddSortedByFilters)
                    {
                        foreach (string file in kv.Value)
                        {
                            addNewFileToProj(file, ref projXml);
                            addNewFileToFilters(file, ref filtersXml);
                        }
                    }

                    writeXmlToFile(projFileName, ref projXml.doc);
                    writeXmlToFile(filtersFileName, ref filtersXml.doc);

                    projXml.doc = null;
                    filtersXml.doc = null;
                }

                correctNameSpace(projFileName);
                correctNameSpace(filtersFileName);
            }
        }
    }
}
