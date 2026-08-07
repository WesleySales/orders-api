namespace OrdersApi.Models;

public class ItemPedido
{
    public int Id { get; set; }
    public int ProdutoId { get; set; }      
    public string NomeProduto { get; set; } = string.Empty; 
    public int Quantidade { get; set; }     
    public decimal PrecoUnitario { get; set; } 
}