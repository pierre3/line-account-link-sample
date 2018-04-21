using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace LineAccountLinkApp.Models
{
    public class LineLink
    {
        [Key]
        public string UserId { get; set; }
        public string Nonce { get; set; }
    }
}
