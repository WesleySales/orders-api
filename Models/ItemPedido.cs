namespace OrdersApi.Models;

public class ItemPedido
{
    public int Id { get; set; }
    public int ProdutoId { get; set; }      // Vem na requisição (ex: 1)
    public string NomeProduto { get; set; } = string.Empty; // Vem na requisição (ex: "Notebook")
    public int Quantidade { get; set; }     // Vem na requisição
    public decimal PrecoUnitario { get; set; } // Vem na requisição
}