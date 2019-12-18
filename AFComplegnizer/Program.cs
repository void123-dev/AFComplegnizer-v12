using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using FC_Recognize;
using System.IO;

namespace AFComplegnizer
{
    class Program
    {
/*
        static List<string> _listImages = new List<string>();
*/
        static string _errorText = "";
        static bool _useTemplatesLocal = true;
        static bool _useInProcEngine = true;

        static int _modePreprocessingImage = 1;

        static void Main(string[] args)
        {
            Console.WriteLine("==== Параметры работы программы ==== ");
            Console.WriteLine("Использовать Inproc режим: {0}", _useInProcEngine);
            Console.WriteLine("Использовать локальные шаблоны распознавания: {0}", _useTemplatesLocal);
            Console.WriteLine("Режим предобработки изображений: {0}", _modePreprocessingImage);

            
            if (args.Length < 4)
            {
                _errorText = "Неверное количество аргументов - " + args.Length + ", должно быть 4 аргумента";
                Console.WriteLine(_errorText);
                Console.ReadKey();
                Environment.Exit(0);
            }

            string pathFolderWorkArgs = args[1];
            string pathFolderTmplts = args[2];

            // выбор папки с шаблонами для загрузки в движок
            if (_useTemplatesLocal)
            {
                pathFolderTmplts = Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly()?.Location);
            }
            else
            {
                // проверка переданной папки,
                // если она пустая - используем встроенную
                if (!Directory.Exists(pathFolderTmplts + "\\Templates"))
                {
                    Console.WriteLine("Отсутствует папка с шаблонами по пути: " + pathFolderTmplts);
                    pathFolderTmplts = Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly()?.Location);
                    Console.WriteLine("Будет использована папка: " + pathFolderTmplts);
                }
            }

            string pathFileRead = args[3];
            if (String.IsNullOrEmpty(pathFileRead))
            {
                _errorText = "4й параметр (путь к txt файлу) для приложения пустой";
                Console.WriteLine(_errorText);
                Console.ReadKey();
                Environment.Exit(0);
            }

            if (!File.Exists(pathFileRead))
            {
                _errorText = "txt файл с путями к изображениям не существует по пути: " + pathFileRead;
                Console.WriteLine(_errorText);
                Console.ReadKey();
                Environment.Exit(0);
            }
            Console.WriteLine("Получены изображения:");
            var listPathsImages = new List<string>();
            const int bufferSize = 4096;
            using (var fileStream = File.OpenRead(pathFileRead))
            using (var streamReader = new StreamReader(fileStream, Encoding.UTF8, true, bufferSize))
            {
                string line;
                while ((line = streamReader.ReadLine()) != null)
                {
                    listPathsImages.Add(line);
                    Console.WriteLine(line);
                }
                
            }
            File.Delete(pathFileRead);
            var arrayImages = listPathsImages.ToArray();

            var methodStart = args[0];
            Console.WriteLine("Начинается распознавание...");
            switch (methodStart)
            {
                case "FCProcessor":
                {
                    // проверка наличия файлов шаблонов по указанному пути
                    if (Directory.EnumerateFileSystemEntries(pathFolderTmplts + "\\Templates", "*.fcdot").ToList().Count == 0)
                    {
                        Console.WriteLine("Отсутствует файлы с шаблонами по пути: " + pathFolderTmplts + "\\Templates");
                        pathFolderTmplts = Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly()?.Location);
                        Console.WriteLine("Будет использована папка: " + pathFolderTmplts);
                    }              

                    RecognizeWithFCProcessor(pathFolderWorkArgs, pathFolderTmplts, arrayImages, _useInProcEngine);
                    break;
                }
                case "FCProject":
                {
                    if (Directory.EnumerateFileSystemEntries(pathFolderTmplts + "\\Templates\\DocScan1CProject", "*.fcproj").ToList().Count == 0)
                    {
                        Console.WriteLine("Отсутствует файлы с шаблонами по пути: " + pathFolderTmplts + "\\Templates");
                        pathFolderTmplts = Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly()?.Location);
                        Console.WriteLine("Будет использована папка: " + pathFolderTmplts);
                    }


                    RecognizeWithFlexiProject(pathFolderWorkArgs, pathFolderTmplts, arrayImages);
                    break;
                }
                default:
                    _errorText = "Ошибка переданных аргументов, некорректен первый аргумент";
                    _errorText += Environment.NewLine + "Значение должно быть: FCProcessor или FCProject";
                    Console.WriteLine(_errorText);
                    Console.ReadKey();
                    Environment.Exit(0);
                    break;
            }
            Console.WriteLine("Распознавание завершено!");
        }

        private static void RecognizeWithFCProcessor(string pathFolderWork, string pathFlderTmplates, string[] manyImages, bool loadInproc = true)
        {
            var outProcRecognizing = new DocScan();

            try
            {
                if (loadInproc)
                   outProcRecognizing.SetInprocLoading();

                if (!outProcRecognizing.LoadEngine())
                    PrintConsole(outProcRecognizing.GetErrorText());

                // отображение версий шаблонов
                outProcRecognizing.SetTemplatesDirectory(pathFlderTmplates);

                // установить режим предобработки изображений
                outProcRecognizing.SetImagePreproccesingMode(_modePreprocessingImage);

                Console.WriteLine("Версия компоненты распознавания:");
                Console.WriteLine(outProcRecognizing.GetComponentVersion());

                Console.WriteLine("Версии шаблонов распознавания: ");
                string nameTmplte = outProcRecognizing.GetTemplateVersion();
                Console.WriteLine(nameTmplte);
                while (!String.IsNullOrEmpty(nameTmplte))
                {
                    nameTmplte = outProcRecognizing.GetTemplateVersion();
                    Console.WriteLine(nameTmplte);
                }

                foreach (string image in manyImages)
                {
                    outProcRecognizing.AddImage(image);
                }
                try
                {
                    Console.WriteLine("Идет распознавание...");
                    outProcRecognizing.RecognizeFiles(pathFolderWork, pathFlderTmplates);
                } catch (Exception ex)
                {
                    string tt = ex.Message + Environment.NewLine + ex.StackTrace + Environment.NewLine + outProcRecognizing.GetErrorText();
                    PrintConsole(tt);
                }

                outProcRecognizing.UnloadEngine();
            }
            catch (Exception ex)
            {
                _errorText = "Сбой распознавания: " + ex.Message;
                _errorText += Environment.NewLine + outProcRecognizing.GetErrorText();
                _errorText += Environment.NewLine + ex.StackTrace;
                _errorText += Environment.NewLine + "Images:" + String.Join(" ", manyImages);
                PrintConsole(_errorText);
            }
        }

        private static void RecognizeWithFlexiProject(string pathFolderWork, string pathFlderTmplates, string[] manyImages, bool loadInproc = true)
        {
            DocScan1CCommon.DocScan myProject = new DocScan1CCommon.DocScan(pathFlderTmplates);
            myProject.LoadEngine(loadInproc);
            var result = myProject.RecognizeFiles(manyImages, pathFolderWork, true);
            myProject.UnloadEngine();            
        }

        public static bool RunExternalExe(string pathExe, string pathFile, bool waitForExit = false)
        {
            try
            {
                string fileName = Path.GetFileName(pathFile);
                string pathDirectory = Directory.GetParent(pathFile).FullName;

                var p = new Process
                {
                    StartInfo =
                    {
                        FileName = pathExe,
                        Arguments = "\"" + pathDirectory + "\"",
                        RedirectStandardInput = true,
                        RedirectStandardOutput = false,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };

                // параметры для скрытого запуска
                //p.StartInfo.WindowStyle = ProcessWindowStyle.Maximized;

                if (!string.IsNullOrEmpty(fileName))
                    p.StartInfo.Arguments += " \"" + fileName + "\"";

                p.Start();

                if (waitForExit)
                    p.WaitForExit();

                return true;
            }
            catch (Exception ex)
            {
                _errorText = "Ошибка запуска внешнего exe: " + ex.Message + Environment.NewLine + ex.StackTrace;
                Console.WriteLine(_errorText);
                Console.ReadKey();
                Environment.Exit(0);
                return false;
            }
        }

        public static string Base64Encode(string plainText)
        {
            var plainTextBytes = Encoding.UTF8.GetBytes(plainText);
            return System.Convert.ToBase64String(plainTextBytes);
        }

        public static string Base64Decode(string base64EncodedData)
        {
            var result = "";
            try
            {
                var base64EncodedBytes = System.Convert.FromBase64String(base64EncodedData);
                result = System.Text.Encoding.UTF8.GetString(base64EncodedBytes);

            } catch (Exception ex)
            {
                _errorText = "Не удалось преобразовать base64 строку | " + base64EncodedData;
                _errorText += Environment.NewLine + ex.Message;
                throw new Exception(_errorText);
            }

            return result;
        }

        private static void PrintConsole(string text)
        {
            Console.WriteLine(text);
            Console.ReadKey();
            Environment.Exit(0);
        }
    }
}
