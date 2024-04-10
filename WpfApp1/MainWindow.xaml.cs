using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;

namespace FileSearchApp
{
    public partial class MainWindow : Window
    {
        private bool isSearching = false;
        private TreeViewItem rootNode;

        public MainWindow()
        {
            InitializeComponent();
            rootNode = new TreeViewItem();
        }

        private async void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            if (!isSearching)
            {
                string startDirectory = startDirectoryTextBox.Text;
                string fileNamePattern = fileNamePatternTextBox.Text;

                if (Directory.Exists(startDirectory))
                {
                    await Task.Run(() => StartSearch(startDirectory, fileNamePattern));
                }
                else
                {
                    MessageBox.Show("Указанная директория не существует или недоступна.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                isSearching = false;
                statusTextBlock.Text = "Поиск остановлен";
            }
        }

        private void StartSearch(string startDirectory, string fileNamePattern)
        {
            isSearching = true;
            Dispatcher.Invoke(() =>
            {
                resultsTreeView.Items.Clear();
                rootNode = CreateDirectoryNode(startDirectory);
                resultsTreeView.Items.Add(rootNode);
            });

            try
            {
                int allFiles = 0;
                int totalFiles = 0;
                int totalDirectories = 0;
                Stopwatch timer = new Stopwatch();

                timer.Start();
                ProcessDirectory(startDirectory, fileNamePattern, rootNode, ref totalFiles, ref totalDirectories, ref allFiles);
                timer.Stop();

                Dispatcher.Invoke(() =>
                {
                    statusTextBlock.Text = $"Найдено файлов: {totalFiles}, Найдено директорий: {totalDirectories}, Заняло времени: {timer.ElapsedMilliseconds}ms\nВсего файлов: {allFiles}";
                });

                isSearching = false;
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() =>
                {
                    MessageBox.Show($"Произошла ошибка во время поиска файлов: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    statusTextBlock.Text = "Ошибка поиска файлов";
                });
            }
        }

        private void ProcessDirectory(string directory, string fileNamePattern, TreeViewItem parentNode, ref int totalFiles, ref int totalDirectories, ref int allFiles)
        {
            try
            {
                string[] subDirectories = Directory.GetDirectories(directory);

                foreach (string subDirectory in subDirectories)
                {
                    totalDirectories++;
                    Dispatcher.Invoke(() =>
                    {
                        var subDirectoryNode = CreateDirectoryNode(subDirectory);
                        parentNode.Items.Add(subDirectoryNode);
                    });
                    ProcessDirectory(subDirectory, fileNamePattern, (TreeViewItem)parentNode.Items[parentNode.Items.Count - 1], ref totalFiles, ref totalDirectories, ref allFiles);
                }

                foreach (string file in Directory.GetFiles(directory))
                {
                    if (!isSearching)
                        break;

                    string fileName = Path.GetFileName(file);
                    if (Regex.IsMatch(fileName, fileNamePattern))
                    {
                        Dispatcher.Invoke(() =>
                        {
                            var fileNode = CreateFileNode(fileName);
                            parentNode.Items.Add(fileNode);
                        });
                        totalFiles++;
                    }
                    allFiles++;
                }
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() =>
                {
                    Console.WriteLine($"Ошибка обработки директории: {ex.Message}");
                });
            }
        }

        private TreeViewItem CreateDirectoryNode(string directory)
        {
            var node = new TreeViewItem();
            node.Header = directory;
            node.Tag = directory;
            node.Items.Add("");
            return node;
        }

        private TreeViewItem CreateFileNode(string fileName)
        {
            var node = new TreeViewItem();
            node.Header = fileName;
            node.Tag = fileName;
            return node;
        }

        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            config.AppSettings.Settings["StartDirectory"].Value = startDirectoryTextBox.Text;
            config.AppSettings.Settings["FileNamePattern"].Value = fileNamePatternTextBox.Text;
            config.Save(ConfigurationSaveMode.Modified);
        }

        private void OnStartUp(object sender, EventArgs e)
        {
            Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            startDirectoryTextBox.Text = config.AppSettings.Settings["StartDirectory"].Value;
            fileNamePatternTextBox.Text = config.AppSettings.Settings["FileNamePattern"].Value;
        }
    }
}