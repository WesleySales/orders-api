# 📦 Orders API - Sistema de Gestão de Pedidos

Uma Web API em .NET desenvolvida com foco em arquitetura orientada a eventos, utilizando **SQL Server** para persistência de dados e **RabbitMQ** para mensageria assíncrona.

---

## 🛠️ Tecnologias Utilizadas

- **.NET (C#)**: Framework base para o desenvolvimento da API.
- **Entity Framework Core**: ORM para mapeamento e manipulação do banco de dados.
- **SQL Server 2022**: Banco de dados relacional para armazenamento de pedidos.
- **RabbitMQ**: Message Broker para comunicação assíncrona entre serviços.
- **Docker & Docker Compose**: Conteinerização do ambiente de infraestrutura.

---

## 🏗️ Arquitetura e Decisões Técnicas

### 1. Mensageria Assíncrona com RabbitMQ
- **Justificativa**: A criação e o cancelamento de pedidos são operações que podem disparar ações secundárias (envio de e-mails, atualização de estoque, faturamento). O uso de filas desacopla o processamento principal da API, garantindo alta disponibilidade e resiliência.
- **Filas Configuradas**:
  - `PedidoCriado`: Processa eventos de criação de novos pedidos.
  - `PedidoCancelado`: Processa eventos de cancelamento de pedidos.

### 2. Consumidores via `BackgroundService` (Hosted Services)
- **Justificativa**: Ao invés de dependermos de um worker externo separado no início do desenvolvimento, os consumidores (`PedidoCriadoConsumer` e `PedidoCanceladoConsumer`) rodam em segundo plano na própria aplicação via `BackgroundService` do .NET, simplificando a execução em ambiente local sem perder o desacoplamento das camadas.

### 3. Conteinerização com Docker Compose
- **Justificativa**: Garante que qualquer desenvolvedor execute o projeto com exatamente as mesmas versões de dependências (SQL Server 2022 e RabbitMQ com Painel de Management), sem necessidade de instalações locais complexas.

---

## 🚀 Como Executar o Projeto

### Pré-requisitos

- [.NET SDK 10]
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) rodando na máquina.
- Ferramenta EF Core CLI instalada (`dotnet tool install --global dotnet-ef`).

---

### Passo a Passo

#### 1. Clonar o repositório
```bash
git clone [https://github.com/SeuUsuario/OrdersApi.git](https://github.com/SeuUsuario/OrdersApi.git)
cd OrdersApi

#### 2. Subir os Containers de Infraestrutura (SQL Server & RabbitMQ)
Certifique-se de que o Docker Desktop esteja aberto e execute:
```bash
docker compose up -d

#### 3. Aplicar as Migrations do Entity Framework
```bash
dotnet ef database update

#### 4. Rodar a aplicação
```bash
dotnet run
