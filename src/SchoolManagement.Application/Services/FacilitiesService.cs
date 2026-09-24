using SchoolManagement.Application.Authorization;
using SchoolManagement.Application.Common;
using SchoolManagement.Application.DTOs;
using SchoolManagement.Application.Interfaces;
using SchoolManagement.Domain.Entities;
using SchoolManagement.Domain.Enums;
using SchoolManagement.Shared;

namespace SchoolManagement.Application.Services;

public interface IFacilitiesService
{
    Task<Result<NoticeDto>> CreateNoticeAsync(CreateNoticeRequest request, CancellationToken ct = default);
    Task<List<NoticeDto>> GetActiveNoticesAsync(CancellationToken ct = default);

    Task<Result<DocumentDto>> RegisterDocumentAsync(UploadDocumentRequest request, CancellationToken ct = default);
    Task<List<DocumentDto>> GetDocumentsForStudentAsync(int studentId, CancellationToken ct = default);

    Task<Result<BookDto>> AddBookAsync(CreateBookRequest request, CancellationToken ct = default);
    Task<Result<LibraryLoanDto>> BorrowBookAsync(BorrowBookRequest request, CancellationToken ct = default);
    Task<Result> ReturnBookAsync(int loanId, CancellationToken ct = default);
    Task<List<LibraryLoanDto>> GetOverdueLoansAsync(CancellationToken ct = default);

    Task<Result<AssetDto>> AddAssetAsync(CreateAssetRequest request, CancellationToken ct = default);
    Task<List<AssetDto>> GetAllAssetsAsync(CancellationToken ct = default);
}

public class FacilitiesService : IFacilitiesService
{
    private readonly IUnitOfWork _uow;
    private readonly IAuthorizationService _authz;
    private readonly ICurrentUserContext _currentUser;
    private readonly IAuditLogger _audit;

    public FacilitiesService(IUnitOfWork uow, IAuthorizationService authz, ICurrentUserContext currentUser, IAuditLogger audit)
    {
        _uow = uow; _authz = authz; _currentUser = currentUser; _audit = audit;
    }

    public async Task<Result<NoticeDto>> CreateNoticeAsync(CreateNoticeRequest request, CancellationToken ct = default)
    {
        _authz.EnsurePermission(Permission.ManageNotices);
        if (string.IsNullOrWhiteSpace(request.Title)) return Result.Failure<NoticeDto>("عنوان اطلاعیه الزامی است.");

        var notice = new Notice
        {
            Title = request.Title, Body = request.Body, Audience = request.Audience,
            TargetSchoolClassId = request.TargetSchoolClassId, PublishDate = DateTime.UtcNow,
            ExpiryDate = request.ExpiryDate, CreatedByUserId = _currentUser.UserId
        };
        await _uow.Repository<Notice>().AddAsync(notice, ct);
        await _uow.SaveChangesAsync(ct);
        return Result.Success(ToDto(notice));
    }

    public async Task<List<NoticeDto>> GetActiveNoticesAsync(CancellationToken ct = default)
    {
        _authz.EnsurePermission(Permission.ViewNotices);
        var now = DateTime.UtcNow;
        var notices = await _uow.Repository<Notice>().FindAsync(n => !n.ExpiryDate.HasValue || n.ExpiryDate >= now, ct);
        return notices.OrderByDescending(n => n.PublishDate).Select(ToDto).ToList();
    }

    public async Task<Result<DocumentDto>> RegisterDocumentAsync(UploadDocumentRequest request, CancellationToken ct = default)
    {
        _authz.EnsurePermission(Permission.ManageDocuments);
        if (string.IsNullOrWhiteSpace(request.FilePath)) return Result.Failure<DocumentDto>("مسیر فایل الزامی است.");

        var doc = new DocumentFile
        {
            Title = request.Title, OwnerType = request.OwnerType, StudentId = request.StudentId, TeacherId = request.TeacherId,
            FilePath = request.FilePath, Category = request.Category, UploadedAtUtc = DateTime.UtcNow,
            UploadedByUserId = _currentUser.UserId ?? 0, CreatedByUserId = _currentUser.UserId
        };
        await _uow.Repository<DocumentFile>().AddAsync(doc, ct);
        await _uow.SaveChangesAsync(ct);
        return Result.Success(new DocumentDto(doc.Id, doc.Title, doc.OwnerType, doc.FilePath, doc.Category, doc.UploadedAtUtc));
    }

    public async Task<List<DocumentDto>> GetDocumentsForStudentAsync(int studentId, CancellationToken ct = default)
    {
        _authz.EnsurePermission(Permission.ViewDocuments);
        var docs = await _uow.Repository<DocumentFile>().FindAsync(d => d.StudentId == studentId, ct);
        return docs.OrderByDescending(d => d.UploadedAtUtc).Select(d => new DocumentDto(d.Id, d.Title, d.OwnerType, d.FilePath, d.Category, d.UploadedAtUtc)).ToList();
    }

    public async Task<Result<BookDto>> AddBookAsync(CreateBookRequest request, CancellationToken ct = default)
    {
        _authz.EnsurePermission(Permission.ManageLibrary);
        if (request.TotalCopies < 0) return Result.Failure<BookDto>("تعداد نسخه‌ها نمی‌تواند منفی باشد.");

        var book = new Book
        {
            Title = request.Title, Author = request.Author, Isbn = request.Isbn, Category = request.Category,
            TotalCopies = request.TotalCopies, AvailableCopies = request.TotalCopies, CreatedByUserId = _currentUser.UserId
        };
        await _uow.Repository<Book>().AddAsync(book, ct);
        await _uow.SaveChangesAsync(ct);
        return Result.Success(new BookDto(book.Id, book.Title, book.Author, book.Category, book.TotalCopies, book.AvailableCopies));
    }

    public async Task<Result<LibraryLoanDto>> BorrowBookAsync(BorrowBookRequest request, CancellationToken ct = default)
    {
        _authz.EnsurePermission(Permission.ManageLibrary);
        var book = await _uow.Repository<Book>().GetByIdAsync(request.BookId, ct);
        if (book is null) return Result.Failure<LibraryLoanDto>("کتاب یافت نشد.");
        if (book.AvailableCopies <= 0) return Result.Failure<LibraryLoanDto>("هیچ نسخه‌ای از این کتاب در دسترس نیست.");
        if (request.StudentId is null && request.TeacherId is null) return Result.Failure<LibraryLoanDto>("باید یک شاگرد یا استاد امانت‌گیرنده انتخاب شود.");

        var loan = new LibraryLoan
        {
            BookId = request.BookId, StudentId = request.StudentId, TeacherId = request.TeacherId,
            BorrowedDate = DateTime.UtcNow, DueDate = request.DueDate, Status = LibraryLoanStatus.Borrowed,
            CreatedByUserId = _currentUser.UserId
        };
        await _uow.Repository<LibraryLoan>().AddAsync(loan, ct);

        book.AvailableCopies -= 1;
        _uow.Repository<Book>().Update(book);
        await _uow.SaveChangesAsync(ct);

        string borrowerName = "";
        if (request.StudentId.HasValue)
        {
            var s = await _uow.Repository<Student>().GetByIdAsync(request.StudentId.Value, ct);
            borrowerName = s?.FullName ?? "";
        }
        else if (request.TeacherId.HasValue)
        {
            var t = await _uow.Repository<Teacher>().GetByIdAsync(request.TeacherId.Value, ct);
            borrowerName = t?.FullName ?? "";
        }
        return Result.Success(new LibraryLoanDto(loan.Id, book.Title, borrowerName, loan.BorrowedDate, loan.DueDate, loan.ReturnedDate, loan.Status));
    }

    public async Task<Result> ReturnBookAsync(int loanId, CancellationToken ct = default)
    {
        _authz.EnsurePermission(Permission.ManageLibrary);
        var loan = await _uow.Repository<LibraryLoan>().GetByIdAsync(loanId, ct);
        if (loan is null) return Result.Failure("رکورد امانت یافت نشد.");
        if (loan.Status == LibraryLoanStatus.Returned) return Result.Failure("این کتاب قبلاً برگشت داده شده است.");

        loan.Status = LibraryLoanStatus.Returned;
        loan.ReturnedDate = DateTime.UtcNow;
        loan.ModifiedByUserId = _currentUser.UserId;
        _uow.Repository<LibraryLoan>().Update(loan);

        var book = await _uow.Repository<Book>().GetByIdAsync(loan.BookId, ct);
        if (book is not null)
        {
            book.AvailableCopies += 1;
            _uow.Repository<Book>().Update(book);
        }
        await _uow.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<List<LibraryLoanDto>> GetOverdueLoansAsync(CancellationToken ct = default)
    {
        _authz.EnsurePermission(Permission.ViewLibrary);
        var now = DateTime.UtcNow;
        var loans = await _uow.Repository<LibraryLoan>().FindAsync(l => l.Status == LibraryLoanStatus.Borrowed && l.DueDate < now, ct);
        var books = (await _uow.Repository<Book>().GetAllAsync(ct)).ToDictionary(b => b.Id, b => b.Title);
        var students = (await _uow.Repository<Student>().GetAllAsync(ct)).ToDictionary(s => s.Id, s => s.FullName);
        var teachers = (await _uow.Repository<Teacher>().GetAllAsync(ct)).ToDictionary(t => t.Id, t => t.FullName);

        return loans.Select(l => new LibraryLoanDto(
            l.Id, books.GetValueOrDefault(l.BookId, ""),
            l.StudentId.HasValue ? students.GetValueOrDefault(l.StudentId.Value, "") : teachers.GetValueOrDefault(l.TeacherId ?? 0, ""),
            l.BorrowedDate, l.DueDate, l.ReturnedDate, LibraryLoanStatus.Overdue))
            .ToList();
    }

    public async Task<Result<AssetDto>> AddAssetAsync(CreateAssetRequest request, CancellationToken ct = default)
    {
        _authz.EnsurePermission(Permission.ManageAssets);
        if (string.IsNullOrWhiteSpace(request.Name)) return Result.Failure<AssetDto>("نام اموال الزامی است.");

        var asset = new Asset
        {
            Name = request.Name, Category = request.Category, SerialNumber = request.SerialNumber, Location = request.Location,
            PurchaseDate = request.PurchaseDate, PurchaseValue = request.PurchaseValue, Condition = AssetCondition.New,
            CreatedByUserId = _currentUser.UserId
        };
        await _uow.Repository<Asset>().AddAsync(asset, ct);
        await _uow.SaveChangesAsync(ct);
        return Result.Success(new AssetDto(asset.Id, asset.Name, asset.Category, asset.Location, asset.Condition, asset.PurchaseValue));
    }

    public async Task<List<AssetDto>> GetAllAssetsAsync(CancellationToken ct = default)
    {
        _authz.EnsurePermission(Permission.ViewAssets);
        var assets = await _uow.Repository<Asset>().GetAllAsync(ct);
        return assets.OrderBy(a => a.Name).Select(a => new AssetDto(a.Id, a.Name, a.Category, a.Location, a.Condition, a.PurchaseValue)).ToList();
    }

    private static NoticeDto ToDto(Notice n) => new(n.Id, n.Title, n.Body, n.Audience, n.PublishDate, n.ExpiryDate, n.ExpiryDate.HasValue && n.ExpiryDate < DateTime.UtcNow);
}
