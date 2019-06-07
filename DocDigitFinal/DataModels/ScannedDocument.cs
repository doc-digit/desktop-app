using DocDigitFinal.Helpers;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace DocDigitFinal.DataModels
{
    public class ScannedDocument
    {
        public int UserId { get; set; }
        public int StudentId { get; set; }
        public int DocumentId { get; set; }
        public string ScanId { get; set; }
        public Dictionary<string, ImageSource> ScannedPages { get; set; } = new Dictionary<string, ImageSource>();

        public ScannedDocument(int userId, int studentId, int documentId)
        {
            UserId = userId;
            StudentId = studentId;
            DocumentId = documentId;
        }

        public async Task InitScan()
        {
            try
            {
                var scanData = $"{{ \"user\": {UserId}, \"student\": {StudentId}, \"document_type\": {DocumentId}}}";
                var response = await WebRequestHelper.PostAsync(ConfigurationManager.AppSettings["ApiUri"] + "/document/", scanData, "application/json");

                var definition = new { user = 0, student = 0, document_type = 0, id = "", created_date = "" };
                ScanId = (JsonConvert.DeserializeAnonymousType(response, definition)).id;
            }
            catch { }
        }

        public async Task AddPage(ImageSource img)
        {
            try
            {
                var data = $"{{ \"parent_id\": \"{ScanId}\", \"parent_type\": \"document\" }}";
                var response = await WebRequestHelper.PostAsync(ConfigurationManager.AppSettings["ApiUri"] + "/storage/upload/scan", data, "application/json");
                var definition = new { id = "", url = "", exp = 0 };
                var respObj = JsonConvert.DeserializeAnonymousType(response, definition);
                ScannedPages.Add(respObj.id, img);

                using (var fileStream = new FileStream("tmp.png", FileMode.Create))
                {
                    BitmapEncoder encoder = new PngBitmapEncoder();
                    encoder.Frames.Add(BitmapFrame.Create((BitmapImage)img));
                    encoder.Save(fileStream);
                }

                var wc = new WebClient();
                var png = File.ReadAllBytes("tmp.png");
                wc.UploadData(respObj.url, "PUT", png);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }
            
        public async Task Upload()
        {
            try
            {
                var data = $"{{ \"document\": \"{ScanId}\", \"page_order\": [ ";
                foreach (var page in ScannedPages)
                {
                    data += $"\"{page.Key}\", ";
                }
                data = data.Substring(0, data.Length - 2);
                data += " ]}";
                await WebRequestHelper.PostAsync(ConfigurationManager.AppSettings["ApiUri"] + "/document/create_pdf", data, "application/json");
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }
    }
}
