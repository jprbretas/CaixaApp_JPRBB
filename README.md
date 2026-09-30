# Pré-análise de Crédito Pessoal

Aplicação que avalia pedidos de crédito pessoal segundo as regras de negócio do exercício
PND2026 da CGD. Para cada pedido devolve a **decisão** (APROVADO, ANÁLISE MANUAL, RECUSADO ou PEDIDO
INVÁLIDO), os **motivos** e os **indicadores** calculados (prestação, taxa de esforço, idade no fim
do contrato e montante máximo). Os pedidos que ficam em análise manual são depois decididos por um
analista.

Feita em **C# / .NET 10**, com **Blazor** nas páginas, uma **API** à parte, **SQLite** como base de
dados e **.NET Aspire** para arrancar tudo de uma vez.

---

## Como correr

**Requisitos:** o SDK do .NET 10 e, de preferência, o Visual Studio 2026. Não é preciso instalar
nenhuma base de dados: o ficheiro `caixa.db` é criado sozinho no primeiro arranque.

**No Visual Studio:** abrir `CaixaProjeto.slnx`, escolher `CaixaProjeto.AppHost` como projeto de
arranque e carregar em F5. Abre-se o dashboard do Aspire; o link **"Abrir a aplicação"**, no serviço
`webfrontend`, abre as páginas.

**Na linha de comandos:**

```bash
dotnet run --project CaixaProjeto.AppHost
```

O endereço do dashboard aparece no terminal.

**Testes** (71: 30 das regras, 40 da API e da base de dados, 1 da aplicação inteira):

```bash
dotnet test CaixaProjeto.slnx
```

---

## O que a aplicação faz

- **Novo pedido:** preencher os dados (ou escolher um dos cenários A a D do enunciado) e
  **Analisar** (simula) ou **Submeter pedido** (grava e atribui um número, por exemplo 20260001).
- **Pedidos:** lista paginada, com filtro por estado.
- **Detalhe de um pedido:** dados, análise automática, estado atual e histórico. Nos pedidos em
  análise manual, o analista aprova ou recusa com uma justificação.

---

## Respostas ao enunciado

| Tarefa | Onde está a resposta |
|---|---|
| **1. Análise funcional** (interpretação, ordem das regras, casos ambíguos, perguntas ao negócio, user stories, modelo de dados) | [docs/Tarefa1_Analise_Funcional.md](docs/Tarefa1_Analise_Funcional.md) |
| **2. Casos de teste** | Secção [Testes](DECISIONS.md#testes) do DECISIONS.md; os testes estão em `CaixaProjeto.UnitTests` (regras e cenários A a D) e `CaixaProjeto.ApiTests` |
| **3. Desenvolvimento** | A aplicação neste repositório; as decisões em [DECISIONS.md](DECISIONS.md) |
| **4. Extrações / queries à BD** | [sql/Tarefa4_Consultas.sql](sql/Tarefa4_Consultas.sql) |
| **5. Melhoria da solução** | [docs/Tarefa5_Melhoria_da_Solucao.md](docs/Tarefa5_Melhoria_da_Solucao.md) |
| **6. Resolução de problemas** | [docs/Tarefa6_Resolucao_de_Problemas.md](docs/Tarefa6_Resolucao_de_Problemas.md) |

---

## Documentação

- [DECISIONS.md](DECISIONS.md): as decisões de design e desenvolvimento, porquê, e como a aplicação
  foi feita passo a passo.
- [docs/Estrutura.md](docs/Estrutura.md): o guia dos projetos e dos ficheiros.

## Organização

| Projeto | Responsabilidade |
|---|---|
| `CaixaProjeto.Core` | Regras de negócio (o motor de decisão), sem web nem base de dados. |
| `CaixaProjeto.ApiService` | A API: recebe os pedidos, chama o motor e grava na base de dados. |
| `CaixaProjeto.Web` | As páginas (Blazor). Fala só com a API. |
| `CaixaProjeto.AppHost` | Arranca a API e a Web e liga-as (Aspire). |
| `CaixaProjeto.ServiceDefaults` | Configuração comum: logs, health checks, service discovery. |
| `CaixaProjeto.UnitTests` / `ApiTests` / `Tests` | Testes das regras / da API e da base de dados / da aplicação inteira. |
