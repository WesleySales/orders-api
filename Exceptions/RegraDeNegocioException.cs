namespace OrdersApi.Exceptions;

public class RegraDeNegocioException : Exception
{
    public RegraDeNegocioException(string mensagem) : base(mensagem) { }
}