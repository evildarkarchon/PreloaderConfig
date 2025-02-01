namespace PreloaderConfigurator.Models;

/// <summary>
/// Provides constant strings for comments and summaries used in the preloader configuration.
/// These strings represent detailed explanations for various preloader settings and their behaviors.
/// </summary>
public static class PreloaderDocs
{
    public const string OriginalLibraryComment = @"
If you have a mod that uses the same DLL as the preloader, you can rename the DLL from your mod and set
its new name here. Empty by default. For example, if the preloader uses 'IpHlpAPI.dll' and your mod uses
the same DLL, rename the DLL from your mod to something else like 'IpHlpAPI MyMod.dll'";

    public const string LoadMethodComment = "Load method for xSE plugins, 'ImportAddressHook' by default. Don't change unless required.";

    public const string ImportAddressHookComment = @"
Sets an import table hook for the specified function inside DLL loaded by the host process.
When the hook is called, it'll load plugins and then it'll get back to its usual operations.

Uses Detours library by Nukem: https://github.com/Nukem9/detours

Remarks:
  Fallout 4:
    LibraryName: MSVCR110.dll
    FunctionName: _initterm_e
    The function *must* have a signature compatible with 'void*(__cdecl*)(void*, void*)'.
  
  Skyrim (Legendary Edition):
    LibraryName: kernel32.dll
    FunctionName: GetCommandLineA
    The function *must* have a signature compatible with 'void*(__stdcall*)()'.

  Skyrim (Special and Anniversary Edition):
    LibraryName: api-ms-win-crt-runtime-l1-1-0.dll
    FunctionName: _initterm_e
    The function *must* have a signature compatible with 'void*(__cdecl*)(void*, void*)'.";

    public const string OnThreadAttachComment = @"
Same as 'OnProcessAttach' above, but loads plugins after certain number of threads have been created
by the host process (inside 'DLL_THREAD_ATTACH' notification). This methods has all the disadvantages
of the previous one but it can be triggered too late.

Remarks:
  The method mainly designed for Mod Organizer 2 (MO2) to give some time to its virtual file system to
  initialize itself, otherwise there will be no plugins to preload if they're installed as MO2 virtual
  mods.";

    public const string ThreadNumberComment = @"
Specifies a thread number which will trigger the loading. That is, when the value is '2',
the second attached thread will trigger the loading process. Negative numbers are not allowed.";

    public const string OnProcessAttachComment = @"
Loads plugins inside 'DLLMain' of the preloader DLL when it receives 'DLL_PROCESS_ATTACH' notification.
In other words, right after the host process starts. Executing certain kinds of code inside 'DLLMain' is
risky (see: https://docs.microsoft.com/ru-ru/windows/win32/dlls/dynamic-link-library-best-practices#general-best-practices)
so this method may fail in some cases.

Remarks:
  The preloader calls 'DisableThreadLibraryCalls' when it's done interfacing with 'DLLMain' thread notifications.";

    public const string InstallExceptionHandlerComment = 
        "Usually vectored exception handler is installed right before plugins loading and removed after it's done.";

    public const string KeepExceptionHandlerComment = 
        "Allows you to keep the exception handler if you need more information in case the host process crashes.";

    public const string LoadDelayComment = @"
Sets the amount of time the preloader will pause the loading thread, in milliseconds. 0 means no delay.
Don't change unless you need some time to attach debugger before loading starts, for example.";

    public const string HookDelayComment = 
        "HookDelay works only for 'ImportAddressHook' methods and additionally waits before hooking the required function.";

    public const string ProcessesComment = @"
This block defines a list of processes which are allowed to preload plugins. Only processes in this list
with the attribute 'Allow' set to 'true' will be allowed to preload. Name comparison is *not* case-sensitive.";

    public const string AllowComment = 
        "Only processes in this list with 'Allow' set to 'true' will be allowed to preload.";
}