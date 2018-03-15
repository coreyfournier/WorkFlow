using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Workflow.Core.Models
{
    /// <summary>
    /// Represents an instance that is ready to run again.
    /// Intended to only be used on the view [Instances]
    /// </summary>
    [Table("Instances")]
    public class InstanceView
    {
        [Key]
        public Guid InstanceId { get; set; }
        public string IdentityName { get; set; }
        public string IdentityPackage { get; set; }
        public long Build { get; set; }
        public long Major { get; set; }
        public long Minor { get; set; }
        public long Revision { get; set; }
        public string LastMachine { get; set; }
        public string CurrentMachine { get; set; }
        public string ActiveBookMarks { get; set; }
        public bool IsCompleted { get; set; }
    }
}
