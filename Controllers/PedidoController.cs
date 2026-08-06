using System.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using Dapper;
using OrdersApi.Data;
using OrdersApi.DTOs;
using OrdersApi.Models;
using OrdersApi.Enums;
using OrdersApi.Services;
using OrdersApi.Events;
using OrdersApi.Exceptions;

namespace OrdersApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PedidoController : ControllerBase
{
    private readonly AppDbContext _appDbContext;
    private readonly IConfiguration _configuration;
    private readonly IRabbitMqProducer _rabbitMqProducer;

    public PedidoController(AppDbContext appDbContext, IConfiguration configuration, IRabbitMqProducer rabbitMqProducer)
    {
        _appDbContext = appDbContext;
        _configuration = configuration;
        _rabbitMqProducer = rabbitMqProducer;
    }


    /// <summary>
    /// Criar um novo pedido
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<PedidoResponseDto>> CriarPedido([FromBody] PedidoRequestDto data)
    {
        if (data.Itens == null || !data.Itens.Any())
        {
            return BadRequest("O pedido deve conter pelo menos um item.");
        }

        // cria um novo pedido com os dados validados pelo dto
        var pedido = new Pedido
        {
            NomeCliente = data.NomeCliente,
            DataCriacao = DateTime.UtcNow,
            Itens = data.Itens.Select(itemDto => new ItemPedido
            {
                ProdutoId = itemDto.ProdutoId,
                NomeProduto = itemDto.NomeProduto,
                Quantidade = itemDto.Quantidade,
                PrecoUnitario = itemDto.PrecoUnitario
            }).ToList()
        };

        pedido.CalcularTotal();

        // salva o pedido no banco
        _appDbContext.Pedidos.Add(pedido);
        await _appDbContext.SaveChangesAsync();

        var evento = new PedidoCriadoEvent
        {
            PedidoId = pedido.Id,
            NomeCliente = pedido.NomeCliente,
            ValorTotal = pedido.ValorTotal,
            DataCriacao = pedido.DataCriacao
        };

        // adiciona o pedido a fila no RabbitMq
        await _rabbitMqProducer.PublicarEventoAsync("PedidoCriado", evento);

        var responseDto = new PedidoResponseDto
        {
            Id = pedido.Id,
            NomeCliente = pedido.NomeCliente,
            DataCriacao = pedido.DataCriacao,
            ValorTotal = pedido.ValorTotal,
            StatusPedido = pedido.Status.ToString(),
            Itens = pedido.Itens.Select(i => new ItemPedidoResponseDto
            {
                Id = i.Id,
                ProdutoId = i.ProdutoId,
                NomeProduto = i.NomeProduto,
                Quantidade = i.Quantidade,
                PrecoUnitario = i.PrecoUnitario
            }).ToList()
        };

        return CreatedAtAction(nameof(ObterPorId), new { id = pedido.Id }, responseDto);
    }

    /// <summary>
    /// Buscar pedido por ID
    /// </summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<PedidoResponseDto>> ObterPorId(int id)
    {
        var connectionString = _configuration.GetConnectionString("DefaultConnection");

        using IDbConnection db = new SqlConnection(connectionString);

        // usa o Dapper pra fazer query nativa e otimizar a busca
        var sql = @"
            SELECT 
                p.Id, 
                p.NomeCliente, 
                p.DataCriacao, 
                p.ValorTotal, 
                p.Status AS StatusPedido,
                i.Id, 
                i.ProdutoId, 
                i.NomeProduto, 
                i.Quantidade, 
                i.PrecoUnitario
            FROM Pedidos p
            LEFT JOIN Itens i ON p.Id = i.PedidoId
            WHERE p.Id = @Id";

        PedidoResponseDto? pedidoDto = null;

        // executa a query e retorna o pedido
        await db.QueryAsync<PedidoResponseDto, ItemPedidoResponseDto, PedidoResponseDto>(
            sql,
            (pedido, item) =>
            {
                if (pedidoDto == null)
                {
                    pedidoDto = pedido;
                    pedidoDto.Itens = new List<ItemPedidoResponseDto>();
                }

                if (item != null)
                {
                    pedidoDto.Itens.Add(item);
                }

                return pedidoDto;
            },
            new { Id = id },
            splitOn: "Id"
        );

        if (pedidoDto == null)
        {
            return NotFound(new { mensagem = $"Pedido com ID {id} não encontrado." });
        }

        // Muda pro status em texto
        pedidoDto.StatusPedido = ((StatusPedido)int.Parse(pedidoDto.StatusPedido)).ToString();

        return Ok(pedidoDto);
    }


    /// <summary>
    /// Listar pedidos paginados com informações resumidas
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<PagedResultDto<PedidoResumoResponseDto>>> ObterTodos(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 10;

        var query = _appDbContext.Pedidos.AsNoTracking();

        // busca o total de pedidos 
        var totalItems = await query.CountAsync();

        var pedidos = await query
            .OrderByDescending(p => p.DataCriacao) // Mais recentes primeiro
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new PedidoResumoResponseDto
            {
                Id = p.Id,
                NomeCliente = p.NomeCliente,
                DataCriacao = p.DataCriacao,
                ValorTotal = p.ValorTotal,
                StatusPedido = p.Status.ToString(), // altera para exibir o status em texto
                QuantidadeItens = p.Itens.Count // Informação resumida agregada
            })
            .ToListAsync();

        var result = new PagedResultDto<PedidoResumoResponseDto>(pedidos, page, pageSize, totalItems);

        return Ok(result);
    }



    /// <summary>
    /// Cancelar um pedido existente
    /// </summary>
    /// <param name="id">ID do pedido a ser cancelado</param>
    [HttpPut("{id:int}/cancelar")]
    public async Task<IActionResult> CancelarPedido(int id)
    {
        var pedido = await _appDbContext.Pedidos.FindAsync(id);

        if (pedido == null)
        {
            throw new PedidoNaoEncontradoException(id);
        }

        if (pedido.Status == StatusPedido.CANCELADO)
        {
            throw new RegraDeNegocioException("Este pedido já se encontra cancelado.");
        }

        pedido.Status = StatusPedido.CANCELADO;
        await _appDbContext.SaveChangesAsync();

        var eventoCancelamento = new PedidoCanceladoEvent
        {
            PedidoId = pedido.Id,
            DataCancelamento = DateTime.UtcNow
        };

        await _rabbitMqProducer.PublicarEventoAsync("PedidoCancelado", eventoCancelamento);

        return Ok(new
        {
            mensagem = "Pedido cancelado com sucesso.",
            pedidoId = pedido.Id,
            status = pedido.Status.ToString()
        });
    }
}