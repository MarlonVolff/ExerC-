using System.Linq;
using System.Threading.Tasks;
using loja.data;
using loja.models;
using Microsoft.EntityFrameworkCore;

namespace loja.services
{
    public class VendaService
    {
        private readonly LojaDbContext _dbContext;

        public VendaService(LojaDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<bool> GravarVendaAsync(Venda venda)
        {
            var cliente = await _dbContext.Clientes.FindAsync(venda.ClienteId);
            var produto = await _dbContext.Produtos.FindAsync(venda.ProdutoId);

            if (cliente == null || produto == null)
            {
                return false;
            }

            _dbContext.Vendas.Add(venda);
            await _dbContext.SaveChangesAsync();
            return true;
        }

        public async Task<object> ConsultarVendasPorProdutoDetalhadaAsync(int produtoId)
        {
            return await _dbContext.Vendas
                .Where(v => v.ProdutoId == produtoId)
                .Select(v => new
                {
                    v.Produto.Nome,
                    v.DataVenda,
                    v.Id,
                    ClienteNome = v.Cliente.Nome,
                    v.Quantidade,
                    v.PrecoUnitario
                })
                .ToListAsync();
        }

        public async Task<object> ConsultarVendasPorProdutoSumarizadaAsync(int produtoId)
        {
            return await _dbContext.Vendas
                .Where(v => v.ProdutoId == produtoId)
                .GroupBy(v => v.Produto.Nome)
                .Select(g => new
                {
                    ProdutoNome = g.Key,
                    QuantidadeTotal = g.Sum(v => v.Quantidade),
                    PrecoTotal = g.Sum(v => v.Quantidade * v.PrecoUnitario)
                })
                .ToListAsync();
        }

        public async Task<object> ConsultarVendasPorClienteDetalhadaAsync(int clienteId)
        {
            return await _dbContext.Vendas
                .Where(v => v.ClienteId == clienteId)
                .Select(v => new
                {
                    v.Produto.Nome,
                    v.DataVenda,
                    v.Id,
                    v.Quantidade,
                    v.PrecoUnitario
                })
                .ToListAsync();
        }

        public async Task<object> ConsultarVendasPorClienteSumarizadaAsync(int clienteId)
        {
            return await _dbContext.Vendas
                .Where(v => v.ClienteId == clienteId)
                .GroupBy(v => v.Cliente.Nome)
                .Select(g => new
                {
                    ClienteNome = g.Key,
                    QuantidadeTotal = g.Sum(v => v.Quantidade),
                    PrecoTotal = g.Sum(v => v.Quantidade * v.PrecoUnitario)
                })
                .ToListAsync();
        }
    }
}
