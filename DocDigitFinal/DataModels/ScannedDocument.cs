using DocDigitFinal.Helpers;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration;
using System.IO;
using System.Net;
using System.Threading;
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
        public List<ScannedPage> ScannedPages = new List<ScannedPage>();
        private Thread consumer;
        public static BlockingCollection<ImageSource> UploadQueue = new BlockingCollection<ImageSource>();

        public ScannedDocument(int userId, int studentId, int documentId)
        {
            if (UserId != userId || StudentId != studentId || DocumentId != documentId)
            {
                UserId = userId;
                StudentId = studentId;
                DocumentId = documentId;
                if (consumer != null) consumer.Abort();
                consumer = new Thread(Uploader);
                consumer.Start();
            }
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
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }
            
        public async Task CreatePDF(ObservableCollection<ImageSource> scans)
        {
            try
            {
                var data = $"{{ \"document\": \"{ScanId}\", \"page_order\": [ ";
                foreach (var page in scans)
                {
                    data += $"\"{ScannedPages.Find((k) => k.Scan == page).Id}\", ";
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

        private async void Uploader()
        {
            await InitScan();
            Console.WriteLine($"Init new doc: {UserId} {StudentId} {DocumentId} {ScanId}");

            while (true)
            {
                foreach (var img in UploadQueue.GetConsumingEnumerable())
                {
                    try
                    {
                        var data = $"{{ \"parent_id\": \"{ScanId}\", \"parent_type\": \"document\" }}";
                        var response = await WebRequestHelper.PostAsync(ConfigurationManager.AppSettings["ApiUri"] + "/storage/upload/scan", data, "application/json");
                        var definition = new { id = "", url = "", exp = 0 };
                        var respObj = JsonConvert.DeserializeAnonymousType(response, definition);
                        ScannedPages.Add(new ScannedPage(respObj.id, img, false));

                        using (var fileStream = new FileStream("tmp.png", FileMode.Create))
                        {
                            BitmapEncoder encoder = new PngBitmapEncoder();
                            encoder.Frames.Add(BitmapFrame.Create((BitmapSource)img));
                            encoder.Save(fileStream);
                        }

                        var wc = new WebClient();
                        var png = File.ReadAllBytes("tmp.png");
                        wc.UploadData(respObj.url, "PUT", png);
                        ScannedPages.Find((k) => k.Scan == img).IsUploaded = true;
                        Console.WriteLine($"Uploaded: {respObj.id}");
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                        UploadQueue.Add(img);
                    }
                }
                Thread.Sleep(100);
            }
        }
    }
}
