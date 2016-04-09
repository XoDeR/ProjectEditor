http://danwright.info/blog/2010/10/xcode-pbxproject-files/

http://danwright.info/blog/2010/10/xcode-pbxproject-files-2/

http://danwright.info/blog/2010/10/xcode-pbxproject-files-3/



A brief look at the Xcode project format
3 October 2010 by Dan
Let’s take a quick little tour of the Xcode project file format. Anyone doing Mac OS X or iOS development in a team environment (i.e. just about everybody) has had to deal with the joy of merging project changes in version control (subversion, cvs, git, you name it).

An Xcode project, of course, is actually a package, a directory with bundled files within. A typical project might look like this:


MyProject.xcodeproj/
	danwr.mode1v3
	danwr.pbxuser
	project.pbxproj

The first two files have your username. If multiple users use the same project, you will have a set of .mode1v3 and .pbxuser files for each user. These files contain various user settings (preferences, really) that are associated with the project, for example:

the size and position of the project window
which groups are open and which are closed
which item in the project is selected
…and more
This information is not normally shared among multiple users, so it is neither necessary nor desirable to add these (.mode1v3 and .pbxuser) files to your repository. If you already have, go ahead and remove them (svn remove if you use subversion, for instance). I’ll wait.

PROJECT.PBXPROJ

This leaves us with the project file itself, project.pbxproj. Oh, look how smug it is, all undecipherable and all. Or… is it? Let’s open up that bad boy in a text editor and see what it looks like. First, the overall layout:


// !$*UTF8*$!
{
	archiveVersion = 1;
	classes = {
	};
	objectVersion = 45;
	objects = {
            [[ : snip! : ]]
	};
	rootObject = 29B97313FDCFA39411CA2CEA /* Project object */;
}
We start with a comment indicating the text encoding used in the file, UTF-8. Presumably other encodings are possible, however in practice all project files use UTF-8. You will notice other comments (C-style: /* ... */) sprinkled throughout the project file. While presumably Xcode’s lexer handles multi-line comments, Xcode itself does not generate multi-line comments. If one were attempting the read or write project.pbxproj files, the parser would need to be able to handle multi-line comments, while ideally avoiding writing them (unless preserving existing comments).

A set of brackets { } enclose a record of key-value pairs. The keys are:

archiveVersion; It is set to 1 in all versions of Xcode that use the file format described here.
classes; A list of classes, usually empty.
objectVersion; This relates to which object types are used in this project.pbxproj, and which keys are defined. This changes based on the version of Xcode that wrote the project; it is controlled by the â€œProject Formatâ€ popup menu in Xcode’s “Project Info” window. The value 45 corresponds to “Xcode 3.1-compatible”.
objects; This is a list (actually, a record, or hash) of objects in the project. This is the meat of the file format.
rootObject; This identifies the root object, the object that represents the project itself.
Now let’s take a look at the objects in general. There is an object for every file, group, target, build phase, and so on. Each object is identified by a UUID. If they are universally-unique, then any number of projects from any number of original machines can be opened at the same time by a single copy of Xcode without any problems. These UUIDs are 12 bytes—24 hexadecimal digits without any separating hyphens. Each object has a set of properties, one of which, “isa” specifies the class of the object. The other properties are determined by this class.

OBJECT CLASSES

The object classes supported depends upon the objectVersion. Here is a partial list:

PBXBuildFile
PBXFileReference
PBXFrameworksBuildPhase
PBXGroup
PBXNativeTarget
PBXProject
PBXResourcesBuildPhase
PBXSourcesBuildPhase
PBXVariantGroup
XCBuildConfiguration
XCConfigurationList
The most important one here is PBXFileReference; every file referenced by the project (source files, headers, libraries, frameworks, xcconfig files, other projects…)1 is represented by a PBXFileReference.


	089C165DFE840E0CC02AAC07 /* English */ = {
		isa = PBXFileReference; 
		fileEncoding = 4; 
		lastKnownFileType = text.plist.strings; 
		name = English; 
		path = English.lproj/InfoPlist.strings; 
		sourceTree = ""; 
	};
There are two different types of files: input (e.g. source files) and output (e.g. the output application or library). lastKnownFileType is present and set for input files. The list of possible values can be found in Xcode in the file “Get Info” window. Additional values are, of course, possible.


	8D1107320486CEB800E47090 /* MyProject.app */ = {
		isa = PBXFileReference; 
		explicitFileType = wrapper.application; 
		includeInIndex = 0; 
		path = MyProject.app; 
		sourceTree = BUILT_PRODUCTS_DIR; 
	};
Output files always have the explicitFileType key, includeInIndex (typically set to 0, or false, for binaries and packages). Both input and output files have a path and sourceTree specified. Path names are not normally quoted unless necessary (for example, if the pathname includes a semicolon, space, or other special character).

In part 2, I’ll look at PBXVariantGroup, XCBuildConfiguration, and XCConfigurationList.

¶

1 There are exceptions; two of the most well-known are Info.plist files and precompiled header source (.pch) files. These two files are always identified either by absolute pathname, or by a path relative to the .xcodeproj project.

More on the Xcode project format
10 October 2010 by
A 90-SECOND PROJECT PARSER IN RUBY

Last week we looked at the overall format of Xcode project files. Here’s an easy parser written in Ruby; this one will only run on Mac OS X, because it uses Foundation from Ruby:


#!/usr/bin/ruby
# http://danwright.info/blog/xcode-pbxproject-files-2

require 'osx/cocoa'

xcodeproj = "/Users/danwr/Documents/MyProject/MyProject.xcodeproj"

projectpbxproj = "#{xcodeproj}/project.pbxproj"
data = OSX::NSData.dataWithContentsOfFile(projectpbxproj)
plist = OSX::NSPropertyListSerialization.propertyListFromData_mutabilityOption_format_errorDescription(data, 0, nil, nil)

rootObject = plist['rootObject']
objects    = plist['objects']

if ARGV.length == 0
	puts "rootObject = #{objects[rootObject]}"
else
	what = ARGV.shift
	if /^[0-9a-fA-F]{24}$/ =~ what
		puts "object #{what} = #{objects[what]}"
	else
		results = objects.keys.find_all {|key| objects[key]['isa'] == what }
		puts "isa '#{what}': #{results}"
	end
end
If you run this command without any arguments, it will show you the root object (PBXProject). If you run it with a UUID, it will display the corresponding object; if you provide it with an object class (‘isa’), it will display the UUIDs for all matching objects. For purposes of this article, I’ve hard-coded the path to a project; you can edit that to point to your own project, or modify the script to allow a path to be specified as an argument.

Wow, that’s silly-easy. If you want your script to run on another platform—Windows or Linux—you would need another solution (but it isn’t exactly difficult to write a custom parser in a modern scripting language such as Ruby, Python, or even Old Man Perl.

XCCONFIGURATIONLIST

An XCConfigurationList is simply a list of configurations. A configuration refers to a group of settings, and commonly we have at least two: Debug and Release, the former for debugging the project, the latter optimized for customers.


C01FCF4E08A954540054247B = {
    buildConfigurations =     (
        C01FCF4F08A954540054247B,
        C01FCF5008A954540054247B
    );
    defaultConfigurationIsVisible = 0;
    defaultConfigurationName = Release;
    isa = XCConfigurationList;
}
The defaultConfigurationName and defaultConfigurationIsVisible properties indicate which configuration is the default when building with the xcodebuild tool, as well as whether this information should be exposed in the Xcode user interface. The buildConfigurations array contains references to objects of type XCBuildConfiguration.

XCBUILDCONFIGURATION

An XCBuildConfiguration is a collection of build settings, like so:


C01FCF4F08A954540054247B = {
    buildSettings =     {
        ARCHS = "$(ARCHS_STANDARD_32_64_BIT)";
        "GCC_C_LANGUAGE_STANDARD" = gnu99;
        "GCC_OPTIMIZATION_LEVEL" = 0;
        "GCC_WARN_ABOUT_RETURN_TYPE" = YES;
        "GCC_WARN_UNUSED_VARIABLE" = YES;
        "ONLY_ACTIVE_ARCH" = YES;
        PREBINDING = NO;
        SDKROOT = "macosx10.6";
    };
    isa = XCBuildConfiguration;
    name = Debug;
}
The buildSettings property is the heart of an XCBuildConfiguration. Each build setting should look familiar: these are the same names and settings you would use in an .xcconfig file. Of course, buildSettings can be empty, as it often will be when you have an .xcconfig file specified instead.

PBXVARIANTGROUP

A PBXVariantGroup describes a group of files that act like one; this is used to described localized files (strings and xibs).


1DDD58140DA1D0A300B32029 = {
    children =     (
        1DDD58150DA1D0A300B32029
    );
    isa = PBXVariantGroup;
    name = "MainMenu.xib";
    sourceTree = "";
}
This one describes the application’s main xib (describing the menu bar and main window). The name is the name of the file. The children contains a list of localizations; here, there is just one, for the English version. Let’s look at that child:


1DDD58150DA1D0A300B32029 = {
    isa = PBXFileReference;
    lastKnownFileType = "file.xib";
    name = English;
    path = "English.lproj/MainMenu.xib";
    sourceTree = "";
}
The path is the path of the actual .xib file (relative to the encoding group). lastKnownFileType indicates the file type.

NEXT TIME…

Next time, a look at PBXFileReference, PBXBuildFile, and PBXSourcesBuildPhase.

Xcode project object UUIDs


UNIQUE XCODE OBJECT IDS USING RUBY

The “UUIDs” used in project files are shorter than true UUIDs (only 12 bytes/16 characters), and have no punctuation. We can’t just use system UUID services to generate new ones, then. In practice, our UUIDs usually do not need to be universally unique; they must be unique within a project file, and ideally would be unique across all projects built or opened on a given machine. Here, nonetheless, is a quickie ruby class that generates Xcode project UUIDs:


class XcodeUUIDGenerator

    def initialize
        @num = [Time.now.to_i, Process.pid, getMAC]
    end
	
    # Get the ethernet hardware address ("MAC"). This version 
    # works on Mac OS X 10.6 (Snow Leopard); it has not been tested
    # on other versions.

    def getMAC(interface='en0')
        addrMAC = `ifconfig #{interface} ether`.split("\n")[1]
        addrMAC ? addrMAC.strip.split[1].gsub(':','').to_i(16) : 0
    end

    def generate
        @num[0] += 1
        self
    end
	
    def to_s
        "%08X%04X%012X" % @num
    end
end
Usage is simple:

    gen = XcodeUUIDGenerator.new
    id1 = gen.generate.to_s
    id2 = gen.generate.to_s
    id3 = gen.generate.to_s
PBXFILEREFERENCE

A PBXFileReference is used to track every external file referenced by the project: source files, resource files, libraries, generated application files, and so on. A source file might look like this:


 29B97316FDCFA39411CA2CEA /* main.m */ = {
	isa = PBXFileReference; 
	fileEncoding = 4; 
	lastKnownFileType = sourcecode.c.objc; 
	path = main.m; 
	sourceTree = ""; 
 };
The values for lastKnownFileType may be found within Xcode itself, by selecting the file and choosing “Get Info”. A sourceTree of “” corresponds to “Relative to Enclosing Group”. A fileEncoding value of “4” is UTF-8. Here, the path is only a file name, however it can be a (longer) relative path or an absolute path (sourceTree = ““). Relative paths may also be relative to the chosen SDK, the Xcode application (rare), the project file (that is, the .xcodeproj bundle), or the built product.


 1058C7A1FEA54F0111CA2CBB /* Cocoa.framework */ = {
	isa = PBXFileReference; 
	lastKnownFileType = wrapper.framework; 
	name = Cocoa.framework; 
	path = /System/Library/Frameworks/Cocoa.framework; 
	sourceTree = ""; 
 };
Some Xcode templates contain absolute paths to some frameworks (as in the above example), however this is, arguably, “wrong”—SDK files should always use SDK-relative paths (as, indeed, Xcode 3.2.x will do if you add a framework to a project manually):


 2D04AF89126B8A7A00073224 /* AppleScriptObjC.framework */ = {
	isa = PBXFileReference; 
	lastKnownFileType = wrapper.framework; 
	name = AppleScriptObjC.framework; 
	path = System/Library/Frameworks/AppleScriptObjC.framework; 
	sourceTree = SDKROOT; 
 };
Finally, the final output of your target also has a PBXFileReference. It looks slightly different:


 8D1107320486CEB800E47090 /* MyProject.app */ = {
	isa = PBXFileReference; 
	explicitFileType = wrapper.application; 
	includeInIndex = 0; 
	path = MyProject.app; 
	sourceTree = BUILT_PRODUCTS_DIR; 
 };
Instead of lastKnownFileType, it has an explicitFileType; it also has a property includeInIndex, set to 0 (FALSE).

You can add comment to files; these are stored as a comments property on the PBXFileReference.

PBXBUILDFILE

Files that need to be processed in the build (for example compiled, linked, or copied) also have a PBXBuildFile. These are very simple:

 8D11072D0486CEB800E47090 /* main.m in Sources */ = {
	isa = PBXBuildFile; 
	fileRef = 29B97316FDCFA39411CA2CEA /* main.m */; 
	settings = {ATTRIBUTES = (); }; 
};
The fileRef is the id of the PBXFileReference. The settings property is usually omitted entirely. If you specify per-file compiler flags, they will be stored in the COMPILER_FLAGS property of the settings property.

PBXSOURCESBUILDPHASE

Projects commonly have several build phases: compiling, linking, copying resources, copying other files, and perhaps running shell scripts. PBXSourcesBuildPhase describes the compiling phase for a target.

 8D11072C0486CEB800E47090 /* Sources */ = {
    isa = PBXSourcesBuildPhase;
    buildActionMask = 2147483647;
    files = (
      8D11072D0486CEB800E47090 /* main.m in Sources */,
      256AC3DA0F4B6AC300CF3369 /* MyProjectAppDelegate.m in Sources */,
    );
    runOnlyForDeploymentPostprocessing = 0;
 };
The files property contains an array of PBXBuildFile references. buildActionMask is usually 2147483647 (that’s 0x7FFFFFFF in hexadecimal). The runOnlyForDeploymentPostprocessing property is normally 0 (FALSE).