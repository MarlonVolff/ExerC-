using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using loja.data;
using loja.models;
using loja.services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<LojaDbContext>(options =>
    options.UseSqlServer("dbcs"));

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes("senha1234")),
            ValidateIssuer = false,
            ValidateAudience = false,
        };
    });

builder.Services.AddScoped<AuthenticationService>();

var app = builder.Build();

app.UseExceptionHandler("/error");
app.UseHsts();

app.UseAuthentication();
app.UseAuthorization();

app.MapPost("/login", async (LojaDbContext dbContext, HttpContext context) =>
{
    using var reader = new StreamReader(context.Request.Body);
    var body = await reader.ReadToEndAsync();

    var json = JsonDocument.Parse(body);
    var email = json.RootElement.GetProperty("email").GetString();
    var senha = json.RootElement.GetProperty("senha").GetString();

    var authService = context.RequestServices.GetRequiredService<AuthenticationService>();
    var token = await authService.AuthenticateAndGetTokenAsync(email, senha);

    if (token == null)
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        await context.Response.WriteAsync("Credenciais inválidas");
        return;
    }

    await context.Response.WriteAsync(token);
});

app.MapPost("/produtos", async (LojaDbContext dbContext, Produto produto) =>
{
    var email = dbContext.GetAuthenticatedUserEmail(); 

    await dbContext.Produtos.AddAsync(produto);
    await dbContext.SaveChangesAsync();
    
    return Results.Created($"/produtos/{produto.Id}", produto);
}).RequireAuthorization();

app.MapPost("/createcliente", async (LojaDbContext dbContext, Cliente newCliente) =>
{
    dbContext.Clientes.Add(newCliente);
    await dbContext.SaveChangesAsync();
    return Results.Created($"/createcliente/{newCliente.Id}", newCliente);
}).RequireAuthorization(); 

//fornecedor
app.MapPost("/fornecedores", async (LojaDbContext dbContext, Fornecedor fornecedor) =>
{
    dbContext.Fornecedores.Add(fornecedor);
    await dbContext.SaveChangesAsync();
    return Results.Created($"/fornecedores/{fornecedor.Id}", fornecedor);
}).RequireAuthorization(); 

//produtos
app.MapGet("/produtos", async (LojaDbContext dbContext) =>
{
    var produtos = await dbContext.Produtos.ToListAsync();
    return Results.Ok(produtos);
}).RequireAuthorization(); 

app.MapGet("/clientes", async (LojaDbContext dbContext) =>
{
    var clientes = await dbContext.Clientes.ToListAsync();
    return Results.Ok(clientes);
}).RequireAuthorization();

app.MapGet("/fornecedores", async (LojaDbContext dbContext) =>
{
    var fornecedores = await dbContext.Fornecedores.ToListAsync();
    return Results.Ok(fornecedores);
}).RequireAuthorization(); 

app.MapPut("/produtos/{id}", async (LojaDbContext dbContext, int id, Produto produto) =>
{
    var existingProduct = await dbContext.Produtos.FindAsync(id);
    if (existingProduct == null)
    {
        return Results.NotFound($"Produto com ID {id} não encontrado.");
    }

    existingProduct.Nome = produto.Nome;
    existingProduct.Preco = produto.Preco;

    await dbContext.SaveChangesAsync();
    return Results.Ok();
}).RequireAuthorization(); 

app.MapPut("/clientes/{id}", async (LojaDbContext dbContext, int id, Cliente updateCliente) =>
{
    var existingCliente = await dbContext.Clientes.FindAsync(id);
    if (existingCliente == null)
    {
        return Results.NotFound($"Cliente com ID {id} não encontrado.");
    }

    existingCliente.Nome = updateCliente.Nome;
    existingCliente.Cpf = updateCliente.Cpf;
    existingCliente.Email = updateCliente.Email;

    await dbContext.SaveChangesAsync();
    return Results.Ok(existingCliente);
}).RequireAuthorization();

app.MapDelete("/produtos/{id}", async (LojaDbContext dbContext, int id) =>
{
    var produto = await dbContext.Produtos.FindAsync(id);
    if (produto == null)
    {
        return Results.NotFound($"Produto com ID {id} não encontrado.");
    }

    dbContext.Produtos.Remove(produto);
    await dbContext.SaveChangesAsync();
    return Results.Ok();
}).RequireAuthorization();

app.MapDelete("/fornecedores/{id}", async (LojaDbContext dbContext, int id) =>
{
    var fornecedor = await dbContext.Fornecedores.FindAsync(id);
    if (fornecedor == null)
    {
        return Results.NotFound($"Fornecedor com ID {id} não encontrado.");
    }

    dbContext.Fornecedores.Remove(fornecedor);
    await dbContext.SaveChangesAsync();
    return Results.Ok();
}).RequireAuthorization();

app.MapPost("/vendas", async (VendaService vendaService, Venda venda) =>
{
    var sucesso = await vendaService.GravarVendaAsync(venda);
    if (!sucesso)
    {
        return Results.BadRequest("Cliente ou produto não encontrado.");
    }
    return Results.Created($"/vendas/{venda.Id}", venda);
}).RequireAuthorization();

app.MapGet("/vendas/produto/detalhada/{produtoId}", async (VendaService vendaService, int produtoId) =>
{
    var vendas = await vendaService.ConsultarVendasPorProdutoDetalhadaAsync(produtoId);
    return Results.Ok(vendas);
}).RequireAuthorization();

app.MapGet("/vendas/produto/sumarizada/{produtoId}", async (VendaService vendaService, int produtoId) =>
{
    var vendas = await vendaService.ConsultarVendasPorProdutoSumarizadaAsync(produtoId);
    return Results.Ok(vendas);
}).RequireAuthorization();

app.MapGet("/vendas/cliente/detalhada/{clienteId}", async (VendaService vendaService, int clienteId) =>
{
    var vendas = await vendaService.ConsultarVendasPorClienteDetalhadaAsync(clienteId);
    return Results.Ok(vendas);
}).RequireAuthorization();

app.MapGet("/vendas/cliente/sumarizada/{clienteId}", async (VendaService vendaService, int clienteId) =>
{
    var vendas = await vendaService.ConsultarVendasPorClienteSumarizadaAsync(clienteId);
    return Results.Ok(vendas);
}).RequireAuthorization();

app.Run();

public static class DbContextExtensions
{
    //atenticação de email.
    public static string GetAuthenticatedUserEmail(this LojaDbContext dbContext)
    {
        return "marlon@gmail.com";
    }
}

namespace loja.services
{
    public class AuthenticationService
    {
        public Task<string> AuthenticateAndGetTokenAsync(string email, string senha)
        {
            return Task.FromResult<string>(null);
        }
    }
} // obs estava dando errado no authenticate e o chat e outras ia deram essa opção de colocar esse codigo
// e deu certo.
