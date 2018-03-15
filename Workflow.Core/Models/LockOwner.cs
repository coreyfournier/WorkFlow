using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Workflow.Core.Models
{
    [Table("LockOwnersTable")]
    public class LockOwner
    {
        public Guid Id { get; set; }
        public DateTime LockExpiration { get; set; }
        public string MachineName { get; set; }

        [Key]
        public long SurrogateLockOwnerId { get; set; }

        /// <summary>
        /// Total time in seconds the lock will expire for this owner. If it already expired, then just a -1 is returned.
        /// </summary>
        public double TimeToLockExpire
        {
            get
            {
                if (DateTime.UtcNow > LockExpiration)
                    return -1;
                else
                    return LockExpiration.Subtract(DateTime.UtcNow).TotalSeconds;
            }
        }
    }
}
