using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace LicenseParser.DTOs
{
    public class ParseLicenseResponse
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string RawData { get; set; }
    }
}