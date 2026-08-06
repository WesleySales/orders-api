namespace OrdersApi.Events;

public class PedidoCriadoEvent
{
    public int PedidoId { get; set; }
    public string NomeCliente { get; set; } = string.Empty;
    public decimal ValorTotal { get; set; }
    public DateTime DataCriacao { get; set; }
}