using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Farmacia_site.Data;
using Farmacia_site.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace Farmacia_site.Controllers;

[ApiController]
[Route("api/produtos")]
public sealed class ProdutosApiController(Contexto db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ProdutoDto>>> Listar(CancellationToken ct)
    {
        var produtos = await db.Produtos.AsNoTracking()
            .Where(p => p.Ativo)
            .OrderBy(p => p.Categoria).ThenBy(p => p.Nome)
            .Select(p => new ProdutoDto(p.Id, p.Nome, p.Descricao, p.Categoria,
                p.Ilustracao, p.Selo, p.Preco, p.PrecoAnterior, p.Estoque))
            .ToListAsync(ct);
        return Ok(produtos);
    }
}

[ApiController]
[Route("api/auth")]
public sealed class AuthApiController(
    Contexto db,
    IConfiguration configuration,
    IPasswordHasher<Usuario> passwordHasher) : ControllerBase
{
    [HttpPost("registrar")]
    public async Task<ActionResult<AuthResponse>> Registrar(
        CadastroRequest request, CancellationToken ct)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        if (await db.Usuarios.AnyAsync(x => x.Email.ToLower() == email, ct))
            return Conflict(new { message = "Este e-mail já está cadastrado." });

        var usuario = new Usuario
        {
            Nome = request.Nome.Trim(),
            Email = email,
            Perfil = "Cliente",
            DataCadastro = DateTime.UtcNow
        };
        usuario.SenhaHash = passwordHasher.HashPassword(usuario, request.Senha);
        db.Usuarios.Add(usuario);
        await db.SaveChangesAsync(ct);
        return Ok(CriarResposta(usuario));
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login(
        LoginRequest request, CancellationToken ct)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var usuario = await db.Usuarios.SingleOrDefaultAsync(
            x => x.Email.ToLower() == email, ct);
        if (usuario is null || passwordHasher.VerifyHashedPassword(
                usuario, usuario.SenhaHash, request.Senha) == PasswordVerificationResult.Failed)
            return Unauthorized(new { message = "E-mail ou senha inválidos." });

        return Ok(CriarResposta(usuario));
    }

    private AuthResponse CriarResposta(Usuario usuario)
    {
        var issuer = configuration["Jwt:Issuer"]!;
        var audience = configuration["Jwt:Audience"]!;
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["Jwt:Key"]!));
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, usuario.Id.ToString()),
            new Claim(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
            new Claim(ClaimTypes.Name, usuario.Nome),
            new Claim(ClaimTypes.Email, usuario.Email)
        };
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(issuer, audience, claims,
            expires: DateTime.UtcNow.AddHours(8), signingCredentials: credentials);
        return new AuthResponse(new JwtSecurityTokenHandler().WriteToken(token),
            usuario.Id, usuario.Nome, usuario.Email);
    }
}

[ApiController]
[Authorize]
[Route("api/pedidos")]
public sealed class PedidosApiController(Contexto db) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<PedidoDto>> Criar(
        CriarPedidoRequest request, CancellationToken ct)
    {
        var usuarioClaim = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);
        if (!int.TryParse(usuarioClaim, out var usuarioId)) return Unauthorized();

        var quantidades = request.Itens
            .GroupBy(x => x.ProdutoId)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.Quantidade));
        var ids = quantidades.Keys.ToArray();

        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        var produtos = await db.Produtos
            .Where(p => p.Ativo && ids.Contains(p.Id))
            .ToListAsync(ct);
        if (produtos.Count != ids.Length)
            return BadRequest(new { message = "Um ou mais produtos não estão disponíveis." });

        foreach (var produto in produtos)
        {
            if (produto.Estoque < quantidades[produto.Id])
                return Conflict(new { message = $"Estoque insuficiente para {produto.Nome}." });
        }

        var pedido = new Pedido
        {
            UsuarioId = usuarioId,
            DataPedido = DateTime.UtcNow,
            Status = "Recebido",
            Itens = produtos.Select(p => new ItemPedido
            {
                ProdutoId = p.Id,
                NomeProduto = p.Nome,
                Quantidade = quantidades[p.Id],
                PrecoUnitario = p.Preco
            }).ToList()
        };
        pedido.Total = pedido.Itens.Sum(x => x.PrecoUnitario * x.Quantidade);

        foreach (var produto in produtos)
            produto.Estoque -= quantidades[produto.Id];

        db.Pedidos.Add(pedido);
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);

        return Created($"/api/pedidos/{pedido.Id}", new PedidoDto(
            pedido.Id, pedido.DataPedido, pedido.Status, pedido.Total));
    }
}

public sealed record ProdutoDto(int Id, string Nome, string Descricao,
    string Categoria, string Ilustracao, string Selo, decimal Preco,
    decimal? PrecoAnterior, int Estoque);
public sealed record CadastroRequest(
    [Required, StringLength(100, MinimumLength = 2)] string Nome,
    [Required, EmailAddress, StringLength(180)] string Email,
    [Required, StringLength(100, MinimumLength = 8)] string Senha);
public sealed record LoginRequest(
    [Required, EmailAddress] string Email,
    [Required] string Senha);
public sealed record AuthResponse(string Token, int UsuarioId, string Nome, string Email);
public sealed record CriarPedidoRequest(
    [Required, MinLength(1)] List<ItemPedidoRequest> Itens);
public sealed record ItemPedidoRequest(
    [Range(1, int.MaxValue)] int ProdutoId,
    [Range(1, 99)] int Quantidade);
public sealed record PedidoDto(int Id, DateTime DataPedido, string Status, decimal Total);
