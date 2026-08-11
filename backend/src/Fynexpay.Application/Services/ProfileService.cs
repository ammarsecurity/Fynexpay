using System.Text.Json;
using Fynexpay.Application.Abstractions;
using Fynexpay.Application.DTOs;
using Fynexpay.Application.Security;
using Fynexpay.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Fynexpay.Application.Services;

public class ProfileService
{
    private readonly IAppDbContext _db;
    private readonly IJwtTokenService _jwt;
    private readonly OtpService _otp;

    public ProfileService(IAppDbContext db, IJwtTokenService jwt, OtpService otp)
    {
        _db = db;
        _jwt = jwt;
        _otp = otp;
    }

    public async Task<UserProfileDto> GetAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await _db.Users.AsNoTracking()
            .Include(u => u.Merchant)
            .FirstOrDefaultAsync(u => u.Id == userId, ct)
            ?? throw new InvalidOperationException("المستخدم غير موجود");

        return Map(user);
    }

    public async Task<AuthResponse> UpdateAdminAsync(Guid userId, UpdateAdminProfileRequest request, CancellationToken ct = default)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct)
            ?? throw new InvalidOperationException("المستخدم غير موجود");

        if (user.Role != UserRole.Admin)
            throw new UnauthorizedAccessException("غير مصرح");

        PasswordRules.ValidateRequired(request.FullName, "الاسم الكامل");
        PasswordRules.ValidateEmail(request.Email);

        var email = request.Email.Trim().ToLowerInvariant();
        if (await _db.Users.AnyAsync(u => u.Email == email && u.Id != userId, ct))
            throw new InvalidOperationException("البريد الإلكتروني مستخدم مسبقاً");

        string? phone = null;
        if (!string.IsNullOrWhiteSpace(request.Phone))
            phone = _otp.NormalizePhone(request.Phone);

        user.FullName = request.FullName.Trim();
        user.Email = email;
        user.Phone = phone;
        await _db.SaveChangesAsync(ct);

        return ToAuth(user);
    }

    public async Task<OtpSendResultDto> RequestMerchantChangeAsync(
        Guid userId,
        UpdateMerchantProfileRequest request,
        CancellationToken ct = default)
    {
        var user = await _db.Users
            .Include(u => u.Merchant)
            .FirstOrDefaultAsync(u => u.Id == userId, ct)
            ?? throw new InvalidOperationException("المستخدم غير موجود");

        if (user.Role is not (UserRole.MerchantOwner or UserRole.MerchantStaff) || user.Merchant is null)
            throw new UnauthorizedAccessException("غير مصرح");

        PasswordRules.ValidateRequired(request.FullName, "الاسم الكامل");
        PasswordRules.ValidateEmail(request.Email);
        PasswordRules.ValidateRequired(request.BusinessName, "اسم النشاط");

        var email = request.Email.Trim().ToLowerInvariant();
        if (await _db.Users.AnyAsync(u => u.Email == email && u.Id != userId, ct))
            throw new InvalidOperationException("البريد الإلكتروني مستخدم مسبقاً");

        var phoneRaw = !string.IsNullOrWhiteSpace(request.Phone)
            ? request.Phone
            : user.Phone ?? user.Merchant.ContactPhone;
        PasswordRules.ValidateRequired(phoneRaw, "رقم الهاتف");

        var result = await _otp.SendProfileChangeOtpAsync(
            userId,
            email,
            request.FullName.Trim(),
            phoneRaw!,
            request.BusinessName.Trim(),
            string.IsNullOrWhiteSpace(request.BusinessNameAr) ? null : request.BusinessNameAr.Trim(),
            string.IsNullOrWhiteSpace(request.WebsiteUrl) ? null : request.WebsiteUrl.Trim(),
            ct);

        return new OtpSendResultDto(result.ChallengeId, result.MaskedDestination, result.ExpiresInSeconds, result.DevCode, result.Via);
    }

    public async Task<AuthResponse> ConfirmMerchantChangeAsync(
        Guid userId,
        ConfirmProfileOtpRequest request,
        CancellationToken ct = default)
    {
        var pending = await _otp.ConsumeProfileChangeChallengeAsync(request.ChallengeId, request.Code, ct);
        if (pending.UserId != userId)
            throw new UnauthorizedAccessException("رمز التحقق غير مرتبط بهذا الحساب");

        var user = await _db.Users
            .Include(u => u.Merchant)
            .FirstOrDefaultAsync(u => u.Id == userId, ct)
            ?? throw new InvalidOperationException("المستخدم غير موجود");

        if (user.Merchant is null)
            throw new InvalidOperationException("حساب التاجر غير موجود");

        if (await _db.Users.AnyAsync(u => u.Email == pending.Email && u.Id != userId, ct))
            throw new InvalidOperationException("البريد الإلكتروني مستخدم مسبقاً");

        user.FullName = pending.FullName;
        user.Email = pending.Email;
        user.Phone = pending.Phone;

        user.Merchant.BusinessName = pending.BusinessName;
        user.Merchant.BusinessNameAr = pending.BusinessNameAr;
        user.Merchant.ContactEmail = pending.Email;
        user.Merchant.ContactPhone = pending.Phone;
        user.Merchant.WebsiteUrl = pending.WebsiteUrl;

        await _db.SaveChangesAsync(ct);
        await _otp.InvalidateChallengeAsync(request.ChallengeId, ct);

        return ToAuth(user);
    }

    private AuthResponse ToAuth(Domain.Entities.User user)
    {
        var token = _jwt.CreateToken(user.Id, user.Email, user.Role.ToString(), user.MerchantId, user.FullName);
        return new AuthResponse(
            token,
            user.Id,
            user.Email,
            user.FullName,
            user.Role.ToString(),
            user.MerchantId,
            user.Merchant?.Status.ToString());
    }

    private static UserProfileDto Map(Domain.Entities.User user) => new(
        user.Id,
        user.Email,
        user.FullName,
        user.Phone ?? user.Merchant?.ContactPhone,
        user.Role.ToString(),
        user.MerchantId,
        user.Merchant?.Status.ToString(),
        user.Merchant?.BusinessName,
        user.Merchant?.BusinessNameAr,
        user.Merchant?.WebsiteUrl);
}
