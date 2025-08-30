namespace FraudDetectorWebApp.Models
{
    public interface ISoftDelete
    {
        bool IsDeleted { get; set; }
        DateTime? DeletedAt { get; set; }
    }
}
