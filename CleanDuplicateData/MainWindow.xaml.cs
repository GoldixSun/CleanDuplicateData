using Microsoft.Win32;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;

namespace CleanDuplicateData
{
    public partial class MainWindow : Window
    {
        private Log _log;

        public MainWindow()
        {
            InitializeComponent();

            string appFileName = Path.GetFileNameWithoutExtension(AppDomain.CurrentDomain.FriendlyName);
            string logFileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"{appFileName}.log");
            CountLoadRows = 0;
            CountDeleteRowsDuplicate = 0;

            _log = new Log(logFileName);

            _log.Write(MessageCreate($"Запуск программы"));
        }

        #region Properties

        public string FileNameLoad
        {
            get { return (string)GetValue(FileNameLoadProperty); }
            set { SetValue(FileNameLoadProperty, value); }
        }

        public static readonly DependencyProperty FileNameLoadProperty = DependencyProperty.Register("FileNameLoad", typeof(string), typeof(MainWindow));

        public int CountLoadRows
        {
            get { return (int)GetValue(CountLoadRowsProperty); }
            set { SetValue(CountLoadRowsProperty, value); }
        }

        public static readonly DependencyProperty CountLoadRowsProperty = DependencyProperty.Register("CountLoadRows", typeof(int), typeof(MainWindow));

        public int CountDeleteRowsDuplicate
        {
            get { return (int)GetValue(CountDeleteRowsDuplicateProperty); }
            set { SetValue(CountDeleteRowsDuplicateProperty, value); }
        }

        public static readonly DependencyProperty CountDeleteRowsDuplicateProperty = DependencyProperty.Register("CountDeleteRowsDuplicate", typeof(int), typeof(MainWindow));

        #endregion


        #region Methods

        private void ButtonOpen_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.InitialDirectory = AppDomain.CurrentDomain.BaseDirectory;

            if (openFileDialog.ShowDialog() == true)
            {
                FileNameLoad = openFileDialog.FileName;
            }
        }

        private void ButtonRun_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                int countLoadRows = 0;
                int countDeleteRowsDuplicate = 0;
                Run(FileNameLoad, out countLoadRows, out countDeleteRowsDuplicate);

                CountLoadRows = countLoadRows;
                CountDeleteRowsDuplicate = countDeleteRowsDuplicate;
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message, "", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void MainWindowView_Closed(object sender, EventArgs e)
        {
            _log.Write(MessageCreate($"Выход из программы"));
            _log.Dispose();
        }

        private void Run(string pathLoadData, out int countDataRows, out int countDuplicateDataRows)
        {
            _log.Write(MessageCreate($"Загрузка данных из файла: {pathLoadData}"));

            if (!File.Exists(pathLoadData))
            {
                _log.Write(MessageCreate($"Ошибка загрузки данных: Файл не найден!"));
                throw new FileNotFoundException($"Файл не найден!\r{pathLoadData}");
            }

            try
            {
                using (StreamReader reader = new StreamReader(pathLoadData))
                using (JsonTextReader readerJSON = new JsonTextReader(reader))
                {
                    var deviceDataRows = new JsonSerializer().Deserialize<List<DeviceData>>(readerJSON);

                    _log.Write(MessageCreate($"Выполняется операция удаления дубликатов..."));

                    var result = from deviceDataRow in deviceDataRows
                                 orderby deviceDataRow.rec_id
                                 group deviceDataRow by new { deviceDataRow.rec_id, deviceDataRow.timestamp } into grResult
                                 select new { item = grResult.FirstOrDefault(), count = grResult.Count() };

                    countDataRows = deviceDataRows.Count();
                    countDuplicateDataRows = result.Sum(x => x.count - 1);

                    _log.Write(MessageCreate($"Кол-во записей загруженных данных: {countDataRows}"));
                    _log.Write(MessageCreate($"Кол-во удаленных дубликатов: {countDuplicateDataRows}"));

                    SaveFileDialog saveFileDialog = new SaveFileDialog();
                    saveFileDialog.DefaultExt = "json";
                    saveFileDialog.Filter = "Все|*.*|Файлы JSON|*.json";
                    saveFileDialog.FileName = $"{System.IO.Path.GetFileNameWithoutExtension(FileNameLoad)}_result";
                    if (saveFileDialog.ShowDialog() == true)
                    {
                        SaveResult(result, saveFileDialog.FileName);
                    }
                }
            }
            catch (Exception e)
            {
                string msg = $"Ошибка выполнения операции удаления дубликатов: {e.Message}";
                _log.Write(MessageCreate(msg));
                throw new Exception(msg);
            }
        }

        private void SaveResult(object result, string path)
        {
            using (FileStream stream = new FileStream(path, FileMode.Create))
            {
                using (StreamWriter writer = new StreamWriter(stream))
                using (JsonTextWriter writerJSON = new JsonTextWriter(writer))
                {
                    var serializer = new JsonSerializer
                    {
                        Formatting = Formatting.Indented,
                        DateFormatString = "yyyy-MM-dd HH:mm:ss",
                        FloatParseHandling = FloatParseHandling.Decimal
                    };

                    serializer.Serialize(writerJSON, result, typeof(List<DeviceData>));
                }
            }

            _log.Write(MessageCreate($"Результат сохранен в файл: {path}"));
        }

        private string MessageCreate(string message)
        {
            return $"{DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss")}\t{message}";
        }

        #endregion
    }
}
