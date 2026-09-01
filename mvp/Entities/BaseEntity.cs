namespace mvp.Entities
{
    public class BaseEntity
    {
        public Guid Id { get; protected set; }
        public DateTime CreatedAt { get; protected set; }
        public DateTime UpdatedAt { get; protected set; }
        public DateTime? RemovedAt { get; protected set; }

        public BaseEntity()
        {
            Id = Guid.NewGuid();
            CreatedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
        }

        public void UpdateTimestamps()
        {
            UpdatedAt = DateTime.UtcNow;
        }

        public void SoftDelete()
        {
            RemovedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
        }

        public void Restore()
        {
            RemovedAt = null;
            UpdatedAt = DateTime.UtcNow;
        }
    }
}
