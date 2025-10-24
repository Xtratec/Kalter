using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace kalter
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            // Set up directories
            string homeDir = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            string currentDir = homeDir;
            string installDir = Path.Combine(homeDir, "kalter_programs");

            if (!Directory.Exists(installDir))
                Directory.CreateDirectory(installDir);

            List<string> history = new List<string>();

            Console.WriteLine("Welcome to kalter terminal! Type 'help' for commands.");

            while (true)
            {
                Console.Write($"user@kalter:{currentDir.Replace(homeDir, "~")}$ ");
                string input = Console.ReadLine()?.Trim();
                if (string.IsNullOrEmpty(input)) continue;

                history.Add(input);
                string lowerInput = input.ToLower();

                // Exit terminal
                if (lowerInput == "exit") break;

                // Help
                else if (lowerInput == "help")
                {
                    Console.WriteLine("Commands:");
                    Console.WriteLine("ls, cd, pwd, mkdir, rm, touch, echo, cat, clear, exit, help");
                    Console.WriteLine("wrt <file>        - Open Wrt text editor");
                    Console.WriteLine("install <url>     - Download and install package from server");
                }

                // pwd
                else if (lowerInput == "pwd")
                    Console.WriteLine(currentDir);

                // clear
                else if (lowerInput == "clear")
                    Console.Clear();

                // ls
                else if (lowerInput == "ls")
                {
                    try
                    {
                        string[] dirs = Directory.GetDirectories(currentDir);
                        string[] files = Directory.GetFiles(currentDir);

                        foreach (var d in dirs) Console.WriteLine("[DIR]  " + Path.GetFileName(d));
                        foreach (var f in files) Console.WriteLine("       " + Path.GetFileName(f));
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Error listing directory: " + ex.Message);
                    }
                }

                // cd
                else if (lowerInput.StartsWith("cd "))
                {
                    string path = input.Substring(3).Trim('"');
                    string newPath = Path.IsPathRooted(path) ? path : Path.Combine(currentDir, path);

                    if (Directory.Exists(newPath))
                        currentDir = newPath;
                    else
                        Console.WriteLine($"cd: no such file or directory: {path}");
                }

                // mkdir
                else if (lowerInput.StartsWith("mkdir "))
                {
                    string folderName = input.Substring(6).Trim('"');
                    string fullPath = Path.Combine(currentDir, folderName);

                    try
                    {
                        Directory.CreateDirectory(fullPath);
                        Console.WriteLine($"Folder '{folderName}' created.");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("mkdir error: " + ex.Message);
                    }
                }

                // rm
                else if (lowerInput.StartsWith("rm "))
                {
                    string name = input.Substring(3).Trim('"');
                    string fullPath = Path.Combine(currentDir, name);

                    try
                    {
                        if (Directory.Exists(fullPath))
                            Directory.Delete(fullPath, true);
                        else if (File.Exists(fullPath))
                            File.Delete(fullPath);
                        else
                            Console.WriteLine($"rm: cannot remove '{name}': No such file or directory");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("rm error: " + ex.Message);
                    }
                }

                // touch
                else if (lowerInput.StartsWith("touch "))
                {
                    string fileName = input.Substring(6).Trim('"');
                    string fullPath = Path.Combine(currentDir, fileName);

                    try
                    {
                        if (!File.Exists(fullPath))
                            File.Create(fullPath).Close();
                        else
                            Console.WriteLine($"touch: file '{fileName}' already exists");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("touch error: " + ex.Message);
                    }
                }

                // echo > file
                else if (lowerInput.Contains(">"))
                {
                    string[] parts = input.Split('>', 2);
                    string text = parts[0].Replace("echo", "").Trim('"', ' ');
                    string fileName = parts[1].Trim('"', ' ');
                    string fullPath = Path.Combine(currentDir, fileName);

                    try
                    {
                        File.WriteAllText(fullPath, text);
                        Console.WriteLine($"Wrote to {fileName}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("echo error: " + ex.Message);
                    }
                }

                // cat
                else if (lowerInput.StartsWith("cat "))
                {
                    string fileName = input.Substring(4).Trim('"');
                    string fullPath = Path.Combine(currentDir, fileName);

                    try
                    {
                        if (File.Exists(fullPath))
                        {
                            string content = File.ReadAllText(fullPath);
                            Console.WriteLine(content);
                        }
                        else
                        {
                            Console.WriteLine($"cat: {fileName}: No such file");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("cat error: " + ex.Message);
                    }
                }

                // Wrt text editor
                else if (lowerInput.StartsWith("wrt "))
                {
                    string fileName = input.Substring(4).Trim('"');
                    string fullPath = Path.Combine(currentDir, fileName);
                    WrtEditor(fullPath);
                }

                // Install from server
                else if (lowerInput.StartsWith("install "))
                {
                    string url = input.Substring(8).Trim();
                    await InstallPackage(url, installDir);
                }

                else
                {
                    Console.WriteLine($"Unknown command: {input}");
                }
            }

            Console.WriteLine("Exiting kalter... Bye!");
        }

        static void WrtEditor(string filePath)
        {
            Console.WriteLine("=== Wrt Editor ===");
            Console.WriteLine("Type text. Commands:");
            Console.WriteLine(":w  - save");
            Console.WriteLine(":wq - save and quit");
            Console.WriteLine(":q  - quit without saving");

            List<string> lines = new List<string>();
            if (File.Exists(filePath))
                lines.AddRange(File.ReadAllLines(filePath));

            while (true)
            {
                string line = Console.ReadLine();
                if (line == ":w")
                {
                    File.WriteAllLines(filePath, lines);
                    Console.WriteLine($"Saved '{filePath}'");
                }
                else if (line == ":wq")
                {
                    File.WriteAllLines(filePath, lines);
                    Console.WriteLine($"Saved '{filePath}' and exiting Wrt.");
                    break;
                }
                else if (line == ":q")
                {
                    Console.WriteLine("Exited Wrt without saving.");
                    break;
                }
                else
                {
                    lines.Add(line);
                }
            }
        }

        static async Task InstallPackage(string url, string installDir)
        {
            try
            {
                using HttpClient client = new HttpClient();
                var data = await client.GetByteArrayAsync(url);

                string fileName = Path.GetFileName(new Uri(url).LocalPath);
                if (string.IsNullOrEmpty(fileName))
                    fileName = "package";

                string savePath = Path.Combine(installDir, fileName);
                await File.WriteAllBytesAsync(savePath, data);

                Console.WriteLine($"Installed '{fileName}' in {installDir}");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Install error: " + ex.Message);
            }
        }
    }
}
