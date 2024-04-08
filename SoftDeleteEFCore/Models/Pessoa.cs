namespace SoftDeleteEFCore.Models
{
    public class Pessoa : ISoftDeletable
    {
        private DateTime? deletedOnUtc;

        public bool IsDeleted { get; set; }
        public DateTime? DeletedOnUtc { get => deletedOnUtc; set => deletedOnUtc = IsDeleted ? DateTime.UtcNow : null; }
        public Guid Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string Sobrenome { get; set; } = string.Empty;
    }
}
