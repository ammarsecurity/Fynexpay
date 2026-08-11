using System.Text.Json;
using Fynexpay.Application.Abstractions;
using Fynexpay.Application.DTOs;
using Fynexpay.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Fynexpay.Application.Services;

public class LandingContentService
{
    public const string SettingsKey = "landing_content";
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    private readonly IAppDbContext _db;
    private LandingContentDto? _cache;

    public LandingContentService(IAppDbContext db) => _db = db;

    public async Task<LandingContentDto> GetAsync(CancellationToken ct = default)
    {
        if (_cache != null) return Clone(_cache);

        var row = await _db.PlatformSettings.AsNoTracking()
            .FirstOrDefaultAsync(s => s.Key == SettingsKey, ct);

        if (row == null || string.IsNullOrWhiteSpace(row.Value))
        {
            var seeded = LandingDefaults.Create();
            await PersistAsync(seeded, ct);
            _cache = seeded;
            return Clone(seeded);
        }

        var content = JsonSerializer.Deserialize<LandingContentDto>(row.Value, JsonOpts)
                      ?? LandingDefaults.Create();
        content = MergeWithDefaults(content);
        if (ScrubProviderBrandNames(content))
            await PersistAsync(content, ct);
        _cache = content;
        return Clone(content);
    }

    public async Task<LandingContentDto> SaveAsync(LandingContentDto content, CancellationToken ct = default)
    {
        content = MergeWithDefaults(content);
        ScrubProviderBrandNames(content);
        await PersistAsync(content, ct);
        _cache = content;
        return Clone(content);
    }

    public async Task<LandingContentDto> ResetAsync(CancellationToken ct = default)
    {
        var defaults = LandingDefaults.Create();
        await PersistAsync(defaults, ct);
        _cache = defaults;
        return Clone(defaults);
    }

    private async Task PersistAsync(LandingContentDto content, CancellationToken ct)
    {
        var json = JsonSerializer.Serialize(content, JsonOpts);
        var row = await _db.PlatformSettings.FirstOrDefaultAsync(s => s.Key == SettingsKey, ct);
        if (row == null)
        {
            _db.PlatformSettings.Add(new PlatformSetting { Key = SettingsKey, Value = json });
        }
        else
        {
            row.Value = json;
            row.UpdatedAtUtc = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync(ct);
    }

    private static LandingContentDto MergeWithDefaults(LandingContentDto content)
    {
        var defaults = LandingDefaults.Create();
        content.Ar = MergeLocale(content.Ar, defaults.Ar);
        content.En = MergeLocale(content.En, defaults.En);
        return content;
    }

    private static LandingLocaleDto MergeLocale(LandingLocaleDto? src, LandingLocaleDto fallback)
    {
        src ??= new LandingLocaleDto();
        src.NavFeatures = Pick(src.NavFeatures, fallback.NavFeatures);
        src.NavProviders = Pick(src.NavProviders, fallback.NavProviders);
        src.NavDevelopers = Pick(src.NavDevelopers, fallback.NavDevelopers);
        src.NavContact = Pick(src.NavContact, fallback.NavContact);
        src.Login = Pick(src.Login, fallback.Login);
        src.StartNow = Pick(src.StartNow, fallback.StartNow);
        src.Badge = Pick(src.Badge, fallback.Badge);
        src.HeroTitle = Pick(src.HeroTitle, fallback.HeroTitle);
        src.HeroSubtitle = Pick(src.HeroSubtitle, fallback.HeroSubtitle);
        src.CtaMerchant = Pick(src.CtaMerchant, fallback.CtaMerchant);
        src.CtaDocs = Pick(src.CtaDocs, fallback.CtaDocs);
        src.FeaturesEyebrow = Pick(src.FeaturesEyebrow, fallback.FeaturesEyebrow);
        src.FeaturesTitle = Pick(src.FeaturesTitle, fallback.FeaturesTitle);
        src.FeaturesSubtitle = Pick(src.FeaturesSubtitle, fallback.FeaturesSubtitle);
        src.ProvidersEyebrow = Pick(src.ProvidersEyebrow, fallback.ProvidersEyebrow);
        src.ProvidersTitle = Pick(src.ProvidersTitle, fallback.ProvidersTitle);
        src.ProvidersSubtitle = Pick(src.ProvidersSubtitle, fallback.ProvidersSubtitle);
        src.ApiEyebrow = Pick(src.ApiEyebrow, fallback.ApiEyebrow);
        src.ApiTitle = Pick(src.ApiTitle, fallback.ApiTitle);
        src.ApiSubtitle = Pick(src.ApiSubtitle, fallback.ApiSubtitle);
        src.CtaTitle = Pick(src.CtaTitle, fallback.CtaTitle);
        src.CtaSubtitle = Pick(src.CtaSubtitle, fallback.CtaSubtitle);
        src.CtaRegister = Pick(src.CtaRegister, fallback.CtaRegister);
        src.CtaContact = Pick(src.CtaContact, fallback.CtaContact);
        src.Footer = Pick(src.Footer, fallback.Footer);
        src.MockDashboard = Pick(src.MockDashboard, fallback.MockDashboard);
        src.MockToday = Pick(src.MockToday, fallback.MockToday);
        src.MockSuccess = Pick(src.MockSuccess, fallback.MockSuccess);
        src.MockAmount = Pick(src.MockAmount, fallback.MockAmount);
        src.MockChooseProvider = Pick(src.MockChooseProvider, fallback.MockChooseProvider);
        src.ContactEyebrow = Pick(src.ContactEyebrow, fallback.ContactEyebrow);
        src.ContactTitle = Pick(src.ContactTitle, fallback.ContactTitle);
        src.ContactSubtitle = Pick(src.ContactSubtitle, fallback.ContactSubtitle);
        src.ContactEmailLabel = Pick(src.ContactEmailLabel, fallback.ContactEmailLabel);
        src.ContactEmail = Pick(src.ContactEmail, fallback.ContactEmail);
        src.ContactPhoneLabel = Pick(src.ContactPhoneLabel, fallback.ContactPhoneLabel);
        src.ContactPhone = Pick(src.ContactPhone, fallback.ContactPhone);
        src.ContactAddressLabel = Pick(src.ContactAddressLabel, fallback.ContactAddressLabel);
        src.ContactAddress = Pick(src.ContactAddress, fallback.ContactAddress);
        src.ContactHoursLabel = Pick(src.ContactHoursLabel, fallback.ContactHoursLabel);
        src.ContactHours = Pick(src.ContactHours, fallback.ContactHours);
        src.ContactFormName = Pick(src.ContactFormName, fallback.ContactFormName);
        src.ContactFormEmail = Pick(src.ContactFormEmail, fallback.ContactFormEmail);
        src.ContactFormMessage = Pick(src.ContactFormMessage, fallback.ContactFormMessage);
        src.ContactFormSubmit = Pick(src.ContactFormSubmit, fallback.ContactFormSubmit);
        src.ContactFormNote = Pick(src.ContactFormNote, fallback.ContactFormNote);
        src.ContactFormSuccess = Pick(src.ContactFormSuccess, fallback.ContactFormSuccess);

        if (src.Features == null || src.Features.Count == 0)
            src.Features = fallback.Features.Select(f => new LandingFeatureDto { Icon = f.Icon, Title = f.Title, Body = f.Body }).ToList();
        src.ProviderPills ??= [];

        return src;
    }

    /// <summary>
    /// Strip hardcoded PSP brand names from marketing copy (logos are shown instead).
    /// </summary>
    private static bool ScrubProviderBrandNames(LandingContentDto content)
    {
        var changed = false;
        var defaults = LandingDefaults.Create();
        changed |= ScrubLocale(content.Ar, defaults.Ar);
        changed |= ScrubLocale(content.En, defaults.En);
        return changed;
    }

    private static bool ScrubLocale(LandingLocaleDto src, LandingLocaleDto fallback)
    {
        var changed = false;
        if (ContainsProviderBrand(src.Badge)) { src.Badge = fallback.Badge; changed = true; }
        if (ContainsProviderBrand(src.HeroSubtitle)) { src.HeroSubtitle = fallback.HeroSubtitle; changed = true; }
        if (ContainsProviderBrand(src.ProvidersTitle)) { src.ProvidersTitle = fallback.ProvidersTitle; changed = true; }
        if (ContainsProviderBrand(src.ProvidersSubtitle)) { src.ProvidersSubtitle = fallback.ProvidersSubtitle; changed = true; }
        if (ContainsProviderBrand(src.ApiSubtitle)) { src.ApiSubtitle = fallback.ApiSubtitle; changed = true; }
        if (ContainsProviderBrand(src.NavProviders)) { src.NavProviders = fallback.NavProviders; changed = true; }
        if (ContainsProviderBrand(src.ProvidersEyebrow)) { src.ProvidersEyebrow = fallback.ProvidersEyebrow; changed = true; }
        if (ContainsProviderBrand(src.MockChooseProvider)) { src.MockChooseProvider = fallback.MockChooseProvider; changed = true; }

        if (src.ProviderPills != null && src.ProviderPills.Any(ContainsProviderBrand))
        {
            src.ProviderPills = [];
            changed = true;
        }

        if (src.Features != null)
        {
            for (var i = 0; i < src.Features.Count; i++)
            {
                var f = src.Features[i];
                if (!ContainsProviderBrand(f.Title) && !ContainsProviderBrand(f.Body) && !ContainsProviderBrand(f.Icon))
                    continue;
                var fb = i < fallback.Features.Count ? fallback.Features[i] : null;
                if (fb == null) continue;
                f.Icon = fb.Icon;
                f.Title = fb.Title;
                f.Body = fb.Body;
                changed = true;
            }
        }

        return changed;
    }

    private static bool ContainsProviderBrand(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return false;
        return text.Contains("ZainCash", StringComparison.OrdinalIgnoreCase)
               || text.Contains("Zain Cash", StringComparison.OrdinalIgnoreCase)
               || text.Contains("SuperQi", StringComparison.OrdinalIgnoreCase)
               || text.Contains("Alqaseh", StringComparison.OrdinalIgnoreCase)
               || text.Contains("Al Qaseh", StringComparison.OrdinalIgnoreCase)
               || text.Contains("القصّة", StringComparison.OrdinalIgnoreCase)
               || text.Contains("القصة", StringComparison.OrdinalIgnoreCase)
               || text.Contains("QI Gate", StringComparison.OrdinalIgnoreCase)
               || text.Contains("FIB Web", StringComparison.OrdinalIgnoreCase)
               || System.Text.RegularExpressions.Regex.IsMatch(text, @"\bFIB\b", System.Text.RegularExpressions.RegexOptions.IgnoreCase)
               || System.Text.RegularExpressions.Regex.IsMatch(text, @"\bQI\b");
    }

    private static string Pick(string? value, string fallback) =>
        string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();

    private static LandingContentDto Clone(LandingContentDto s) =>
        JsonSerializer.Deserialize<LandingContentDto>(JsonSerializer.Serialize(s, JsonOpts), JsonOpts)!;
}
