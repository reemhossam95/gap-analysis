using ContentGapAnalyzer.Domain.Common;

namespace ContentGapAnalyzer.Domain.Entities;

public class User : BaseEntity
{
    // الأعمدة الخاصة فقط بجدول User
    public string UserName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public bool EmailVerified { get; set; }
    
    public string Role { get; set; } = "User";
    public DateTime? DateOfBirth { get; set; }
    public string? ProfileImageUrl { get; set; }
    public string? PhoneNumber { get; set; }
    
    public string AuthProvider { get; set; } = "local";
    public string? GoogleId { get; set; }
    
    public bool IsActive { get; set; } = true;
    public DateTime? LastLoginAt { get; set; }
    
    public int Credits { get; set; } = 0; 
    
    // ملاحظة: لا تضعي هنا CreatedAt أو UpdatedAt أو IsDeleted
    // لأنها موجودة بالفعل في BaseEntity الذي يرث منه هذا الكلاس
}