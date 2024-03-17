using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Linq;


        var bundleCommand = new Command("bundle", "Bundle code files in the current directory to a single file: ");
        var outputOption = new Option<FileInfo>("--output", "File path and name: ");
        var languagesOption = new Option<string>("--languages", "List of programming languages to include in the bundle: ");
        languagesOption.IsRequired = true;
        var noteOption = new Option<bool>("--note", "Include source code comments in the bundle file: ");
        var sortOption = new Option<string>("--sort", "Sort order for copying code files: ('name'/'type'): ");
        var removeEmptyLinesOption = new Option<bool>("--remove-empty-lines", "Remove empty lines from source code: ");
        var authorOption = new Option<string>("--author", "Name of the file creator: ");

        bundleCommand.AddOption(outputOption);
        bundleCommand.AddOption(languagesOption);
        bundleCommand.AddOption(noteOption);
        bundleCommand.AddOption(sortOption);
        bundleCommand.AddOption(removeEmptyLinesOption);
        bundleCommand.AddOption(authorOption);

        bundleCommand.SetHandler((output, languages, note, sort, removeEmptyLines, author) =>
        {
            try
            {
                var currentDirectory = Directory.GetCurrentDirectory();
                string[] LanguageArr = languages.Split(',');
                var files = Directory.GetFiles(currentDirectory, "*", SearchOption.AllDirectories)
                    .Where(file => IsCodeFile(file) && !IsExcludedFolder(file, currentDirectory))
                    .ToArray();

                //var selectedFiles = files
                //  .Where((file => LanguageArr.Any(lang => file.EndsWith($".{lang}", StringComparison.OrdinalIgnoreCase))))
                //  .OrderBy(file => (sort == "type") ? Path.GetExtension(file) : Path.GetFileName(file))
                //  .ToArray();
                var selectedFiles = (LanguageArr.Contains("all"))
          ? files.OrderBy(file => (sort == "type") ? Path.GetExtension(file) : Path.GetFileName(file)).ToArray()
          : files
              .Where(file => LanguageArr.Any(lang => file.EndsWith($".{lang}", StringComparison.OrdinalIgnoreCase)))
              .OrderBy(file => (sort == "type") ? Path.GetExtension(file) : Path.GetFileName(file))
              .ToArray();



                using (StreamWriter outputFile = new StreamWriter(output.FullName))
                {
                    if (note)
                    {
                        outputFile.WriteLine("// Bundle created with the following files:");
                        foreach (var file in selectedFiles)
                        {
                            string relativePath = GetRelativePath(file, currentDirectory);
                            outputFile.WriteLine($"// - {Path.GetFileName(file)} ({relativePath})");
                        }
                        outputFile.WriteLine();
                    }

                    if (!string.IsNullOrEmpty(author))
                    {
                        outputFile.WriteLine($"// Author: {author}");
                    }

                    foreach (var file in selectedFiles)
                    {
                        string fileContent = File.ReadAllText(file);
                        if (removeEmptyLines)
                        {
                            fileContent = RemoveEmptyLines(fileContent);
                        }

                        outputFile.WriteLine($"// Contents of {GetRelativePath(file, currentDirectory)}\n{fileContent}\n\n");
                    }
                }

                Console.WriteLine($"Packaging successful! :) . Output saved to: {output.FullName}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
            }
        }, outputOption, languagesOption, noteOption, sortOption, removeEmptyLinesOption, authorOption);

var rootCommand = new RootCommand("Root command for File Bundle CLI");
rootCommand.AddCommand(bundleCommand);


var createRspCommand = new Command("create-rsp", "Create response file for bundle command options");
createRspCommand.SetHandler(() =>
{

    try
    {


        // Prompt the user to enter values for each option
        var outputPath = PromptForValue("Enter the output file path and name");
        var selectedLanguages = PromptForValue("Enter the list of programming languages (comma-separated)");
        var includeNote = PromptForBoolean("Include source code comments in the bundle file (y/n)");
        var sortOrder = PromptForValue("Enter the sort order for copying code files ('name' or 'type')");
        var removeEmpty = PromptForBoolean("Remove empty lines from source code (y/n)");
        var authorName = PromptForValue("Enter the name of the file creator");

        // Save the responses to a response file
        using (StreamWriter rspFile = new StreamWriter("bundle.rsp"))
        {
            rspFile.WriteLine($"--output {outputPath}");
            rspFile.WriteLine($"--languages {selectedLanguages}");
            rspFile.WriteLine($"--note {includeNote}");
            rspFile.WriteLine($"--sort {sortOrder}");
            rspFile.WriteLine($"--remove-empty-lines {removeEmpty}");
            rspFile.WriteLine($"--author {authorName}");
        }

        Console.WriteLine($"Response file created successfully: bundle");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"An error occurred: {ex.Message}");
    }
});

rootCommand.AddCommand(createRspCommand);
rootCommand.InvokeAsync(args).Wait();

//Helper method to prompt user for a value
static string PromptForValue(string prompt)
{
    Console.Write(prompt);
    var userInput = Console.ReadLine();
    return userInput.Trim();
}

// Helper method to prompt user for a boolean value
static bool PromptForBoolean(string prompt)
{
    Console.Write(prompt);
    var userInput = Console.ReadLine()?.Trim().ToLower();
    return  userInput == "y";
}
    

    static bool IsCodeFile(string filePath)
    {
        string extension = Path.GetExtension(filePath);
        // Define the code file extensions you want to include
        string[] codeFileExtensions = { ".cs", ".java", ".cpp", ".py", ".js", ".html", ".css" };
        return codeFileExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase);
    }

    static bool IsExcludedFolder(string filePath, string currentDirectory)
    {
        string[] excludedFolders ={ "bin", "obj", "debug", "release" };
        var folder = Path.GetDirectoryName(GetRelativePath(filePath, currentDirectory));
        return excludedFolders.Any(excludedFolder => folder.Contains(excludedFolder, StringComparison.OrdinalIgnoreCase));
    }

    static string GetRelativePath(string filePath, string referencePath)
    {
        var fileUri = new Uri(filePath);
        var referenceUri = new Uri(referencePath + Path.DirectorySeparatorChar);
        return Uri.UnescapeDataString(referenceUri.MakeRelativeUri(fileUri).ToString().Replace('/', Path.DirectorySeparatorChar));
    }

    static string RemoveEmptyLines(string content)
    {
        return string.Join("\n", content.Split('\n').Where(line => !string.IsNullOrWhiteSpace(line)));
    }



