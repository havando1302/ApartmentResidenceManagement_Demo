using System;
using System.Text.RegularExpressions;

namespace ApartmentResidenceManagement.Application.Validators;

public static class InputValidator
{
    private const RegexOptions ValidationOptions = RegexOptions.CultureInvariant | RegexOptions.NonBacktracking;
    private static readonly Regex PhoneRegex = new(@"^0(?:3|5|7|8|9)[0-9]{8}$", ValidationOptions);
    private static readonly Regex IdentityCardRegex = new(@"^(?:[0-9]{9}|[0-9]{12})$", ValidationOptions);
    private static readonly Regex LicensePlateRegex = new(@"^(?:[0-9]{2}[A-Z0-9]{1,2}-[0-9]{4,5}|[0-9]{2}[A-Z]{1,2}[0-9]{4,5})$", ValidationOptions); // Ví dụ: 29A-12345 hoặc 29A12345

    public static bool ValidatePhone(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone)) return false;
        return PhoneRegex.IsMatch(phone);
    }

    public static bool ValidateIdentityCard(string? idCard)
    {
        if (string.IsNullOrWhiteSpace(idCard)) return false;
        return IdentityCardRegex.IsMatch(idCard);
    }

    public static bool ValidateLicensePlate(string? licensePlate)
    {
        if (string.IsNullOrWhiteSpace(licensePlate)) return false;
        // Chuẩn hóa biển số xe bằng cách viết hoa và bỏ khoảng trắng dư thừa
        var cleaned = licensePlate.Replace(" ", "").ToUpper();
        return LicensePlateRegex.IsMatch(cleaned);
    }

    public static bool ValidateDateOfBirth(DateTime dob)
    {
        if (dob.Date > DateTime.Today) return false;
        if (dob.Date < DateTime.Today.AddYears(-120)) return false; // Không sống quá 120 tuổi
        return true;
    }
}
