namespace FreeBudget.SharedKernel.Domain;

public interface IAuditableEntity
{
    DateTime CreatedAt { get; set; }
    DateTime? ModifiedAt { get; set; }
}
