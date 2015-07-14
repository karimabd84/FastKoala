# FastKoala
Enables build-time config transforms for various project types including web apps

Current status: Initial commit performs basic functionality for empty web sites that need build-time transformations.

  Web.config
  Web.Debug.config
  Web.Release.config
    
.. become ..

  App_Config\Web.Base.config
  App_Config\Web.Debug.config
  App_Config\Web.Release.config
  
and Web.config at project root becomes transient (and should never be added to source control).

Initial commit also supports basic class libraries (which can have config files) and Windows apps (other than ClickOnce apps) that need to transform out to the bin\Debug or bin\Release directory as AssemblyName.exe.config.

In all cases, to use, right-click on the project node in Solution Explorer and choose "Enable build-time transformations"
