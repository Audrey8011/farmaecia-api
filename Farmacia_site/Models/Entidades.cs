namespace Farmacia_site.Models;

public sealed class Produto
{
    public int Id { get; set; }
    public string Nome { get; set; } = "";
    public string Descricao { get; set; } = "";
    public string Categoria { get; set; } = "";
    public string Ilustracao { get; set; } = "";
    public string Selo { get; set; } = "";
    public decimal Preco { get; set; }
    public decimal? PrecoAnterior { get; set; }
    public int Estoque { get; set; }
    public bool Ativo { get; set; }
    public byte[] RowVersion { get; set; } = [];
    public DateTime CriadoEm { get; set; }
}

public sealed class Usuario
{
    public int Id { get; set; }
    public string Nome { get; set; } = "";
    public string Email { get; set; } = "";
    public string SenhaHash { get; set; } = "";
    public string Perfil { get; set; } = "Cliente";
    public DateTime DataCadastro { get; set; }
}

public sealed class Pedido
{
    public int Id { get; set; }
    public int UsuarioId { get; set; }
    public DateTime DataPedido { get; set; }
    public string Status { get; set; } = "";
    public decimal Total { get; set; }
    public Usuario? Usuario { get; set; }
    public List<ItemPedido> Itens { get; set; } = [];
}

public sealed class ItemPedido
{
    public int Id { get; set; }
    public int PedidoId { get; set; }
    public int ProdutoId { get; set; }
    public string NomeProduto { get; set; } = "";
    public int Quantidade { get; set; }
    public decimal PrecoUnitario { get; set; }
    public Pedido? Pedido { get; set; }
    public Produto? Produto { get; set; }
}
