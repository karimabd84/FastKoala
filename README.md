# Fast Koala
Enables build-time config transforms for various project types including ASP.NET 4.6-or-below web apps (but not web sites, and Azure is not directly supported at this time), with future plans to also ease config name management and add MSBuild scripts (Imports directives to custom .targets files) to a project.

###"Build-time" means F5###

All references to "build-time" refer to F6 (Build) or F5 ([Build and] Debug). This means that you can finally test web apps with different configuration transformations applied *without* publishing, you can simply select the configuration and hit F5.

###Inline Build-Time Transformations###
This tool enables build-time transformations for ASP.NET 4.6-or-below web apps (not websites), including ASP.NET MVC 5.

    Web.config
    Web.Debug.config
    Web.Release.config
    
.. become ..

    App_Config\Web.Base.config
    App_Config\Web.Debug.config
    App_Config\Web.Release.config
  
and Web.config at project root becomes transient (and should never be added to source control).

<sub><sup>(This was a feature I and many others always wanted from Slow Cheetah.)</sup></sub>

###Bin-Targeted Build-Time Transformations###
This tool also supports enabling build-time transformations for class library projects (which can have config files) and for Windows apps (other than ClickOnce apps -- support for ClickOnce is coming but will use Inline Transformations) that need to transform out to the bin\Debug or bin\Release directory as AssemblyName.exe.config.

###Where to get it###
You can download the official current release from the gallery here:
https://visualstudiogallery.msdn.microsoft.com/7bc82ddf-e51b-4bb4-942f-d76526a922a0

###How to use###
In all cases, to use, right-click on the project node or the [Web|App].config in Solution Explorer and choose "Enable build-time transformations". 

If a transform file (i.e. Web.Debug.config) has been deleted or removed, right-click on the base config file and choose "Add missing transforms".

####Setting the config directory####

For web apps, which use inline transformations in a nested folder, the default folder name is "App_Config", but you can choose any name you like when prompted--you must keep that folder name unless you edit the project file--and you can use backslashes in the folder name to deeply nest the config files, i.e. "cfg\server". To leave the base config and its transforms in the project root, use simply a dot ("."). You can also share configs further up in the solution using "..", i.e. "..\CommonConfigs\Web".

###Limitations###

Web sites are not supported and will never be supported.

ASP.NET 5 is not supported; it might not ever be supported.

### How it works ###

This Visual Studio extension will modify your project by injecting a custom MSBuild target that invokes the TransformXml task with the custom config paths as parameters. It does not use NuGet and it does not import an external .targets file in order to support build-time transformations--at least, not at this time, these behaviors might be added down the road but there are several reasons to avoid any of that.

The complete and simple explanation of the core method of how this is accomplished is laid out in the following very useful resource from EdCharbeneau which upon reading it started this whole effort: https://gist.github.com/EdCharbeneau/9135216

### Development notes ###

This project *does not* use automated unit tests in source code. :(
