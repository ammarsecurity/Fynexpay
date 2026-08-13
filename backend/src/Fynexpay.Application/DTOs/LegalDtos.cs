namespace Fynexpay.Application.DTOs;

public class LegalBundleDto
{
    public LegalPageDto Terms { get; set; } = new();
    public LegalPageDto Privacy { get; set; } = new();
    public LegalPageDto Prohibited { get; set; } = new();
    public LegalPageDto Brand { get; set; } = new();
    public CompanyPageDto Company { get; set; } = new();
}

public class LegalPageDto
{
    public string Nav { get; set; } = "";
    public string Title { get; set; } = "";
    public string Updated { get; set; } = "";
    public string TocTitle { get; set; } = "";
    public string Intro { get; set; } = "";
    public List<LegalSectionDto> Sections { get; set; } = [];
}

public class LegalSectionDto
{
    public string Heading { get; set; } = "";
    public string Body { get; set; } = "";
    public List<string> Items { get; set; } = [];
}

public class CompanyPageDto
{
    public string Nav { get; set; } = "";
    public string Title { get; set; } = "";
    public string Updated { get; set; } = "";
    public string Intro { get; set; } = "";
    public string RegistrationTitle { get; set; } = "";
    public string IraqTitle { get; set; } = "";
    public string IraqLegalNameLabel { get; set; } = "";
    public string IraqLegalName { get; set; } = "";
    public string IraqRegistryLabel { get; set; } = "";
    public string IraqRegistry { get; set; } = "";
    public string IraqHqLabel { get; set; } = "";
    public string IraqHq { get; set; } = "";
    public string CertsTitle { get; set; } = "";
    public string CertsBody { get; set; } = "";
    public List<LegalSectionDto> Certs { get; set; } = [];
    public string ContactTitle { get; set; } = "";
    public string ContactEmail { get; set; } = "";
    public string ContactPhone { get; set; } = "";
    public string ContactWebsite { get; set; } = "";
    public string Disclaimer { get; set; } = "";
}
