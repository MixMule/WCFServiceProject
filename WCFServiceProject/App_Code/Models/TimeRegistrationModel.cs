using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WCFServiseProject.Models
{
    /// <summary>
    /// Class used to represent the info
    /// </summary>
    public class TimeRegistration 
    {
        public int Id { get; set; }
        public DateTime TimeIn { get; set; }
        public DateTime TimeOut { get; set; }

    }
}