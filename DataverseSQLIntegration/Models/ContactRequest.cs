using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataverseSQLIntegration;

public class ContactRequest
{
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public string EmailAddress1 { get; set; } = null!;
    public string? MobilePhone { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string? ParentAccountId { get; set; }
    public int? GenderCode { get; set; }
    public bool? DoNotEmail { get; set; }
}