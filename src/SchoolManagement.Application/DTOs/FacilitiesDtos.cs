using SchoolManagement.Domain.Enums;

namespace SchoolManagement.Application.DTOs;

public record CreateNoticeRequest(string Title, string Body, NoticeAudience Audience, int? TargetSchoolClassId, DateTime? ExpiryDate);
public record NoticeDto(int Id, string Title, string Body, NoticeAudience Audience, DateTime PublishDate, DateTime? ExpiryDate, bool IsExpired);

public record UploadDocumentRequest(string Title, DocumentOwnerType OwnerType, int? StudentId, int? TeacherId, string FilePath, string? Category);
public record DocumentDto(int Id, string Title, DocumentOwnerType OwnerType, string FilePath, string? Category, DateTime UploadedAtUtc);

public record CreateBookRequest(string Title, string? Author, string? Isbn, string? Category, int TotalCopies);
public record BookDto(int Id, string Title, string? Author, string? Category, int TotalCopies, int AvailableCopies);
public record BorrowBookRequest(int BookId, int? StudentId, int? TeacherId, DateTime DueDate);
public record LibraryLoanDto(int Id, string BookTitle, string BorrowerName, DateTime BorrowedDate, DateTime DueDate, DateTime? ReturnedDate, LibraryLoanStatus Status);

public record CreateAssetRequest(string Name, string? Category, string? SerialNumber, string? Location, DateTime? PurchaseDate, decimal? PurchaseValue);
public record AssetDto(int Id, string Name, string? Category, string? Location, AssetCondition Condition, decimal? PurchaseValue);
