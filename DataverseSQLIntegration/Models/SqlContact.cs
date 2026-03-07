using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataverseSQLIntegration;

public class SqlContact
{
    public string? DataverseId { get; set; }
    public string FullName { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string? MobilePhone { get; set; }
    public DateTime? CreatedOn { get; set; }
    public DateTime? LastSyncedOn { get; set; }
    public string? SyncDirection { get; set; }
}