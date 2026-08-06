using System.ComponentModel.DataAnnotations;

namespace OrdersApi.DTOs;

public class PedidoRequestDto
{
    /// <example>Joao</example>
    [Required(ErrorMessage = "O nome do cliente é obrigatório.")]
    [StringLength(100, MinimumLength = 3, ErrorMessage = "O nome do cliente deve ter entre 3 e 100 caracteres.")]
    public string NomeCliente { get; set; } = string.Empty;

    [Required(ErrorMessage = "O pedido precisa ter pelo menos um item.")]
    [MinLength(1, ErrorMessage = "A lista de itens não pode estar vazia.")]
    public List<ItemPedidoRequestDto> Itens { get; set; } = new();
}