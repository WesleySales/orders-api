# 📦 Orders API - Sistema de Gestão de Pedidos

Uma Web API em .NET desenvolvida com foco em arquitetura orientada a eventos, utilizando **SQL Server** para persistência de dados e **RabbitMQ** para mensageria assíncrona.

---

## 🛠️ Tecnologias Utilizadas

- **.NET 10 (C#)**: Framework base para o desenvolvimento da API.
- **Entity Framework Core**: ORM para escrita e mapeamento do banco de dados (criação, atualização e paginação de pedidos).
- **Dapper**: Utilizado para consultas otimizadas (busca de pedido por ID, com join direto entre `Pedidos` e `Itens`).
- **SQL Server**: Banco de dados relacional para armazenamento de pedidos.
- **RabbitMQ**: Message Broker para comunicação assíncrona entre serviços.
- **Swagger**: Documentação e exploração interativa da API.
- **Docker & Docker Compose**: Conteinerização do ambiente de infraestrutura.

---

## 🏗️ Arquitetura e Decisões Técnicas

### 1. Mensageria Assíncrona com RabbitMQ
- **Justificativa**: A criação e o cancelamento de pedidos são operações que podem disparar ações secundárias (envio de e-mails, atualização de estoque, faturamento). O uso de filas desacopla o processamento principal da API, garantindo alta disponibilidade e resiliência.
- **Filas Configuradas**:
  - `PedidoCriado`: Publicada ao criar um pedido (`POST /api/pedido`) e processada pelo `PedidoCriadoConsumer`.
  - `PedidoCancelado`: Publicada ao cancelar um pedido (`PUT /api/pedido/{id}/cancelar`) e processada pelo `PedidoCanceladoConsumer`.

### 2. Consumidores via `BackgroundService` (Hosted Services)
- **Justificativa**: Ao invés de dependermos de um worker externo separado no início do desenvolvimento, os consumidores (`PedidoCriadoConsumer` e `PedidoCanceladoConsumer`) rodam em segundo plano na própria aplicação via `BackgroundService` do .NET, simplificando a execução em ambiente local sem perder o desacoplamento das camadas.

### 3. EF Core + Dapper lado a lado
- **Justificativa**: O EF Core é usado para as operações de escrita (criação e cancelamento) e para a listagem paginada, aproveitando o tracking e a composição de queries com LINQ. Já a busca de um pedido específico por ID usa Dapper com SQL nativo (`JOIN` entre `Pedidos` e `Itens`), priorizando performance em uma consulta de leitura simples e bem definida.

### 4. Tratamento Global de Exceções
- **Justificativa**: O `GlobalExceptionHandler` (via `IExceptionHandler`) centraliza o tratamento de erros, convertendo exceções de domínio em respostas padronizadas no formato `ProblemDetails`:
  - `PedidoNaoEncontradoException` → `404 Not Found`
  - `RegraDeNegocioException` → `400 Bad Request`
  - Demais exceções → `500 Internal Server Error` (com detalhes ocultos por segurança)

### 5. Conteinerização com Docker Compose
- **Justificativa**: Garante que qualquer desenvolvedor execute o projeto com exatamente as mesmas versões de dependências (SQL Server 2022 e RabbitMQ com painel de Management), sem necessidade de instalações locais complexas.

---

## 🚀 Como Executar o Projeto

### Pré-requisitos

- [.NET SDK 10](https://dotnet.microsoft.com/download)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) rodando na máquina.
- Ferramenta EF Core CLI instalada (`dotnet tool install --global dotnet-ef`).

### Passo a Passo

#### 1. Clonar o repositório
```bash
git clone https://github.com/SeuUsuario/OrdersApi.git
cd OrdersApi
```

#### 2. Subir os Containers de Infraestrutura (SQL Server & RabbitMQ)
Certifique-se de que o Docker Desktop esteja aberto e execute, na raiz do projeto (onde está o `docker-compose.yml`):
```bash
docker compose up -d
```

Aguarde alguns segundos até o SQL Server terminar de inicializar. Para confirmar que os dois containers estão de pé:
```bash
docker compose ps
```
Você deve ver `orders-sqlserver` e `rabbitmq_broker` com status `running``.

#### 3. Aplicar as Migrations do Entity Framework
Cria o banco `OrdersDb` e as tabelas `Pedidos`/`Itens` no SQL Server que subiu no passo 2:
```bash
dotnet ef database update
```
> Se o comando falhar com erro de conexão, o SQL Server provavelmente ainda está inicializando dentro do container — aguarde alguns segundos e tente novamente.

#### 4. Rodar a aplicação
```bash
dotnet run
```

Com isso a API sobe (via `Properties/launchSettings.json`) em:
- HTTP: `http://localhost:5190`

### Acesse o swagger para testar a API: `http://localhost:5190/swagger` para explorar e testar os endpoints interativamente.

O RabbitMQ Management fica disponível em `http://localhost:15672` (usuário/senha `guest`/`guest`) — útil para acompanhar as filas `PedidoCriado` e `PedidoCancelado` sendo consumidas em tempo real.
obs: os consumers estao exibindo uma mensagem no console da aplicação.

#### 5. Parar o ambiente
Para encerrar a API, use `Ctrl+C` no terminal. Para derrubar os containers de infraestrutura:
```bash
docker compose down
```
Use `docker compose down -v` caso queira também apagar o volume `sqlserver_data` (isso apaga os dados do banco).

---

## 📖 Endpoints da API

Base route: `api/pedido`

| Método | Rota                        | Descrição                                             |
|--------|-----------------------------|--------------------------------------------------------|
| POST   | `/api/pedido`                | Cria um novo pedido e publica o evento `PedidoCriado`. |
| GET    | `/api/pedido/{id}`           | Busca um pedido por ID (via Dapper), incluindo itens.  |
| GET    | `/api/pedido`                | Lista pedidos paginados (`page`, `pageSize`).          |
| PUT    | `/api/pedido/{id}/cancelar`  | Cancela um pedido e publica o evento `PedidoCancelado`.|

### Exemplo — Criar pedido
```http
POST /api/pedido
Content-Type: application/json

{
  "nomeCliente": "Joao",
  "itens": [
    {
      "produtoId": 1,
      "nomeProduto": "Camisa do Brasil",
      "quantidade": 1,
      "precoUnitario": 200
    }
  ]
}
```

### Regras de Negócio
- Um pedido precisa ter ao menos um item para ser criado.
```bash
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "Itens": [
      "A lista de itens não pode estar vazia."
    ]
  },
  "traceId": "00-eb59ac613e8c250bf47de9099537c05d-557bdfe0ded1760c-00"
}


- Um pedido já cancelado não pode ser cancelado novamente.
```bash
{
  "title": "Regra de Negócio Violada",
  "status": 400,
  "detail": "Este pedido já se encontra cancelado.",
  "instance": "/api/Pedido/1/cancelar"
}

- Tentar buscar ou cancelar um pedido inexistente retorna `404 Not Found`.
```bash
{
  "title": "Recurso Não Encontrado",
  "status": 404,
  "detail": "Pedido com o ID 20 não foi encontrado.",
  "instance": "/api/Pedido/20/cancelar"
}

- O `ValorTotal` do pedido é calculado automaticamente a partir da soma de `Quantidade * PrecoUnitario` dos itens.
- O status do pedido é representado pelo enum `StatusPedido`: `CRIADO` (1) ou `CANCELADO` (2).

---
