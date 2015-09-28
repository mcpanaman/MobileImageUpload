namespace MobilePictureUpload.Web.Controllers
{
    using System.Threading.Tasks;
    using System;
    using System.Web;
    using System.Web.Mvc;

    using Helper;

    public class HomeController : Controller
    {
        public HomeController()
        {
        }

        public ActionResult Index()
        {
            return this.View();
        }
        
        [HttpPost]
        public async Task<ActionResult> UploadImages(string shrinkedImage, string fileName)
        {
            if (string.IsNullOrEmpty(shrinkedImage))
            {
                return null;
            }

            // BASE64-Image Data to byte MAGIC...
            string img = shrinkedImage.Replace("data:image/jpeg;base64,", "").Replace(" ", "+");
            byte[] bytes = Convert.FromBase64String(img);


            await AzureStorageHelper.Instance.UploadPicture(bytes, fileName);

            return RedirectToAction("Index");
        }
    }
}