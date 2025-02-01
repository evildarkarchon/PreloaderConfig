using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using PreloaderConfigurator.Models;

namespace PreloaderConfigurator.ViewModels;

public class AsyncCommand : ICommand
{
    private readonly Func<object?, Task> execute;
    private readonly Func<object?, bool>? canExecute;
    private bool isExecuting;
    private readonly Action<Exception>? onError;

    /// Represents a command that supports asynchronous execution.
    /// Provides mechanisms for determining execution eligibility, handling exceptions, and preventing overlapping executions.
    public AsyncCommand(
        Func<object?, Task> execute,
        Func<object?, bool>? canExecute = null,
        Action<Exception>? onError = null)
    {
        this.execute = execute;
        this.canExecute = canExecute;
        this.onError = onError;
    }

    public event EventHandler? CanExecuteChanged;

    /// Determines whether the command can execute in its current state
    public bool CanExecute(object? parameter)
    {
        return !isExecuting && (canExecute?.Invoke(parameter) ?? true);
    }

    /// Executes the associated action of the command synchronously.
    /// Ensures that the command does not execute if it is already running and
    /// appropriately updates the command's execution state.
    /// Calls the asynchronous execution method and triggers the necessary changes to the execution state.
    /// <param name="parameter">
    /// The parameter passed to the synchronous execution of the command. Can be null.
    /// </param>
    public void Execute(object? parameter)
    {
        if (isExecuting) return;

        isExecuting = true;
        RaiseCanExecuteChanged();

        ExecuteAsync(parameter);
    }

    /// Executes the provided asynchronous action associated with the command.
    /// Ensures proper handling of exceptions and updates the command's execution state.
    /// Upon execution completion, the UI is notified of changes in the command's ability to execute.
    /// Calls the associated asynchronous function, manages the execution state, and handles errors as necessary.
    /// <param name="parameter">
    /// The parameter passed to the asynchronous action. Can be null.
    /// </param>
    private async void ExecuteAsync(object? parameter)
    {
        try
        {
            await execute(parameter);
        }
        catch (Exception ex)
        {
            onError?.Invoke(ex);
        }
        finally
        {
            isExecuting = false;
            RaiseCanExecuteChanged();
        }
    }

    /// Notifies the UI of changes to the command's execution state.
    /// Invokes the CanExecuteChanged event to indicate that the ability
    /// to execute the command may have changed.
    public void RaiseCanExecuteChanged()
    {
        if (Dispatcher.UIThread.CheckAccess())
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
        else
        {
            Dispatcher.UIThread.InvokeAsync(() => CanExecuteChanged?.Invoke(this, EventArgs.Empty));
        }
    }
}

public class MainWindowViewModel : ViewModelBase
{
    private PreloaderConfig config;
    private bool showAdvancedOptions;
    private string? currentFilePath;

    /// Represents the main view model for the application’s main window.
    /// Provides commands for opening, saving, and saving configurations under a new name, as well as managing the application's state and configuration data.
    public MainWindowViewModel()
    {
        config = new PreloaderConfig();
        showAdvancedOptions = false;

        OpenCommand = new AsyncCommand(
            async param =>
            {
                if (param is Window window)
                {
                    await OpenConfigurationFile(window);
                }
            },
            null,
            ex => Console.WriteLine($"Error in OpenCommand: {ex}")
        );

        SaveCommand = new AsyncCommand(
            async _ => await SaveConfiguration(),
            _ => !string.IsNullOrEmpty(CurrentFilePath),
            ex => Console.WriteLine($"Error in SaveCommand: {ex}")
        );

        SaveAsCommand = new AsyncCommand(
            async param =>
            {
                if (param is Window window)
                {
                    await SaveConfigurationAs(window);
                }
            },
            null,
            ex => Console.WriteLine($"Error in SaveAsCommand: {ex}")
        );
    }

    public PreloaderConfig Config
    {
        get => config;
        private set
        {
            if (SetField(ref config, value))
            {
                (SaveCommand as AsyncCommand)?.RaiseCanExecuteChanged();
            }
        }
    }

    public bool ShowAdvancedOptions
    {
        get => showAdvancedOptions;
        set => SetField(ref showAdvancedOptions, value);
    }

    public string? CurrentFilePath
    {
        get => currentFilePath;
        set
        {
            if (SetField(ref currentFilePath, value))
            {
                (SaveCommand as AsyncCommand)?.RaiseCanExecuteChanged();
            }
        }
    }

    public ICommand OpenCommand { get; }
    public ICommand SaveCommand { get; }
    public ICommand SaveAsCommand { get; }

    public List<string> LoadMethods => new()
    {
        "ImportAddressHook",
        "OnThreadAttach",
        "OnProcessAttach"
    };

    /// Opens a configuration file selected by the user through a file picker dialog.
    /// The selected file is then read, and its contents are used to populate the application's configuration.
    /// If no file is selected, the method does nothing.
    /// <param name="window">The window from which the file picker dialog will be displayed.</param>
    /// <returns>A Task representing the asynchronous file opening and configuration loading operation.</returns>
    private async Task OpenConfigurationFile(Window window)
    {
        var options = new FilePickerOpenOptions
        {
            Title = "Open XML File",
            AllowMultiple = false,
            FileTypeFilter = new[]
            {
                new FilePickerFileType("XML Files")
                {
                    Patterns = new[] { "*.xml" }
                }
            }
        };

        var result = await window.StorageProvider.OpenFilePickerAsync(options);
        if (result.Count >= 1)
        {
            var file = result[0];
            var path = file.Path.LocalPath;
            var xmlContent = await File.ReadAllTextAsync(path);

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                CurrentFilePath = path;
                Config = PreloaderConfig.FromXml(xmlContent);
            });
        }
    }

    /// Saves the current configuration to the file specified by the CurrentFilePath property.
    /// If the CurrentFilePath is null or empty, the method does nothing.
    /// <returns>A Task representing the asynchronous save operation.</returns>
    private async Task SaveConfiguration()
    {
        if (string.IsNullOrEmpty(CurrentFilePath)) return;

        var xmlContent = Config.SaveToXml();
        await File.WriteAllTextAsync(CurrentFilePath, xmlContent);
    }

    /// Saves the current configuration as a new file, allowing the user to select the file path and name through a save file dialog.
    /// Updates the current file path and saves the configuration to the specified location.
    /// <param name="window">The window instance from which the save file dialog will be displayed.</param>
    /// <returns>A Task representing the asynchronous save operation.</returns>
    private async Task SaveConfigurationAs(Window window)
    {
        var options = new FilePickerSaveOptions
        {
            Title = "Save XML File As",
            DefaultExtension = ".xml",
            FileTypeChoices = new[]
            {
                new FilePickerFileType("XML Files")
                {
                    Patterns = new[] { "*.xml" }
                }
            }
        };

        var file = await window.StorageProvider.SaveFilePickerAsync(options);
        if (file != null)
        {
            var path = file.Path.LocalPath;
            await Dispatcher.UIThread.InvokeAsync(() => CurrentFilePath = path);
            await SaveConfiguration();
        }
    }
}