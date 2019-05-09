using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using LicenseParser.DTOs;
using LicenseParser.Services;
using System.IO;
using System.Web;

namespace LicenseParser.Controllers
{
    public class LicenseController : ApiController
    {
        
        [HttpGet]
        public ParseLicenseResponse GetLicense() // For testing only
        {
            return new ParseLicenseResponse()
            {
                FirstName = "John",
                LastName = "Doe",
                RawData = "RawData from John Doe - whoop whoop!"
            };
        }

        [HttpPost]
        public ParseLicenseResponse ParseLicense()
        {
            var httpRequest = HttpContext.Current.Request;
            if (httpRequest.Files.Count > 0)
            {
                var docfiles = new List<string>();
                foreach (string file in httpRequest.Files)
                {
                    var f = httpRequest.Files[file];

                    var svc = new ParserServices();
                    using (var memoryStream = new MemoryStream())
                    {
                        f.InputStream.CopyTo(memoryStream);
                        var arr = memoryStream.ToArray();

                        var ocrResult = svc.OcrFront(arr);

                        var rawData = "";
                        foreach(string s in ocrResult.Item1)
                        {
                            rawData += s + ", ";
                        }

                        return new ParseLicenseResponse()
                        {
                            FirstName = ocrResult.Item1[0],
                            LastName = ocrResult.Item1[1],
                            RawData = rawData.TrimEnd(',')
                        };
                    }
                }
            }
            return new ParseLicenseResponse();
        }
        
    }
}
