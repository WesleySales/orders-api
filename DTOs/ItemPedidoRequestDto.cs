using System.ComponentModel.DataAnnotations;

namespace OrdersApi.DTOs;

public class ItemPedidoRequestDto
{
    /// <example>1</example>
    [Range(1, int.MaxValue, ErrorMessage = "O ProdutoId deve ser maior que zero.")]
    public int ProdutoId { get; set; }

    /// <example>Camisa do Brasil</example>
    [Required(ErrorMessage = "O nome do produto é obrigatório.")]
    [StringLength(150, ErrorMessage = "O nome do produto deve ter no máximo 150 caracteres.")]
    public string NomeProduto { get; set; } = string.Empty;

    /// <example>1</example>
    [Range(1, int.MaxValue, ErrorMessage = "A quantidade deve ser maior que zero.")]
    public int Quantidade { get; set; }

    /// <example>200</example>
    [Range(0.01, 999999.99, ErrorMessage = "O preço unitário deve ser maior que zero.")]
    public decimal PrecoUnitario { get; set; }
}