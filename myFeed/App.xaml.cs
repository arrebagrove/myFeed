using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Storage;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace myFeed {

    sealed partial class App : Application
    {
        /// <summary>
        /// Контейнер для сериализации информации о пользовательских настройках.
        /// </summary>
        [DataContract]
        public class ConfigFile
        {
            public int FontSize = 17;
            public uint CheckTime = 60;
            public bool DownloadImages = true;
            public int RequestedTheme = 0; // 0 = N/A, 1 = Light, 2 = Dark
        }

        /// <summary>
        /// Содержит информацию о настройках приложения.
        /// </summary>
        internal static ConfigFile config = new ConfigFile();
        internal static int ChosenIndex;
        internal static bool CanNavigate = false;
        internal static string Read = string.Empty;

        /// <summary>
        /// Инициализирует одноэлементный объект приложения.  Это первая выполняемая строка разрабатываемого
        /// кода; поэтому она является логическим эквивалентом main() или WinMain().
        /// </summary>
        public App()
        {
            try
            {
                this.LoadConfig();
                this.CheckFiles();
            }
            catch
            {

            }

            Microsoft.ApplicationInsights.WindowsAppInitializer.InitializeAsync(
                Microsoft.ApplicationInsights.WindowsCollectors.Metadata |
                Microsoft.ApplicationInsights.WindowsCollectors.Session);

            this.InitializeComponent();
            this.Suspending += OnSuspending;
        }
        
        /// <summary>
        /// Загружает файл настроек приложения и устанавливает тему оформления в зависимости от содержимого.
        /// </summary>
        private async void LoadConfig()
        {
            try
            {
                try
                {
                    StorageFile configfile = await ApplicationData.Current.LocalFolder.GetFileAsync("config");
                    App.config = await SerializerExtensions.DeSerializeObject<App.ConfigFile>(configfile);
                }
                catch
                {
                    StorageFile configfile = await ApplicationData.Current.LocalFolder.CreateFileAsync("config");
                    SerializerExtensions.SerializeObject(App.config, configfile);
                }
            }
            catch
            {

            }
        }
        
        /// <summary>
        /// Вызывается при обычном запуске приложения пользователем.  Будут использоваться другие точки входа,
        /// например, если приложение запускается для открытия конкретного файла.
        /// </summary>
        /// <param name="e">Сведения о запросе и обработке запуска.</param>
        protected override void OnLaunched(LaunchActivatedEventArgs e)
        {
            Frame rootFrame = Window.Current.Content as Frame;

            if (rootFrame == null)
            {
                rootFrame = new Frame();
                rootFrame.NavigationFailed += OnNavigationFailed; 
                        
                if (e.PreviousExecutionState == ApplicationExecutionState.Terminated)
                {
                    // TODO: Загрузить состояние из ранее приостановленного приложения
                }

                Window.Current.Content = rootFrame;
            }

            // Если приложение уже запущено, а пользователь нажал на вторичную плитку.
            if (rootFrame.Content is MainPage)
            {
                if (!string.IsNullOrEmpty(e.Arguments) && (e.Kind != ActivationKind.ToastNotification))
                    (rootFrame.Content as MainPage).FromSecondaryTile(e.Arguments);
            }

            // Первый запуск приложения. Открываем MainPage и передаем туда параметры запуска.
            if (rootFrame.Content == null)
            {
                rootFrame.Navigate(typeof(MainPage), e.Arguments);
            }
            
            Window.Current.Activate();
        }

        /// <summary>
        /// Вызывается при переходе в приложение из Toast уведомлений
        /// </summary>
        /// <param name="e">Аргументы Toast уведомления</param>
        protected override void OnActivated(IActivatedEventArgs e)
        {
            Frame rootFrame = Window.Current.Content as Frame;

            if (rootFrame == null)
            {
                rootFrame = new Frame();
                rootFrame.NavigationFailed += OnNavigationFailed;

                if (e.PreviousExecutionState == ApplicationExecutionState.Terminated)
                {
                    //TODO: Загрузить состояние из ранее приостановленного приложения
                }

                Window.Current.Content = rootFrame;
            }

            if (rootFrame.Content == null)
            {
                rootFrame.Navigate(typeof(MainPage), "N");
            }
            
            // Обработчик клика на уведомления.
            if (e.Kind == ActivationKind.ToastNotification)
            {
                ToastNotificationActivatedEventArgs toastArgs = e as ToastNotificationActivatedEventArgs;
                if (rootFrame.Content is MainPage)
                {
                    (rootFrame.Content as MainPage).FindNotification(toastArgs.Argument);
                }
            }
            
            Window.Current.Activate();
        }

        /// <summary>
        /// Вызывается в случае сбоя навигации на определенную страницу
        /// </summary>
        /// <param name="sender">Фрейм, для которого произошел сбой навигации</param>
        /// <param name="e">Сведения о сбое навигации</param>
        void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            throw new Exception("Failed to load Page " + e.SourcePageType.FullName);
        }

        /// <summary>
        /// Вызывается при приостановке выполнения приложения.  Состояние приложения сохраняется
        /// без учета информации о том, будет ли оно завершено или возобновлено с неизменным
        /// содержимым памяти.
        /// </summary>
        /// <param name="sender">Источник запроса приостановки.</param>
        /// <param name="e">Сведения о запросе приостановки.</param>
        private void OnSuspending(object sender, SuspendingEventArgs e)
        {
            var deferral = e.SuspendingOperation.GetDeferral();
            //TODO: Сохранить состояние приложения и остановить все фоновые операции
            deferral.Complete();
        }

        /// <summary>
        /// Страшный и дикий кусок кода, конвертирующий данные из старого странного формата хранения
        /// данных в новый. Также проверяет, существуют ли нужные файлы. Если нет - делает новые.
        /// </summary>
        private async void CheckFiles()
        {
            StorageFolder storageFolder = ApplicationData.Current.LocalFolder;

            try
            {
                string temp_ = await MigrateData("loadimg.txt");
                if (temp_ != string.Empty) App.config.DownloadImages = bool.Parse(temp_);

                temp_ = await MigrateData("checktime");
                if (temp_ != string.Empty) App.config.CheckTime = uint.Parse(temp_);

                temp_ = await MigrateData("settings.txt");
                if (temp_ != string.Empty) App.config.FontSize = int.Parse(temp_);

                StorageFile configfile = await storageFolder.CreateFileAsync("config");
                SerializerExtensions.SerializeObject(App.config, configfile);
            }
            catch
            {

            }

            try
            {
                await storageFolder.GetFileAsync("sites");
            }
            catch
            {
                await storageFolder.CreateFileAsync("sites");
                Categories cats = new Categories();
                cats.categories = new List<Category>();
                SerializerExtensions.SerializeObject(cats, await ApplicationData.Current.LocalFolder.GetFileAsync("sites"));
            }

            /// Here we do some stupid stuff in order to migrade 
            /// from old strange data storing format.
            string temp = await MigrateData("sources");
            if (temp != string.Empty)
            {
                try
                {
                    Categories cats = new Categories();
                    cats.categories = new List<Category>();
                    List<string> sourceslist = temp.Split(new string[] { Environment.NewLine }, StringSplitOptions.None).ToList();
                    if (sourceslist.Count > 0) sourceslist.Remove(sourceslist.Last());
                    foreach (string str in sourceslist)
                    {
                        Category cat = new Category();
                        List<string> slist = str.Split(';').ToList();
                        if (slist.Count > 0) slist.RemoveAt(slist.Count - 1);
                        if (slist.Count > 0) cat.title = slist.First();
                        if (slist.Count > 0) slist.Remove(slist.First());
                        cat.websites = new List<Website>();
                        foreach (string s in slist)
                        {
                            Website wb = new Website();
                            wb.url = s;
                            cat.websites.Add(wb);
                        }
                        cats.categories.Add(cat);
                    }
                    SerializerExtensions.SerializeObject(cats, await ApplicationData.Current.LocalFolder.GetFileAsync("sites"));

                }
                catch { }
            }

            try
            {
                await storageFolder.GetFileAsync("datecutoff");
            }
            catch
            {
                await storageFolder.CreateFileAsync("datecutoff");
            }

            try
            {
                await storageFolder.GetFolderAsync("favorites");
            }
            catch
            {
                await storageFolder.CreateFolderAsync("favorites");
            }

            try
            {
                await storageFolder.GetFileAsync("saved_cache");
            }
            catch
            {
                await storageFolder.CreateFileAsync("saved_cache");
            }

            try
            {
                /// Here we delete info about old articles.
                App.Read = await FileIO.ReadTextAsync(await storageFolder.GetFileAsync("read.txt"));
                List<string> read_list = App.Read.Split(';').ToList();
                read_list.RemoveAt(read_list.Count - 1);
                if (read_list.Count > 90) read_list = read_list.Skip(Math.Max(0, read_list.Count() - 90)).ToList();
                App.Read = string.Empty;
                foreach (string item in read_list) App.Read = App.Read + item + ';';
                await FileIO.WriteTextAsync(await storageFolder.GetFileAsync("read.txt"), App.Read);
            }
            catch
            {
                await storageFolder.CreateFileAsync("read.txt", CreationCollisionOption.ReplaceExisting);
            }
        }

        /// <summary>
        /// Функция для куска кода выше.
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        private async Task<string> MigrateData(string filename)
        {
            try
            {
                StorageFile file = await ApplicationData.Current.LocalFolder.GetFileAsync(filename);
                string setting = await FileIO.ReadTextAsync(file);
                await file.DeleteAsync();
                return setting;
            }
            catch
            {
                return string.Empty;
            }
        }
    }
}
