namespace OrdersApi.Events;

public class PedidoCanceladoEvent
{
    public int PedidoId { get; set; }
    public DateTime DataCancelamento { get; set; }
    public string Motivo { get; set; } = "Solicitado pelo cliente";
}