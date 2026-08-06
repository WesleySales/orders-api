using OrdersApi.Enums;

namespace OrdersApi.Models;

public class Pedido
{
    public int Id { get; set; }
    public string NomeCliente { get; set; } = string.Empty;
    public DateTime DataCriacao { get; set; } = DateTime.UtcNow;
    public StatusPedido Status { get; set; } = StatusPedido.CRIADO;
    public decimal ValorTotal { get; set; }
    
    public List<ItemPedido> Itens { get; set; } = new();

    public void CalcularTotal()
    {
        ValorTotal = Itens.Sum(i => i.Quantidade * i.PrecoUnitario);
    }
}