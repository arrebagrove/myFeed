using System;
using System.Runtime.Serialization;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
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
            Microsoft.ApplicationInsights.WindowsAppInitializer.InitializeAsync(
                Microsoft.ApplicationInsights.WindowsCollectors.Metadata |
                Microsoft.ApplicationInsights.WindowsCollectors.Session);
            this.InitializeComponent();
            this.Suspending += OnSuspending;
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
                if (!string.IsNullOrEmpty(e.Arguments) && (e.Kind != ActivationKind.ToastNotification)) (rootFrame.Content as MainPage).FromSecondaryTile(e.Arguments);
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
        /// <param name="args">Аргументы Toast уведомления</param>
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
                rootFrame.Navigate(typeof(MainPage), string.Empty);
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
    }
}
