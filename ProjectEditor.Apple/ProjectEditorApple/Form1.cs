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
using System.Net.NetworkInformation;
using System.Diagnostics;

namespace ProjectEditorApple
{
    // for each cpp file there are 3 guids: fileRef, ios, mac
    public struct FileGuidSet
    {
        public string fileRef;
        public string ios;
        public string mac;
    }

    public struct LastKnownFileTypeMap
    {
        public void initLastKnownFileTypeMap()
        {
            folder = "folder";
            imagePng = "image.png";
            cpp = "sourcecode.cpp.cpp"; // fileEncoding = 4
            header = "sourcecode.c.h"; // fileEncoding = 4
            xml = "text.plist.xml"; // fileEncoding = 4
        }

        public string folder; // = "folder";
        public string imagePng; // = "image.png";
        public string cpp; // = "sourcecode.cpp.cpp"; // fileEncoding = 4
        public string header; // = "sourcecode.c.h"; // fileEncoding = 4
        public string xml; // = "text.plist.xml"; // fileEncoding = 4
    }

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
        private string resourcesRootFolderName = @"d:\Dev\Svn\RealmChronicles\RC.2016.0001\RealmChronicles\Resources\";

        // relative to proj file
        private string inputFileName = Application.StartupPath + @"\..\..\" + @"InputData\project.pbxproj";
        private string outputFileName = Application.StartupPath + @"\..\..\" + @"OutputData\project.pbxproj";

        private string srcFolderPrefix = @"../Classes/";
        private string resourceFolderPrefix = @"../Resources/";

        private List<string> text = new List<string>();

        private SortedDictionary<string, string> filesToAddDict = new SortedDictionary<string, string>();
        private SortedDictionary<string, string> resourceFilesToAddDict = new SortedDictionary<string, string>();

        private List<string> cppFiles = new List<string>();
        private List<string> headerFiles = new List<string>();

        private SortedSet<string> srcFolders = new SortedSet<string>();
        private SortedSet<string> resourceFolders = new SortedSet<string>();

        private Dictionary<string, SortedSet<string>> srcFolderToFileSetMap = new Dictionary<string, SortedSet<string>>();
        private Dictionary<string, SortedSet<string>> resourceFolderToFileSetMap = new Dictionary<string, SortedSet<string>>();

        private Dictionary<string, string> headerSrcFileToGuidMap = new Dictionary<string, string>();
        private Dictionary<string, FileGuidSet> cppFileToGuidSetMap = new Dictionary<string, FileGuidSet>();
        private Dictionary<string, FileGuidSet> resourceFileToGuidSetMap = new Dictionary<string, FileGuidSet>();
        private Dictionary<string, FileGuidSet> resourceFolderToGuidSetMap = new Dictionary<string, FileGuidSet>();
        
        private Dictionary<string, string> srcGroupToGuidMap = new Dictionary<string, string>(); // [srcGroup(srcFolder) : Guid]
        private Dictionary<string, string> resourceGroupToGuidMap = new Dictionary<string, string>(); // [resourceGroup(srcFolder) : Guid]

        private Dictionary<string, SortedSet<string>> srcGroupRootToSrcGroupChildren = new Dictionary<string, SortedSet<string>>();
        private Dictionary<string, SortedSet<string>> srcGroupLevelOneToSrcGroupChildren = new Dictionary<string, SortedSet<string>>();
        private Dictionary<string, SortedSet<string>> resourceGroupRootToResourceGroupChildren = new Dictionary<string, SortedSet<string>>();
        private Dictionary<string, SortedSet<string>> resourceGroupLevelTwoToResourceGroupChildren = new Dictionary<string, SortedSet<string>>();
        private Dictionary<string, SortedSet<string>> resourceGroupLevelOneToResourceGroupChildren = new Dictionary<string, SortedSet<string>>();

        // 1. Resource folders 1 level (Data, Asset)
        // 2. Resource folders 2 level (Asset/Sound, etc)
        // 3. Resource folders 3 level (Asset/Sound/Common, etc)

        private List<string> resourceFoldersLevelOne = new List<string>();
        private List<string> resourceFoldersLevelTwo = new List<string>();
        private List<string> resourceFoldersLevelThree = new List<string>();

        private List<string> srcFoldersLevelOne = new List<string>();
        private List<string> srcFoldersLevelTwo = new List<string>();

        private void generateCppFileGuids()
        {
            // input: cppFiles
            // output: cppFileToGuidSetMap

            foreach (string cppFile in cppFiles)
            {
                FileGuidSet fileGuidSet = new FileGuidSet();
                fileGuidSet.fileRef = generateNext();
                fileGuidSet.ios = generateNext();
                fileGuidSet.mac = generateNext();
                cppFileToGuidSetMap.Add(cppFile, fileGuidSet);
            }
        }

        private void generateHeaderSrcFileGuid()
        {
            foreach (string headerFile in headerFiles)
            {
                string guid = generateNext();
                headerSrcFileToGuidMap.Add(headerFile, guid);
            }
        }

        private void generateResourceFileGuids()
        {
            foreach (var mapPair in resourceFilesToAddDict)
            {
                string resourceFile = mapPair.Key;

                FileGuidSet fileGuidSet = new FileGuidSet();
                fileGuidSet.fileRef = generateNext();
                fileGuidSet.ios = generateNext();
                fileGuidSet.mac = generateNext();
                resourceFileToGuidSetMap.Add(resourceFile, fileGuidSet);
            }
        }

        private void generateResourceFoldersGuids()
        {
            // input: resourceFolders
            // output: resourceFolderToGuidSetMap

            foreach (string folder in resourceFolders)
            {
                FileGuidSet fileGuidSet = new FileGuidSet();
                fileGuidSet.fileRef = generateNext();
                fileGuidSet.ios = generateNext();
                fileGuidSet.mac = generateNext();
                resourceFolderToGuidSetMap.Add(folder, fileGuidSet);
            }
        }

        private void generateSrcGroupGuid()
        {
            foreach (string folder in srcFolders)
            {
                string guid = generateNext();
                srcGroupToGuidMap.Add(folder, guid);
            }
        }

        private void generateResourceGroupGuid()
        {
            foreach (string folder in resourceFolders)
            {
                string guid = generateNext();
                resourceGroupToGuidMap.Add(folder, guid);
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            // delete sample files

            // 1. read input as a list of strings
            text = File.ReadAllLines(inputFileName).ToList();

            // Resources folders to remove:
            // res
            // fonts

            // Resources files to remove:
            // CloseNormal.png
            // CloseSelected.png
            // HelloWorld.png

            // Classes files to remove:
            // AppDelegate.cpp
            // AppDelegate.h
            // HelloWorldScene.cpp
            // HelloWorldScene.h

            // start deleting from the end
            for (int i = text.Count - 1; i >= 0; --i)
            {
                string current = text[i];

                if (current.Contains(@"/* fonts ")
                    || current.Contains(@"/* res ")
                    || current.Contains(@"/* CloseNormal.png ")
                    || current.Contains(@"/* CloseSelected.png ")
                    || current.Contains(@"/* HelloWorld.png ")
                    || current.Contains(@"/* AppDelegate.cpp ")
                    || current.Contains(@"/* AppDelegate.h ")
                    || current.Contains(@"/* HelloWorldScene.cpp ")
                    || current.Contains(@"/* HelloWorldScene.h ")
                    )
                {
                    text.RemoveAt(i);
                    continue;
                }
            }
        }

        private void replaceWindowsSlashWithUnix(ref string path)
        {
            path = path.Replace("\\\\", @"/");
            path = path.Replace("\\", @"/");
        }

        private void insertInDictionarySet(ref Dictionary<string, SortedSet<string>> dictSet, string folder, string file)
        {
            if (!dictSet.ContainsKey(folder))
            {
                dictSet[folder] = new SortedSet<string>();
            }
            dictSet[folder].Add(file);
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

                string folderPrefix = "";
                folder = folderPrefix + folder;

                if (file.EndsWith(".cpp"))
                {
                    cppFiles.Add(file);
                }
                else if (file.EndsWith(".h") || file.EndsWith(".hpp"))
                {
                    headerFiles.Add(file);
                }

                srcFolders.Add(folder);
                insertInDictionarySet(ref srcFolderToFileSetMap, folder, file);
            }

            // parse Resource files
            parseResourcesFiles(ref resourceFilesToAddDict);

            foreach (var pair in resourceFilesToAddDict)
            {
                string file = pair.Key;
                string folder = pair.Value;
                replaceWindowsSlashWithUnix(ref file);
                replaceWindowsSlashWithUnix(ref folder);

                resourceFolders.Add(folder);
                insertInDictionarySet(ref resourceFolderToFileSetMap, folder, file);
            }

            resourceFolders.Add(@"../Resources/Asset");
            resourceFolders.Add(@"../Resources/Data");

            string dummyGuid = generateFirst();

            generateHeaderSrcFileGuid();
            generateCppFileGuids();
            generateResourceFileGuids();
            generateResourceFoldersGuids();
            generateSrcGroupGuid();
            generateResourceGroupGuid();

            // sort resource folders by level
            foreach (string resourceFolder in resourceFolders)
            {
                string shortResourcesFolderName = getShortResourcesFolderName(resourceFolder);
                // find symbol occurrences
                int count = shortResourcesFolderName.Split('/').Length - 1;
                if (count == 0)
                {
                    resourceFoldersLevelOne.Add(resourceFolder);
                }
                else if (count == 1)
                {
                    resourceFoldersLevelTwo.Add(resourceFolder);
                }
                else if (count == 2)
                {
                    resourceFoldersLevelThree.Add(resourceFolder);
                }
            }

            // sort src folders by level
            foreach (string srcFolder in srcFolders)
            {
                string shortSrcFolderName = getShortSrcFolderName(srcFolder);
                // find symbol occurrences
                int count = shortSrcFolderName.Split('/').Length - 1;
                if (count == 0)
                {
                    srcFoldersLevelOne.Add(srcFolder);
                }
                else if (count == 1)
                {
                    srcFoldersLevelTwo.Add(srcFolder);
                }
            }
        }

        int findLastIndexOfMarker(string marker)
        {
            int result = 0;
            for (int i = text.Count - 1; i >= 0; --i)
            {
                string current = text[i];

                if (current.Contains(marker))
                {
                    result = i;
                    break;
                }
            }
            return result;
        }

        private string getFileNameFromFilePath(string filePath)
        {
            string filePathMod = String.Copy(filePath);
            string[] fragments = filePathMod.Split('/');
            // take last
            return fragments[fragments.Length - 1];
        }

        private string getFolderNameFromFolderPath(string folderPath)
        {
            string folderPathMod = String.Copy(folderPath);
            string[] fragments = folderPathMod.Split('/');
            // take last
            return fragments[fragments.Length - 1];
        }

        private string getShortResourcesFolderName(string fullResourceFolderName)
        {
            string result = fullResourceFolderName.Replace(@"../Resources/", "");
            return result;
        }

        private string getShortSrcFolderName(string fullSrcFolderName)
        {
            string result = fullSrcFolderName.Replace(@"../Classes/", "");
            return result;
        }

        private string getFolderParent(string folderFullPath)
        {
            string filePathMod = String.Copy(folderFullPath);
            string[] fragments = filePathMod.Split('/');
            return filePathMod.Replace(@"/" + fragments[fragments.Length - 1], "");
        }

        private void addHeaderSearchPaths()
        {
            // 1 Add header search paths to Release and Debug configuration

            // start adding from the end
            // find markers
            int releaseHeaderSearchPathLast = 0;
            int debugHeaderSearchPathLast = 0;

            string headerSearchPathMarker = @"					""$(SRCROOT)/../cocos2d/external/chipmunk/include/chipmunk"",";

            for (int i = text.Count - 1; i >= 0; --i)
            {
                string current = text[i];

                if (current.Contains(headerSearchPathMarker))
                {
                    if (releaseHeaderSearchPathLast == 0)
                    {
                        releaseHeaderSearchPathLast = i;
                    }
                    else if (debugHeaderSearchPathLast == 0)
                    {
                        debugHeaderSearchPathLast = i;
                    }
                    else
                    {
                        break;
                    }
                }
            }

            // Form list of strings to add
            List<string> headersSearchPathsToAdd = new List<string>();

            foreach (string folder in srcFolders)
            {
                string newFolder = folder;
                // remove leading @"../../../"
                newFolder = newFolder.Replace(@"../", "");

                // add prefix 
                string prefix = @"					""$(SRCROOT)/../";
                string suffix = @""",";

                newFolder = prefix + newFolder + suffix;
                headersSearchPathsToAdd.Add(newFolder);
            }

            // add to releaseHeaderSearchPathLast
            for (int i = headersSearchPathsToAdd.Count - 1; i >= 0; --i)
            {
                text.Insert(releaseHeaderSearchPathLast + 1, headersSearchPathsToAdd[i]);
            }

            // add to debugHeaderSearchPathLast
            for (int i = headersSearchPathsToAdd.Count - 1; i >= 0; --i)
            {
                text.Insert(debugHeaderSearchPathLast + 1, headersSearchPathsToAdd[i]);
            }
        }

        private void addSourcesToBuild()
        {
            // add to mac PBXSourcesBuildPhase
            string markerSrcBuildMacLast = @"				503AE10517EB98FF00D1A890 /* main.cpp in Sources */,";
            int indexSrcBuildMacLast = findLastIndexOfMarker(markerSrcBuildMacLast);
            for (int i = cppFiles.Count - 1; i >= 0; --i)
            {
                string file = cppFiles[i];
                string prefix = "				";
                string guid = cppFileToGuidSetMap[file].mac;
                string infix = @" /* ";
                string filename = getFileNameFromFilePath(file);
                string suffix = @" in Sources */,";
                string toWrite = prefix + guid + infix + filename + suffix;
                text.Insert(indexSrcBuildMacLast + 1, toWrite);
            }

            // add to ios PBXSourcesBuildPhase
            string markerSrcBuildIosLast = @"				503AE10117EB989F00D1A890 /* main.m in Sources */,";
            int indexSrcBuildIosLast = findLastIndexOfMarker(markerSrcBuildIosLast);
            for (int i = cppFiles.Count - 1; i >= 0; --i)
            {
                string file = cppFiles[i];
                string prefix = "				";
                string guid = cppFileToGuidSetMap[file].ios;
                string infix = @" /* ";
                string filename = getFileNameFromFilePath(file);
                string suffix = @" in Sources */,";
                string toWrite = prefix + guid + infix + filename + suffix;
                text.Insert(indexSrcBuildIosLast + 1, toWrite);
            }
        }

        private void addResourcesToBuild()
        {
            // add all direct children of Resources folder

            // mac
            // add to mac PBXResourcesBuildPhase
            string markerResourcesBuildMacLast = @"				503AE0F817EB97AB00D1A890 /* Icon.icns in Resources */,";
            int indexResourcesBuildMacLast = findLastIndexOfMarker(markerResourcesBuildMacLast);
            for (int i = resourceFoldersLevelOne.Count - 1; i >= 0; --i)
            {
                string resourceFolder = resourceFoldersLevelOne[i];
                string prefix = "				";
                string guid = resourceFolderToGuidSetMap[resourceFolder].mac;
                string infix = @" /* ";
                string folderName = getFolderNameFromFolderPath(resourceFolder);
                string suffix = @" in Resources */,";
                string toWrite = prefix + guid + infix + folderName + suffix;
                text.Insert(indexResourcesBuildMacLast + 1, toWrite);
            }

            // ios
            // add to ios PBXResourcesBuildPhase
            string markerResourcesBuildIosLast = @"				50EF629717ECD46A001EB2F8 /* Icon-58.png in Resources */,";
            int indexResourcesBuildIosLast = findLastIndexOfMarker(markerResourcesBuildIosLast);
            for (int i = resourceFoldersLevelOne.Count - 1; i >= 0; --i)
            {
                string resourceFolder = resourceFoldersLevelOne[i];
                string prefix = "				";
                string guid = resourceFolderToGuidSetMap[resourceFolder].ios;
                string infix = @" /* ";
                string folderName = getFolderNameFromFolderPath(resourceFolder);
                string suffix = @" in Resources */,";
                string toWrite = prefix + guid + infix + folderName + suffix;
                text.Insert(indexResourcesBuildIosLast + 1, toWrite);
            }
        }

        private void addToPBXGroup(ref List<string> toAdd)
        {
            string markerWriteBefore = @"/* End PBXGroup section */";
            int indexAtLine = findLastIndexOfMarker(markerWriteBefore);
            for (int i = toAdd.Count - 1; i >= 0; --i)
            {
                string lineToAdd = toAdd[i];
                text.Insert(indexAtLine, lineToAdd);
            }
        }

        private void addPBXGroupHeader(string folder, ref List<string> stringsToAdd, bool isSrcFolder)
        {
            //@"		29B97323FDCFA39411CA2CEA /* Frameworks */ = {";
            string firstString = "";
            {
                string prefix = @"		";

                string groupGuid = "";
                if (isSrcFolder == true)
                {
                    // src
                    groupGuid = srcGroupToGuidMap[folder];
                }
                else
                {
                    // resource
                    groupGuid = resourceGroupToGuidMap[folder];
                }
                string infix = @" /* ";
                string name = getFolderNameFromFolderPath(folder);
                string suffix = @" */ = {";
                firstString = prefix + groupGuid + infix + name + suffix;
            }
            stringsToAdd.Add(firstString);

            string secondString = @"			isa = PBXGroup;";
            stringsToAdd.Add(secondString);
            string childrenBegin = @"			children = (";
            stringsToAdd.Add(childrenBegin);
        }

        private void addPBXGroupFooter(string folder, ref List<string> stringsToAdd)
        {
            string childrenEnd = @"			);";
            stringsToAdd.Add(childrenEnd);
            string nameString = @"			name = ";
            nameString += getFolderNameFromFolderPath(folder);
            nameString += ";";
            stringsToAdd.Add(nameString);
            string pathString = @"			path = ";
            // "			path = ../Classes;"
            pathString += folder;
            pathString += ";";
            stringsToAdd.Add(pathString);
            string preLastString = @"			sourceTree = ""<group>"";";
            stringsToAdd.Add(preLastString);
            string lastString = @"		};";
            stringsToAdd.Add(lastString);
        }

        private void addSrcGroupChildFilesToPBXGroup(string folder, ref List<string> stringsToAdd)
        {
            // add children
            foreach (string file in srcFolderToFileSetMap[folder])
            {
                // "				46880B8419C43A87006E1F66 /* AppDelegate.cpp */,"
                string prefix = @"				";
                string fileRefGuid = "";
                string fileName = getFileNameFromFilePath(file);
                if (fileName.EndsWith(".cpp"))
                {
                    // .cpp
                    fileRefGuid = cppFileToGuidSetMap[file].fileRef;
                }
                else
                {
                    // .h or .hpp
                    fileRefGuid = headerSrcFileToGuidMap[file];
                }
                string infix = @" /* ";
                string suffix = @" */,";

                string strToAdd = prefix + fileRefGuid + infix + fileName + suffix;
                stringsToAdd.Add(strToAdd);
            }
        }

        private void addResourecGroupChildFilesToPBXGroup(string folder, ref List<string> stringsToAdd)
        {
            if (resourceFolderToFileSetMap.ContainsKey(folder))
            {
                foreach (string resourceFile in resourceFolderToFileSetMap[folder])
                {
                    // "				46880B8419C43A87006E1F66 /* AppDelegate.cpp */,"
                    string prefix = @"				";
                    string fileRefGuid = resourceFileToGuidSetMap[resourceFile].fileRef;
                    string fileName = getFileNameFromFilePath(resourceFile);
                    string infix = @" /* ";
                    string suffix = @" */,";

                    string strToAdd = prefix + fileRefGuid + infix + fileName + suffix;
                    stringsToAdd.Add(strToAdd);
                }
            }
        }

        private void addSrcGroupChildLevelTwoGroups(string folder, ref List<string> stringsToAdd)
        {
            if (srcGroupLevelOneToSrcGroupChildren.ContainsKey(folder))
            {
                foreach (string srcGroup in srcGroupLevelOneToSrcGroupChildren[folder])
                {
                    // "				46880B8419C43A87006E1F66 /* AppDelegate.cpp */,"
                    string prefix = @"				";
                    string srcGroupGuid = srcGroupToGuidMap[srcGroup];
                    string srcGroupName = getFileNameFromFilePath(srcGroup);
                    string infix = @" /* ";
                    string suffix = @" */,";

                    string strToAdd = prefix + srcGroupGuid + infix + srcGroupName + suffix;
                    stringsToAdd.Add(strToAdd);
                }
            }
        }

        private void addResourceGroupChildLevelTwoGroups(string folder, ref List<string> stringsToAdd)
        {
            if (resourceGroupLevelOneToResourceGroupChildren.ContainsKey(folder))
            {
                foreach (string resourceGroup in resourceGroupLevelOneToResourceGroupChildren[folder])
                {
                    // "				46880B8419C43A87006E1F66 /* AppDelegate.cpp */,"
                    string prefix = @"				";
                    string resourceGroupGuid = resourceGroupToGuidMap[resourceGroup];
                    string resourceGroupName = getFileNameFromFilePath(resourceGroup);
                    string infix = @" /* ";
                    string suffix = @" */,";

                    string strToAdd = prefix + resourceGroupGuid + infix + resourceGroupName + suffix;
                    stringsToAdd.Add(strToAdd);
                }
            }
        }

        private void addResourceGroupChildLevelThreeGroups(string folder, ref List<string> stringsToAdd)
        {
            if (resourceGroupLevelTwoToResourceGroupChildren.ContainsKey(folder))
            {
                foreach (string resourceGroup in resourceGroupLevelTwoToResourceGroupChildren[folder])
                {
                    // "				46880B8419C43A87006E1F66 /* AppDelegate.cpp */,"
                    string prefix = @"				";
                    string resourceGroupGuid = resourceGroupToGuidMap[resourceGroup];
                    string resourceGroupName = getFileNameFromFilePath(resourceGroup);
                    string infix = @" /* ";
                    string suffix = @" */,";

                    string strToAdd = prefix + resourceGroupGuid + infix + resourceGroupName + suffix;
                    stringsToAdd.Add(strToAdd);
                }
            }
        }

        private void addSrcGroupLevelOneToPBXGroup(string folder)
        {
            List<string> toAdd = new List<string>();
            addPBXGroupHeader(folder, ref toAdd, true);
            addSrcGroupChildLevelTwoGroups(folder, ref toAdd);
            addSrcGroupChildFilesToPBXGroup(folder, ref toAdd);
            addPBXGroupFooter(folder, ref toAdd);
            addToPBXGroup(ref toAdd);
        }

        private void addSrcGroupLevelTwoToPBXGroup(string folder)
        {
            List<string> toAdd = new List<string>();
            addPBXGroupHeader(folder, ref toAdd, true);
            addSrcGroupChildFilesToPBXGroup(folder, ref toAdd);
            addPBXGroupFooter(folder, ref toAdd);
            addToPBXGroup(ref toAdd);
        }

        private void addResourceGroupLevelOneToPBXGroup(string folder)
        {
            List<string> toAdd = new List<string>();
            addPBXGroupHeader(folder, ref toAdd, false);
            addResourceGroupChildLevelTwoGroups(folder, ref toAdd);
            addResourecGroupChildFilesToPBXGroup(folder, ref toAdd);
            addPBXGroupFooter(folder, ref toAdd);
            addToPBXGroup(ref toAdd);
        }

        private void addResourceGroupLevelTwoToPBXGroup(string folder)
        {
            List<string> toAdd = new List<string>();
            addPBXGroupHeader(folder, ref toAdd, false);
            addResourceGroupChildLevelThreeGroups(folder, ref toAdd);
            addResourecGroupChildFilesToPBXGroup(folder, ref toAdd);
            addPBXGroupFooter(folder, ref toAdd);
            addToPBXGroup(ref toAdd);
        }

        private void addResourceGroupLevelThreeToPBXGroup(string folder)
        {
            List<string> toAdd = new List<string>();
            addPBXGroupHeader(folder, ref toAdd, false);
            addResourecGroupChildFilesToPBXGroup(folder, ref toAdd);
            addPBXGroupFooter(folder, ref toAdd);
            addToPBXGroup(ref toAdd);
        }

        private void addClassesGroupChildren()
        {
            List<string> toAdd = new List<string>();

            SortedSet<string> children = srcGroupRootToSrcGroupChildren.First().Value;
            foreach (string group in children)
            {
                // @"521A8EA819F11F5000D177D7 /* fonts */,";
                string prefix = @"				";
                string guid = srcGroupToGuidMap[group];
                string infix = @" /* ";
                string groupName = getFolderNameFromFolderPath(group);
                string suffix = @" */,";
                string strToAdd = prefix + guid + infix + groupName + suffix;
                toAdd.Add(strToAdd);
            }

            string marker = @"		46880B8319C43A87006E1F66 /* Classes */ = {";
            int indexAtLine = findLastIndexOfMarker(marker) + 3;
            for (int i = toAdd.Count - 1; i >= 0; --i)
            {
                string lineToAdd = toAdd[i];
                text.Insert(indexAtLine, lineToAdd);
            }
        }

        private void addResourcesGroupChildren()
        {
            List<string> toAdd = new List<string>();

            SortedSet<string> children = resourceGroupRootToResourceGroupChildren.First().Value;
            foreach (string group in children)
            {
                // @"521A8EA819F11F5000D177D7 /* fonts */,";
                string prefix = @"				";
                string guid = resourceGroupToGuidMap[group];
                string infix = @" /* ";
                string groupName = getFolderNameFromFolderPath(group);
                string suffix = @" */,";
                string strToAdd = prefix + guid + infix + groupName + suffix;
                toAdd.Add(strToAdd);
            }

            string marker = @"		46880B7519C43A67006E1F66 /* Resources */ = {";
            int indexAtLine = findLastIndexOfMarker(marker) + 3;
            for (int i = toAdd.Count - 1; i >= 0; --i)
            {
                string lineToAdd = toAdd[i];
                text.Insert(indexAtLine, lineToAdd);
            }
        }

        private int getPBXFileReferenceSectionStringToWriteIndex()
        {
            int result = -1;

            string marker = @"/* End PBXFileReference section */";
            result = findLastIndexOfMarker(marker);

            return result;
        }

        private void addPBXFileRefsResourceGroups()
        {
            // 1. Resource groups
            // "		3EACC98E19EE6D4300EB3C5E /* res */ = {isa = PBXFileReference; lastKnownFileType = folder; path = res; sourceTree = "<group>"; };"
            List<string> toAdd = new List<string>();

            foreach (var mapPair in resourceFolderToGuidSetMap)
            {
                string name = getFolderNameFromFolderPath(mapPair.Key);
                string fileRef = mapPair.Value.fileRef;

                string prefix = @"		";
                string infix01 = @" /* ";
                string infix02 = @" */ = {isa = PBXFileReference; lastKnownFileType = ";
                string fileType = "folder";
                string infix03 = @"; path = ";
                string suffix = @"; sourceTree = ""<group>""; };";

                string strToAdd = prefix + fileRef + infix01 + name + infix02 + fileType + infix03 + name + suffix;

                toAdd.Add(strToAdd);
            }

            int indexAtLine = getPBXFileReferenceSectionStringToWriteIndex();
            for (int i = toAdd.Count - 1; i >= 0; --i)
            {
                string lineToAdd = toAdd[i];
                text.Insert(indexAtLine, lineToAdd);
            }
        }

        private void addPBXFileRefsSrcGroups()
        {
            //// 2. Src groups

            //List<string> toAdd = new List<string>();

            //foreach (var mapPair in resourceFolderToGuidSetMap)
            //{
            //    string name = mapPair.Key;
            //    string ios = mapPair.Value.ios;
            //    string mac = mapPair.Value.mac;
            //    string fileRef = mapPair.Value.fileRef;

            //    string prefix = @"		";
            //    string infix01 = @" /* ";
            //    string infix02 = @" in Resources */ = {isa = PBXBuildFile; fileRef = ";
            //    string infix03 = @" /* ";
            //    string suffix = @" */; };";

            //    string strToAdd = prefix + ios + infix01 + name + infix02 + fileRef + infix03 + name + suffix;

            //    toAdd.Add(strToAdd);
            //}

            //int indexAtLine = getPBXFileReferenceSectionStringToWriteIndex();
            //for (int i = toAdd.Count - 1; i >= 0; --i)
            //{
            //    string lineToAdd = toAdd[i];
            //    text.Insert(indexAtLine, lineToAdd);
            //}
        }

        private void addPBXFileRefsResourceFiles()
        {
            // 3. Resource files
            // 3.1. png
            // "		46880B7619C43A67006E1F66 /* CloseNormal.png */ = {isa = PBXFileReference; lastKnownFileType = image.png; path = CloseNormal.png; sourceTree = "<group>"; };"
            // 3.2. xml
            // "503AE0F717EB97AB00D1A890 /* Info.plist */ = {isa = PBXFileReference; fileEncoding = 4; lastKnownFileType = text.plist.xml; path = Info.plist; sourceTree = "<group>"; };"
            // 3.3. other (like mp3)

            Dictionary<string, string> fileTypeMap = new Dictionary<string, string>();
            fileTypeMap["png"] = "image.png";
            fileTypeMap["other"] = "text.plist.xml";

            List<string> toAdd = new List<string>();

            foreach (var mapPair in resourceFileToGuidSetMap)
            {
                string name = getFileNameFromFilePath(mapPair.Key);
                string fileRef = mapPair.Value.fileRef;

                string prefix = @"		";
                string infix01 = @" /* ";
                string infix02 = @" */ = {isa = PBXFileReference; lastKnownFileType = ";

                string fileType = "";
                if (name.EndsWith(".png"))
                {
                    fileType = fileTypeMap["png"];
                }
                else
                {
                    fileType = fileTypeMap["other"];
                    infix02 = @" */ = {isa = PBXFileReference; fileEncoding = 4; lastKnownFileType = ";
                }

                string infix03 = @"; path = ";
                string suffix = @"; sourceTree = ""<group>""; };";

                string strToAdd = prefix + fileRef + infix01 + name + infix02 + fileType + infix03 + name + suffix;

                toAdd.Add(strToAdd);
            }

            int indexAtLine = getPBXFileReferenceSectionStringToWriteIndex();
            for (int i = toAdd.Count - 1; i >= 0; --i)
            {
                string lineToAdd = toAdd[i];
                text.Insert(indexAtLine, lineToAdd);
            }
        }

        private void addPBXFileRefsSrcFiles()
        {
            // 4. Src files
            // 4.1. header (h or hpp)
            // "		503AE10417EB98FF00D1A890 /* Prefix.pch */ = {isa = PBXFileReference; fileEncoding = 4; lastKnownFileType = sourcecode.c.h; name = Prefix.pch; path = mac/Prefix.pch; sourceTree = "<group>"; };"
            // 4.2. cpp file
            // "		503AE10317EB98FF00D1A890 /* main.cpp */ = {isa = PBXFileReference; fileEncoding = 4; lastKnownFileType = sourcecode.cpp.cpp; name = main.cpp; path = mac/main.cpp; sourceTree = "<group>"; };"

            Dictionary<string, string> fileTypeMap = new Dictionary<string, string>();
            fileTypeMap["cpp"] = "sourcecode.cpp.cpp";
            fileTypeMap["h"] = "sourcecode.c.h";

            List<string> toAdd = new List<string>();

            // h
            foreach (var mapPair in headerSrcFileToGuidMap)
            {
                string name = getFileNameFromFilePath(mapPair.Key);
                string fileRef = mapPair.Value;

                string prefix = @"		";
                string infix01 = @" /* ";
                string infix02 = @" */ = {isa = PBXFileReference; fileEncoding = 4; lastKnownFileType = ";

                string fileType = fileTypeMap["h"];

                string infix03 = @"; path = ";
                string suffix = @"; sourceTree = ""<group>""; };";

                string strToAdd = prefix + fileRef + infix01 + name + infix02 + fileType + infix03 + name + suffix;

                toAdd.Add(strToAdd);
            }

            // cpp 
            foreach (var mapPair in cppFileToGuidSetMap)
            {
                string name = getFileNameFromFilePath(mapPair.Key);
                string fileRef = mapPair.Value.fileRef;

                string prefix = @"		";
                string infix01 = @" /* ";
                string infix02 = @" */ = {isa = PBXFileReference; fileEncoding = 4; lastKnownFileType = ";

                string fileType = fileTypeMap["cpp"];

                string infix03 = @"; path = ";
                string suffix = @"; sourceTree = ""<group>""; };";

                string strToAdd = prefix + fileRef + infix01 + name + infix02 + fileType + infix03 + name + suffix;

                toAdd.Add(strToAdd);
            }

            int indexAtLine = getPBXFileReferenceSectionStringToWriteIndex();
            for (int i = toAdd.Count - 1; i >= 0; --i)
            {
                string lineToAdd = toAdd[i];
                text.Insert(indexAtLine, lineToAdd);
            }
        }

        private void addPBXFileReferences()
        {
            addPBXFileRefsResourceGroups();
            // not needed to add Src folders
            // addPBXFileRefsSrcGroups();
            addPBXFileRefsResourceFiles();
            addPBXFileRefsSrcFiles();
        }

        private int getPBXBuildFileSectionStringToWriteIndex()
        {
            int result = -1;

            string marker = @"/* End PBXBuildFile section */";
            result = findLastIndexOfMarker(marker);

            return result;
        }

        private void addPBXBuildFilesResourceGroups()
        {
            // TODO add Asset folder, add Data folder

            //List<string> toAdd = new List<string>();

            ////		521A8EA919F11F5000D177D7 /* fonts in Resources */ = {isa = PBXBuildFile; fileRef = 521A8EA819F11F5000D177D7 /* fonts */; };
            //foreach (var mapPair in resourceFolderToGuidSetMap)
            //{
            //    string name = getFolderNameFromFolderPath(mapPair.Key);
            //    string ios = mapPair.Value.ios;
            //    string mac = mapPair.Value.mac;
            //    string fileRef = mapPair.Value.fileRef;

            //    string prefix = @"		";
            //    string infix01 = @" /* ";
            //    string infix02 = @" in Resources */ = {isa = PBXBuildFile; fileRef = ";
            //    string infix03 = @" /* ";
            //    string suffix = @" */; };";

            //    string strToAddIos = prefix + ios + infix01 + name + infix02 + fileRef + infix03 + name + suffix;
            //    string strToAddMac = prefix + mac + infix01 + name + infix02 + fileRef + infix03 + name + suffix;

            //    toAdd.Add(strToAddIos);
            //    toAdd.Add(strToAddMac);
            //}

            //int indexAtLine = getPBXBuildFileSectionStringToWriteIndex();
            //for (int i = toAdd.Count - 1; i >= 0; --i)
            //{
            //    string lineToAdd = toAdd[i];
            //    text.Insert(indexAtLine, lineToAdd);
            //}
        }

        private void addPBXBuildSrcGroups()
        {
            // it is not needed to add src folders (groups)

            //List<string> toAdd = new List<string>();

            ////		503AE10117EB989F00D1A890 /* BehaviorTree in Sources */ = {isa = PBXBuildFile; fileRef = 503AE0FC17EB989F00D1A890 /* BehaviorTree */; };
            //foreach (var mapPair in resourceFolderToGuidSetMap)
            //{
            //    string name = mapPair.Key;
            //    string ios = mapPair.Value.ios;
            //    string mac = mapPair.Value.mac;
            //    string fileRef = mapPair.Value.fileRef;

            //    string prefix = @"		";
            //    string infix01 = @" /* ";
            //    string infix02 = @" in Sources */ = {isa = PBXBuildFile; fileRef = ";
            //    string infix03 = @" /* ";
            //    string suffix = @" */; };";

            //    string strToAddIos = prefix + ios + infix01 + name + infix02 + fileRef + infix03 + name + suffix;
            //    string strToAddMac = prefix + mac + infix01 + name + infix02 + fileRef + infix03 + name + suffix;

            //    toAdd.Add(strToAddIos);
            //    toAdd.Add(strToAddMac);
            //}

            //int indexAtLine = getPBXBuildFileSectionStringToWriteIndex();
            //for (int i = toAdd.Count - 1; i >= 0; --i)
            //{
            //    string lineToAdd = toAdd[i];
            //    text.Insert(indexAtLine, lineToAdd);
            //}
        }

        private void addPBXBuildResourceFiles()
        {
            // not needed, all resources will be added automatically

            //List<string> toAdd = new List<string>();

            ////		46880B7B19C43A67006E1F66 /* CloseNormal.png in Resources */ = {isa = PBXBuildFile; fileRef = 46880B7619C43A67006E1F66 /* CloseNormal.png */; };
            //foreach (var mapPair in resourceFileToGuidSetMap)
            //{
            //    string name = getFolderNameFromFolderPath(mapPair.Key);
            //    string ios = mapPair.Value.ios;
            //    string mac = mapPair.Value.mac;
            //    string fileRef = mapPair.Value.fileRef;

            //    string prefix = @"		";
            //    string infix01 = @" /* ";
            //    string infix02 = @" in Resources */ = {isa = PBXBuildFile; fileRef = ";
            //    string infix03 = @" /* ";
            //    string suffix = @" */; };";

            //    string strToAddIos = prefix + ios + infix01 + name + infix02 + fileRef + infix03 + name + suffix;
            //    string strToAddMac = prefix + mac + infix01 + name + infix02 + fileRef + infix03 + name + suffix;

            //    toAdd.Add(strToAddIos);
            //    toAdd.Add(strToAddMac);
            //}

            //int indexAtLine = getPBXBuildFileSectionStringToWriteIndex();
            //for (int i = toAdd.Count - 1; i >= 0; --i)
            //{
            //    string lineToAdd = toAdd[i];
            //    text.Insert(indexAtLine, lineToAdd);
            //}
        }

        private void addPBXBuildSrcFiles()
        {
            List<string> toAdd = new List<string>();

            //		46880B8819C43A87006E1F66 /* AppDelegate.cpp in Sources */ = {isa = PBXBuildFile; fileRef = 46880B8419C43A87006E1F66 /* AppDelegate.cpp */; };
            foreach (var mapPair in cppFileToGuidSetMap)
            {
                string name = getFolderNameFromFolderPath(mapPair.Key);
                string ios = mapPair.Value.ios;
                string mac = mapPair.Value.mac;
                string fileRef = mapPair.Value.fileRef;

                string prefix = @"		";
                string infix01 = @" /* ";
                string infix02 = @" in Sources */ = {isa = PBXBuildFile; fileRef = ";
                string infix03 = @" /* ";
                string suffix = @" */; };";

                string strToAddIos = prefix + ios + infix01 + name + infix02 + fileRef + infix03 + name + suffix;
                string strToAddMac = prefix + mac + infix01 + name + infix02 + fileRef + infix03 + name + suffix;

                toAdd.Add(strToAddIos);
                toAdd.Add(strToAddMac);
            }

            int indexAtLine = getPBXBuildFileSectionStringToWriteIndex();
            for (int i = toAdd.Count - 1; i >= 0; --i)
            {
                string lineToAdd = toAdd[i];
                text.Insert(indexAtLine, lineToAdd);
            }
        }

        private void addPBXBuildFiles()
        {
            addPBXBuildFilesResourceGroups();
            // it is not needed to add src groups
            //addPBXBuildSrcGroups();
            addPBXBuildResourceFiles();
            addPBXBuildSrcFiles();

            // 1. Resource folders, 2 times: mac and ios
            // 2. Src folders, 2 times: mac and ios
            // png files
            // cpp src files
        }

        private void button3_Click(object sender, EventArgs e)
        {
            addHeaderSearchPaths();
            addSourcesToBuild();
            addResourcesToBuild();

            // PBXGroup
            // contains file references or group references
            // each group both src and resource should be added as a fileReference as well
            foreach (string folder in srcFoldersLevelTwo)
            {
                //2
                addSrcGroupLevelTwoToPBXGroup(folder);
                string parentFolder = getFolderParent(folder);
                insertInDictionarySet(ref srcGroupLevelOneToSrcGroupChildren, parentFolder, folder);
            }

            foreach (string folder in srcFoldersLevelOne)
            {
                //1
                addSrcGroupLevelOneToPBXGroup(folder);
                string parentFolder = getFolderParent(folder);
                insertInDictionarySet(ref srcGroupRootToSrcGroupChildren, parentFolder, folder);
            }

            foreach (string folder in resourceFoldersLevelThree)
            {
                //3
                addResourceGroupLevelThreeToPBXGroup(folder);
                string parentFolder = getFolderParent(folder);
                insertInDictionarySet(ref resourceGroupLevelTwoToResourceGroupChildren, parentFolder, folder);
            }

            foreach (string folder in resourceFoldersLevelTwo)
            {
                //2
                addResourceGroupLevelTwoToPBXGroup(folder);
                string parentFolder = getFolderParent(folder);
                insertInDictionarySet(ref resourceGroupLevelOneToResourceGroupChildren, parentFolder, folder);
            }

            foreach (string folder in resourceFoldersLevelOne)
            {
                //1
                addResourceGroupLevelOneToPBXGroup(folder);
                string parentFolder = getFolderParent(folder);
                insertInDictionarySet(ref resourceGroupRootToResourceGroupChildren, parentFolder, folder);
            }

            // add first level src folders to Classes children
            addClassesGroupChildren();

            // add first level resources folders to Resources children
            addResourcesGroupChildren();

            // PBXFileReference
            addPBXFileReferences();

            // PBXBuildFile
            addPBXBuildFiles();

            // write to output file
            // add just LF instead of default windows CR LF
            File.WriteAllText(outputFileName, string.Join("\n", text.ToArray()) + "\n");
        }

        private void addResourceFolder(string resourceFolder)
        {
            // 1
            // 521A8EA919F11F5000D177D7 /* fonts in Resources */ = {isa = PBXBuildFile; fileRef = 521A8EA819F11F5000D177D7 /* fonts */; };
            // insert in place of /* End PBXBuildFile section */
		    
            // 2
            // 521A8EAA19F11F5000D177D7 /* fonts in Resources */ = {isa = PBXBuildFile; fileRef = 521A8EA819F11F5000D177D7 /* fonts */; };
            // insert in place of /* End PBXBuildFile section */

            // 3
            // 521A8EA819F11F5000D177D7 /* fonts */ = {isa = PBXFileReference; lastKnownFileType = folder; path = fonts; sourceTree = "<group>"; };
            // /* End PBXFileReference section */

            // 4
            // 521A8EA819F11F5000D177D7 /* fonts */,
            // 46880B7519C43A67006E1F66 /* Resources */ = {
            //    isa = PBXGroup;
            //    children = (
            //        521A8EA819F11F5000D177D7 /* fonts */,
            //        3EACC98E19EE6D4300EB3C5E /* res */,
            //        46880B7619C43A67006E1F66 /* CloseNormal.png */,
            //        46880B7719C43A67006E1F66 /* CloseSelected.png */,
            //        46880B7A19C43A67006E1F66 /* HelloWorld.png */,
            //    );
            //    name = Resources;
            //    path = ../Resources;
            //    sourceTree = "<group>";
            //};

            // 5
            //				521A8EA919F11F5000D177D7 /* fonts in Resources */,

            //        /* Begin PBXResourcesBuildPhase section */
            //        1D60588D0D05DD3D006BFB54 /* Resources */ = {
            //            isa = PBXResourcesBuildPhase;
            //            buildActionMask = 2147483647;
            //            files = (
            //                46880B7B19C43A67006E1F66 /* CloseNormal.png in Resources */,
				
            //                5087E77D17EB970100C73F5D /* Default-568h@2x.png in Resources */,
				
            //                521A8EA919F11F5000D177D7 /* fonts in Resources */,
            //                3EACC98F19EE6D4300EB3C5E /* res in Resources */,
				
            //                50EF629717ECD46A001EB2F8 /* Icon-58.png in Resources */,
            //            );
            //            runOnlyForDeploymentPostprocessing = 0;
            //        };
            //        5087E74817EB910900C73F5D /* Resources */ = {
            //            isa = PBXResourcesBuildPhase;
            //            buildActionMask = 2147483647;
            //            files = (
            //                503AE0F817EB97AB00D1A890 /* Icon.icns in Resources */,
				
            //                521A8EAA19F11F5000D177D7 /* fonts in Resources */,
            //                46880B7E19C43A67006E1F66 /* CloseSelected.png in Resources */,
            //            );
            //            runOnlyForDeploymentPostprocessing = 0;
            //        };
            ///* End PBXResourcesBuildPhase section */

            // 6
            //				521A8EAA19F11F5000D177D7 /* fonts in Resources */,
        }

        private void addResourceFile(string resourceFolder, string resourceFile)
        {
            // 1
            // 46880B7B19C43A67006E1F66 /* CloseNormal.png in Resources */ = {isa = PBXBuildFile; fileRef = 46880B7619C43A67006E1F66 /* CloseNormal.png */; };
            // /* End PBXBuildFile section */

            // 2
            // 46880B7C19C43A67006E1F66 /* CloseNormal.png in Resources */ = {isa = PBXBuildFile; fileRef = 46880B7619C43A67006E1F66 /* CloseNormal.png */; };
            // /* End PBXBuildFile section */

            // 3
            // 46880B7619C43A67006E1F66 /* CloseNormal.png */ = {isa = PBXFileReference; lastKnownFileType = image.png; path = CloseNormal.png; sourceTree = "<group>"; };
            // /* End PBXFileReference section */

            // 4
            //				46880B7619C43A67006E1F66 /* CloseNormal.png */,

            ///* Begin PBXGroup section */
            //        080E96DDFE201D6D7F000001 /* ios */ = {
            //            isa = PBXGroup;
            //            children = (
            //                5087E77117EB970100C73F5D /* Icons */,
            //                503AE0FA17EB989F00D1A890 /* AppController.h */,
            //            );
            //            name = ios;
            //            path = Classes;
            //            sourceTree = "<group>";
            //        };
            //        19C28FACFE9D520D11CA2CBB /* Products */ = {
            //            isa = PBXGroup;
            //            children = (
            //                1D6058910D05DD3D006BFB54 /* RealmChronicles-mobile.app */,
            //                5087E76F17EB910900C73F5D /* RealmChronicles-desktop.app */,
            //            );
            //            name = Products;
            //            sourceTree = "<group>";
            //        };
            //        1AC6FAE6180E9839004C840B /* Products */ = {
            //            isa = PBXGroup;
            //            children = (
            //                1AC6FAF9180E9839004C840B /* libcocos2d Mac.a */,
            //                1AC6FB07180E9839004C840B /* libcocos2d iOS.a */,
            //            );
            //            name = Products;
            //            sourceTree = "<group>";
            //        };
            //        29B97314FDCFA39411CA2CEA /* CustomTemplate */ = {
            //            isa = PBXGroup;
            //            children = (
            //                46880B8319C43A87006E1F66 /* Classes */,
            //                46880B7519C43A67006E1F66 /* Resources */,
            //                1AC6FAE5180E9839004C840B /* cocos2d_libs.xcodeproj */,
            //                29B97323FDCFA39411CA2CEA /* Frameworks */,
            //                080E96DDFE201D6D7F000001 /* ios */,
            //                503AE10617EB990700D1A890 /* mac */,
            //                19C28FACFE9D520D11CA2CBB /* Products */,
            //            );
            //            name = CustomTemplate;
            //            sourceTree = "<group>";
            //        };
            //        29B97323FDCFA39411CA2CEA /* Frameworks */ = {
            //            isa = PBXGroup;
            //            children = (
            //                ED545A7D1B68A1FA00C3958E /* libiconv.dylib */,
            //            );
            //            name = Frameworks;
            //            sourceTree = "<group>";
            //        };
            //        46880B7519C43A67006E1F66 /* Resources */ = {
            //            isa = PBXGroup;
            //            children = (
            //                46880B7619C43A67006E1F66 /* CloseNormal.png */,
            //                46880B7719C43A67006E1F66 /* CloseSelected.png */,
            //                46880B7A19C43A67006E1F66 /* HelloWorld.png */,
            //            );
            //            name = Resources;
            //            path = ../Resources;
            //            sourceTree = "<group>";
            //        };
            //        46880B8319C43A87006E1F66 /* Classes */ = {
            //            isa = PBXGroup;
            //            children = (
            //                46880B8419C43A87006E1F66 /* AppDelegate.cpp */,
            //                46880B8519C43A87006E1F66 /* AppDelegate.h */,
            //                46880B8619C43A87006E1F66 /* HelloWorldScene.cpp */,
            //                46880B8719C43A87006E1F66 /* HelloWorldScene.h */,
            //            );
            //            name = Classes;
            //            path = ../Classes;
            //            sourceTree = "<group>";
            //        };
            //        503AE0F517EB97AB00D1A890 /* Icons */ = {
            //            isa = PBXGroup;
            //            children = (
            //                503AE0F617EB97AB00D1A890 /* Icon.icns */,
            //                503AE0F717EB97AB00D1A890 /* Info.plist */,
            //            );
            //            name = Icons;
            //            path = mac;
            //            sourceTree = SOURCE_ROOT;
            //        };
            //        503AE10617EB990700D1A890 /* mac */ = {
            //            isa = PBXGroup;
            //            children = (
            //                503AE0F517EB97AB00D1A890 /* Icons */,
            //                503AE10317EB98FF00D1A890 /* main.cpp */,
            //                503AE10417EB98FF00D1A890 /* Prefix.pch */,
            //            );
            //            name = mac;
            //            sourceTree = "<group>";
            //        };
            //        5087E77117EB970100C73F5D /* Icons */ = {
            //            isa = PBXGroup;
            //            children = (
            //                521A8E6219F0C34300D177D7 /* Default-667h@2x.png */,
            //            );
            //            name = Icons;
            //            path = ios;
            //            sourceTree = SOURCE_ROOT;
            //        };
            ///* End PBXGroup section */


            // 5
            //				46880B7B19C43A67006E1F66 /* CloseNormal.png in Resources */,
            ///* Begin PBXResourcesBuildPhase section */
            //        1D60588D0D05DD3D006BFB54 /* Resources */ = {
            //            isa = PBXResourcesBuildPhase;
            //            buildActionMask = 2147483647;
            //            files = (
            //                5087E78117EB970100C73F5D /* Icon-120.png in Resources */,
				
            //                46880B8119C43A67006E1F66 /* HelloWorld.png in Resources */,
            //                46880B7D19C43A67006E1F66 /* CloseSelected.png in Resources */,
            //                46880B7B19C43A67006E1F66 /* CloseNormal.png in Resources */,
				
            //                50EF629717ECD46A001EB2F8 /* Icon-58.png in Resources */,
            //            );
            //            runOnlyForDeploymentPostprocessing = 0;
            //        };
            //        5087E74817EB910900C73F5D /* Resources */ = {
            //            isa = PBXResourcesBuildPhase;
            //            buildActionMask = 2147483647;
            //            files = (
            //                46880B8219C43A67006E1F66 /* HelloWorld.png in Resources */,
            //                46880B7C19C43A67006E1F66 /* CloseNormal.png in Resources */,
            //                46880B7E19C43A67006E1F66 /* CloseSelected.png in Resources */,
				
            //                503AE0F817EB97AB00D1A890 /* Icon.icns in Resources */,
            //            );
            //            runOnlyForDeploymentPostprocessing = 0;
            //        };
            ///* End PBXResourcesBuildPhase section */


            //// !! After 50EF629717ECD46A001EB2F8 /* Icon-58.png in Resources */,



            // 6
            // 				46880B7C19C43A67006E1F66 /* CloseNormal.png in Resources */,
            ///* Begin PBXResourcesBuildPhase section */
            //        1D60588D0D05DD3D006BFB54 /* Resources */ = {
            //            isa = PBXResourcesBuildPhase;
            //            buildActionMask = 2147483647;
            //            files = (
            //                5087E78117EB970100C73F5D /* Icon-120.png in Resources */,
				
            //                46880B8119C43A67006E1F66 /* HelloWorld.png in Resources */,
            //                46880B7D19C43A67006E1F66 /* CloseSelected.png in Resources */,
            //                46880B7B19C43A67006E1F66 /* CloseNormal.png in Resources */,
				
            //                50EF629717ECD46A001EB2F8 /* Icon-58.png in Resources */,
            //            );
            //            runOnlyForDeploymentPostprocessing = 0;
            //        };
            //        5087E74817EB910900C73F5D /* Resources */ = {
            //            isa = PBXResourcesBuildPhase;
            //            buildActionMask = 2147483647;
            //            files = (
            //                46880B8219C43A67006E1F66 /* HelloWorld.png in Resources */,
            //                46880B7C19C43A67006E1F66 /* CloseNormal.png in Resources */,
            //                46880B7E19C43A67006E1F66 /* CloseSelected.png in Resources */,
				
            //                503AE0F817EB97AB00D1A890 /* Icon.icns in Resources */,
            //            );
            //            runOnlyForDeploymentPostprocessing = 0;
            //        };
            ///* End PBXResourcesBuildPhase section */


            //// !! After 503AE0F817EB97AB00D1A890 /* Icon.icns in Resources */,


        }

        private void addClassesFolder(string classesFolder)
        {

        }

        private void addCppSrcFile(string classesFolder, string cppSrcFile)
        {
            // 1
            //		46880B8A19C43A87006E1F66 /* HelloWorldScene.cpp in Sources */ = {isa = PBXBuildFile; fileRef = 46880B8619C43A87006E1F66 /* HelloWorldScene.cpp */; };
            ///* End PBXBuildFile section */

            // 2
            //		46880B8B19C43A87006E1F66 /* HelloWorldScene.cpp in Sources */ = {isa = PBXBuildFile; fileRef = 46880B8619C43A87006E1F66 /* HelloWorldScene.cpp */; };
            ///* End PBXBuildFile section */

            // 3
            //		46880B8619C43A87006E1F66 /* HelloWorldScene.cpp */ = {isa = PBXFileReference; fileEncoding = 4; lastKnownFileType = sourcecode.cpp.cpp; path = HelloWorldScene.cpp; sourceTree = "<group>"; };
            ///* End PBXFileReference section */

            // 4
            //				46880B8619C43A87006E1F66 /* HelloWorldScene.cpp */,
            ///* Begin PBXGroup section */
            //        080E96DDFE201D6D7F000001 /* ios */ = {
            //            isa = PBXGroup;
            //            children = (
            //                5087E77117EB970100C73F5D /* Icons */,
            //                503AE0FA17EB989F00D1A890 /* AppController.h */,
            //                503AE0FB17EB989F00D1A890 /* AppController.mm */,
            //                503AE0FC17EB989F00D1A890 /* main.m */,
            //                503AE0FD17EB989F00D1A890 /* Prefix.pch */,
            //                503AE0FE17EB989F00D1A890 /* RootViewController.h */,
            //                503AE0FF17EB989F00D1A890 /* RootViewController.mm */,
            //            );
            //            name = ios;
            //            path = Classes;
            //            sourceTree = "<group>";
            //        };
            //        19C28FACFE9D520D11CA2CBB /* Products */ = {
            //            isa = PBXGroup;
            //            children = (
            //                1D6058910D05DD3D006BFB54 /* RealmChronicles-mobile.app */,
            //                5087E76F17EB910900C73F5D /* RealmChronicles-desktop.app */,
            //            );
            //            name = Products;
            //            sourceTree = "<group>";
            //        };
            //        1AC6FAE6180E9839004C840B /* Products */ = {
            //            isa = PBXGroup;
            //            children = (
            //                1AC6FAF9180E9839004C840B /* libcocos2d Mac.a */,
            //                1AC6FB07180E9839004C840B /* libcocos2d iOS.a */,
            //            );
            //            name = Products;
            //            sourceTree = "<group>";
            //        };
            //        29B97314FDCFA39411CA2CEA /* CustomTemplate */ = {
            //            isa = PBXGroup;
            //            children = (
            //                46880B8319C43A87006E1F66 /* Classes */,
            //                46880B7519C43A67006E1F66 /* Resources */,
            //                1AC6FAE5180E9839004C840B /* cocos2d_libs.xcodeproj */,
            //                29B97323FDCFA39411CA2CEA /* Frameworks */,
            //                080E96DDFE201D6D7F000001 /* ios */,
            //                503AE10617EB990700D1A890 /* mac */,
            //                19C28FACFE9D520D11CA2CBB /* Products */,
            //            );
            //            name = CustomTemplate;
            //            sourceTree = "<group>";
            //        };
            //        29B97323FDCFA39411CA2CEA /* Frameworks */ = {
            //            isa = PBXGroup;
            //            children = (
            //                ED545A7D1B68A1FA00C3958E /* libiconv.dylib */,
            //            );
            //            name = Frameworks;
            //            sourceTree = "<group>";
            //        };
            //        46880B7519C43A67006E1F66 /* Resources */ = {
            //            isa = PBXGroup;
            //            children = (
            //            );
            //            name = Resources;
            //            path = ../Resources;
            //            sourceTree = "<group>";
            //        };
            //        46880B8319C43A87006E1F66 /* Classes */ = {
            //            isa = PBXGroup;
            //            children = (
            //                46880B8419C43A87006E1F66 /* AppDelegate.cpp */,
            //                46880B8619C43A87006E1F66 /* HelloWorldScene.cpp */,
            //            );
            //            name = Classes;
            //            path = ../Classes;
            //            sourceTree = "<group>";
            //        };
            //        503AE0F517EB97AB00D1A890 /* Icons */ = {
            //            isa = PBXGroup;
            //            children = (
            //                503AE0F617EB97AB00D1A890 /* Icon.icns */,
            //                503AE0F717EB97AB00D1A890 /* Info.plist */,
            //            );
            //            name = Icons;
            //            path = mac;
            //            sourceTree = SOURCE_ROOT;
            //        };
            //        503AE10617EB990700D1A890 /* mac */ = {
            //            isa = PBXGroup;
            //            children = (
            //                503AE0F517EB97AB00D1A890 /* Icons */,
            //                503AE10317EB98FF00D1A890 /* main.cpp */,
            //                503AE10417EB98FF00D1A890 /* Prefix.pch */,
            //            );
            //            name = mac;
            //            sourceTree = "<group>";
            //        };
            //        5087E77117EB970100C73F5D /* Icons */ = {
            //            isa = PBXGroup;
            //            children = (
            //                521A8E6219F0C34300D177D7 /* Default-667h@2x.png */,
            //                5087E77C17EB970100C73F5D /* Info.plist */,
            //            );
            //            name = Icons;
            //            path = ios;
            //            sourceTree = SOURCE_ROOT;
            //        };
            ///* End PBXGroup section */

            // 5
            //				46880B8A19C43A87006E1F66 /* HelloWorldScene.cpp in Sources */,
            ///* Begin PBXSourcesBuildPhase section */
            //        1D60588E0D05DD3D006BFB54 /* Sources */ = {
            //            isa = PBXSourcesBuildPhase;
            //            buildActionMask = 2147483647;
            //            files = (
            //                46880B8819C43A87006E1F66 /* AppDelegate.cpp in Sources */,
            //                46880B8A19C43A87006E1F66 /* HelloWorldScene.cpp in Sources */,
				
            //                503AE10017EB989F00D1A890 /* AppController.mm in Sources */,
            //                503AE10217EB989F00D1A890 /* RootViewController.mm in Sources */,
            //                503AE10117EB989F00D1A890 /* main.m in Sources */,
            //            );
            //            runOnlyForDeploymentPostprocessing = 0;
            //        };
            //        5087E75617EB910900C73F5D /* Sources */ = {
            //            isa = PBXSourcesBuildPhase;
            //            buildActionMask = 2147483647;
            //            files = (
            //                46880B8919C43A87006E1F66 /* AppDelegate.cpp in Sources */,
            //                503AE10517EB98FF00D1A890 /* main.cpp in Sources */,
				
            //                46880B8B19C43A87006E1F66 /* HelloWorldScene.cpp in Sources */,
            //            );
            //            runOnlyForDeploymentPostprocessing = 0;
            //        };
            ///* End PBXSourcesBuildPhase section */

            // 6
            //				46880B8B19C43A87006E1F66 /* HelloWorldScene.cpp in Sources */,
            // the same as 5
        }

        private void addCppHeaderFile(string classesFolder, string headerSrcFile)
        {
            // !! header is both .h and .hpp (pugixml)

            // 1
            //		46880B8519C43A87006E1F66 /* AppDelegate.h */ = {isa = PBXFileReference; fileEncoding = 4; lastKnownFileType = sourcecode.c.h; path = AppDelegate.h; sourceTree = "<group>"; };
            ///* End PBXFileReference section */

            // 2
            //				46880B8519C43A87006E1F66 /* AppDelegate.h */,
            //46880B8319C43A87006E1F66 /* Classes */ = {
            //    isa = PBXGroup;
            //    children = (
            //        46880B8419C43A87006E1F66 /* AppDelegate.cpp */,
            //        46880B8519C43A87006E1F66 /* AppDelegate.h */,
            //        46880B8619C43A87006E1F66 /* HelloWorldScene.cpp */,
            //        46880B8719C43A87006E1F66 /* HelloWorldScene.h */,
            //    );
            //    name = Classes;
            //    path = ../Classes;
            //    sourceTree = "<group>";
            //};
        }

        private void addSrcFolderReference(string classesFolder)
        {

        }

        private void parseSrcFiles(ref SortedDictionary<string, string> dict)
        {
            traverseTree(ref dict, srcRootFolderName, srcFolderPrefix);
        }

        private void parseResourcesFiles(ref SortedDictionary<string, string> dict)
        {
            traverseTree(ref dict, resourcesRootFolderName, resourceFolderPrefix);
        }

        private void traverseTree(ref SortedDictionary<string, string> dict, string rootFolderName, string folderPrefix)
        {
            string root = rootFolderName;
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

                        string relativeFolderName = currentDir.Replace(rootFolderName, "");
                        string fileName = folderPrefix + relativeFolderName + @"\" + fi.Name;
                        string folderName = folderPrefix + relativeFolderName;

                        replaceWindowsSlashWithUnix(ref fileName);
                        replaceWindowsSlashWithUnix(ref folderName);

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
                {
                    dirs.Push(str);
                }
            }
        }

        public static PhysicalAddress getMacAddress()
        {
            foreach (NetworkInterface nic in NetworkInterface.GetAllNetworkInterfaces())
            {
                // Only consider Ethernet network interfaces
                if (nic.NetworkInterfaceType == NetworkInterfaceType.Ethernet &&
                    nic.OperationalStatus == OperationalStatus.Up)
                {
                    return nic.GetPhysicalAddress();
                }
            }
            return null;
        }

        public static int getCurrentProcessId()
        {
            return Process.GetCurrentProcess().Id;
        }

        private Int64 time64 = 0;
        private string processId = "0000";
        private string macAddress = "000000000000";

        public string generateFirst()
        {
            string result = "";

            macAddress = getMacAddress().ToString();
            int stringLength = macAddress.Length;
            if (stringLength < 12)
            {
                int toAdd = 12 - stringLength;
                for (int i = 0; i < toAdd; ++i)
                {
                    macAddress = macAddress + "A";
                }
            }

            string timeStr = DateTime.Now.ToString("hmsfffff");
            time64 = Int64.Parse(timeStr);
            string timeStrHex = time64.ToString("X8");

            processId = getCurrentProcessId().ToString("X4");

            // [8 hex time] [4 hex pid] [12 hex mac]

            result = timeStrHex + processId + macAddress;

            return result;
        }

        public string generateNext()
        {
            string result = "";

            time64 = time64 + 1;
            string timeStrHex = time64.ToString("X8");

            // [8 hex time] [4 hex pid] [12 hex mac]

            result = timeStrHex + processId + macAddress;

            return result;
        }
    }
}
