namespace MobilePictureUpload.Web.Helper
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading.Tasks;

    using Microsoft.WindowsAzure.Storage;
    using Microsoft.WindowsAzure.Storage.Blob;
    using Microsoft.WindowsAzure.Storage.Queue;

    public class AzureStorageHelper
    {
        public static AzureStorageHelper Instance { get; } = new AzureStorageHelper();

        /// <summary>
        /// Initializes 
        /// </summary>
        private AzureStorageHelper()
        {
            this.StorageAccount = CloudStorageAccount.Parse("DefaultEndpointsProtocol=https;AccountName={AccountName};AccountKey={AccountKey}");
        }

        /// <summary>
        /// Gets the Storage Account
        /// </summary>
        protected CloudStorageAccount StorageAccount { get; private set; }

        /// <summary>
        /// Uploads a picture to an azure blob storage
        /// </summary>
        /// <param name="fileToUpload">file to upload</param>
        /// <param name="fileName">file name</param>
        /// <returns></returns>
        public async Task<Uri> UploadPicture(byte[] fileToUpload, string fileName)
        {
            try
            {
                var blobContainer = this.GetPictureContainer();

                // Retrieve reference to a blob named "myblob".
                var path = $"{Guid.NewGuid().ToString()}{fileName}";
                CloudBlockBlob blockBlob = blobContainer.GetBlockBlobReference(path);

                // Upload image to Blob Storage
                // Fixed MIME-Type to JPEG
                blockBlob.Properties.ContentType = "image/jpeg";

                // Use memory stream to upload...
                MemoryStream ms = new MemoryStream(fileToUpload);

                await blockBlob.UploadFromStreamAsync(ms);
                await this.QueuePictureUriAsync(path);

                ms.Dispose();

                // Convert to be HTTP based URI (default storage path is HTTPS)
                //var uriBuilder = new UriBuilder(blockBlob.Uri);

                return blockBlob.Uri;
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// Gets a specified amount of queued items
        /// </summary>
        /// <param name="amount">amount of items</param>
        /// <returns></returns>
        public IEnumerable<CloudQueueMessage> GetQueuedItems(int amount)
        {
            // Retrieve a reference to a queue.
            CloudQueue queue = this.GetQueue();

            return queue.GetMessages(amount);
        }

        /// <summary>
        /// Removes item from queue
        /// </summary>
        /// <param name="message">message to delete</param>
        public void RemoveItemFromQueue(CloudQueueMessage message)
        {
            CloudQueue queue = this.GetQueue();

            queue.DeleteMessageAsync(message);
        }

        public bool DownloadBlob(string pictureName, string filePath)
        {
            try
            {
                if (!String.IsNullOrEmpty(pictureName))
                {
                    ////var uri = new UriBuilder(blobUri);
                    ////var cleanUri = uri.Uri.GetComponents(UriComponents.AbsoluteUri & ~UriComponents.Port, UriFormat.UriEscaped);
                    var container = this.GetPictureContainer();
                    var blob = container.GetBlockBlobReference(pictureName);

                    // Strip off any folder structure so the file name is just the file name
                    var lastPos = blob.Name.LastIndexOf('/');
                    var fileName = blob.Name.Substring(lastPos + 1, blob.Name.Length - lastPos - 1);

                    var fileStream = new FileStream($"{filePath}/{fileName}", FileMode.Create);
                    blob.DownloadToStream(fileStream);
                    fileStream.Close();

                    return true;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            return false;
        }



        /// <summary>
        /// Queues a URI of an image in a storage queue
        /// </summary>
        /// <param name="imageName">name of the iamge</param>
        protected async Task QueuePictureUriAsync(string imageName)
        {
            CloudQueue queue = this.GetQueue();

            // Create a message and add it to the queue.
            CloudQueueMessage message = new CloudQueueMessage(imageName);
            await queue.AddMessageAsync(message);
        }

        /// <summary>
        /// Gets the picture container of the blob-storage
        /// </summary>
        /// <returns></returns>
        private CloudBlobContainer GetPictureContainer()
        {
            CloudBlobClient blobClient = this.StorageAccount.CreateCloudBlobClient();

            //Retrieve reference to blob container. Create if it doesn't exist.
            CloudBlobContainer blobContainer = blobClient.GetContainerReference("photos");

            if (blobContainer.CreateIfNotExists())
            {
                //Set public access to the blobs in the container so we can use the picture 
                //   URLs in the HTML client.
                blobContainer.SetPermissions(new BlobContainerPermissions
                {
                    PublicAccess = BlobContainerPublicAccessType.Container
                });
            }

            return blobContainer;
        }

        /// <summary>
        /// Gets the storage queue
        /// </summary>
        /// <returns></returns>
        private CloudQueue GetQueue()
        {
            // Create the queue client.
            CloudQueueClient queueClient = this.StorageAccount.CreateCloudQueueClient();

            // Retrieve a reference to a queue.
            CloudQueue queue = queueClient.GetQueueReference("picturequeue");

            // Create the queue if it doesn't already exist.
            queue.CreateIfNotExists();

            return queue;
        }
    }
}