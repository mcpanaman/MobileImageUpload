namespace BlobPictureDownloader
{
    using System.Threading;
    using System.Timers;

    using MobilePictureUpload.Web.Helper;

    public class Program
    {
        private static System.Timers.Timer testTimer;

        private static bool shutdown = false;

        private static AutoResetEvent reset = null;

        private const string FILEPATH = @"C:\temp\pictures";

        public static void Main(string[] args)
        {
            Init();
            Run();
        }

        private static void Init()
        {
            reset = new AutoResetEvent(false);
            testTimer = new System.Timers.Timer();
            testTimer.Elapsed += new ElapsedEventHandler(FetchPictures);
            testTimer.Interval = (1000 * 60 * 1);
        }

        private static void Run()
        {
            testTimer.Start();
            while (!shutdown && reset.WaitOne())
            {
                // ob der WaitOne() Aufruf in der Bedingung steht oder im Schleifenblock direkt, macht keinen großen Unterschied
                // mögliche check, log meldungen oder sonstiges durchführen
            }
            testTimer.Stop();
        }

        private static void FetchPictures(object ojb, ElapsedEventArgs args)
        {
            var storageHelper = AzureStorageHelper.Instance;
            foreach (var message in storageHelper.GetQueuedItems(20))
            {
                if (storageHelper.DownloadBlob(message.AsString, FILEPATH))
                {
                    storageHelper.RemoveItemFromQueue(message);
                }
            }
        }
    }
}