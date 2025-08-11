using Bt.Ems.Lib.PipelineConfig.DbConfiguration.Model;

namespace fierhub_authcheck_net.Model
{
    public class CurrentSession
    {
        public long UserId { set; get; }
        public string? EmployeeCodePrefix { get; set; }
        public int EmployeeCodeLength { get; set; }
        public string? Authorization { set; get; }
        public int CompanyId { set; get; }
        public string? CompanyName { set; get; }
        public int DesignationId { set; get; }
        public int OrganizationId { set; get; }
        public long ReportingManagerId { set; get; }
        public string Culture { set; get; } = "en";
        public string? ManagerEmail { set; get; }
        public int RoleId { set; get; }
        public string? Email { set; get; }
        public string? Mobile { set; get; }
        public string? FullName { set; get; }
        public string? ManagerName { set; get; }
        public string? TimeZoneName { set; get; }
        public TimeZoneInfo? TimeZone { set; get; }
        public int FinancialStartYear { set; get; }
        public DateTime TimeZoneNow { set; get; }
        public string? CompanyCode { set; get; }
        public DefinedEnvironments Environment { set; get; }
        public string? LocalConnectionString { set; get; }
    }
}
