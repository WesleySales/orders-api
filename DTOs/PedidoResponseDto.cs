
using OrdersApi.Enums;

namespace OrdersApi.DTOs
{
    public class PedidoResponseDto
{
    public int Id { get; set; }
    public string NomeCliente { get; set; } = string.Empty;
    public DateTime DataCriacao { get; set; }
    public decimal ValorTotal { get; set; }

    public string StatusPedido { get; set;}
    public List<ItemPedidoResponseDto> Itens { get; set; } = new();
}
}