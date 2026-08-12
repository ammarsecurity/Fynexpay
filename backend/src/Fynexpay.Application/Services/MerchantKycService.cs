using Fynexpay.Application.Abstractions;
using Fynexpay.Application.Abstractions.Messaging;
using Fynexpay.Application.DTOs;
using Fynexpay.Application.Security;
using Fynexpay.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Fynexpay.Application.Services;

public class MerchantKycService
{
    private readonly IAppDbContext _db;
    private readonly NotificationService _notifications;

    public MerchantKycService(IAppDbContext db, NotificationService notifications)
    {
        _db = db;
        _notifications = notifications;
    }

    public async Task<MerchantKycDto> GetForMerchantAsync(Guid merchantId, CancellationToken ct = default)
    {
        var m = await _db.Merchants.AsNoTracking().FirstOrDefaultAsync(x => x.Id == merchantId, ct)
            ?? throw new InvalidOperationException("التاجر غير موجود");
        return Map(m);
    }

    public async Task<MerchantKycDto> UploadAsync(
        Guid merchantId,
        string docType,
        byte[] bytes,
        string contentRoot,
        CancellationToken ct = default)
    {
        var merchant = await _db.Merchants.FirstOrDefaultAsync(m => m.Id == merchantId, ct)
            ?? throw new InvalidOperationException("التاجر غير موجود");

        if (merchant.KycStatus == KycStatus.Approved)
            throw new InvalidOperationException("تم اعتماد الهوية مسبقاً. تواصل مع الإدارة إن احتجت تحديثاً.");

        if (merchant.KycStatus == KycStatus.Pending)
            throw new InvalidOperationException("الهوية قيد المراجعة حالياً ولا يمكن تعديلها حتى يتم الرد.");

        var ext = KycDocumentValidator.ValidateAndGetExtension(bytes);
        var kind = NormalizeDocType(docType);
        var uploadsDir = Path.Combine(contentRoot, "wwwroot", "uploads", "kyc", merchantId.ToString("N"));
        Directory.CreateDirectory(uploadsDir);

        var fileName = $"{kind}-{Guid.NewGuid():N}{ext}";
        var fullPath = Path.Combine(uploadsDir, fileName);
        await File.WriteAllBytesAsync(fullPath, bytes, ct);

        var url = $"/uploads/kyc/{merchantId:N}/{fileName}?v={DateTimeOffset.UtcNow.ToUnixTimeSeconds()}";
        switch (kind)
        {
            case "id-front":
                TryDeleteOld(contentRoot, merchant.KycIdFrontUrl);
                merchant.KycIdFrontUrl = url;
                break;
            case "id-back":
                TryDeleteOld(contentRoot, merchant.KycIdBackUrl);
                merchant.KycIdBackUrl = url;
                break;
            case "passport":
                TryDeleteOld(contentRoot, merchant.KycPassportUrl);
                merchant.KycPassportUrl = url;
                break;
        }

        merchant.KycAdminNotes = null;
        merchant.KycReviewedAtUtc = null;
        merchant.UpdatedAtUtc = DateTime.UtcNow;

        var complete = HasAllDocs(merchant);
        if (complete)
        {
            merchant.KycStatus = KycStatus.Pending;
            merchant.KycSubmittedAtUtc = DateTime.UtcNow;
        }
        else
        {
            merchant.KycStatus = KycStatus.Incomplete;
            merchant.KycSubmittedAtUtc = null;
        }

        await _db.SaveChangesAsync(ct);

        if (complete)
        {
            await _notifications.NotifyAdminsSafeAsync(
                NotificationTypes.KycSubmitted,
                "طلب تحقق هوية تاجر",
                $"رفع التاجر {merchant.BusinessName} مستندات الهوية للمراجعة.",
                "/admin/merchants",
                merchant.Id,
                new { merchantId = merchant.Id, status = merchant.KycStatus.ToString() },
                ct);
        }

        return Map(merchant);
    }

    public async Task<MerchantKycDto> ReviewAsync(
        Guid merchantId,
        ReviewMerchantKycRequest request,
        CancellationToken ct = default)
    {
        var merchant = await _db.Merchants.FirstOrDefaultAsync(m => m.Id == merchantId, ct)
            ?? throw new InvalidOperationException("التاجر غير موجود");

        if (merchant.KycStatus != KycStatus.Pending && merchant.KycStatus != KycStatus.Approved && merchant.KycStatus != KycStatus.Rejected)
            throw new InvalidOperationException("لا توجد مستندات جاهزة للمراجعة");

        if (!HasAllDocs(merchant))
            throw new InvalidOperationException("المستندات غير مكتملة");

        var action = (request.Action ?? "").Trim();
        if (action.Equals("Approve", StringComparison.OrdinalIgnoreCase)
            || action.Equals("Approved", StringComparison.OrdinalIgnoreCase))
        {
            merchant.KycStatus = KycStatus.Approved;
            merchant.KycAdminNotes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim();
            merchant.KycReviewedAtUtc = DateTime.UtcNow;
            merchant.UpdatedAtUtc = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);

            await _notifications.NotifyMerchantUsersSafeAsync(
                merchant.Id,
                NotificationTypes.KycApproved,
                "تم اعتماد الهوية",
                "تمت الموافقة على مستندات الهوية الخاصة بحسابك.",
                "/merchant/profile",
                new { merchantId = merchant.Id, status = merchant.KycStatus.ToString() },
                ct);
        }
        else if (action.Equals("Reject", StringComparison.OrdinalIgnoreCase)
                 || action.Equals("Rejected", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(request.Notes))
                throw new ArgumentException("أضف ملاحظة توضح سبب الرفض لإعادة الرفع");

            merchant.KycStatus = KycStatus.Rejected;
            merchant.KycAdminNotes = request.Notes.Trim();
            merchant.KycReviewedAtUtc = DateTime.UtcNow;
            merchant.UpdatedAtUtc = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);

            await _notifications.NotifyMerchantUsersSafeAsync(
                merchant.Id,
                NotificationTypes.KycRejected,
                "تم رفض مستندات الهوية",
                $"يرجى إعادة رفع المستندات. ملاحظة الإدارة: {merchant.KycAdminNotes}",
                "/merchant/profile",
                new { merchantId = merchant.Id, status = merchant.KycStatus.ToString() },
                ct);
        }
        else
        {
            throw new ArgumentException("الإجراء يجب أن يكون Approve أو Reject");
        }

        return Map(merchant);
    }

    public static MerchantKycDto Map(Domain.Entities.Merchant m)
    {
        var canUpload = m.KycStatus is KycStatus.None or KycStatus.Incomplete or KycStatus.Rejected;
        return new MerchantKycDto(
            m.KycStatus.ToString(),
            m.KycIdFrontUrl,
            m.KycIdBackUrl,
            m.KycPassportUrl,
            m.KycAdminNotes,
            m.KycSubmittedAtUtc,
            m.KycReviewedAtUtc,
            canUpload,
            HasAllDocs(m));
    }

    private static bool HasAllDocs(Domain.Entities.Merchant m) =>
        !string.IsNullOrWhiteSpace(m.KycIdFrontUrl)
        && !string.IsNullOrWhiteSpace(m.KycIdBackUrl)
        && !string.IsNullOrWhiteSpace(m.KycPassportUrl);

    private static string NormalizeDocType(string docType)
    {
        var t = (docType ?? "").Trim().ToLowerInvariant().Replace('_', '-');
        return t switch
        {
            "id-front" or "idfront" or "front" => "id-front",
            "id-back" or "idback" or "back" => "id-back",
            "passport" or "epassport" or "e-passport" => "passport",
            _ => throw new ArgumentException("نوع المستند غير معروف")
        };
    }

    private static void TryDeleteOld(string contentRoot, string? url)
    {
        if (string.IsNullOrWhiteSpace(url)) return;
        try
        {
            var pathOnly = url.Split('?', 2)[0];
            if (!pathOnly.StartsWith("/uploads/kyc/", StringComparison.OrdinalIgnoreCase)) return;
            var full = Path.Combine(contentRoot, "wwwroot", pathOnly.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
            if (File.Exists(full)) File.Delete(full);
        }
        catch
        {
            // ignore cleanup failures
        }
    }
}
