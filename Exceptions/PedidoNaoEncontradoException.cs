namespace OrdersApi.Exceptions;

public class PedidoNaoEncontradoException : Exception
{
    public PedidoNaoEncontradoException(int id) 
        : base($"Pedido com o ID {id} não foi encontrado.") 
    { 
    }
}