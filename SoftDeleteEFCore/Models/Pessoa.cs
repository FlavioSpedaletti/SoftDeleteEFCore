namespace SoftDeleteEFCore.Models
{
    public class Pessoa
    {
        public Guid Id { get; set; }
        public string Nome { get; set; } = string.Empty;

        public string Sobrenome { get; set; } = string.Empty;
    }
}
