namespace loja.models
{
    public class Produto
    {
        public int Id { get; set; }
        public string? Nome { get; set; }
        public double Preco { get; set; }
        public string? Fornecedor { get; set; }
    }
}